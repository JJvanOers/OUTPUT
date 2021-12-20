using CSSL.Modeling.Elements;
using CSSL.RL;
using RLToyFab.Elements.Dispatchers;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Elements
{
    public class RLDispatcher : DispatcherBase
    {
        public RLDispatcher(ModelElementBase parent,
            string name,
            RLLayerBase reinforcementLearningLayer,
            WorkStation workStation) : base(parent, name, reinforcementLearningLayer)
        {
            this.workStation = workStation;
        }

        private WorkStation workStation;

        public override void Act(int action)
        {
            throw new NotImplementedException();
        }

        public override bool TryAct(int action)
        {
            throw new NotImplementedException();
        }

        public override void DispatchTo(Machine machine)
        {
            throw new NotImplementedException();
        }

        public override void DispatchToAny()
        {
            throw new NotImplementedException();
        }

        public override void NotifyLotOut()
        {
            throw new NotImplementedException();
        }

        //public override void HandleArrival(Lot lot)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void HandleDeparture()
        //{
        //    throw new NotImplementedException();
        //}

        //public override void HandleFirstDeparture()
        //{
        //    throw new NotImplementedException();
        //}

        //public override void HandleInitialization()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
