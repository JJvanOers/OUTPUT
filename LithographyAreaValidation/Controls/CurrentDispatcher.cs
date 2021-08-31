using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LithographyAreaValidation.Controls
{
    public class CurrentDispatcher : DispatcherBase
    {
        public CurrentDispatcher(ModelElementBase parent, string name) : base(parent, name)
        {
            
        }

        Boolean FirstScheduleGenerated { get; set; }

        protected Dictionary<string, string> MachineLayerAllocation { get; set; }

        protected Dictionary<string,string> LayerMachineAllocation { get; set; }

        protected Dictionary<string, string> LayerSecondMachineAllocation { get; set; }

        protected override void OnReplicationStart()
        {
            base.OnReplicationStart();

            FirstScheduleGenerated = false;

            // Schedule Reschedule Event
            ScheduleEvent(GetTime, RescheduleAllMachines);
        }


        public override Lot Dispatch(Machine machine)
        {
            Dictionary<string, int> nrLotsInQueuePerLayer = GetNrLotsInQueuePerLayer();

            // Check if a lot of the assinged layer is available in the queue
            if (MachineLayerAllocation[machine.Name] == "None")
            {
                // Reschedule layer on this machine
                RescheduleOneMachine(machine);
            }
            else
            {
                string allocatedLayer = MachineLayerAllocation[machine.Name];

                if (nrLotsInQueuePerLayer.ContainsKey(MachineLayerAllocation[machine.Name]))
                {
                    if (GetNrLotsEligible(machine.Name, allocatedLayer) <= 0)
                    {
                        // Reschedule layer on this machine
                        RescheduleOneMachine(machine);
                    }
                }
                else if (!nrLotsInQueuePerLayer.ContainsKey(MachineLayerAllocation[machine.Name]))
                {
                    // Reschedule layer on this machine
                    RescheduleOneMachine(machine);
                }
            }

            // Check if any hot lots are in the queue and choose the one with the EDD
            Lot hotLot = GetHotLot(machine);

            if (hotLot != null)
            {
                // Dequeue earliestDueDateLot
                Lot dispatchedLot = HandleDeparture(hotLot,machine);

                return dispatchedLot;
            }
            else
            {
                // Check if any lot with a non dedicated layer type is in the queue
                Lot nonDedicatedLayerLot = GetNonDedicatedLayerLot(machine);

                if (nonDedicatedLayerLot != null)
                {
                    // Dequeue earliestDueDateLot
                    Lot dispatchedLot = HandleDeparture(nonDedicatedLayerLot,machine);

                    return dispatchedLot;
                }
                else
                {
                    Lot nonEligibleLot = GetNonEligibleLot(machine);

                    if (nonEligibleLot != null)
                    {
                        // Dequeue earliestDueDateLot
                        Lot dispatchedLot = HandleDeparture(nonEligibleLot, machine);

                        return dispatchedLot;

                    }
                    else
                    {
                        // Check if any lots of the assigned layer are in the queue and choose the one with the EDD
                        Lot assignedLayerLot = GetAssignedLayerLot(machine);

                        if (assignedLayerLot != null)
                        {
                            // Dequeue earliestDueDateLot
                            Lot dispatchedLot = HandleDeparture(assignedLayerLot, machine);

                            return dispatchedLot;
                        }
                        else
                        {
                            return assignedLayerLot;
                        }
                    }
                }
            }
        }

        protected Lot GetHotLot(Machine machine)
        {
            // Get first lot in queue of this layer

            Lot bestLot = null;
            DateTime? earliestDueDate = null;

            // Loop through queue
            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);

                if (peekLot.Speed != "Hot")
                {
                    continue;
                }

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
                    // Check due date
                    DateTime dueDate = peekLot.ImprovedDueDate;

                    if (earliestDueDate == null)
                    {
                        bestLot = peekLot;
                        earliestDueDate = dueDate;
                    }
                    else
                    {
                        if (dueDate.CompareTo(earliestDueDate) < 0)
                        {
                            bestLot = peekLot;
                            earliestDueDate = dueDate;
                        }
                    }
                }
            }
            return bestLot;
        }

        protected Lot GetNonDedicatedLayerLot(Machine machine)
        {
            // Get first lot in queue of this layer
            Lot bestLot = null;
            DateTime? earliestDueDate = null;

            // Loop through queue
            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);

                if (LayerMachineAllocation.ContainsKey(peekLot.IrdName))
                {
                    continue;
                }

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
                    // Check due date
                    DateTime dueDate = peekLot.ImprovedDueDate;

                    if (earliestDueDate == null)
                    {
                        bestLot = peekLot;
                        earliestDueDate = dueDate;
                    }
                    else
                    {
                        if (dueDate.CompareTo(earliestDueDate) < 0)
                        {
                            bestLot = peekLot;
                            earliestDueDate = dueDate;
                        }
                    }
                }
            }
            return bestLot;
        }

        protected Lot GetNonEligibleLot(Machine machine)
        {
            // Get first lot in queue of this layer
            Lot bestLot = null;
            DateTime? earliestDueDate = null;

            // Loop through queue
            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);

                Boolean eligibleOnAllocatedMachine = true;

                // Check if Layer is allocated to a machine, and if so, if that lot is eligible on that machine
                foreach (KeyValuePair<string, string> entry in MachineLayerAllocation)
                {
                    string machineName = entry.Key;
                    string allocatedLayer = entry.Value;
                    Machine thisMachine = null;

                    foreach (Machine equipmentID in LithographyArea.Machines)
                    {
                        if (equipmentID.Name == machineName)
                        {
                            thisMachine = equipmentID;
                        }
                    }

                    if (thisMachine != machine && allocatedLayer == peekLot.IrdName)
                    {
                        // Check if eligible
                        string recipe2 = GetRecipe(peekLot, thisMachine);
                        string recipeKey2 = thisMachine.Name + "__" + recipe2;

                        if (!CheckMachineEligibility(recipeKey2) || !CheckProcessingTimeKnown(peekLot, thisMachine, recipe2))
                        {
                            eligibleOnAllocatedMachine = false;
                        }
                    }
                }

                // Get needed recipe for the lot
                string recipe = GetRecipe(peekLot, machine);
                string recipeKey = machine.Name + "__" + recipe;

                // Check if needed recipe is eligible on machine
                Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                // Check if resource is available
                Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                // Check if processingTime is known
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe); //TODO: Check this

                Boolean allowedToProduce = false;
                int totalOnAllocatedMachines = 0;
                int totalOnOtherMachines = 0;

                foreach (Machine machine2 in LithographyArea.Machines )
                {
                    if (machine2 != machine)
                    {
                        if (MachineLayerAllocation[machine2.Name] == peekLot.IrdName)
                        {
                            totalOnAllocatedMachines += 1;
                        }
                        else
                        {
                            if (machine2.Queue.Length > 0)
                            {
                                Lot peekLotMachine = machine2.Queue.PeekFirst();
                                if (peekLotMachine.IrdName == peekLot.IrdName)
                                {
                                    totalOnOtherMachines += 1;
                                }
                            }
                        }
                    }
                }

                if (totalOnOtherMachines < 1 && totalOnAllocatedMachines < 2)
                {
                    allowedToProduce = true;
                }

                if (!eligibleOnAllocatedMachine && resourceAvailable && recipeEligible && processingTimeKnown && allowedToProduce)
                {
                    // Check due date
                    DateTime dueDate = peekLot.ImprovedDueDate;

                    if (earliestDueDate == null)
                    {
                        bestLot = peekLot;
                        earliestDueDate = dueDate;
                    }
                    else
                    {
                        if (dueDate.CompareTo(earliestDueDate) < 0)
                        {
                            bestLot = peekLot;
                            earliestDueDate = dueDate;
                        }
                    }
                }
            }
            return bestLot;
        }

        protected Lot GetAssignedLayerLot(Machine machine)
        {
            //Get layer allocated to this machine
            string allocatedIrdName = MachineLayerAllocation[machine.Name];

            // Get first lot in queue of this layer
            Lot bestLot = null;
            DateTime? earliestDueDate = null;

            // Loop through queue
            // Get Lots with same Reticle
            List<Lot> lotsWithSameReticle = new List<Lot>();

            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);

                if (peekLot.IrdName != allocatedIrdName)
                {
                    continue;
                }

                if (peekLot.ReticleID1 == machine.PreviousReticleID)
                {
                    lotsWithSameReticle.Add(peekLot);
                }
            }

            if (lotsWithSameReticle.Count>0) //If there are any lots in the queue which use the same reticle as previous lot
            {
                foreach (Lot lot in lotsWithSameReticle)
                {
                    // Get needed recipe for the lot
                    string recipe = GetRecipe(lot, machine);
                    string recipeKey = machine.Name + "__" + recipe;

                    // Check if needed recipe is eligible on machine
                    Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                    // Check if resource is available
                    Boolean resourceAvailable = CheckResourceAvailability(lot);

                    // Check if processingTime is known
                    Boolean processingTimeKnown = CheckProcessingTimeKnown(lot, machine, recipe);

                    if (recipeEligible && resourceAvailable && processingTimeKnown)
                    {
                        // Check due date
                        DateTime dueDate = lot.ImprovedDueDate;

                        if (earliestDueDate == null)
                        {
                            bestLot = lot;
                            earliestDueDate = dueDate;
                        }
                        else
                        {
                            if (dueDate.CompareTo(earliestDueDate) < 0)
                            {
                                bestLot = lot;
                                earliestDueDate = dueDate;
                            }
                        }
                    }
                }
            }
            else //If there are no lots in the queue which use the same reticle as previous lot
            {
                for (int i = 0; i < Queue.Length; i++)
                {
                    // Peek next lot in queue
                    Lot peekLot = Queue.PeekAt(i);

                    if (peekLot.IrdName != allocatedIrdName)
                    {
                        continue;
                    }

                    // Get needed recipe for the lot
                    string recipe = GetRecipe(peekLot, machine);
                    string recipeKey = machine.Name + "__" + recipe;

                    // Check if needed recipe is eligible on machine
                    Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                    // Check if resource is available
                    Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                    // Check if processingTime is known
                    Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                    if (recipeEligible && resourceAvailable && processingTimeKnown)
                    {
                        // Check due date
                        DateTime dueDate = peekLot.ImprovedDueDate;

                        if (earliestDueDate == null)
                        {
                            bestLot = peekLot;
                            earliestDueDate = dueDate;
                        }
                        else
                        {
                            if (dueDate.CompareTo(earliestDueDate) < 0)
                            {
                                bestLot = peekLot;
                                earliestDueDate = dueDate;
                            }
                        }
                    }
                }
            }
            return bestLot;
        }

        protected void RescheduleAllMachines(CSSLEvent e)
        {
            if (!LithographyArea.Dynamic && !FirstScheduleGenerated)
            {
                // Set weight of each Lot
                for (int j = 0; j < Queue.Length; j++)
                {
                    Lot peekLot = Queue.PeekAt(j);

                    Queue.PeekAt(j).WeightDueDate = GetDueDateWeight(peekLot);
                    Queue.PeekAt(j).WeightWIPBalance = GetWIPBalanceWeight(peekLot);
                }

                FirstScheduleGenerated = true;
            }



            // Reset all dictionaries
            ResetDictionaries();

            // Reschedule Machine Layer Allocation
            RescheduleMachineLayerAllocation();

            List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);

            // Trigger HandleStartRun for machines waiting
            foreach (Machine machine in copyList)
            {
                machine.DispatchNextLot();
            }

            // Schedule reschedule event every 4 hours
            ScheduleEvent(GetTime + 4 * 3600, RescheduleAllMachines);
        }

        protected void RescheduleOneMachine(Machine machine)
        {
            // Reset the machine and layer
            string currentLayer = MachineLayerAllocation[machine.Name];

            if (currentLayer != "None")
            {
                MachineLayerAllocation[machine.Name] = "None";
                if (LayerMachineAllocation[currentLayer] == machine.Name)
                {
                    LayerMachineAllocation[currentLayer] = "None";
                }
                else if (LayerSecondMachineAllocation[currentLayer] == machine.Name)
                {
                    LayerSecondMachineAllocation[currentLayer] = "None";
                }
            }

            // Reschedule Machine Layer Allocation
            RescheduleMachineLayerAllocation();
        }

        public override void HandleMachineDown(Machine machine)
        {
            string assignedLayer = MachineLayerAllocation[machine.Name];

            if (assignedLayer != "None")
            {
                LayerMachineAllocation[assignedLayer] = "None";
            }

            // Reset all dictionaries
            ResetDictionaries();

            // Reschedule Machine Layer Allocation
            RescheduleMachineLayerAllocation();

            List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);

            // Trigger HandleStartRun for machines waiting //TODO: Check this
            foreach (Machine equipmentID in copyList)
            {
                equipmentID.DispatchNextLot();
            }
        }
        public override void HandleEndMachineDown()
        {
            // Reset all dictionaries
            ResetDictionaries();

            // Reschedule Machine Layer Allocation
            RescheduleMachineLayerAllocation();

            List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);

            // Trigger HandleStartRun for machines waiting //TODO: Check this
            foreach (Machine machine in copyList)
            {
                machine.DispatchNextLot();
            }
        }

        protected void RescheduleMachineLayerAllocation()
        {
            Dictionary<string, int> nrLotsInQueuePerLayer = GetNrLotsInQueuePerLayer();

            int threshold_a= 4; // Preferred number of lots of the same layer type to produce after each other
            int threshold_b = 24; // Preferred number of lots of the same layer type in queue when producing on a second machine and target not achievable by one machine
            int threshold_c = 8; // Preferred number of lots of the same layer type in queue when producing on a second machine

            string[] firstSequence = { "TR Photo", "CO Photo", "PS Photo", "IN Photo", "AP T3 Photo", "OC Photo", "TC Photo", "VI Photo", "CB Photo" };

            string[] secondSequence = {"ZL Photo", "OW Photo", "DC Photo", "DP Photo","OD Photo", "SN Photo"};

            // Allocate layers which does not have fulfilled the production target and have enough lots in the queue to a first machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_a) & TargetNotMet(layer))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_a) & TargetNotMet(layer))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            AllocateFirstMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers, of which the production target cannot be fulfilled anymore by one machine, to a second machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_b) & TargetNotAchievableByOneMachine(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_b) & TargetNotAchievableByOneMachine(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            AllocateSecondMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which have fulfilled the production target and have enough lots in the queue to a first machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_a))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_a))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            AllocateFirstMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which does not have fulfilled the production target and have one lot in the queue to a first machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & TargetNotMet(layer))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & TargetNotMet(layer))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            AllocateFirstMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which have fulfilled the production target and have one lot in the queue to a first machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1))
                {
                    AllocateFirstMachine(layer);
                }
            }
            AllocateFirstMachineLayersAnyASML();
            AllocateFirstMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which does not have fulfilled the production target and have enough lots in the queue to a second machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_c) & TargetNotMet(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_c) & TargetNotMet(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            AllocateSecondMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which have fulfilled the production target and have enough lots in the queue to a second machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_c) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, threshold_c) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            AllocateSecondMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which does not have fulfilled the production target and have one lot in the queue to a second machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & TargetNotMet(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & TargetNotMet(layer) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            AllocateSecondMachineLayersAny();
            ResetLayerMachineAllocation();

            // Allocate layers which have fulfilled the production target and have one lot in the queue to a second machine
            for (int i = 0; i < firstSequence.Length; i++)
            {
                string layer = firstSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            for (int i = 0; i < secondSequence.Length; i++)
            {
                string layer = secondSequence[i];

                if (InQueue(nrLotsInQueuePerLayer, layer, 1) & LayerMachineAllocation[layer] != "None")
                {
                    AllocateSecondMachine(layer);
                }
            }
            AllocateSecondMachineLayersAnyASML();
            AllocateSecondMachineLayersAny();
            ResetLayerMachineAllocation();
        }

        protected Boolean InQueue(Dictionary<string, int> nrLotsInQueuePerLayer, string layer, int minimum)
        {
            if (nrLotsInQueuePerLayer.ContainsKey(layer))
            {
                if (nrLotsInQueuePerLayer[layer] >= minimum)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        protected Boolean TargetNotAchievableByOneMachine(string layer)
        {
            int wafersToBeProduced = LithographyArea.LayerTargets[layer] - LithographyArea.LayerActivities[layer];

            double remainingTime = 24 * 3600 - (GetTime) % (24 * 3600);

            double timePerWafer = 45; //TODO: Check this time

            double remainingNeededMachineTime = (double)wafersToBeProduced * timePerWafer;

            double threshold = 4 * 3600; //TODO: threshold before activating a second machine 

            if (remainingNeededMachineTime > (remainingTime + threshold))
            {
                return true;
            }
            else
            {
                return false;
            }

            //double remainingCapacityOneMachine = remainingTime / timePerWafer;

            //if (remainingCapacityOneMachine < wafersToBeProduced)
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
        }

        protected Boolean TargetNotMet(string layer)
        {
            if (LithographyArea.LayerTargets[layer] - LithographyArea.LayerActivities[layer] > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void AllocateFirstMachine(string layer)
        {
            string[] layerMachinePreferences = LayerMachinePreferences[layer];

            for (int i = 0; i < layerMachinePreferences.Length; i++)
            {
                string nextPreferredMachine = layerMachinePreferences[i];

                if (LayerMachineAllocation[layer] == "None")
                {
                    if (nextPreferredMachine == "Any")
                    {
                        LayerMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else if (nextPreferredMachine == "Any ASML")
                    {
                        LayerMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else if (nextPreferredMachine == "No options")
                    {
                        LayerMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else
                    {
                        if (layer == "TR Photo" || layer == "CO Photo") // If a machine is already assigned, but not to this layer
                        {
                            if (MachineLayerAllocation[nextPreferredMachine] != "None" && MachineLayerAllocation[nextPreferredMachine] != layer)
                            {
                                break;
                            }
                        }

                        if (LithographyArea.MachineStates[nextPreferredMachine] != "Down" & MachineLayerAllocation[nextPreferredMachine] == "None" & GetNrLotsEligible(nextPreferredMachine, layer) > 0)
                        {
                            LayerMachineAllocation[layer] = nextPreferredMachine;
                            MachineLayerAllocation[nextPreferredMachine] = layer;
                            break;
                        }
                    }
                }
            }
        }

        protected void AllocateSecondMachine(string layer)
        {
            string[] layerMachinePreferences = LayerMachinePreferences[layer];

            for (int i = 0; i < layerMachinePreferences.Length; i++)
            {
                string nextPreferredMachine = layerMachinePreferences[i];

                if (LayerSecondMachineAllocation[layer] == "None")
                {
                    if (nextPreferredMachine == "Any")
                    {
                        LayerSecondMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else if (nextPreferredMachine == "Any ASML")
                    {
                        LayerSecondMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else if (nextPreferredMachine == "No options")
                    {
                        LayerSecondMachineAllocation[layer] = nextPreferredMachine;
                        break;
                    }
                    else
                    {
                        if (LithographyArea.MachineStates[nextPreferredMachine] != "Down" & MachineLayerAllocation[nextPreferredMachine] == "None" & GetNrLotsEligible(nextPreferredMachine, layer) > 0)
                        {
                            LayerSecondMachineAllocation[layer] = nextPreferredMachine;
                            MachineLayerAllocation[nextPreferredMachine] = layer;
                            break;
                        }
                    }
                }
            }
        }

        protected void AllocateFirstMachineLayersAnyASML()
        {
            List<string> layers = new List<string>();
            List<string> machines = new List<string>();

            foreach (KeyValuePair<string, string> entry in LayerMachineAllocation)
            {
                if (entry.Value == "Any ASML")
                {
                    layers.Add(entry.Key);
                }
            }

            foreach (KeyValuePair<string, string> entry in MachineLayerAllocation)
            {
                if (entry.Value == "None" & entry.Key.Contains("ASML") & LithographyArea.MachineStates[entry.Key] != "Down")
                {
                    machines.Add(entry.Key);
                }
            }
            BestFirstMachineLayerAllocation(layers, machines);
        }

        protected void AllocateSecondMachineLayersAnyASML()
        {
            List<string> layers = new List<string>();
            List<string> machines = new List<string>();

            foreach (KeyValuePair<string, string> entry in LayerSecondMachineAllocation)
            {
                if (entry.Value == "Any ASML")
                {
                    layers.Add(entry.Key);
                }
            }

            foreach (KeyValuePair<string, string> entry in MachineLayerAllocation)
            {
                if (entry.Value == "None" & entry.Key.Contains("ASML") & LithographyArea.MachineStates[entry.Key] != "Down")
                {
                    machines.Add(entry.Key);
                }
            }
            BestSecondMachineLayerAllocation(layers, machines);
        }

        protected void AllocateFirstMachineLayersAny()
        {
            List<string> layers = new List<string>();
            List<string> machines = new List<string>();

            foreach (KeyValuePair<string, string> entry in LayerMachineAllocation)
            {
                if (entry.Value == "Any")
                {
                    layers.Add(entry.Key);
                }
            }

            foreach (KeyValuePair<string, string> entry in MachineLayerAllocation)
            {
                if (entry.Value == "None" & LithographyArea.MachineStates[entry.Key] != "Down")
                {
                    machines.Add(entry.Key);
                }
            }
            BestFirstMachineLayerAllocation(layers, machines);
        }

        protected void AllocateSecondMachineLayersAny()
        {
            List<string> layers = new List<string>();
            List<string> machines = new List<string>();

            foreach (KeyValuePair<string, string> entry in LayerSecondMachineAllocation)
            {
                if (entry.Value == "Any")
                {
                    layers.Add(entry.Key);
                }
            }

            foreach (KeyValuePair<string, string> entry in MachineLayerAllocation)
            {
                if (entry.Value == "None" & LithographyArea.MachineStates[entry.Key] != "Down")
                {
                    machines.Add(entry.Key);
                }
            }
            BestSecondMachineLayerAllocation(layers, machines);
        }


        protected void BestFirstMachineLayerAllocation(List<string> layers, List<string> machines)
        {
            // TODO: Permutate

            List<string> unassignedLayers = layers;
            List<string> unassignedMachines = machines;

            while (unassignedLayers.Count > 0 & unassignedMachines.Count > 0)
            {
                int highestValue = 0;
                string bestLayer = null;
                string bestMachine = null;
                foreach (string layer in unassignedLayers)
                {
                    foreach (string machine in unassignedMachines)
                    {
                        int nrLotsEligible = GetNrLotsEligible(machine, layer);
                        if (nrLotsEligible > highestValue)
                        {
                            highestValue = nrLotsEligible;
                            bestLayer = layer;
                            bestMachine = machine;
                        }
                    }
                }

                if (highestValue > 0)
                {
                    LayerMachineAllocation[bestLayer] = bestMachine;
                    MachineLayerAllocation[bestMachine] = bestLayer;

                    unassignedLayers.Remove(bestLayer);
                    unassignedMachines.Remove(bestMachine);
                }

                if (bestLayer == null)
                {
                    break;
                } 
            }
        }

        protected void BestSecondMachineLayerAllocation(List<string> layers, List<string> machines)
        {
            // TODO: Permutate

            List<string> unassignedLayers = layers;
            List<string> unassignedMachines = machines;

            while (unassignedLayers.Count > 0 & unassignedMachines.Count > 0)
            {
                int highestValue = 0;
                string bestLayer = null;
                string bestMachine = null;
                foreach (string layer in unassignedLayers)
                {
                    foreach (string machine in unassignedMachines)
                    {
                        int nrLotsEligible = GetNrLotsEligible(machine, layer);
                        if (nrLotsEligible > highestValue)
                        {
                            highestValue = nrLotsEligible;
                            bestLayer = layer;
                            bestMachine = machine;
                        }
                    }
                }

                if (highestValue > 0)
                {
                    LayerSecondMachineAllocation[bestLayer] = bestMachine;
                    MachineLayerAllocation[bestMachine] = bestLayer;

                    unassignedLayers.Remove(bestLayer);
                    unassignedMachines.Remove(bestMachine);
                }

                if (bestLayer == null)
                {
                    break;
                }
            }
        }

        protected Dictionary<string, int> GetNrLotsInQueuePerLayer()
        {
            Dictionary<string, int> nrLotsPerIRDName = new Dictionary<string, int>();
            // Loop through queue
            for (int i = 0; i < Queue.Length; i++)
            {
                // Peek next lot in queue
                Lot peekLot = Queue.PeekAt(i);
                string irdName = peekLot.IrdName;

                if (!(nrLotsPerIRDName.ContainsKey(irdName)))
                {
                    nrLotsPerIRDName.Add(irdName, 1);
                }
                else
                {
                    int currentValue = nrLotsPerIRDName[irdName];
                    nrLotsPerIRDName[irdName] = currentValue + 1;
                }
            }
            return nrLotsPerIRDName;
        }


        protected int GetNrLotsEligible(string machineName, string layer)
        {
            int nrLotsEligible = 0;
            foreach (Machine machine in LithographyArea.Machines)
            {
                if (machine.Name == machineName)
                {
                    // Loop through queue
                    for (int i = 0; i < Queue.Length; i++)
                    {
                        // Peek next lot in queue
                        Lot peekLot = Queue.PeekAt(i);
                        string irdName = peekLot.IrdName;
                        string recipe = GetRecipe(peekLot, machine);
                        string recipeKey = machine.Name + "__" + recipe;
                        Boolean recipeEligible = CheckMachineEligibility(recipeKey);

                        // Check if reticle is available
                        //Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                        // Check if processingTime is known
                        Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                        if (recipeEligible && processingTimeKnown && layer == irdName)
                        {
                            nrLotsEligible += 1;
                        }
                    }
                }
            }
            return nrLotsEligible;
        }

        protected void ResetLayerMachineAllocation()
        {
            List<string> keys = new List<string>(LayerMachineAllocation.Keys);
            foreach (string key in keys)
            {
                if (LayerMachineAllocation[key] == "Any ASML" || LayerMachineAllocation[key] == "Any" || LayerMachineAllocation[key] == "No options")
                {
                    LayerMachineAllocation[key] = "None";
                }
            }

            keys = new List<string>(LayerSecondMachineAllocation.Keys);
            foreach (string key in keys)
            {
                if (LayerSecondMachineAllocation[key] == "Any ASML" || LayerSecondMachineAllocation[key] == "Any" || LayerSecondMachineAllocation[key] == "No options")
                {
                    LayerSecondMachineAllocation[key] = "None";
                }
            }
        }

        protected void ResetDictionaries()
        {
            MachineLayerAllocation = new Dictionary<string, string>
            {
                { "StepCluster#1", "None" },
                { "StepCluster#2", "None" },
                { "StepCluster#3", "None" },
                { "StepCluster#5", "None" },
                { "StepCluster#7", "None" },
                { "StepCluster#8", "None" },
                { "StepCluster#10", "None" },
                { "StepCluster#11", "None" },
                { "StepCluster#13", "None" },
                { "ASML#4", "None" },
                { "ASML#6", "None" },
                { "ASML#9", "None" }
            };

            LayerMachineAllocation = new Dictionary<string, string>
            {
                { "OW Photo", "None" },
                { "ZL Photo", "None" },
                { "DC Photo", "None" },
                { "DP Photo", "None" },
                { "OD Photo", "None" },
                { "OC Photo", "None" },
                { "TR Photo", "None" },
                { "PS Photo", "None" },
                { "AP T3 Photo", "None" },
                { "SN Photo", "None" },
                { "CO Photo", "None" },
                { "IN Photo", "None" },
                { "CB Photo", "None" },
                { "VI Photo", "None" },
                { "TC Photo", "None" }
            };

            LayerSecondMachineAllocation = new Dictionary<string, string>
            {
                { "OW Photo", "None" },
                { "ZL Photo", "None" },
                { "DC Photo", "None" },
                { "DP Photo", "None" },
                { "OD Photo", "None" },
                { "OC Photo", "None" },
                { "TR Photo", "None" },
                { "PS Photo", "None" },
                { "AP T3 Photo", "None" },
                { "SN Photo", "None" },
                { "CO Photo", "None" },
                { "IN Photo", "None" },
                { "CB Photo", "None" },
                { "VI Photo", "None" },
                { "TC Photo", "None" }
            };
        }
    }
}
