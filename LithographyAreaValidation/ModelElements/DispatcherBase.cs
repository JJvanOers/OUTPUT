using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LithographyAreaValidation.ModelElements
{
    public abstract class DispatcherBase : SchedulingElementBase
    {
        public DispatcherBase(ModelElementBase parent, string name) : base(parent, name)
        {
            LithographyArea = (LithographyArea)parent;

            // Create new queue object
            Queue = new CSSLQueue<Lot>(this, name + "_Queue");

            ReticleDictionary = new Dictionary<string, string>();

            ScheduledLotsPerMachine = new Dictionary<string, List<Lot>>();

            // Create processingTimeDictionary
            MachineEligibilityDictionary = LithographyArea.Reader.ReadLotMachineEligibilities(LithographyArea.StartDate);

            LayerMachinePreferences = new Dictionary<string, string[]>() {

            { "OW Photo", new string[]      {"Any"                                                  } },
            { "ZL Photo", new string[]      {"ASML#4"           ,"ASML#6"           ,"Any"          } },
            { "DC Photo", new string[]      {"Any"                                                  } },
            { "DP Photo", new string[]      {"Any"                                                  } },
            { "OD Photo", new string[]      {"Any"                                                  } },
            { "OC Photo", new string[]      {"StepCluster#2"    , "StepCluster#3"   , "No options"  } },
            { "TR Photo", new string[]      {"StepCluster#5"    , "StepCluster#3"   , "No options"  } },
            { "PS Photo", new string[]      {"StepCluster#13"   , "Any"                             } },
            { "AP T3 Photo", new string[]   {"StepCluster#10"   , "StepCluster#13"  , "Any"         } },
            { "SN Photo", new string[]      {"Any"                                                  } },
            { "CO Photo", new string[]      {"StepCluster#8"    , "StepCluster#1"   , "No options"  } },
            { "IN Photo", new string[]      {"StepCluster#11"   , "Any ASML"        , "Any"         } },
            { "CB Photo", new string[]      {"Any ASML"         , "Any"                             } },
            { "VI Photo", new string[]      {"Any ASML"         , "Any"                             } },
            { "TC Photo", new string[]      {"ASML#9"           , "Any ASML"        , "No options"  } }

            };

            //LayerMachinePreferences = new Dictionary<string, string[]>() {

            //{ "OW Photo", new string[]      {"Any"                                                  } },
            //{ "ZL Photo", new string[]      {"Any"          } },
            //{ "DC Photo", new string[]      {"Any"                                                  } },
            //{ "DP Photo", new string[]      {"Any"                                                  } },
            //{ "OD Photo", new string[]      {"Any"                                                  } },
            //{ "OC Photo", new string[]      { "Any" } },
            //{ "TR Photo", new string[]      { "Any" } },
            //{ "PS Photo", new string[]      { "Any" } },
            //{ "AP T3 Photo", new string[]   { "Any" } },
            //{ "SN Photo", new string[]      {"Any"                                                  } },
            //{ "CO Photo", new string[]      { "Any" } },
            //{ "IN Photo", new string[]      { "Any" } },
            //{ "CB Photo", new string[]      { "Any" } },
            //{ "VI Photo", new string[]      { "Any" } },
            //{ "TC Photo", new string[]      { "Any" } }

            //};

            AvailableResources = new Dictionary<string, int>
            {
                { "AP Photo", 2 },
                { "AP T3 Photo", 2 },
                { "CB Photo", 2 },
                { "CO Photo", 2 },
                { "DB Photo", 2 },
                { "DC Photo", 2 },
                { "DL Photo", 2 },
                { "DP Photo", 2 },
                { "IN Photo", 2 },
                { "LR Photo", 2 },
                { "NW Photo", 2 },
                { "OC Photo", 2 },
                { "OD Photo", 2 },
                { "OW Photo", 2 },
                { "PB Photo", 2 },
                { "PI Photo", 2 },
                { "PR Photo", 2 },
                { "PS Photo", 2 },
                { "SN Photo", 2 },
                { "SP Photo", 2 },
                { "TB Photo", 2 },
                { "TC Photo", 2 },
                { "TR Photo", 2 },
                { "VI Photo", 2 },
                { "ZL Photo", 2 }
            };

            WeightsWIPBalance = LithographyArea.Reader.ReadWeightsWIPBalance(LithographyArea.StartDate);
        }

        protected LithographyArea LithographyArea { get; }

        // Properties
        protected int SetupSameReticleARMS { get; }
        protected int SetupDifferentReticleARMS { get; }
        protected int SetupDifferentIRDARMS { get; }

        protected int SetupSameReticleRMS { get; }
        protected int SetupDifferentReticleRMS { get; }
        protected int SetupDifferentIRDRMS { get; }
        protected CSSLQueue<Lot> Queue { get; }

        public Dictionary<string, double> WeightsWIPBalance {get;}

        protected Dictionary<string, string[]> LayerMachinePreferences { get; set; }

        protected Dictionary<string, List<Lot>> ScheduledLotsPerMachine{get;set;}

        protected Dictionary<string, string> MachineEligibilityDictionary { get; }

        protected Dictionary<string, string> ReticleDictionary { get; set; }

        protected Dictionary<string, int> AllocatedResources { get; set; }

        protected Dictionary<string, int> AvailableResources { get; set; }

        public List<Lot> ValidationLots { get; set; }

        public abstract Lot Dispatch(Machine machine);
        public abstract void HandleMachineDown(Machine machine);
        public abstract void HandleEndMachineDown();


        protected override void OnReplicationStart()
        {
            // Load stocker with reticles
            ReticleDictionary.Clear();

            for (int i = 0; i <= 10000; i++)
            {
                if (i == 4106)
                {
                    ReticleDictionary.Add($"{i}_a", "Stocker");
                    ReticleDictionary.Add($"{i}_b", "Stocker");
                }
                else
                {
                    ReticleDictionary.Add($"{i}", "Stocker");
                }
            }


            AllocatedResources = new Dictionary<string, int>
            {
                { "AP Photo", 0 },
                { "AP T3 Photo", 0 },
                { "CB Photo", 0 },
                { "CO Photo", 0 },
                { "DB Photo", 0 },
                { "DC Photo", 0 },
                { "DL Photo", 0 },
                { "DP Photo", 0 },
                { "IN Photo", 0 },
                { "LR Photo", 0 },
                { "NW Photo", 0 },
                { "OC Photo", 0 },
                { "OD Photo", 0 },
                { "OW Photo", 0 },
                { "PB Photo", 0 },
                { "PI Photo", 0 },
                { "PR Photo", 0 },
                { "PS Photo", 0 },
                { "SN Photo", 0 },
                { "SP Photo", 0 },
                { "TB Photo", 0 },
                { "TC Photo", 0 },
                { "TR Photo", 0 },
                { "VI Photo", 0 },
                { "ZL Photo", 0 }
            };
        }

        public void HandleArrival(Lot lot)
        {
            //Place lot in queue
            Queue.EnqueueLast(lot);
            TriggerMachinesWaiting();

            //// Check if Processing time is known on any machine
            //if (CheckProcessingTimeKnown(lot) == 1)
            //{
            //    // Place lot in queue
            //    Queue.EnqueueLast(lot);

            //    TriggerMachinesWaiting();
            //}
            //else
            //{
            //    Console.WriteLine($"No Processing Time known of Lot {lot.LotID}");
            //}
        }

        public Lot HandleDeparture(Lot lot, Machine machine)
        {
            // Dequeue earliestDueDateLot
            Lot dispatchedLot = Queue.Dequeue(lot);

            // Allocate Resource
            AllocateResource(dispatchedLot);

            // Change reticle location
            ChangeReticleLocation(dispatchedLot, machine);

            return dispatchedLot;
        }

        public void TriggerMachinesWaiting()
        {
            // If machines are waiting
            if (LithographyArea.WaitingMachines.Count > 0)
            {
                List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);
                // Trigger HandleStartRun for machines waiting
                foreach (Machine machine in copyList)
                {
                    machine.DispatchNextLot();
                }
            }
        }

        public void TriggerMachinesWaitingEvent(CSSLEvent e)
        {
            // If machines are waiting
            if (LithographyArea.WaitingMachines.Count > 0)
            {
                List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);
                // Trigger HandleStartRun for machines waiting
                foreach (Machine machine in copyList)
                {
                    machine.DispatchNextLot();
                }
            }
            ScheduleEvent(GetTime + 3600, TriggerMachinesWaitingEvent);
        }

        public void HandleReticleArrival(string reticleID, string machine)
        {
            // Special case if reticle = 4106
            if (reticleID == "4106")
            {
                // if reticlelocation == machine name

                if (ReticleDictionary[$"{reticleID}_a"] == machine)
                {
                    ReticleDictionary[$"{reticleID}_a"] = "Stocker";
                }
                else if (ReticleDictionary[$"{reticleID}_b"] == machine)
                {
                    ReticleDictionary[$"{reticleID}_b"] = "Stocker";
                }
                else
                {
                    //TODO: Print error
                }
            }
            else
            {
                ReticleDictionary[reticleID] = "Stocker";
            }
        }

        // Get total weighted squared "lateness" of lots in queue
        public double GetSquaredLatenessQueue() // Nog aan te vullen
        {
            double totalSquaredLatenessQueue = 0;
            
            // Get lateness of each lot in the queue
            for (int i = 0; i< Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime estimatedProductionTime = LithographyArea.StartDate.AddSeconds(GetTime).AddDays(1); //Next day

                double lateness = (peekLot.ImprovedDueDate.Subtract(estimatedProductionTime)).TotalDays;
                totalSquaredLatenessQueue += lateness * lateness;
            }
            return totalSquaredLatenessQueue;
        }

        public double GetEarlinessQueue(bool squared) // Nog aan te vullen
        {
            double totalEarlinessQueue = 0;

            // Get lateness of each lot in the queue
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime estimatedProductionTime = LithographyArea.StartDate.AddSeconds(GetTime).AddDays(1); //Next day

                double lateness = (peekLot.ImprovedDueDate.Subtract(estimatedProductionTime)).TotalDays;

                if (lateness>0)
                {
                    if (squared)
                    {
                        totalEarlinessQueue += lateness*lateness;
                    }
                    else
                    {
                        totalEarlinessQueue += lateness;
                    }
                }
            }
            return totalEarlinessQueue;
        }

        public double GetTardinessQueue(bool squared) // Nog aan te vullen
        {
            double totalTardinessQueue = 0;

            // Get lateness of each lot in the queue
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime estimatedProductionTime = LithographyArea.StartDate.AddSeconds(GetTime).AddDays(1); //Next day

                double lateness = (peekLot.ImprovedDueDate.Subtract(estimatedProductionTime)).TotalDays;

                if (lateness <= 0)
                {
                    if (squared)
                    {
                        totalTardinessQueue += lateness*lateness;
                    }
                    else
                    {
                        totalTardinessQueue -= lateness;
                    }
                }
            }
            return totalTardinessQueue;
        }

        public int GetQueueLength()
        {
            return Queue.Length;
        }

        public int GetNrJobsEarly()
        {
            int total = 0;
            
            // Get lateness of each lot in the queue
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime estimatedProductionTime = LithographyArea.StartDate.AddSeconds(GetTime).AddDays(1); //Next day

                double lateness = (peekLot.ImprovedDueDate.Subtract(estimatedProductionTime)).TotalDays;
                if (lateness>=0)
                {
                    total += 1;
                }
            }

            return total;
        }

        public int GetNrJobsTardy()
        {
            int total = 0;

            // Get lateness of each lot in the queue
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime estimatedProductionTime = LithographyArea.StartDate.AddSeconds(GetTime).AddDays(1); //Next day

                double lateness = (peekLot.ImprovedDueDate.Subtract(estimatedProductionTime)).TotalDays;
                if (lateness < 0)
                {
                    total += 1;
                }
            }
            return total;
        }

        public string GetRecipe(Lot lot, Machine machine)
        {
            string recipe;
            if (machine.Name.Contains("StepCluster"))
            {
                recipe = lot.RecipeStepCluster;
            }
            else
            {
                recipe = lot.RecipeStandAlone;
            }
            return recipe;
        }

        public Boolean CheckMachineEligibility(string recipeKey)
        {
            Boolean recipeEligible;
            if (MachineEligibilityDictionary.ContainsKey(recipeKey))
            {
                if (MachineEligibilityDictionary[recipeKey] == "Yes")
                {
                    recipeEligible = true;
                }
                else
                {
                    recipeEligible = false;
                }
            }
            else
            {
                recipeEligible = false;
            }
            return recipeEligible;
        }

        protected int CheckReticleAvailability(Lot lot)
        {
            string reticle = lot.ReticleID1;
            int reticleAvailable;

            // Special check if reticle = 4106
            if (reticle == "4106")
            {
                if (ReticleDictionary[$"{reticle}_a"] == "Stocker" || ReticleDictionary[$"{reticle}_b"] == "Stocker")
                {
                    reticleAvailable = 1;
                }
                else
                {
                    reticleAvailable = 0;
                }
            }
            else
            {
                if (ReticleDictionary[reticle] == "Stocker")
                {
                    reticleAvailable = 1;
                }
                else
                {
                    reticleAvailable = 0;
                }
            }
            return reticleAvailable;
        }

        protected Boolean CheckResourceAvailability(Lot lot)
        {
            Boolean allAvailable;

            string auxResourceA = lot.ReticleID1;
            Boolean auxResourceA_Available;

            // Special check if reticle = 4106
            if (auxResourceA == "4106")
            {
                if (ReticleDictionary[$"{auxResourceA}_a"] == "Stocker" || ReticleDictionary[$"{auxResourceA}_b"] == "Stocker")
                {
                    auxResourceA_Available = true;
                }
                else
                {
                    auxResourceA_Available = false;
                }
            }
            else
            {
                if (ReticleDictionary[auxResourceA] == "Stocker")
                {
                    auxResourceA_Available = true;
                }
                else
                {
                    auxResourceA_Available = false;
                }
            }

            string auxResourceB = lot.IrdName;
            Boolean auxResourceB_Available;

            if (AvailableResources.ContainsKey(auxResourceB))
            {
                if (AvailableResources[auxResourceB] - AllocatedResources[auxResourceB] <= 0)
                {
                    auxResourceB_Available = false;
                }
                else
                {
                    auxResourceB_Available = true;
                }
            }
            else
            {
                auxResourceB_Available = true;
            }

            if (auxResourceA_Available && auxResourceB_Available)
            {
                allAvailable = true;
            }
            else
            {
                allAvailable = false;
            }

            return allAvailable;
        }

        public Boolean CheckProcessingTimeKnown(Lot lot, Machine machine, string recipe)
        {
            Boolean processingTimeKnown = false;
            if (machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.MasksetLayer))
            {
                processingTimeKnown = true;
            }
            else
            {
                if (machine.Name.Contains("StepCluster"))
                {
                    if (machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStepCluster))
                    {
                        processingTimeKnown = true;
                    }
                }
                else
                {
                    if (machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStandAlone))
                    {
                        processingTimeKnown = true;
                    }
                }
            }

            return processingTimeKnown;
        }

        private int CheckProcessingTimeKnown(Lot lot)
        {
            int processingTimeKnown = 0;

            foreach (Machine machine in LithographyArea.Machines)
            {
                if (machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.MasksetLayer) || machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStandAlone) || machine.DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStepCluster))
                {
                    processingTimeKnown = 1;
                }
            }
            
            return processingTimeKnown;
        }

        protected void ChangeReticleLocation(Lot lot, Machine machine)
        {
            string reticle = lot.ReticleID1;

            // Special check if reticle = 4106
            if (reticle == "4106")
            {
                if (ReticleDictionary[$"{reticle}_a"] == "Stocker")
                {
                    ReticleDictionary[$"4106_a"] = machine.Name;
                }
                else if (ReticleDictionary[$"{reticle}_b"] == "Stocker")
                {
                    ReticleDictionary[$"4106_b"] = machine.Name;
                }
            }
            else
            {
                ReticleDictionary[reticle] = machine.Name;
            }
        }

        protected void AllocateResource(Lot lot)
        {
            string resource = lot.IrdName;

            AllocatedResources[resource] += 1;

        }

        public void MakeResourceAvailable(Lot lot)
        {
            string resource = lot.IrdName;

            AllocatedResources[resource] -= 1;
        }

        protected double CalculateWeight(Lot lot)
        {
            //Get weightSize
            double maxProcessingTime = 0;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                foreach (Machine machine in LithographyArea.Machines)
                {
                    // Get needed recipe for the lot
                    string recipe = GetRecipe(peekLot, machine);
                    string recipeKey = machine.Name + "__" + recipe;

                    // Check if processingTime is known
                    Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                    if (processingTimeKnown)
                    {
                        double processingTime = machine.GetDeterministicProcessingTime(peekLot);
                        if (processingTime > maxProcessingTime)
                        {
                            maxProcessingTime = processingTime;
                        }
                    }
                }
            }
            double totalProcessingTime = 0;
            double count = 0;
            foreach (Machine machine in LithographyArea.Machines)
            {
                // Get needed recipe for the lot
                string recipe = GetRecipe(lot, machine);
                string recipeKey = machine.Name + "__" + recipe;

                // Check if processingTime is known
                Boolean processingTimeKnown = CheckProcessingTimeKnown(lot, machine, recipe);

                if (processingTimeKnown)
                {
                    totalProcessingTime += machine.GetDeterministicProcessingTime(lot);
                    count += 1;
                }
            }
            double averageProcessingTime = totalProcessingTime / count;
            double weightSize = ((double)lot.LotQty / 25) * (averageProcessingTime / maxProcessingTime);

            // Get weightDueDate
            DateTime? minimumDueDate = null;
            DateTime? maximumDueDate = null;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime dueDate = peekLot.ImprovedDueDate;

                if (minimumDueDate == null || dueDate < minimumDueDate)
                {
                    minimumDueDate = dueDate;
                }
                if (maximumDueDate == null || dueDate > maximumDueDate)
                {
                    maximumDueDate = dueDate;
                }
            }

            double weightDueDate;

            // Scale Alpha by queue length
            double scale = 1;
            for (int i = 0; i <= (Math.Ceiling((double)Queue.Length / (double)LithographyArea.Machines.Count)); i++)
            {
                scale += i;
            }
            double scaledAlpha = (scale * LithographyArea.WeightA)/100;

            if ((DateTime)maximumDueDate - (DateTime)minimumDueDate == new TimeSpan(0))
            {
                weightDueDate = 1 + scaledAlpha;
            }
            else
            {
                weightDueDate = 1 + scaledAlpha * (((DateTime)maximumDueDate - lot.ImprovedDueDate) / ((DateTime)maximumDueDate - (DateTime)minimumDueDate));
            }

            //Get weightWIPBalance
            double weightWIPBalance = GetWIPBalanceWeight(lot);

            double weight = weightSize * (LithographyArea.WeightB * weightDueDate + ((1 - LithographyArea.WeightB) * weightWIPBalance));

            if (lot.Speed == "Hot")
            {
                weight = weightSize * (1 + scaledAlpha);
            }

            return weight;
        }


        protected double CalculateShiftedDueDate(Lot lot)
        {
            // Get weightDueDate
            DateTime? minimumDueDate = null;
            DateTime? maximumDueDate = null;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime dueDate = peekLot.ImprovedDueDate;

                if (minimumDueDate == null || dueDate < minimumDueDate)
                {
                    minimumDueDate = dueDate;
                }
                if (maximumDueDate == null || dueDate > maximumDueDate)
                {
                    maximumDueDate = dueDate;
                }
            }

            TimeSpan deltaDueDate = TimeSpan.Zero;

            if (maximumDueDate > LithographyArea.StartDate)
            {
                deltaDueDate = (DateTime)maximumDueDate - LithographyArea.StartDate;
            }

            DateTime shiftedDueDate = lot.ImprovedDueDate - deltaDueDate;

            if (lot.Speed == "Hot")
            {
                shiftedDueDate = (DateTime)minimumDueDate - deltaDueDate;
            }

            double shiftedDueDateInSimulatedSeconds = (shiftedDueDate - LithographyArea.StartDate).TotalSeconds;

            return shiftedDueDateInSimulatedSeconds;
        }

        protected double CalculateWeight(Lot lot, Machine machine)
        {
            //Get weightSize
            double maxProcessingTime = 0;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                // Get needed recipe for the lot
                string recipe = GetRecipe(peekLot, machine);
                string recipeKey = machine.Name + "__" + recipe;

                // Check if processingTime is known
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                // Check if needed recipe is eligible on machine
                Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                if (processingTimeKnown && recipeEligible)
                {
                    double checkProcessingTime = machine.GetDeterministicProcessingTime(peekLot);
                    if (checkProcessingTime > maxProcessingTime)
                    {
                        maxProcessingTime = checkProcessingTime;
                    }
                }
            }

            double processingTime = machine.GetDeterministicProcessingTime(lot);

            double weightSize = ((double)lot.LotQty / 25) * (processingTime / maxProcessingTime);

            // Get weightDueDate
            DateTime? minimumDueDate = null;
            DateTime? maximumDueDate = null;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime dueDate = peekLot.ImprovedDueDate;

                if (minimumDueDate == null || dueDate < minimumDueDate)
                {
                    minimumDueDate = dueDate;
                }
                if (maximumDueDate == null || dueDate > maximumDueDate)
                {
                    maximumDueDate = dueDate;
                }
            }

            double weightDueDate;

            // Scale Alpha by queue length
            double scale = 1;
            for (int i = 0; i <= (Math.Ceiling((double)Queue.Length / (double)LithographyArea.Machines.Count)); i++)
            {
                scale += i;
            }
            double scaledAlpha = (scale * LithographyArea.WeightA) / 100;

            if ((DateTime)maximumDueDate - (DateTime)minimumDueDate == new TimeSpan(0))
            {
                weightDueDate = 1 + scaledAlpha;
            }
            else
            {
                weightDueDate = 1 + scaledAlpha * (((DateTime)maximumDueDate - lot.ImprovedDueDate) / ((DateTime)maximumDueDate - (DateTime)minimumDueDate));
            }

            //Get weightWIPBalance
            double weightWIPBalance = GetWIPBalanceWeight(lot);

            double weight = weightSize * (LithographyArea.WeightB * weightDueDate + ((1 - LithographyArea.WeightB) * weightWIPBalance));

            if (lot.Speed == "Hot")
            {
                weight = weightSize * (1 + scaledAlpha);
            }

            return weight;
        }

        public double GetDueDateWeight(Lot lot)
        {
            // Get weightDueDate
            DateTime? minimumDueDate = null;
            DateTime? maximumDueDate = null;
            for (int i = 0; i < Queue.Length; i++)
            {
                Lot peekLot = Queue.PeekAt(i);

                DateTime dueDate = peekLot.ImprovedDueDate;

                if (minimumDueDate == null || dueDate < minimumDueDate)
                {
                    minimumDueDate = dueDate;
                }
                if (maximumDueDate == null || dueDate > maximumDueDate)
                {
                    maximumDueDate = dueDate;
                }
            }

            double weightDueDate;

            if ((DateTime)maximumDueDate - (DateTime)minimumDueDate == new TimeSpan(0))
            {
                weightDueDate = 1;
            }
            else
            {
                weightDueDate = (((DateTime)maximumDueDate - lot.ImprovedDueDate) / ((DateTime)maximumDueDate - (DateTime)minimumDueDate));
            }

            if (lot.Speed == "Hot")
            {
                weightDueDate = 1;
            }

            return weightDueDate;
        }

        public double GetWIPBalanceWeight(Lot lot)
        {
            //Get weightWIPBalance
            double thisWeightWIPBalance;
            double weightWIPBalance;
            if (LithographyArea.Dynamic)
            {
                weightWIPBalance = 0;
            }
            else
            {
                double maxWeightWIPBalance = WeightsWIPBalance.Values.Max();
                double minWeightWIPBalance = WeightsWIPBalance.Values.Min();
                string lotID;
                if (lot.LotID.Contains('.'))
                {
                    lotID = lot.LotID.Split('.')[0];
                }
                else
                {
                    lotID = lot.LotID;
                }

                if (WeightsWIPBalance.ContainsKey(lotID))
                {
                    thisWeightWIPBalance = WeightsWIPBalance[lotID];
                }
                else
                {
                    thisWeightWIPBalance = 0;
                }

                if (maxWeightWIPBalance - minWeightWIPBalance == 0)
                {
                    weightWIPBalance = 1;
                }
                else
                {
                    weightWIPBalance = ((thisWeightWIPBalance - minWeightWIPBalance) / (maxWeightWIPBalance - minWeightWIPBalance));
                }
            }

            if (lot.Speed == "Hot")
            {
                weightWIPBalance = 1;
            }

            return weightWIPBalance;
        }

        protected double CalculateWeightILP(Lot lot)
        {
            // Get weightDueDate
            double weightDueDate = GetDueDateWeight(lot);

            //Get weightWIPBalance
            double weightWIPBalance = GetWIPBalanceWeight(lot);

            double weight = (LithographyArea.WeightA * weightDueDate + ((1 - LithographyArea.WeightB) * weightWIPBalance));

            if (lot.Speed == "Hot")
            {
                weight = 1;
            }

            return weight;
        }
    }
}
