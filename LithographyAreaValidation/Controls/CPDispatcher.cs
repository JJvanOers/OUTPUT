using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;
using ILOG.CP;
using ILOG.OPL;
using CSSL.Modeling.CSSLQueue;
using System.IO;
using System.Linq;
using System.Threading;
using ILOG.Concert;

namespace LithographyAreaValidation.Controls
{
    public class CPDispatcher : DispatcherBase
    {
        public CPDispatcher(ModelElementBase parent, string name, int timeLimit) : base(parent, name)
        {
            TimeLimit = timeLimit;
        }

        public int TimeLimit { get; set; }
        bool FirstScheduleGenerated { get; set; }

        protected override void OnReplicationStart()
        {
            base.OnReplicationStart();

            int test = 0;

            ScheduledLotsPerMachine.Clear();

            foreach (Machine machine in LithographyArea.Machines)
            {
                ScheduledLotsPerMachine.Add(machine.Name, new List<Lot>());
            }

            FirstScheduleGenerated = false;

            // Schedule Reschedule Event
            if (LithographyArea.Dynamic)
            {
                ScheduleEvent(GetTime + 1 * 3600, RescheduleEvent); // TODO: Determine interval
            }
        }

        public override Lot Dispatch(Machine machine)
        {
            Lot dispatchedLot = null;

            if (!FirstScheduleGenerated)
            {
                RescheduleLotMachineAllocation();
                FirstScheduleGenerated = true;
            }

            if (ScheduledLotsPerMachine[machine.Name].Count > 0)
            {
                Lot scheduledLot = ScheduledLotsPerMachine[machine.Name][0];

                double scheduledStartTime = scheduledLot.ScheduledTime;

                if (scheduledStartTime != GetTime && !LithographyArea.Stochastic && !LithographyArea.Dynamic)
                {
                    // Schedule next startRun
                    ScheduleEvent(scheduledStartTime, machine.DispatchNextLotEvent);
                }
                else if (scheduledStartTime == GetTime && !LithographyArea.Stochastic && !LithographyArea.Dynamic)
                {
                    dispatchedLot = HandleDeparture(scheduledLot, machine);
                    ScheduledLotsPerMachine[machine.Name].Remove(dispatchedLot);
                }
                else
                {
                    bool resourceAvailable = CheckResourceAvailability(scheduledLot);

                    if (resourceAvailable)
                    {
                        dispatchedLot = HandleDeparture(scheduledLot, machine);
                        ScheduledLotsPerMachine[machine.Name].Remove(dispatchedLot);
                    }
                }
            }
            else // This is the case when machine goes up or all jobs are produced
            {
                // Collect all lots from queue which are not scheduled yet.
                // For each collected lot, check on how many other machines the similar IRD is already scheduled.
                Dictionary<Lot, int> lotsNotScheduled = new Dictionary<Lot, int>();

                for (int i = 0; i < Queue.Length; i++)
                {
                    Lot lot = Queue.PeekAt(i);
                    bool lotScheduled = false;

                    foreach (var scheduledLotsPerMachine in ScheduledLotsPerMachine.Values)
                    {
                        if (scheduledLotsPerMachine.Contains(lot))
                        {
                            lotScheduled = true;
                            break;
                        }
                    }

                    if (!lotScheduled)
                    {
                        int sameIrdCount = 0;
                        foreach (var scheduledLotsPerMachine in ScheduledLotsPerMachine.Values)
                        {
                            if (scheduledLotsPerMachine.Select(x => x.IrdName).Contains(lot.IrdName))
                            {
                                sameIrdCount++;
                            }
                        }
                        lotsNotScheduled.Add(lot, sameIrdCount);
                    }
                }

                // Select eligible lot with minimal other IRDs scheduled and earliest due date
                IEnumerable<int> countsOrderded = lotsNotScheduled.Values.Distinct().OrderBy(x => x);

                foreach (int count in countsOrderded)
                {
                    IEnumerable<Lot> lots = lotsNotScheduled.Where(x => x.Value == count).Select(x => x.Key).OrderBy(x => x.ImprovedDueDate);

                    if (lots.Any())
                    {
                        foreach (Lot lot in lots)
                        {
                            // Get needed recipe for the lot
                            string recipe = GetRecipe(lot, machine);
                            bool recipeEligible = CheckMachineEligibility(machine.Name + "__" + recipe);
                            bool resourceAvailable = CheckResourceAvailability(lot);
                            bool processingTimeKnown = CheckProcessingTimeKnown(lot, machine, recipe);

                            // Dispatch if lot is eligible
                            if (resourceAvailable && recipeEligible && processingTimeKnown)
                            {
                                // Dequeue earliestDueDateLot
                                dispatchedLot = HandleDeparture(lot, machine);
                                break;
                            }
                        }
                    }
                    if (dispatchedLot != null) break;
                }
            }

            return dispatchedLot;
        }

        public override void HandleEndMachineDown()
        {
            //throw new NotImplementedException();
        }

        public override void HandleMachineDown(Machine machine)
        {
            //throw new NotImplementedException();
        }

        protected void RescheduleEvent(CSSLEvent e)
        {
            // Reschedule Machine Layer Allocation
            RescheduleLotMachineAllocation();
            TriggerMachinesWaiting();
            ScheduleEvent(GetTime + 1 * 3600, RescheduleEvent); // TODO: Determine interval
        }

        public void RescheduleLotMachineAllocation()
        {
            // Clear scheduled lots
            foreach (Machine machine in LithographyArea.Machines)
            {
                ScheduledLotsPerMachine[machine.Name].Clear();
            }

            // Set weight of each Lot
            for (int j = 0; j < Queue.Length; j++)
            {
                Lot peekLot = Queue.PeekAt(j);

                Queue.PeekAt(j).WeightDueDate = GetDueDateWeight(peekLot);
                Queue.PeekAt(j).WeightWIPBalance = GetWIPBalanceWeight(peekLot);
            }

            int status = 127;
            string solutionAsString = "";

            try
            {
                OplFactory.DebugMode = false;
                OplFactory oplF = new OplFactory();
                OplErrorHandler errHandler = oplF.CreateOplErrorHandler(Console.Out);
                OplModelSource modelSource = oplF.CreateOplModelSource($"{Directory.GetCurrentDirectory()}/Input/LithoFinal.mod");
                OplSettings settings = oplF.CreateOplSettings(errHandler);
                OplModelDefinition def = oplF.CreateOplModelDefinition(modelSource, settings);

                // Change stream to be able to read solution as a string (probably not the correct way of doing)
                StringWriter strWriter = new StringWriter();
                oplF.SetOut(strWriter);

                CP cp = oplF.CreateCP();
                OplModel opl = oplF.CreateOplModel(def, cp);
                OplDataSource dataSource = new MyCustomDataSource(oplF, Queue, LithographyArea, this);
                opl.AddDataSource(dataSource);
                opl.Generate();

                if (cp.Solve())
                {
                    Console.Out.WriteLine("OBJECTIVE: " + opl.CP.ObjValue);
                    opl.PostProcess();

                    // Get solution as string
                    solutionAsString = strWriter.ToString();

                    status = 0;
                }
                else
                {
                    Console.Out.WriteLine("No solution!");
                    status = 1;
                    this.LithographyArea.HandleDispatcherError();
                    ScheduleEndEvent(GetTime);
                    return;
                }
                oplF.End();
            }
            catch (OplException ex)
            {
                Console.WriteLine(ex.Message);
                status = 2;
            }
            catch (ILOG.Concert.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 3;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                status = 4;
            }

            //Console.WriteLine("--Press <Enter> to exit--");
            //Console.WriteLine(status);
            //Console.ReadLine();
            //Console.WriteLine(solutionAsString);
            //Console.ReadLine();
            ChangeLotMachineAllocation(solutionAsString);
            Console.WriteLine($"Scheduled for {LithographyArea.StartDate.AddSeconds(GetTime)}");
        }

        private void ChangeLotMachineAllocation(string solutionAsString)
        {
            solutionAsString = solutionAsString.Split("Objective")[1];

            string[] solutionAsStringArray = solutionAsString.Split('\n');

            List<Object[]> lotsOrdenedByStartTime = new List<Object[]>();

            for (int i = 2; i < solutionAsStringArray.Length; i++)
            {
                string orderID;
                string machineID;
                double startTime;

                try
                {
                    orderID = solutionAsStringArray[i].Split(':')[0];
                    machineID = solutionAsStringArray[i].Split("on ")[1].Split(';')[0];
                    startTime = Convert.ToDouble(solutionAsStringArray[i].Split('[')[1].Split(',')[0]);
                }
                catch
                {
                    continue;
                }

                Lot lot = null;
                Machine machine = null;

                // Get Lot
                for (int j = 0; j < Queue.Length; j++)
                {
                    Lot peekLot = Queue.PeekAt(j);

                    if (peekLot.Id.ToString() == orderID)
                    {
                        lot = peekLot;
                        break;
                    }
                }

                // Get Machine
                foreach (Machine equipmentID in LithographyArea.Machines)
                {
                    if (equipmentID.Name == machineID)
                    {
                        machine = equipmentID;
                        break;
                    }
                }

                lotsOrdenedByStartTime.Add(new Object[] { lot, machine, startTime });
                //Console.WriteLine(solutionAsStringArray[i]);
            }

            lotsOrdenedByStartTime = lotsOrdenedByStartTime.OrderBy(o => o[2]).ToList();

            foreach (Object[] item in lotsOrdenedByStartTime)
            {
                Lot lot = (Lot)item[0];
                Machine machine = (Machine)item[1];
                double startTime = (double)item[2];

                lot.ScheduledTime = startTime;

                ScheduledLotsPerMachine[machine.Name].Add(lot);
            }
        }

        internal class MyCustomDataSource : CustomOplDataSource
        {
            internal MyCustomDataSource(OplFactory oplF, CSSLQueue<Lot> queue, LithographyArea lithographyArea, CPDispatcher cpDispatcher)
                : base(oplF)
            {
                Queue = queue;
                LithographyArea = lithographyArea;
                CPDispatcher = cpDispatcher;
            }

            private CSSLQueue<Lot> Queue { get; }
            public LithographyArea LithographyArea { get; }
            public CPDispatcher CPDispatcher { get; }

            private Dictionary<string, int> ProductNameToID;

            public override void CustomRead()
            {
                OplDataHandler handler = DataHandler;

                GetComputationalSettings(handler);
                GetCostWeights(handler);
                GetProductIDs(handler);
                GetOrders(handler);
                GetResources(handler);
                GetAuxResources(handler);
                GetProductAuxResources(handler);
                GetModes(handler);
                GetSetups(handler);
            }

            private void GetComputationalSettings(OplDataHandler handler)
            {
                // initialize the tuple set
                handler.StartElement("ComputationalSettings");
                handler.StartSet();

                // Add tuple
                handler.StartTuple();
                handler.AddStringItem("timeLimit");
                handler.AddIntItem(CPDispatcher.TimeLimit);
                handler.EndTuple();

                handler.EndSet();
                handler.EndElement();
            }

            private void GetCostWeights(OplDataHandler handler)
            {
                // initialize the tuple set 'CostFunctionWeights'
                handler.StartElement("CostFunctionWeights");
                handler.StartSet();

                // Add tuple
                handler.StartTuple();
                handler.AddStringItem("a");
                handler.AddNumItem(LithographyArea.WeightA);
                handler.EndTuple();

                // Add tuple
                handler.StartTuple();
                handler.AddStringItem("b");
                handler.AddNumItem(LithographyArea.WeightB);
                handler.EndTuple();

                // Add tuple
                handler.StartTuple();
                handler.AddStringItem("c");
                handler.AddNumItem(LithographyArea.WeightC);
                handler.EndTuple();

                handler.EndSet();
                handler.EndElement();
            }

            private void GetProductIDs(OplDataHandler handler)
            {
                ProductNameToID = new Dictionary<string, int>();

                // initialize the tuple set 'Products'
                handler.StartElement("Products");
                handler.StartSet();

                List<Lot> allLots = new List<Lot>();
                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);
                    allLots.Add(peekLot);
                }
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        allLots.Add(lotAtMachine);
                    }
                }

                int productID = 0;

                // Loop through queue
                foreach (Lot lot in allLots)
                {
                    string productName = lot.MasksetLayer_RecipeStepCluster;

                    if (!ProductNameToID.ContainsKey(productName))
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddIntItem(productID);
                        handler.AddStringItem(productName);
                        handler.EndTuple();

                        ProductNameToID.Add(productName, productID);

                        productID += 1;
                    }
                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetOrders(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("Orders");
                handler.StartSet();

                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);

                    bool eligible = false;

                    // Check if elible on any machine (not Down)
                    foreach (Machine machine in LithographyArea.Machines)
                    {
                        // Check if machine is Down
                        if (LithographyArea.MachineStates[machine.Name] == "Down")
                        {
                            continue;
                        }

                        // Get needed recipe for the lot
                        string recipe = CPDispatcher.GetRecipe(peekLot, machine);
                        string recipeKey = machine.Name + "__" + recipe;

                        // Check if needed recipe is eligible on machine
                        bool recipeEligible = CPDispatcher.CheckMachineEligibility(recipeKey);

                        // Check if processingTime is known
                        bool processingTimeKnown = CPDispatcher.CheckProcessingTimeKnown(peekLot, machine, recipe);

                        if (recipeEligible && processingTimeKnown)
                        {
                            eligible = true;
                            break;
                        }
                    }

                    if (eligible)
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddStringItem(peekLot.Id.ToString()); //handler.AddStringItem(peekLot.Id.ToString());
                        handler.AddIntItem(ProductNameToID[peekLot.MasksetLayer_RecipeStepCluster]);
                        handler.AddIntItem(peekLot.LotQty);

                        handler.AddNumItem(CPDispatcher.GetDueDateWeight(peekLot));
                        handler.AddNumItem(CPDispatcher.GetWIPBalanceWeight(peekLot));

                        handler.EndTuple();
                    }
                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetResources(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("Resources");
                handler.StartSet();

                // Loop through queue
                foreach (Machine machine in LithographyArea.Machines)
                {
                    // Check if machine is Down
                    if (LithographyArea.MachineStates[machine.Name] == "Down")
                    {
                        continue;
                    }

                    int initialProductID = -1;
                    int endTimeInitialProductID = 0;

                    // Check if a lot is loaded on the machine
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        initialProductID = ProductNameToID[lotAtMachine.MasksetLayer_RecipeStepCluster];
                        double estimatedEndTime;

                        if (machine.CurrentLot == lotAtMachine)
                        {
                            estimatedEndTime = machine.CurrentStartRun + machine.GetDeterministicProcessingTime(lotAtMachine);

                            if (estimatedEndTime > LithographyArea.GetTime)
                            {
                                endTimeInitialProductID = (int)estimatedEndTime - (int)LithographyArea.GetTime;
                            }
                        }
                        else
                        {
                            estimatedEndTime = machine.GetDeterministicProcessingTime(lotAtMachine);
                            endTimeInitialProductID = (int)estimatedEndTime;
                        }
                    }

                    if (machine.Name.Contains("#5") || machine.Name.Contains("#7") || machine.Name.Contains("#9"))
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddStringItem(machine.Name);
                        handler.AddStringItem("MTRX_RMS");
                        handler.AddIntItem(initialProductID);
                        handler.AddIntItem(endTimeInitialProductID);
                        handler.EndTuple();
                    }
                    else
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddStringItem(machine.Name);
                        handler.AddStringItem("MTRX_ARMS");
                        handler.AddIntItem(initialProductID);
                        handler.AddIntItem(endTimeInitialProductID);
                        handler.EndTuple();
                    }

                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetAuxResources(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("AuxResources");
                handler.StartSet();

                List<Lot> allLots = new List<Lot>();
                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);
                    allLots.Add(peekLot);
                }
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        allLots.Add(lotAtMachine);
                    }
                }

                // Get Reticles

                List<string> allReticles = new List<string>();

                // Loop through queue
                foreach (Lot lot in allLots)
                {
                    if (!allReticles.Contains(lot.ReticleID1))
                    {
                        if (lot.ReticleID1 == "4106")
                        {
                            // Add tuple
                            handler.StartTuple();
                            handler.AddStringItem(lot.ReticleID1);
                            handler.AddIntItem(2);
                            handler.EndTuple();
                        }
                        else
                        {
                            // Add tuple
                            handler.StartTuple();
                            handler.AddStringItem(lot.ReticleID1);
                            handler.AddIntItem(1);
                            handler.EndTuple();
                        }
                        allReticles.Add(lot.ReticleID1);
                    }
                }

                // Get IRDNames

                List<string> allIRDNames = new List<string>();

                // Loop through queue
                foreach (Lot lot in allLots)
                {
                    if (!allIRDNames.Contains(lot.IrdName))
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddStringItem(lot.IrdName);
                        int capacity = 2;
                        if (CPDispatcher.AvailableResources.ContainsKey(lot.IrdName))
                        {
                            capacity = CPDispatcher.AvailableResources[lot.IrdName];
                        }
                        handler.AddIntItem(capacity);
                        handler.EndTuple();

                        allIRDNames.Add(lot.IrdName);
                    }
                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetProductAuxResources(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("ProductAuxResources");
                handler.StartSet();

                List<Lot> allLots = new List<Lot>();
                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);
                    allLots.Add(peekLot);
                }
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        allLots.Add(lotAtMachine);
                    }
                }

                List<string> productNames = new List<string>();

                // Loop through queue
                foreach (Lot lot in allLots)
                {
                    string productName = lot.MasksetLayer_RecipeStepCluster;

                    if (!productNames.Contains(productName))
                    {
                        // Add tuple
                        handler.StartTuple();
                        handler.AddIntItem(ProductNameToID[productName]);
                        handler.AddStringItem(lot.ReticleID1);
                        handler.EndTuple();

                        // Add tuple
                        handler.StartTuple();
                        handler.AddIntItem(ProductNameToID[productName]);
                        handler.AddStringItem(lot.IrdName);
                        handler.EndTuple();

                        productNames.Add(productName);
                    }
                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetModes(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("Modes");
                handler.StartSet();

                List<Lot> allLots = new List<Lot>();
                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);
                    allLots.Add(peekLot);
                }
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        allLots.Add(lotAtMachine);
                    }
                }

                List<string> productNames = new List<string>();

                // Loop through queue
                foreach (Lot lot in allLots)
                {
                    if (!productNames.Contains(lot.MasksetLayer_RecipeStepCluster))
                    {
                        int modeNr = 0;

                        foreach (Machine machine in LithographyArea.Machines)
                        {
                            // Check if machine is Down
                            if (LithographyArea.MachineStates[machine.Name] == "Down")
                            {
                                continue;
                            }

                            // Get needed recipe for the lot
                            string recipe = CPDispatcher.GetRecipe(lot, machine);
                            string recipeKey = machine.Name + "__" + recipe;

                            // Check if needed recipe is eligible on machine
                            bool recipeEligible = CPDispatcher.CheckMachineEligibility(recipeKey);

                            // Check if processingTime is known
                            bool processingTimeKnown = CPDispatcher.CheckProcessingTimeKnown(lot, machine, recipe);

                            if (recipeEligible && processingTimeKnown)
                            {
                                int? processingTime = null;

                                processingTime = (int)machine.GetDeterministicProcessingTimeFullLot(lot);

                                // Add tuple
                                handler.StartTuple();
                                handler.AddIntItem(ProductNameToID[lot.MasksetLayer_RecipeStepCluster]);
                                handler.AddIntItem(modeNr);
                                handler.AddStringItem(machine.Name);
                                handler.AddIntItem((int)processingTime);

                                handler.EndTuple();

                                modeNr += 1;
                            }
                        }
                        productNames.Add(lot.MasksetLayer_RecipeStepCluster);
                    }
                }

                handler.EndSet();
                handler.EndElement();
            }

            private void GetSetups(OplDataHandler handler)
            {
                // initialize the tuple set 'Products'
                handler.StartElement("Setups");
                handler.StartSet();

                List<Lot> allLots = new List<Lot>();
                // Loop through queue
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);
                    allLots.Add(peekLot);
                }
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        allLots.Add(lotAtMachine);
                    }
                }

                List<string> productNamesFrom = new List<string>();

                // Loop through queue
                foreach (Lot lotFrom in allLots)
                {
                    if (!productNamesFrom.Contains(lotFrom.MasksetLayer_RecipeStepCluster))
                    {
                        List<string> productNamesTo = new List<string>();

                        // Loop through queue
                        foreach (Lot lotTo in allLots)
                        {
                            if (!productNamesTo.Contains(lotTo.MasksetLayer_RecipeStepCluster))
                            {
                                if (lotFrom.ReticleID1 == lotTo.ReticleID1)
                                {
                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_ARMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesARMS["SameReticle"]);
                                    handler.EndTuple();

                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_RMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesRMS["SameReticle"]);
                                    handler.EndTuple();
                                }
                                else if (lotFrom.ReticleID1 != lotTo.ReticleID1 && lotFrom.IrdName == lotTo.IrdName)
                                {
                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_ARMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesARMS["DifferentReticle"]);
                                    handler.EndTuple();

                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_RMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesRMS["DifferentReticle"]);
                                    handler.EndTuple();
                                }
                                else
                                {
                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_ARMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesARMS["DifferentIRD"]);
                                    handler.EndTuple();

                                    // Add tuple
                                    handler.StartTuple();
                                    handler.AddStringItem("MTRX_RMS");
                                    handler.AddIntItem(ProductNameToID[lotFrom.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem(ProductNameToID[lotTo.MasksetLayer_RecipeStepCluster]);
                                    handler.AddIntItem((int)LithographyArea.DeterministicNonProductiveTimesRMS["DifferentIRD"]);
                                    handler.EndTuple();
                                }
                                productNamesTo.Add(lotTo.MasksetLayer_RecipeStepCluster);
                            }
                        }
                        productNamesFrom.Add(lotFrom.MasksetLayer_RecipeStepCluster);
                    }
                }
                handler.EndSet();
                handler.EndElement();
            }
        }
    }
}
