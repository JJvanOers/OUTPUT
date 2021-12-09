using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.RL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Elements.Dispatchers
{
    public class BQFDispatcher : DispatcherBase
    {
        public BQFDispatcher(ModelElementBase parent, string name) : base(parent, name, null)
        {
        }

        public override bool TryAct(int action)
        {
            throw new NotImplementedException();
        }

        public override void Act(int action)
        {
            throw new NotImplementedException();
        }

        public override void DispatchToAny()
        {
            foreach (Machine machine in ws.FreeMachines)
            {
                DispatchTo(machine);
            }
        }

        public override void DispatchTo(Machine machine)
        {
            var eligibleNonEmptyQueues = ws.Queues.Where(x => machine.Eligibilities.Contains(x.Key) && x.Value.Length > 0).OrderByDescending(x => x.Value.Length);

            if (eligibleNonEmptyQueues.Any())
            {
                Lot lot = eligibleNonEmptyQueues.First().Value.DequeueFirst();

                machine.HandleArrival(lot);
            }    
        }
    }
}
