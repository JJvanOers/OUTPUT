using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using RLToyFab.Elements.Dispatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Utilities;

namespace RLToyFab.Elements
{
    public class WorkStation : WorkCenter
    {
        public WorkStation(ModelElementBase parent, string name, List<LotStep> lotSteps) : 
            base(parent, name, new ConstantDistribution(1), lotSteps)
        {
            ToyFab = (ToyFab)parent;
            LotSteps = lotSteps;
            LotStepInService = null;
            Queues = new Dictionary<LotStep, CSSLQueue<Lot>>();

            foreach (LotStep lotStep in lotSteps)
            {
                Queues.Add(lotStep, new CSSLQueue<Lot>(this, name + "_" + lotStep.Name + "_Queue"));
            }

            SetWorkStationInLotSteps();
        }

        public ToyFab ToyFab { get; }

        public new DispatcherBase Dispatcher { get; set; }

        public List<Machine> Machines { get; set; }

        public IEnumerable<Machine> FreeMachines => Machines.Where(x => x.LotInService == null);

        public int NrFreeMachines => Machines.Where(x => x.LotInService == null).Count();

        public new List<LotStep> LotSteps { get; set; }

        public new Lot LastArrivedLot { get; set; }

        public new Dictionary<LotStep, CSSLQueue<Lot>> Queues { get; set; }

        /// <summary>
        /// Total WIP in lots at workstation, including lot in service.
        /// </summary>
        public new int TotalQueueLength => Queues.Select(x => x.Value.Length).Sum();

        public new void SetWorkStationInLotSteps()
        {
            foreach (LotStep step in LotSteps)
            {
                step.SetWorkCenter(this);
            }
        }

        public new void HandleArrival(Lot lot)
        {
            Queues[lot.GetCurrentStep].EnqueueLast(lot);

            if (NrFreeMachines > 0) Dispatcher.DispatchToAny();
        }
    }
}
