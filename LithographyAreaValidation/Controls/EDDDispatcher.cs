using CSSL.Modeling.Elements;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Controls
{
    public class EDDDispatcher : DispatcherBase

    {
        public EDDDispatcher(ModelElementBase parent, string name) : base(parent, name)
        {

        }

        public override Lot Dispatch(Machine machine)
        {
            Lot earliestDueDateLot = null;
            DateTime? earliestDueDate = null;

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
                Boolean processingTimeKnown = CheckProcessingTimeKnown(peekLot, machine, recipe);

                if (resourceAvailable && recipeEligible && processingTimeKnown)
                {
                    // Check due date
                    DateTime dueDate = peekLot.DueDate;

                    if (earliestDueDate == null)
                    {
                        earliestDueDateLot = peekLot;
                        earliestDueDate = dueDate;
                    }
                    else
                    {
                        if (dueDate.CompareTo(earliestDueDate)<0)
                        {
                            earliestDueDateLot = peekLot;
                            earliestDueDate = dueDate;
                        }
                    }
                }
            }

            if (earliestDueDateLot == null)
            {
                return earliestDueDateLot;
            }

            else
            {
                // Dequeue earliestDueDateLot
                Lot dispatchedLot = HandleDeparture(earliestDueDateLot, machine);

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
