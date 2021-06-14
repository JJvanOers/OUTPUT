using CSSL.Modeling.Elements;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements.Dispatchers
{
    public class MIVSDispatcher : DispatcherBase
    {
        public MIVSDispatcher(WorkCenter workCenter, string name, int kStepAhead, int jStepBack) : base(workCenter, name)
        {
            this.kStepAhead = kStepAhead;
            this.jStepBack = jStepBack;
        }

        private int kStepAhead { get; set; }
        private int jStepBack { get; set; }

        private Dictionary<LotStep, double> WIPtargets => wc.WaferFab.WIPTargets;

        private Lot lotToDispatch { get; set; }

        private void updateWeights()
        {
            int indexLotMaxWeight = 0;
            double maxWeight = double.MinValue;

            for (int i = 0; i < wc.Queue.Length; i++)
            {
                Lot lot = wc.Queue.PeekAt(i);

                double weight = 0.0;

                for (int relStep = -jStepBack; relStep <= 0; relStep++)
                {
                    if (lot.HasRelativeStep(relStep))
                    {
                        LotStep step = lot.GetRelativeStep(relStep);

                        weight += lot.GetRelativeWorkCenter(relStep).Queues[step].Length - WIPtargets[step];
                    }
                }

                for (int relStep = 1; relStep <= kStepAhead; relStep++)
                {
                    if (lot.HasRelativeStep(relStep))
                    {
                        LotStep step = lot.GetRelativeStep(relStep);

                        weight += WIPtargets[step] - lot.GetRelativeWorkCenter(relStep).Queues[step].Length;
                    }
                }

                //Console.WriteLine($"{lot.Id}\t{lot.GetCurrentWorkCenter.Name}\t{lot.GetCurrentStep.Name}\t{(int)WIPtargets[lot.GetCurrentStep]}\t{lot.GetCurrentWorkCenter.Queues[lot.GetCurrentStep].Length}\t=" +
                //    $"{lot.GetCurrentWorkCenter.Queues[lot.GetCurrentStep].Length - (int)WIPtargets[lot.GetCurrentStep]}");

                // Only if weight is higher, replace lotToDispatch. If weight is equal, do FIFO, so keep old one.
                if (weight > maxWeight)
                {
                    indexLotMaxWeight = i;
                    maxWeight = weight;
                }
            }

            lotToDispatch = wc.Queue.PeekAt(indexLotMaxWeight);
            //Console.WriteLine($"{lotToDispatch.Id} {lotToDispatch.GetCurrentStep.Name}");
        }

        public override void HandleArrival(Lot lot)
        {
            // TO DO: implement overtaking
            wc.Queues[lot.GetCurrentStep].EnqueueLast(lot);
            wc.Queue.EnqueueLast(lot);

            // Queue was empty upon arrival, lot gets taken into service and departure event is scheduled immediately
            if (wc.TotalQueueLength == 1)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }
        }

        public override void HandleDeparture()
        {
            updateWeights();

            wc.Queue.Dequeue(lotToDispatch);
            wc.Queues[lotToDispatch.GetCurrentStep].Dequeue(lotToDispatch);

            // Schedule next departure event, if queue is nonempty
            if (wc.TotalQueueLength > 0)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }

            // Send to next workcenter. Caution: always put this after the schedule next departure event part.
            lotToDispatch.SendToNextWorkCenter();
            lotToDispatch = null;
        }

        public override void HandleFirstDeparture()
        {
            HandleDeparture();
        }

        public override void HandleInitialization(List<Lot> lots)
        {
            // null values will appear at beginning of the lists with this ordering
            foreach (Lot lot in lots.OrderBy(x => x.ArrivalReal))
            {
                HandleArrival(lot);
            }
        }
    }
}
