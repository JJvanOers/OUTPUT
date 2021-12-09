using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Utilities;

namespace RLToyFab.Elements
{
    public class ToyFab : ModelElementBase
    {
        public RLLayer RLLayer { get; set; }

        public LotGenerator LotGenerator { get; private set; }

        public Dictionary<string, WorkStation> WorkStation { get; private set; }

        public Dictionary<string, Sequence> Sequences { get; private set; }

        public Dictionary<string, LotStep> LotSteps { get; set; }

        public Dictionary<string, int> ManualLotStarts { get; set; }


        public ToyFab(ModelElementBase parent, string name)
            : base(parent, name)
        {
            WorkStation = new Dictionary<string, WorkStation>();
            Sequences = new Dictionary<string, Sequence>();
            LotSteps = new Dictionary<string, LotStep>();
            ManualLotStarts = new Dictionary<string, int>();
        }

        public void SetLotGenerator(LotGenerator lotGenerator)
        {
            LotGenerator = lotGenerator;
        }

        public void AddWorkCenter(string name, WorkStation workCenter)
        {
            WorkStation.Add(name, workCenter);
        }

        public void AddSequence(string lotType, Sequence sequence)
        {
            Sequences.Add(lotType, sequence);
        }

        public void AddLotStart(string lotType, int quantity)
        {
            ManualLotStarts.Add(lotType, quantity);
        }

    }
}



