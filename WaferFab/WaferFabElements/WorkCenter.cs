using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Utilities;
using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class WorkCenter : SchedulingElementBase, IGetDateTime
    {
        public WorkCenter(ModelElementBase parent, string name, Distribution serviceTimeDistribution, List<LotStep> lotSteps) : base(parent, name)
        {
            WaferFab = (WaferFab)parent;
            LotSteps = lotSteps;
            ServiceTimeDistribution = serviceTimeDistribution;
            LotStepInService = null;
            Queues = new Dictionary<LotStep, LotQueue>();
            Queue = new CSSLQueue<Lot>(this, name + "_TotalQueue");

            foreach (LotStep lotStep in lotSteps)
            {
                Queues.Add(lotStep, new LotQueue(this, name + "_" + lotStep.Name + "_Queue"));
            }

            SetWorkStationInLotSteps();
        }

        public WaferFab WaferFab { get; }

        public Distribution ServiceTimeDistribution { get; }

        public DispatcherBase dispatcher { get; set; }

        public List<LotStep> LotSteps { get; set; }

        public List<Lot> InitialLots
        {
            get
            {
                WaferFab waferFab = (WaferFab)Parent;

                return waferFab.InitialLots.Where(x => x.GetCurrentWorkCenter == this).ToList();
            }
        }

        public Lot LastArrivedLot { get; set; }

        /// <summary>
        /// Flag for observers to indicate whether NotifyObservers is triggered by arrival or departure event. True = arrival, false = departure.
        /// </summary>
        public bool IsArrivalFlag { get; set; }

        private LotStep _lotStepInService;
        public LotStep LotStepInService
        {
            get { return _lotStepInService; }
            set
            {
                if (LotSteps.Contains(value) || value == null)
                {
                    _lotStepInService = value;
                }
                else
                {
                    throw new Exception($"Try to set lot step in service to {value.Name} in {Name}, but this workcenter does not contain that lotstep.");
                }
            }
        }

        /// <summary>
        /// Use this for dispatching one queue, such as EPTOvertakingDispatcher
        /// </summary>
        public CSSLQueue<Lot> Queue { get; set; }

        /// <summary>
        /// Use this for dispatching individual Queues per lotstep, such as BQF dispather
        /// </summary>
        public Dictionary<LotStep, LotQueue> Queues { get; set; }

        /// <summary>
        /// Total WIP in lots at workstation, including lot in service.
        /// </summary>
        public int TotalQueueLength => Queue.Length;

        /// <summary>
        /// Total WIP in wafers at workstation, including lot in service.
        /// </summary>
        public int TotalQueueLengthWafers { get; set; }

        public DateTime GetDateTime => WaferFab.GetDateTime;

        public void SetDispatcher(DispatcherBase dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public void SetWorkStationInLotSteps()
        {
            foreach (LotStep step in LotSteps)
            {
                step.SetWorkCenter(this);
            }
        }

        public void HandleArrival(Lot lot)
        {
            LastArrivedLot = lot;

            // TEMPORARY for WaferFabSim
            lot.WIPIn = TotalQueueLength;

            IsArrivalFlag = true;

            dispatcher.HandleArrival(lot);

            NotifyObservers(this);
        }

        public void HandleDeparture(CSSLEvent e)
        {
            if (LotStepInService == null)
            {
                dispatcher.HandleFirstDeparture();
            }
            else
            {
                IsArrivalFlag = false;

                dispatcher.HandleDeparture();                
            }

            NotifyObservers(this);
        }

        protected override void OnReplicationStart()
        {
            LotStepInService = null;

            // Initialize queues with deep copy of initial lots (such that stepcount does not change in original lot, which is then used for next replication)
            if (InitialLots.Any())
            {
                List<Lot> initialLotsDeepCopy = InitialLots.ConvertAll(x => new Lot(x));



                dispatcher.HandleInitialization(initialLotsDeepCopy);
            }
        }
    }
}
