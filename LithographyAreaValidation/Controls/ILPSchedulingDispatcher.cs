using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;
using Gurobi;
using CSSL.Modeling.CSSLQueue;

namespace LithographyAreaValidation.Controls
{
    public class ILPSchedulingDispatcher : DispatcherBase
    {
        public ILPSchedulingDispatcher(ModelElementBase parent, string name) : base(parent, name)
        {


        }

        Boolean FirstScheduleGenerated { get; set; }

        protected override void OnReplicationStart()
        {
            base.OnReplicationStart();

            ScheduledLotsPerMachine.Clear();

            foreach (Machine machine in LithographyArea.Machines)
            {
                ScheduledLotsPerMachine.Add(machine.Name, new List<Lot>());
            }

            FirstScheduleGenerated = false;

            if (LithographyArea.Dynamic)
            {
                ScheduleEvent(GetTime + 1 * 3600, RescheduleEvent); // TODO: Determine interval
            }
        }

        public override Lot Dispatch(Machine machine)
        {
            if (!FirstScheduleGenerated)
            {
                RescheduleLotMachineAllocation();
                FirstScheduleGenerated = true;
            }

            Lot bestLot;

            if (ScheduledLotsPerMachine[machine.Name].Count == 0)
            {
                bestLot = GetLotNotScheduledOnThisMachine(machine);



                foreach (Machine equimentID in LithographyArea.Machines)
                {
                    if (ScheduledLotsPerMachine[equimentID.Name].Contains(bestLot))
                    {
                        ScheduledLotsPerMachine[equimentID.Name].Remove(bestLot);
                    }
                }
            }
            else
            {
                bestLot = GetBestLot(machine);
            }



            if (bestLot == null)
            {
                return bestLot;
            }
            else
            {
                ScheduledLotsPerMachine[machine.Name].Remove(bestLot);

                Lot dispatchedLot = HandleDeparture(bestLot, machine);

                return dispatchedLot;
            }
        }

        public override void HandleEndMachineDown()
        {
            //throw new NotImplementedException();
        }

        public override void HandleMachineDown(Machine machine)
        {
            //throw new NotImplementedException();
        }

        public Lot GetBestLot(Machine machine)
        {
            List<Lot> scheduledLots = ScheduledLotsPerMachine[machine.Name];

            Lot bestLot = null;

            List<Lot> lotsWithSameWeight = new List<Lot>();

            // Get lot with highest weight
            double maxWeight = 0;
            foreach (Lot peekLot in scheduledLots)
            {
                // Check if resource is available
                Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                if (resourceAvailable)
                {
                    double thisWeight = CalculateWeightILP(peekLot);


                    if (thisWeight > maxWeight)
                    {
                        lotsWithSameWeight.Clear();
                        lotsWithSameWeight.Add(peekLot);
                        maxWeight = thisWeight;
                    }
                    else if (thisWeight == maxWeight)
                    {
                        lotsWithSameWeight.Add(peekLot);
                    }
                }
            }

            if (bestLot == null)
            {
                // Check if a lot uses the same reticle
                foreach (Lot lotWithSameWeight in lotsWithSameWeight)
                {
                    if (lotWithSameWeight.ReticleID1 == machine.PreviousReticleID)
                    {
                        bestLot = lotWithSameWeight;
                        break;
                    }
                }
            }

            if (bestLot == null)
            {
                //Check if a lot uses the same layer
                foreach (Lot lotWithSameWeight in lotsWithSameWeight)
                {
                    if (lotWithSameWeight.IrdName == machine.PreviousLotIRDName)
                    {
                        bestLot = lotWithSameWeight;
                        break;
                    }
                }
            }

            if (bestLot == null)
            {
                if (lotsWithSameWeight.Count > 0)
                {
                    bestLot = lotsWithSameWeight[0];
                }
            }

            return bestLot;
        }

        public Lot GetLotNotScheduledOnThisMachine(Machine machine)
        {
            Lot bestLot = null;

            List<Lot> lotsWithSameWeight = new List<Lot>();

            // Get lot with highest weight
            double maxWeight = 0;

            // Loop through queue
            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);

                // Get needed recipe for the lot
                string recipe = GetRecipe(peekLot, machine);
                string recipeKey = machine.Name + "__" + recipe;

                // Check if needed recipe is eligible on machine
                Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                // Check if resource is available
                Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                // Check if processingTime is known
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                if (resourceAvailable && recipeEligible && processingTimeKnown)
                {
                    double thisWeight = CalculateWeightILP(peekLot);

                    if (thisWeight > maxWeight)
                    {
                        lotsWithSameWeight.Clear();
                        lotsWithSameWeight.Add(peekLot);
                        maxWeight = thisWeight;
                    }
                    else if (thisWeight == maxWeight)
                    {
                        lotsWithSameWeight.Add(peekLot);
                    }
                }
            }

            if (bestLot == null)
            {
                // Check if a lot uses the same reticle
                foreach (Lot lotWithSameWeight in lotsWithSameWeight)
                {
                    if (lotWithSameWeight.ReticleID1 == machine.PreviousReticleID)
                    {
                        bestLot = lotWithSameWeight;
                        break;
                    }
                }
            }

            if (bestLot == null)
            {
                //Check if a lot uses the same layer
                foreach (Lot lotWithSameWeight in lotsWithSameWeight)
                {
                    if (lotWithSameWeight.IrdName == machine.PreviousLotIRDName)
                    {
                        bestLot = lotWithSameWeight;
                        break;
                    }
                }
            }

            if (bestLot == null)
            {
                if (lotsWithSameWeight.Count > 0)
                {
                    bestLot = lotsWithSameWeight[0];
                }
            }

            return bestLot;
        }

        public double[] GetReadyTimes()
        {
            double[] readyTimes = new double[LithographyArea.Machines.Count];

            for (int i = 0; i < LithographyArea.Machines.Count; ++i)
            {
                Machine machine = LithographyArea.Machines[i];

                double readyTime = 0;

                if (LithographyArea.MachineStates[machine.Name] == "Down")
                {
                    readyTime = 0;
                }
                else
                {
                    // Check if a lot is loaded on the machine
                    if (machine.Queue.Length > 0)
                    {
                        Lot lotAtMachine = machine.Queue.PeekFirst();
                        double estimatedEndTime;

                        if (machine.CurrentLot == lotAtMachine)
                        {
                            estimatedEndTime = machine.CurrentStartRun + machine.GetDeterministicProcessingTime(lotAtMachine);

                            if (estimatedEndTime > LithographyArea.GetTime)
                            {
                                readyTime = estimatedEndTime - LithographyArea.GetTime;
                            }
                            else
                            {
                                readyTime = 0;
                            }
                        }
                        else
                        {
                            estimatedEndTime = machine.GetDeterministicProcessingTime(lotAtMachine);
                            readyTime = estimatedEndTime;
                        }
                    }
                }

                readyTimes[i] = readyTime;
            }

            return readyTimes;
        }


        public double[,] GetProcessingTimes(List<Lot> lotsToBeScheduled)
        {
            double[,] processingTimes = new double[LithographyArea.Machines.Count, lotsToBeScheduled.Count];

            for (int i = 0; i < LithographyArea.Machines.Count; ++i)
            {
                Machine machine = LithographyArea.Machines[i];

                for (int j = 0; j < lotsToBeScheduled.Count; j++)
                {
                    // Peek next lot in queue
                    Lot peekLot = lotsToBeScheduled[j];
                    int lotQty = peekLot.LotQty;

                    // Get needed recipe for the lot
                    string recipe = GetRecipe(peekLot, machine);
                    string recipeKey = machine.Name + "__" + recipe;

                    // Check if needed recipe is eligible on machine
                    Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                    // Check if processingTime is known
                    Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                    double processingTime;

                    if (LithographyArea.MachineStates[machine.Name] == "Down")
                    {
                        processingTime = GRB.INFINITY;
                    }
                    else
                    {
                        if (recipeEligible && processingTimeKnown)
                        {
                            processingTime = machine.GetDeterministicProcessingTime(peekLot);
                            //processingTime = MultiplyByMachinePreference(peekLot, machine, processingTime);
                        }
                        else
                        {
                            processingTime = GRB.INFINITY;
                        }
                    }
                    processingTimes[i, j] = processingTime;
                }
            }

            return processingTimes;
        }

        public List<Lot> RemoveLotsCurrentlyNotEligible(List<Lot> lotsToBeScheduled, double[,] processingTimes)
        {
            List<Lot> lotsToBeScheduledNew = new List<Lot>(lotsToBeScheduled);

            Boolean allUp = true;
            for (int i = 0; i < LithographyArea.Machines.Count; ++i)
            {
                if (LithographyArea.MachineStates[LithographyArea.Machines[i].Name] == "Down")
                {
                    allUp = false;
                }
            }

            for (int j = 0; j < lotsToBeScheduled.Count; j++)
            {
                Boolean allInfinity = true;
                for (int i = 0; i < LithographyArea.Machines.Count; ++i)
                {
                    if (processingTimes[i, j] != GRB.INFINITY)
                    {
                        allInfinity = false;
                    }
                }

                if (allInfinity && allUp)
                {
                    Lot peekLot = lotsToBeScheduled[j];
                    Lot removedLot = Queue.Dequeue(peekLot);
                    lotsToBeScheduledNew.Remove(peekLot);
                    Console.WriteLine($"Lot not eligible on any machine: {peekLot.LotID}");
                    //processingTimes = GetProcessingTimes(lotsToBeScheduled);
                }
                else if (allInfinity && !allUp) //Remove lot from to be scheduled lots
                {
                    Lot peekLot = lotsToBeScheduled[j];
                    lotsToBeScheduledNew.Remove(peekLot);
                    //Console.WriteLine($"Lot not eligible on any machine: {peekLot.LotID}");
                    //processingTimes = GetProcessingTimes(lotsToBeScheduled);
                }
            }
            return lotsToBeScheduledNew;
        }

        public double MultiplyByMachinePreference(Lot lot, Machine machine, double processingTime)
        {
            double multipliedProcessingTime = processingTime;
            if (LayerMachinePreferences.ContainsKey(lot.IrdName))
            {
                string[] layerMachinePreferences = LayerMachinePreferences[lot.IrdName];
                for (int i = 0; i < layerMachinePreferences.Length; i++)
                {
                    string nextPreferredMachine = layerMachinePreferences[i];
                    double multiplier = 1.0 + (double)i;

                    if (i > 0) //Check if first preferred machine is down
                    {
                        string firstPreferredMachine = layerMachinePreferences[0];
                        Boolean isDown = false;
                        foreach (KeyValuePair<string, string> entry in LithographyArea.MachineStates)
                        {
                            if (entry.Key == firstPreferredMachine)
                            {
                                if (entry.Value == "Down")
                                {
                                    isDown = true;
                                }
                            }
                        }
                        if (isDown)
                        {
                            multiplier -= 1.0;
                        }
                    }

                    if (i > 1) //Check if first preferred machine is down
                    {
                        string secondPreferredMachine = layerMachinePreferences[1];
                        Boolean isDown = false;
                        foreach (KeyValuePair<string, string> entry in LithographyArea.MachineStates)
                        {
                            if (entry.Key == secondPreferredMachine)
                            {
                                if (entry.Value == "Down")
                                {
                                    isDown = true;
                                }
                            }
                        }
                        if (isDown)
                        {
                            multiplier -= 1.0;
                        }
                    }

                    if (nextPreferredMachine == machine.Name && LithographyArea.MachineStates[machine.Name] != "Down")
                    {
                        multipliedProcessingTime = multiplier * processingTime;
                        break;
                    }
                    else if (nextPreferredMachine == "Any ASML" && machine.Name.Contains("ASML"))
                    {
                        multipliedProcessingTime = multiplier * processingTime;
                        break;
                    }
                    else if (nextPreferredMachine == "Any")
                    {
                        multipliedProcessingTime = multiplier * processingTime;
                        break;
                    }
                    else if (nextPreferredMachine == "No options")
                    {
                        multipliedProcessingTime = 5.0 * processingTime; // TODO: Check this
                        break;
                    }
                    else
                    {
                        multipliedProcessingTime = GRB.INFINITY;
                    }
                }
            }
            return multipliedProcessingTime;
        }

        public void RescheduleEvent(CSSLEvent e)
        {
            RescheduleLotMachineAllocation();
            TriggerMachinesWaiting();
            ScheduleEvent(GetTime + 1 * 3600, RescheduleEvent); // TODO: Determine interval
        }

        private List<Lot> DecreaseQueueLength(int limitILPSolver)
        {
            List<Lot> lotsToBeScheduled = new List<Lot>();
            Dictionary<string, int> futureLayerActivities = new Dictionary<string, int>(LithographyArea.LayerActivities);

            Dictionary<string, List<Lot>> machineEligibleLots = new Dictionary<string, List<Lot>>();
            Dictionary<string, List<Lot>> machineScheduledLots = new Dictionary<string, List<Lot>>();
            foreach (Machine machine in LithographyArea.Machines)
            {
                machineEligibleLots.Add(machine.Name, new List<Lot>());
                machineScheduledLots.Add(machine.Name, new List<Lot>());

                for (int j = 0; j < Queue.Length; j++)
                {
                    Lot peekLot = Queue.PeekAt(j);
                    string recipe = GetRecipe(peekLot, machine);
                    string recipeKey = machine.Name + "__" + recipe;
                    Boolean recipeEligible = CheckMachineEligibility(recipeKey);
                    Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);
                    if (recipeEligible && processingTimeKnown)
                    {
                        machineEligibleLots[machine.Name].Add(peekLot);
                    }
                }
            }

            while (lotsToBeScheduled.Count < limitILPSolver)
            {
                // Check which machine has the lowest scheduled jobs but still has eligible jobs in front
                int? lowestValue = null;
                Machine nextMachine = null;
                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machineEligibleLots[machine.Name].Count > 0)
                    {
                        int thisValue = machineScheduledLots[machine.Name].Count;

                        if (lowestValue == null || thisValue < lowestValue)
                        {
                            nextMachine = machine;
                            lowestValue = thisValue;
                        }
                    }
                }

                if (nextMachine == null)
                {
                    break;
                }

                string bestLayer = null;
                double? maxLayerWeight = null;
                foreach (Lot peekLot in machineEligibleLots[nextMachine.Name])
                {
                    // Get weight of each layer type in the queue

                    string thisLayerType = peekLot.IrdName;

                    if (!futureLayerActivities.ContainsKey(thisLayerType))
                    {
                        thisLayerType = "Other";
                    }

                    double productionTargetFulfillment = (double)futureLayerActivities[thisLayerType] / (double)LithographyArea.LayerTargets[thisLayerType];

                    double thisLayerWeight = ((double)1 - productionTargetFulfillment);

                    if (maxLayerWeight == null || thisLayerWeight > maxLayerWeight)
                    {
                        bestLayer = peekLot.IrdName;
                        maxLayerWeight = thisLayerWeight;
                    }
                }

                double? minimumPlanDay = null;
                double? maximumPlanDay = null;
                foreach (Lot peekLot in machineEligibleLots[nextMachine.Name])
                {
                    // Get lowest Due Date, Get highest Due Date

                    double thisPlanDay = (peekLot.DueDate.Subtract(LithographyArea.StartDate.AddSeconds(GetTime))).TotalDays;

                    if (minimumPlanDay == null || thisPlanDay < minimumPlanDay)
                    {
                        minimumPlanDay = thisPlanDay;
                    }
                    if (maximumPlanDay == null || thisPlanDay > maximumPlanDay)
                    {
                        maximumPlanDay = thisPlanDay;
                    }
                }

                double? maxWeight = null;
                Lot bestLot = null;

                // Select next lot
                foreach (Lot peekLot in machineEligibleLots[nextMachine.Name])
                {
                    if (peekLot.IrdName == bestLayer)
                    {
                        // Get Weight of each lot

                        // Get lowest Due Date, Get highest Due Date
                        double thisPlanDay = (peekLot.DueDate.Subtract(LithographyArea.StartDate.AddSeconds(GetTime))).TotalDays;

                        double thisPlanDayWeight = ((double)maximumPlanDay - thisPlanDay) / ((double)maximumPlanDay - (double)minimumPlanDay);

                        if (maxWeight == null || thisPlanDayWeight > maxWeight)
                        {
                            bestLot = peekLot;
                            maxWeight = thisPlanDayWeight;
                        }
                    }
                }

                // Remove lot from eligilble lots of all machines and add it to the scheduled jobs
                foreach (Machine machine_2 in LithographyArea.Machines)
                {
                    if (machineEligibleLots[machine_2.Name].Contains(bestLot))
                    {
                        machineEligibleLots[machine_2.Name].Remove(bestLot);
                        machineScheduledLots[machine_2.Name].Add(bestLot);
                    }
                }

                if (!lotsToBeScheduled.Contains(bestLot))
                {
                    lotsToBeScheduled.Add(bestLot);

                    string thisLayerType = bestLot.IrdName;

                    if (!futureLayerActivities.ContainsKey(thisLayerType))
                    {
                        thisLayerType = "Other";
                    }

                    futureLayerActivities[thisLayerType] += bestLot.LotQty;
                }


            }
            return lotsToBeScheduled;
        }

        private List<Lot> DecreaseQueueLengthOld(int limitILPSolver)
        {
            List<Lot> lotsToBeScheduled = new List<Lot>();
            List<Lot> lotsNotScheduled = new List<Lot>();
            Dictionary<string, int> futureLayerActivities = new Dictionary<string, int>(LithographyArea.LayerActivities);

            for (int j = 0; j < Queue.Length; j++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(j);

                if (futureLayerActivities.ContainsKey(peekLot.IrdName) && LithographyArea.LayerTargets.ContainsKey(peekLot.IrdName))
                {
                    if (LithographyArea.LayerTargets[peekLot.IrdName] - futureLayerActivities[peekLot.IrdName] > 0) // Target is not met
                    {
                        lotsToBeScheduled.Add(peekLot);
                    }
                    else
                    {
                        lotsNotScheduled.Add(peekLot);
                    }
                    futureLayerActivities[peekLot.IrdName] += peekLot.LotQty;
                }
                else
                {
                    if (LithographyArea.LayerTargets["Other"] - futureLayerActivities["Other"] > 0) // Target is not met
                    {
                        lotsToBeScheduled.Add(peekLot);
                    }
                    else
                    {
                        lotsNotScheduled.Add(peekLot);
                    }
                    futureLayerActivities["Other"] += peekLot.LotQty;
                }

            }

            int nLotsToBeScheduled = lotsToBeScheduled.Count;

            int delta = limitILPSolver - nLotsToBeScheduled;

            if (delta >= 0)
            {
                double percentageOfremainingLots = (double)delta / (double)lotsNotScheduled.Count; // percentage of remaining lots which are not scheduled which can be added to the to be scheduled lots to make a set of 750 lots

                Dictionary<string, int> nLotsPerLayerNotScheduled = new Dictionary<string, int>
                        {
                            { "OW Photo", 0 },
                            { "ZL Photo", 0 },
                            { "DC Photo", 0 },
                            { "DP Photo", 0 },
                            { "OD Photo", 0 },
                            { "OC Photo", 0 },
                            { "TR Photo", 0 },
                            { "PS Photo", 0 },
                            { "AP T3 Photo", 0 },
                            { "SN Photo", 0 },
                            { "CO Photo", 0 },
                            { "IN Photo", 0 },
                            { "CB Photo", 0 },
                            { "VI Photo", 0 },
                            { "TC Photo", 0 },
                            { "Other", 0 }
                        };

                foreach (Lot lot in lotsNotScheduled)
                {
                    if (nLotsPerLayerNotScheduled.ContainsKey(lot.IrdName))
                    {
                        nLotsPerLayerNotScheduled[lot.IrdName] += 1;
                    }
                    else
                    {
                        nLotsPerLayerNotScheduled["Other"] += 1;
                    }

                }

                // Take percentage of each irdName
                Dictionary<string, int> nLotsPerLayerToBeScheduled = new Dictionary<string, int>
                        {
                            { "OW Photo", 0 },
                            { "ZL Photo", 0 },
                            { "DC Photo", 0 },
                            { "DP Photo", 0 },
                            { "OD Photo", 0 },
                            { "OC Photo", 0 },
                            { "TR Photo", 0 },
                            { "PS Photo", 0 },
                            { "AP T3 Photo", 0 },
                            { "SN Photo", 0 },
                            { "CO Photo", 0 },
                            { "IN Photo", 0 },
                            { "CB Photo", 0 },
                            { "VI Photo", 0 },
                            { "TC Photo", 0 },
                            { "Other", 0 }
                        };

                foreach (KeyValuePair<string, int> entry in nLotsPerLayerNotScheduled)
                {
                    int totalLots = entry.Value;
                    double nLotsOfThisIRDName = (double)totalLots * percentageOfremainingLots;
                    nLotsPerLayerToBeScheduled[entry.Key] = (int)Math.Ceiling(nLotsOfThisIRDName);
                }

                // Take percentage of each ird name to be scheduled
                // TODO: Earliest Due Date

                List<Lot> copyList = new List<Lot>(lotsNotScheduled);

                foreach (Lot lot in copyList)
                {
                    if (nLotsPerLayerToBeScheduled.ContainsKey(lot.IrdName))
                    {
                        if (nLotsPerLayerToBeScheduled[lot.IrdName] > 0)
                        {
                            lotsToBeScheduled.Add(lot);
                            lotsNotScheduled.Remove(lot);
                            nLotsPerLayerToBeScheduled[lot.IrdName] -= 1;
                        }

                    }
                    else
                    {
                        if (nLotsPerLayerToBeScheduled["Other"] > 0)
                        {
                            lotsToBeScheduled.Add(lot);
                            lotsNotScheduled.Remove(lot);
                            nLotsPerLayerToBeScheduled["Other"] -= 1;
                        }
                    }
                }
            }
            return lotsToBeScheduled;
        }
        public void RescheduleLotMachineAllocation()
        {
            Console.WriteLine(GetTime);
            Console.WriteLine("QueueLength:");
            Console.WriteLine(Queue.Length);
            int limitILPSolver = 2000; // maximum number of jobs put into the ILP solver


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


            try
            {
                // Get lots which have to be scheduled

                List<Lot> lotsToBeScheduled = new List<Lot>();


                if (Queue.Length <= limitILPSolver) // Small enough for ILP solver
                {
                    for (int j = 0; j < Queue.Length; j++)
                    {
                        // Peek next lot in queue
                        Lot peekLot = Queue.PeekAt(j);
                        lotsToBeScheduled.Add(peekLot);
                    }
                }
                else //Filter lots to get a set of 750 lots for ILP Solver
                {
                    lotsToBeScheduled = DecreaseQueueLength(limitILPSolver);
                }

                double[] readyTimes = GetReadyTimes();

                double[,] processingTimes = GetProcessingTimes(lotsToBeScheduled);

                List<Lot> lotsToBeScheduledNew = RemoveLotsCurrentlyNotEligible(lotsToBeScheduled, processingTimes);

                if (lotsToBeScheduledNew.Count < lotsToBeScheduled.Count)
                {
                    processingTimes = GetProcessingTimes(lotsToBeScheduledNew);
                    lotsToBeScheduled = lotsToBeScheduledNew;
                }

                // Number of jobs and machines
                int nMachines = processingTimes.GetLength(0);
                int nJobs = processingTimes.GetLength(1);

                Console.WriteLine($"Number of jobs Scheduled: {nJobs}, at time: {GetTime / 3600}");

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                //env.Set("LogFile", "mip1.log");
                env.OutputFlag = 0;
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Decision Variable
                GRBVar[,,] x = new GRBVar[nMachines, nJobs, nJobs];
                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            x[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x");
                        }
                    }
                }

                // Set objective:
                GRBLinExpr exp1 = 0.0;

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            GRBLinExpr exp2 = ((k + 1) * processingTimes[i, j] + readyTimes[i]) * x[i, j, k];
                            exp1.Add(exp2);
                        }
                    }
                }

                model.SetObjective(exp1, GRB.MINIMIZE);

                // Constraints
                for (int j = 0; j < nJobs; ++j)
                {
                    GRBLinExpr exp3 = 0.0;

                    for (int i = 0; i < nMachines; ++i)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            exp3.AddTerm(1.0, x[i, j, k]);
                        }
                    }
                    model.AddConstr(exp3 == 1.0, "c1");
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int k = 0; k < nJobs; ++k)
                    {
                        GRBLinExpr exp4 = 0.0;
                        for (int j = 0; j < nJobs; ++j)
                        {
                            exp4.AddTerm(1.0, x[i, j, k]);
                        }
                        model.AddConstr(exp4 <= 1.0, "c2");
                    }
                }

                // Solve
                model.Optimize();

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; j++)
                    {
                        Lot peekLot = lotsToBeScheduled[j];
                        for (int k = 0; k < nJobs; ++k)
                        {
                            if (x[i, j, k].X == 1.0)
                            {
                                //Console.WriteLine($"({i},{j},{k})" + "ProcessingTime: " + processingTimes[i, j]);

                                //TODO: Schedule on machine
                                ScheduledLotsPerMachine[LithographyArea.Machines[i].Name].Add(peekLot);
                            }
                        }
                    }
                }

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }
    }
}
