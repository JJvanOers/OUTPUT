using CSSL.Modeling.Elements;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements.Dispatchers
{
    public class WLDispatcher : DispatcherBase
    {
        public WLDispatcher(WorkCenter workCenter, string name, int kStepAhead, int jStepBack) : base(workCenter, name)
        {
            this.kStepAhead = kStepAhead;
            this.jStepBack = jStepBack;
            //this.maxBatchSize = 25;
            //this.modX = 0.3;
            //this.modY = 1.4;
        }

        private int kStepAhead { get; set; }
        private int jStepBack { get; set; }
        //private double modX { get; set; }
        //private double modY { get; set; }
        //private double maxBatchSize { get; set; }

        private Dictionary<LotStep, double> WIPtargets => wc.WaferFab.WIPTargets;

        private Lot lotToDispatch { get; set; }

        private void updateWeights()
        {
            int indexLotMaxWeight = 0;
            double maxWeight = double.MinValue;

            for (int i = 0; i < wc.Queue.Length; i++)
            {
                Lot lot = wc.Queue.PeekAt(i);

                //double batchUtil = lot.QtyReal / maxBatchSize;
                //double mod = 1; // Math.Min(Math.Max(modX + (batchUtil - 0.5) * modY, modX), 1.0);

                double workLoadLocal = lot.GetCurrentWorkCenter.GetStepWorkload(lot.GetCurrentStep);
                double weight = workLoadLocal; // mod *

                for (int relStep = -jStepBack; relStep <= 0; relStep++)
                {
                    if (lot.HasRelativeStep(relStep))
                    {
                        LotStep prodStep = lot.GetRelativeStep(relStep);
                        double workLoadUpstream = lot.GetRelativeWorkCenter(relStep).GetStepWorkload(prodStep);
                        weight += workLoadUpstream / Math.Pow(2, Math.Abs(relStep)); // mod *
                    }
                }

                for (int relStep = 1; relStep <= kStepAhead; relStep++)
                {
                    if (lot.HasRelativeStep(relStep))
                    {
                        LotStep prodStep = lot.GetRelativeStep(relStep);
                        double workLoadDownstream = lot.GetRelativeWorkCenter(relStep).GetStepWorkload(prodStep);
                        weight -= workLoadDownstream / Math.Pow(2, Math.Abs(relStep)); // 1/mod * 
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

            //string chosenLotStep = lotToDispatch.GetCurrentStep.Name;
            //Console.WriteLine($"{lotToDispatch.Id} {lotToDispatch.GetCurrentStep.Name}");
        }

        public override void HandleArrival(Lot lot)
        {
            wc.Queues[lot.GetCurrentStep].EnqueueLast(lot);
            wc.Queue.EnqueueLast(lot);
            wc.LatenessBasedQueue?.EnqueueLast(lot); // If ODD is not set, this statement will do nothing

            // Queue was empty upon arrival, lot gets taken into service and departure event is scheduled immediately
            if (wc.TotalQueueLength == 1)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }
        }

        public override void HandleDeparture()
        {
            if (wc.LatenessBasedQueue != null) { wc.WaferFab.UpdateBottleneck(); }
            if (wc.LatenessBasedQueue?.PeekFirst().GetCurrentSchedDev() < 0 && !wc.WCisBottleneck)
            {
                lotToDispatch = wc.LatenessBasedQueue.PeekFirst();
            }
            else { updateWeights(); }

            wc.Queue.Dequeue(lotToDispatch);
            wc.Queues[lotToDispatch.GetCurrentStep].Dequeue(lotToDispatch);
            wc.LatenessBasedQueue?.Dequeue(lotToDispatch); // If ODD is not set, this statement will do nothing

            // Schedule next departure event, if queue is nonempty
            if (wc.TotalQueueLength > 0)
            {
                ScheduleEvent(GetTime + wc.ServiceTimeDistribution.Next(), wc.HandleDeparture);
            }
            
            // Send to next workcenter. Caution: always put this after the schedule next departure event part.
            lotToDispatch.SendToNextWorkCenter();
            
            if (!lotToDispatch.HasNextStep)
            {
                DepartingLot = lotToDispatch;
                NotifyObservers(this);
            }

            lotToDispatch = null;
            DepartingLot = null;
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
