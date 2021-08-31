using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.ModelElements
{
    public class LotGenerator : SchedulingElementBase
    {
        public LotGenerator(ModelElementBase parent, string name, DispatcherBase dispatcher) : base(parent, name)
        {
            // Get lithographyArea object
            LithographyArea = (LithographyArea)parent;

            // Get dispatcher
            Dispatcher = dispatcher;

            // Create new queue object
            Queue = new CSSLQueue<Lot>(this, name + "_Queue");

            // Read LotArrivalsAndStartRuns
            LotArrivalsAndStartRuns = LithographyArea.Reader.ReadLotArrivalsAndStartRuns(LithographyArea.StartDate, LithographyArea.LengthOfReplication);
        }

        private LithographyArea LithographyArea { get; }

        private DispatcherBase Dispatcher { get; }

        private CSSLQueue<Lot> Queue { get; }

        private List<Array> LotArrivalsAndStartRuns { get; }



        protected override void OnReplicationStart()
        {
            // Generate all lots
            foreach (Array array in LotArrivalsAndStartRuns)
            {
                // TODO: Get all needed information of lot from array

                string lotID = (string)array.GetValue(0);
                string irdName = (string)array.GetValue(1);
                double arrivalTime = (double)array.GetValue(2);
                DateTime arrivalTimeDate = (DateTime)array.GetValue(3);
                DateTime dueDate = (DateTime)array.GetValue(4);
                dueDate = dueDate.AddHours(6);
                string speed = (string)array.GetValue(5);
                int lotQty = (int)array.GetValue(6);
                string recipeStepCluster = (string)array.GetValue(7);
                string recipeStandAlone = (string)array.GetValue(8);
                string masksetLayer = (string)array.GetValue(9);
                string reticleID1 = (string)array.GetValue(10);
                string reticleID2 = (string)array.GetValue(11);
                DateTime improvedDueDate = (DateTime)array.GetValue(12);

                // Instantiate a lot
                Lot lot = new Lot(lotID, irdName, arrivalTime, arrivalTimeDate, dueDate, speed, lotQty, recipeStepCluster, recipeStandAlone, masksetLayer, reticleID1, reticleID2, improvedDueDate);

                Boolean eligible = false;

                foreach (Machine machine in LithographyArea.Machines)
                {
                    if (machine.Name.Contains("StepCluster"))
                    {
                        string recipeKey1 = machine.Name + "__" + recipeStepCluster;
                        if (Dispatcher.CheckMachineEligibility(recipeKey1) && Dispatcher.CheckProcessingTimeKnown(lot,machine,recipeKey1))
                        {
                            eligible = true;
                        }
                    }
                    else
                    {
                        string recipeKey2 = machine.Name + "__" + recipeStandAlone;
                        if (Dispatcher.CheckMachineEligibility(recipeKey2) && Dispatcher.CheckProcessingTimeKnown(lot, machine, recipeKey2))
                        {
                            eligible = true;
                        }
                    }
                }

                if (!eligible)
                {
                    continue;
                }

                if (lot.IrdName == "Unidentified")
                {
                    continue;
                }

                if (arrivalTime<0) // If arrivaltime <= startTime simulation: Set in queue of dispatcher
                {
                    // Send lot to dispatcher
                    Dispatcher.HandleArrival(lot);
                }
                else if (LithographyArea.Dynamic) // If arrivaltime > startTime simulation: Set in queue of LotGenerator and Schedule arrival event
                {
                    // Put lot in queue of LotGenerator AND Schedule arrival event
                    Queue.EnqueueLast(lot);
                    ScheduleEvent(arrivalTime, HandleLotArrival);
                }
            }
        }

        // Handle the event of a lot arrival
        private void HandleLotArrival(CSSLEvent e)
        {
            // Get first lot in queue of LotGenerator
            Lot lot = Queue.DequeueFirst();

            // Send lot to dispatcher
            Dispatcher.HandleArrival(lot);
        }
    }
}
