using CSSL.Modeling.Elements;
using CSSL.RL;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Elements.Dispatchers
{
    public abstract class DispatcherBase : RLElementBase
    {
        public DispatcherBase(ModelElementBase parent, string name, RLLayerBase reinforcementLearningLayer = null) : base(parent, name, reinforcementLearningLayer)
        {
            ws = (WorkStation)parent;
        }

        public WorkStation ws;

        public DateTime GetDateTime => ws.GetDateTime;

        public abstract override void Act(int action);

        public abstract override bool TryAct(int action);

        public abstract void DispatchToAny();

        public abstract void DispatchTo(Machine machine);

        public abstract void NotifyLotOut();

        //public abstract void HandleArrival(Lot lot);
        //public abstract void HandleDeparture();
        //public abstract void HandleFirstDeparture();
        //public abstract void HandleInitialization();

        public enum Type
        {
            BQF,
        }
    }
}
