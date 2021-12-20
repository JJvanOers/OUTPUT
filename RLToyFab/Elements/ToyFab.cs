using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Distributions;
using RLToyFab.Observers;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Utilities;

namespace RLToyFab.Elements
{
    public class ToyFab : WaferFab
    {
        public RLLayer RLLayer { get; set; }

        public override LotGenerator LotGenerator { get; set; }

        public Dictionary<string, WorkStation> WorkStations { get; private set; }

        public override Dictionary<string, Sequence> Sequences { get; set; }

        public override Dictionary<string, LotStep> LotSteps { get; set; }

        public override Dictionary<string, int> ManualLotStarts { get; set; }

        public string currentObserverType { get; private set; }
        public enum GetObserverTypes
        {
            HourlyWIP,
            UtilizationUpdate,
            LotOut,
        } 
        public Machine machineToUpdate { get; private set; }


        public ToyFab(ModelElementBase parent, string name, ConstantDistribution samplingDist)
            : base(parent, name, samplingDist)
        {
            WorkStations = new Dictionary<string, WorkStation>();
            Sequences = new Dictionary<string, Sequence>();
            LotSteps = new Dictionary<string, LotStep>();
            ManualLotStarts = new Dictionary<string, int>();
            machineToUpdate = null;
        }
        public ToyFab(ModelElementBase parent, string name, ConstantDistribution samplingDist, RLLayer rlLayer)
            : base(parent, name, samplingDist)
        {
            throw new NotImplementedException();
        }

        new public void SetLotGenerator(LotGenerator lotGenerator)
        {
            LotGenerator = lotGenerator;
        }

        public void AddWorkCenter(string name, WorkStation workCenter)
        {
            WorkStations.Add(name, workCenter);
        }

        new public void AddSequence(string lotType, Sequence sequence)
        {
            Sequences.Add(lotType, sequence);
        }

        new public void AddLotStart(string lotType, int quantity)
        {
            ManualLotStarts.Add(lotType, quantity);
        }

        protected override void HandleGeneration(CSSLEvent e)
        {
            currentObserverType = GetObserverTypes.HourlyWIP.ToString();
            NotifyObservers(this);
            currentObserverType = null;

            ScheduleEvent(NextEventTime(), HandleGeneration);
        }

        public void HandleLotOut(Lot lot)
        {
            currentObserverType = GetObserverTypes.LotOut.ToString();
            NotifyObservers(this);
            currentObserverType = null;
        }

        public void NotifyUtilizationUpdate(Machine machine)
        {
            machineToUpdate = machine;
            currentObserverType = GetObserverTypes.UtilizationUpdate.ToString();
            NotifyObservers(this);
            currentObserverType = null;
            machineToUpdate = null;
        }
    }
}



