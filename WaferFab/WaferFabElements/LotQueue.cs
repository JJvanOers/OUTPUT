using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    public class LotQueue : CSSLQueue<Lot>
    {
        public LotQueue(ModelElementBase parent, string name) : base(parent, name)
        {
        }

        public int LengthInWafers { get; set; }

        public override void EnqueueLast(Lot lot)
        {
            base.EnqueueLast(lot);
            LengthInWafers += lot.QtyReal;
        }

        public override void EnqueueAt(Lot lot, int index)
        {
            base.EnqueueAt(lot, index);
            LengthInWafers += lot.QtyReal;
        }

        public override Lot Dequeue(Lot lot)
        {
            base.Dequeue(lot);
            LengthInWafers -= lot.QtyReal;
            return lot;
        }

        public override Lot DequeueAt(int index)
        {
            Lot lot = base.DequeueAt(index);
            LengthInWafers -= lot.QtyReal;
            return lot;
        }

        public override Lot DequeueFirst()
        {
            Lot lot = base.DequeueFirst();
            LengthInWafers -= lot.QtyReal;
            return lot;
        }
    }
}
