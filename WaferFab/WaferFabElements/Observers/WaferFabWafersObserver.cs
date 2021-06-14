using CSSL.Modeling;
using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements.Observers
{
    class WaferFabWafersObserver : ModelElementObserverBase
    {
        public WaferFabWafersObserver(Simulation mySimulation, string name, WaferFab waferFab) : base(mySimulation, name)
        {
            queueLengths = new Dictionary<LotStep, Variable<int>>();
            queueLengthsStatistics = new Dictionary<LotStep, WeightedStatistic>();

            orderedLotSteps = waferFab.LotSteps.Values.OrderBy(x => x.Id).ToList();

            foreach (LotStep step in orderedLotSteps)
            {
                queueLengths.Add(step, new Variable<int>(this));
                queueLengthsStatistics.Add(step, new WeightedStatistic("QueueLength_" + step.Name));
            }
        }

        private Dictionary<LotStep, Variable<int>> queueLengths;

        private Dictionary<LotStep, WeightedStatistic> queueLengthsStatistics;

        private List<LotStep> orderedLotSteps;

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            WaferFab waferFab = (WaferFab)modelElement;

            foreach (var queueLength in queueLengths.Values)
            {
                queueLength.Reset();
            }

            foreach (var queueLengthStatistic in queueLengthsStatistics.Values)
            {
                queueLengthStatistic.Reset();
            }

            headerToFile(waferFab);
        }
        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
        protected override void OnInitialized(ModelElementBase modelElement)
        {
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            WaferFab waferFab = (WaferFab)modelElement;

            foreach (var workCenter in waferFab.WorkCenters.Values)
            {
                foreach (KeyValuePair<LotStep, CSSLQueue<Lot>> queue in workCenter.Queues)
                {
                    int nrOfWafers = 0;
                    for (int i = 0; i < queue.Value.Length; i++)
                    {
                        Lot lot = queue.Value.PeekAt(i);
                        nrOfWafers += lot.QtyReal;
                    }
                    queueLengths[queue.Key].UpdateValue(nrOfWafers);
                    queueLengthsStatistics[queue.Key].Collect(queueLengths[queue.Key].PreviousValue, queueLengths[queue.Key].Weight);
                }
            }

            writeOutputToFile(waferFab);
            //writeOutputToConsole(waferFab);
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            // Write last system status to file
            WaferFab waferFab = (WaferFab)modelElement;

            foreach (var workCenter in waferFab.WorkCenters.Values)
            {
                foreach (KeyValuePair<LotStep, CSSLQueue<Lot>> queue in workCenter.Queues)
                {
                    int nrOfWafers = 0;
                    for (int i = 0; i < queue.Value.Length; i++)
                    {
                        Lot lot = queue.Value.PeekAt(i);
                        nrOfWafers += lot.QtyReal;
                    }
                    queueLengths[queue.Key].UpdateValue(nrOfWafers);
                    queueLengthsStatistics[queue.Key].Collect(queueLengths[queue.Key].PreviousValue, queueLengths[queue.Key].Weight);
                }
            }

            writeOutputToFile(waferFab);
            //writeOutputToConsole(waferFab);
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(WaferFab waferFab)
        {
            Writer?.Write("Simulation Time, Wall Clock Time,");

            foreach (LotStep step in waferFab.LotSteps.Values.OrderBy(x => x.Id))
            {
                Writer?.Write($"{step.Name},");
            }

            Writer?.Write("\n");
        }

        private void writeOutputToFile(WaferFab waferFab)
        {
            Writer?.Write(waferFab.GetTime + "," + waferFab.GetWallClockTime + ",") ;

            foreach (LotStep step in orderedLotSteps)
            {
                Writer?.Write(queueLengths[step].Value + ",");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToConsole(WaferFab waferFab)
        {
            Console.Write(waferFab.GetTime + "," + waferFab.GetWallClockTime + ",");

            foreach (LotStep step in orderedLotSteps)
            {
                Console.Write($"{step.Name} " + queueLengths[step].Value + ",");
            }
            Console.Write("\n");
        }
    }
}
