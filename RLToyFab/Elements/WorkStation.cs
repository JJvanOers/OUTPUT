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
            Machines = new List<Machine>();
            LotSteps = lotSteps;
            LotStepInService = null;
            DepartingLot = null;
            Queues = new Dictionary<LotStep, CSSLQueue<Lot>>();
            Queue = new CSSLQueue<Lot>(this,name+"_TotalQueue");

            foreach (LotStep lotStep in lotSteps)
            {
                Queues.Add(lotStep, new CSSLQueue<Lot>(this, name + "_" + lotStep.Name + "_Queue"));
            }

            SetWorkStationInLotSteps();
        }

        public ToyFab ToyFab { get; }

        public new DispatcherBase dispatcher { get; set; }

        public List<Machine> Machines { get; set; }

        public IEnumerable<Machine> FreeMachines => Machines.Where(x => x.LotInService == null);

        public int NrFreeMachines => Machines.Where(x => x.LotInService == null).Count();

        public override List<LotStep> LotSteps { get; set; }

        public override Lot LastArrivedLot { get; set; }

        public Lot DepartingLot { get; set; }

        public override bool IsArrivalFlag { get => base.IsArrivalFlag; set => base.IsArrivalFlag = value; }

        public new Dictionary<LotStep, CSSLQueue<Lot>> Queues { get; set; }
        public new CSSLQueue<Lot> Queue { get; set; }

        /// <summary>
        /// Total WIP in lots at workstation, including lot in service.
        /// </summary>
        public override int TotalQueueLength => Queues.Select(x => x.Value.Length).Sum();

        public override void SetWorkStationInLotSteps()
        {
            foreach (LotStep step in LotSteps)
            {
                step.SetWorkCenter(this);
            }
        }

        public override void HandleArrival(Lot lot)
        {
            //NotifyObservers(this);
            lot.WIPIn = TotalQueueLength;
            //LastArrivedLot = lot;
            //IsArrivalFlag = true;
            Queues[lot.GetCurrentStep].EnqueueLast(lot);
            Queue.EnqueueLast(lot);
            if (NrFreeMachines > 0) dispatcher.DispatchToAny();
        }

        public void NotifyUtilizationUpdate()
        {
            NotifyObservers(this);
        }

        public override void HandleDeparture(CSSLEvent e)
        {
            if (NrFreeMachines > 0) dispatcher.DispatchToAny();
            //NotifyObservers(this);
        }

        public void HandleLotOut(Lot lot)
        {
            DepartingLot = lot;
            dispatcher.NotifyLotOut();
            DepartingLot = null;
        }

        protected override void OnReplicationStart()
        {
            LotStepInService = null;

            // Initialize queues with deep copy of initial lots (such that stepcount does not change in original lot, which is then used for next replication)
            //if (InitialLots.Any())
            //{
            //    List<Lot> initialLotsDeepCopy = InitialLots.ConvertAll(x => new Lot(x));
            //    dispatcher.HandleInitialization(initialLotsDeepCopy);
            //}
        }
    }
}
