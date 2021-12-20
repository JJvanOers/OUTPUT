using CSSL.Modeling;
using CSSL.RL;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RLToyFab.Elements
{
    public class RLLayer : RLLayerBase
    {
        public ToyFab toyFab { get; set; }

        public override void BuildTrainingEnvironment()
        {
            Settings.WriteOutput = false;

            Simulation = new RLSimulation("Access_controller_simulation");

            toyFab = new ToyFab(Simulation.MyModel, "Access_controller", new ConstantDistribution(1*60*60), this);  // todo use the sample interval from waferfab settings
        }
    }
}
