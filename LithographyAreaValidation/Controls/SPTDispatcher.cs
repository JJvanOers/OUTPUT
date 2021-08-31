using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Controls
{
    public class SPTDispatcher : DispatcherBase

    {
        public SPTDispatcher(ModelElementBase parent, string name) : base(parent, name)
        {

        }

        public override Lot Dispatch(Machine machine)
        {
            Lot bestLot = null;
            double? shortestProcessingTime = null;

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

                // Check if reticle is available
                Boolean resourceAvailable = CheckResourceAvailability(peekLot);

                // Check if processingTime is known
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe); //TODO: Check

                if (resourceAvailable && recipeEligible && processingTimeKnown)
                {
                    // Check processingTime
                    double processingTime = machine.GetDeterministicProcessingTime(peekLot);

                    if (shortestProcessingTime == null)
                    {
                        bestLot = peekLot;
                        shortestProcessingTime = processingTime;
                    }
                    else
                    {
                        if (processingTime < shortestProcessingTime)
                        {
                            bestLot = peekLot;
                            shortestProcessingTime = processingTime;
                        }
                    }
                }
            }

            if (bestLot == null)
            {
                return bestLot;
            }

            else
            {
                // Dequeue earliestDueDateLot
                Lot dispatchedLot = HandleDeparture(bestLot, machine);

                return dispatchedLot;
            }
        }

        public override void HandleMachineDown(Machine machine)
        {
            //Do nothing
        }
        public override void HandleEndMachineDown()
        {
            //Do nothing
        }


    }
}
