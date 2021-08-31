using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Controls
{
    public class FIFODispatcher : DispatcherBase
    {
        public FIFODispatcher(ModelElementBase parent, string name) : base(parent, name)
        {
        }

        // Dispatch first eligible lot in queue
        public override Lot Dispatch(Machine machine)
        {
            //Lot peekLot = null;
            Lot dispatchedLot = null;

            // Loop through queue till eligible lot is found
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
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine,recipe);

                // Dispatch if lot is eligible
                if (resourceAvailable && recipeEligible && processingTimeKnown)
                {
                    // Dequeue earliestDueDateLot
                    dispatchedLot = HandleDeparture(peekLot, machine);
                    break;
                }
            }

            if (dispatchedLot == null)
            {
                //Console.WriteLine("Error: No lot in queue which can be dispatched");
            }                     

            return dispatchedLot;
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
