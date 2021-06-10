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
    public class TotalQueueObserver : ModelElementObserverBase
    {
        public TotalQueueObserver(Simulation mySimulation, string name) : base(mySimulation, name)
        {
            queueLength = new Variable<int>(this);
            queueLengthStatistic = new WeightedStatistic("QueueLength");
        }

        private Variable<int> queueLength;

        private WeightedStatistic queueLengthStatistic;

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            WorkCenter workCenter = (WorkCenter)modelElement;
            queueLength.UpdateValue(workCenter.TotalQueueLength);
            queueLengthStatistic.Collect(queueLength.PreviousValue, queueLength.Weight);

            Writer?.WriteLine($"{workCenter.GetTime},{queueLength.Value}");
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }

        protected override void OnInitialized(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            queueLength.Reset();
            // Uncomment below if one want to save across replication statistics
            queueLengthStatistic.Reset();

            Writer?.WriteLine("Simulation Time,Queue Length");

            WorkCenter workCenter = (WorkCenter)modelElement;
            queueLength.UpdateValue(workCenter.InitialLots.Count());

            Writer?.WriteLine($"{workCenter.GetTime},{queueLength.Value}");
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            WorkCenter workCenter = (WorkCenter)modelElement;
            queueLength.UpdateValue(workCenter.TotalQueueLength);
            queueLengthStatistic.Collect(queueLength.PreviousValue, queueLength.Weight);

            Writer?.WriteLine($"{workCenter.GetTime},{queueLength.Value}");
        }

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }
    }
}
