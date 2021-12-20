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
    class LotGeneratorStartsObserver : ModelElementObserverBase
    {
        public LotGeneratorStartsObserver(Simulation mySimulation, string name) : base(mySimulation, name)
        {
        }

        private int NrStarts;

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            LotGenerator lotGenerator = (LotGenerator)modelElement;
            NrStarts += lotGenerator.NJobsToStart;
            Writer?.WriteLine($"{lotGenerator.GetTime},{NrStarts}");
        }


        protected override void OnInitialized(ModelElementBase modelElement)
        {
            Writer?.WriteLine("SimulationTime,NumberOfStarts");

            LotGenerator lotGenerator = (LotGenerator)modelElement;
            NrStarts = lotGenerator.GetInitialWIP;
            Writer?.WriteLine($"{lotGenerator.GetTime},{NrStarts}");
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }

        public override void OnError(Exception error)
        {
        }

        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
        }
    }
}
