using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using RLToyFab.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Observers
{
    public class WaferFabLotsObserver : ModelElementObserverBase
    {
        public WaferFabLotsObserver(Simulation mySimulation, string name, ToyFab toyFab) : base(mySimulation, name)
        {
            queueLengths = new Dictionary<LotStep, Variable<int>>();
            queueLengthsStatistics = new Dictionary<LotStep, WeightedStatistic>();

            orderedLotSteps = toyFab.LotSteps.Values.OrderBy(x => x.Id).ToList();

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
            ToyFab toyFab = (ToyFab)modelElement;

            foreach (var queueLength in queueLengths.Values)
            {
                queueLength.Reset();
            }

            foreach (var queueLengthStatistic in queueLengthsStatistics.Values)
            {
                queueLengthStatistic.Reset();
            }

            headerToFile(toyFab);
        }
        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
        protected override void OnInitialized(ModelElementBase modelElement)
        {
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            ToyFab toyFab = (ToyFab)modelElement;
            
            if (toyFab.currentObserverType == ToyFab.GetObserverTypes.HourlyWIP.ToString()) // for multiple observers sampling at different epochs
            {
                foreach (var workCenter in toyFab.WorkStations.Values)
                {
                    foreach (var step in workCenter.LotSteps)
                    {
                        queueLengths[step].UpdateValue(workCenter.Queues[step].Length + workCenter.Machines.Where(x => x.LotInService?.GetCurrentStep == step).Count()); // queue + in progress
                        queueLengthsStatistics[step].Collect(queueLengths[step].PreviousValue, queueLengths[step].Weight);
                    }
                }
                writeOutputToFile(toyFab);
                //writeOutputToConsole(toyFab);
            }
            
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            // Write last system status to file
            ToyFab toyFab = (ToyFab)modelElement;

            foreach (var workCenter in toyFab.WorkStations.Values)
            {
                foreach (var step in workCenter.LotSteps)
                {
                    queueLengths[step].UpdateValue(workCenter.Queues[step].Length);
                    queueLengthsStatistics[step].Collect(queueLengths[step].PreviousValue, queueLengths[step].Weight);
                }
            }

            writeOutputToFile(toyFab);
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(ToyFab toyFab)
        {
            Writer?.Write("Simulation Time, Wall Clock Time,");

            foreach (LotStep step in orderedLotSteps)
            {
                Writer?.Write($"{step.Name},");
            }

            Writer?.Write("\n");
        }

        private void writeOutputToFile(ToyFab toyFab)
        {
            Writer?.Write(toyFab.GetTime + "," + toyFab.GetWallClockTime + ",");

            foreach (LotStep step in orderedLotSteps)
            {
                Writer?.Write(queueLengths[step].Value + ",");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToConsole(ToyFab toyFab)
        {
            Console.Write(toyFab.GetTime + "," + toyFab.GetWallClockTime + ",");

            foreach (LotStep step in orderedLotSteps)
            {
                Console.Write($"{step.Name} " + queueLengths[step].Value + ",");
            }
            Console.Write("\n");
        }
    }
}
