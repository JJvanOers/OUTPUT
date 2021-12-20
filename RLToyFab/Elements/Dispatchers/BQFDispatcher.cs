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
            //NotifyObservers(this); // This is called when a job arrives to an empty machine, or when a machine has finished serving
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
                ws.Queue.Dequeue(lot);
                machine.HandleArrival(lot);
            }
        }

        public override void NotifyLotOut()
        {
            NotifyObservers(this);
        }

        //public override void HandleArrival(Lot lot)
        //{
        //    ws.Queues[lot.GetCurrentStep].EnqueueLast(lot);
        //    if (ws.TotalQueueLength == 1)
        //    {
        //        ScheduleEvent(GetTime + ws.ServiceTimeDistribution.Next(),ws.HandleDeparture);
        //    }
        //}

        //public override void HandleDeparture()
        //{
        //    // machine becomes available
        //    if (ws.TotalQueueLength > 0)
        //    {

        //    }
        //}

        //public override void HandleFirstDeparture()
        //{
        //    ws.LotStepInService = ws.Queues.OrderByDescending(x => x.Value.Length).First().Key;

        //    HandleDeparture();
        //}

        //public override void HandleInitialization()
        //{
        //    throw new NotImplementedException();
        //    // send to all available machines?
        //}
    }
}
