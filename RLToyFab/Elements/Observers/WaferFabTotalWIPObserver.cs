using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using RLToyFab.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Observers
{
    public class WaferFabTotalWIPObserver : ModelElementObserverBase
    {
        public WaferFabTotalWIPObserver(Simulation mySimulation, string name, ToyFab waferFab) : base(mySimulation, name)
        {
            AcceptUpdateType = ToyFab.GetObserverTypes.HourlyWIP.ToString();

            queueLengths = new Dictionary<WorkStation, Variable<int>>();
            queueLengthsStatistics = new Dictionary<WorkStation, WeightedStatistic>();

            orderedWorkCenters = waferFab.WorkStations.Values.OrderBy(x => x.Id).ToList();

            foreach (WorkStation workCenter in orderedWorkCenters)
            {
                queueLengths.Add(workCenter, new Variable<int>(this));
                queueLengthsStatistics.Add(workCenter, new WeightedStatistic("QueueLength_" + workCenter.Name));
            }
        }

        private Dictionary<WorkStation, Variable<int>> queueLengths;

        private Dictionary<WorkStation, WeightedStatistic> queueLengthsStatistics;

        private List<WorkStation> orderedWorkCenters;

        private string AcceptUpdateType;

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            ToyFab waferFab = (ToyFab)modelElement;

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
            ToyFab waferFab = (ToyFab)modelElement;
            if (waferFab.currentObserverType == AcceptUpdateType) // for multiple observers sampling at different epochs
            {
                foreach (WorkStation workCenter in orderedWorkCenters)
                {
                    queueLengths[workCenter].UpdateValue(workCenter.TotalQueueLength + workCenter.Machines.Where(x=>x.LotInService != null).Count()); // queue + in progress
                    queueLengthsStatistics[workCenter].Collect(queueLengths[workCenter].PreviousValue, queueLengths[workCenter].Weight);
                }
                writeOutputToFile(waferFab);
                //writeOutputToConsole(waferFab);
            }
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            // Write last system status to file
            ToyFab waferFab = (ToyFab)modelElement;

            foreach (WorkStation workCenter in orderedWorkCenters)
            {
                queueLengths[workCenter].UpdateValue(workCenter.TotalQueueLength);
                queueLengthsStatistics[workCenter].Collect(queueLengths[workCenter].PreviousValue, queueLengths[workCenter].Weight);
            }

            writeOutputToFile(waferFab);
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(ToyFab waferFab)
        {
            Writer?.Write("Simulation Time,");

            foreach (WorkStation workCenter in orderedWorkCenters)
            {
                string[] words = workCenter.Name.Split('_');
                Writer?.Write($"{words.Last()},");
            }

            Writer?.Write("\n");
        }

        private void writeOutputToFile(ToyFab waferFab)
        {
            Writer?.Write(waferFab.GetTime + ",");

            foreach (WorkStation workCenter in orderedWorkCenters)
            {
                Writer?.Write(queueLengths[workCenter].Value + ",");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToConsole(ToyFab waferFab)
        {
            Console.Write(waferFab.GetTime + "," + waferFab.GetWallClockTime + ",");

            foreach (WorkStation workCenter in orderedWorkCenters)
            {
                Console.Write($"{workCenter.Name} " + queueLengths[workCenter].Value + ",");
            }
            Console.Write("\n");
        }
    }
}
