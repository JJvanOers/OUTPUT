using CSSL.Modeling.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace WaferFabSim.WaferFabElements.Dispatchers
{
    public class LatenessDispatcher : DispatcherBase
    {
        // This class can be either used for EDD or ODD. The only difference between the dispatching strategies is set in ShellModel and handled in workcenter.LatenessbasedQueue
        public LatenessDispatcher(ModelElementBase workCenter, string name) : base(workCenter, name)
        {
        }

        public override void HandleArrival(Lot lot)
        {
            wc.LatenessBasedQueue.EnqueueLast(lot);
            wc.Queue.EnqueueLast(lot);
            wc.Queues[lot.GetCurrentStep].EnqueueLast(lot);

            // Queue was empty upon arrival, lot gets taken into service and departure event is scheduled immediately
            if (wc.TotalQueueLength == 1)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }
        }


        public override void HandleDeparture()
        {
            //for (int i = 0; i<wc.TotalQueueLength)
            Lot lot = wc.LatenessBasedQueue.DequeueFirst();
            wc.Queue.Dequeue(lot);
            wc.Queues[lot.GetCurrentStep].Dequeue(lot);

            // Schedule next departure event, if queue is nonempty
            if (wc.TotalQueueLength > 0)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }

            // Send to next workcenter. Caution: always put this after the schedule next departure event part.
            // Otherwise it causes problems when a lot has to visit the same workstation twice in a row.
            lot.SendToNextWorkCenter();
            if (!lot.HasNextStep)
            {
                DepartingLot = lot;
                NotifyObservers(this);
            }
            DepartingLot = null;
        }

        public override void HandleFirstDeparture()
        {
            HandleDeparture();
        }

        public override void HandleInitialization(List<Lot> lots)
        {
            foreach(Lot lot in lots.OrderBy(x => x.ArrivalReal))
            {
                HandleArrival(lot);
            }
        }
    }
}
