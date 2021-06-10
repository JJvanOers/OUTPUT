using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements.Observers
{
    public class WaferFabTotalQueueObserver : ModelElementObserverBase
    {
        public WaferFabTotalQueueObserver(Simulation mySimulation, string name, WaferFab waferFab) : base(mySimulation, name)
        {
            queueLengths = new Dictionary<WorkCenter, Variable<int>>();
            queueLengthsStatistics = new Dictionary<WorkCenter, WeightedStatistic>();

            orderedWorkCenters = waferFab.WorkCenters.Values.OrderBy(x => x.Id).ToList();

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                queueLengths.Add(workCenter, new Variable<int>(this));
                queueLengthsStatistics.Add(workCenter, new WeightedStatistic("QueueLength_" + workCenter.Name));
            }
        }

        private Dictionary<WorkCenter, Variable<int>> queueLengths;

        private Dictionary<WorkCenter, WeightedStatistic> queueLengthsStatistics;

        private List<WorkCenter> orderedWorkCenters;

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

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                queueLengths[workCenter].UpdateValue(workCenter.Queue.Length);
                queueLengthsStatistics[workCenter].Collect(queueLengths[workCenter].PreviousValue, queueLengths[workCenter].Weight);
            }

            writeOutputToFile(waferFab);
            //writeOutputToConsole(waferFab);
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            // Write last system status to file
            WaferFab waferFab = (WaferFab)modelElement;

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                queueLengths[workCenter].UpdateValue(workCenter.Queue.Length);
                queueLengthsStatistics[workCenter].Collect(queueLengths[workCenter].PreviousValue, queueLengths[workCenter].Weight);
            }

            writeOutputToFile(waferFab);
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(WaferFab waferFab)
        {
            Writer?.Write("Simulation Time,");

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                string[] words = workCenter.Name.Split('_');
                Writer?.Write($"{words.Last()},");
            }

            Writer?.Write("\n");
        }

        private void writeOutputToFile(WaferFab waferFab)
        {
            Writer?.Write(waferFab.GetTime + ",");

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                Writer?.Write(queueLengths[workCenter].Value + ",");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToConsole(WaferFab waferFab)
        {
            Console.Write(waferFab.GetTime + "," + waferFab.GetWallClockTime + ",");

            foreach (WorkCenter workCenter in orderedWorkCenters)
            {
                Console.Write($"{workCenter.Name} " + queueLengths[workCenter].Value + ",");
            }
            Console.Write("\n");
        }
    }
}
