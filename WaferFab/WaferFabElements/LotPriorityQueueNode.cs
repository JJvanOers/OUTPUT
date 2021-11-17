using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    class LotPriorityQueueNode : StablePriorityQueueNode
    {
        public LotPriorityQueueNode(Lot lot) : base()
        {
            this.lot = lot;
        }
        public Lot lot;
    }
}
