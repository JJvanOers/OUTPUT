using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    public class LatenessBasedQueue : CSSLQueue<Lot>
    {
        public LatenessBasedQueue(ModelElementBase parent, string name, string schedulingType) : base(parent, name)
        {
            SchedulingStrat = schedulingType; // Set this to either EDD or ODD
            comparer = GetComparer();
        }
                
        private readonly string SchedulingStrat;

        private IComparer<Lot> comparer;

        private IComparer<Lot> GetComparer()
        {
            if (SchedulingStrat == "EDD") { return new EDDComparer(); }
            else if (SchedulingStrat == "ODD") { return new ODDComparer(); }
            //else if (SchedulingStrat == "CR" || SchedulingStrat == "CR_alt") { return null; }
            else if (SchedulingStrat == "CR") { return new CRComparer(); }
            else if (SchedulingStrat == "CR_alt") { return new CRaltComparer(); }
            else { throw new Exception($"Argument error in Scheduling Type: Unexpected strategy {SchedulingStrat}. Please see implementation for valid inputs."); }
        }

        public override void EnqueueLast(Lot lot)
        {
            int negIndex = items.BinarySearch(lot, comparer);
            int posIndex = ~negIndex;
            if (negIndex >= 0) { base.EnqueueAt(lot, negIndex + 1); } // tied with lowest priority
            else { base.EnqueueAt(lot, ~negIndex); }
            // if (negIndex < 0) { base.EnqueueAt(lot, ~negIndex); }
            // else { base.EnqueueLast(lot); }
        }

        public override void EnqueueAt(Lot lot, int index)
        {
            throw new NotImplementedException(); // The list needs to remain sorted, so EnqueueAt is not usable
        }

        public override Lot Dequeue(Lot lot)
        {
            base.Dequeue(lot);
            return lot;
        }

        public override Lot DequeueAt(int index)
        {
            Lot lot = base.DequeueAt(index);
            if (SchedulingStrat == "CR")
            {
                items = items.OrderBy(x => x.GetCriticalRatio()).ToList(); // for critical ratio, the value needs to be continuously updated, since current time is part of the equation
            }
            else if (SchedulingStrat == "CR_alt")
            {
                items = items.OrderBy(x => x.GetCriticalRatioAlt()).ToList(); // for critical ratio, the value needs to be continuously updated, since current time is part of the equation
            }
            return lot;
        }

        public override Lot DequeueFirst()
        {
            Lot lot = base.DequeueFirst();
            if (SchedulingStrat == "CR")
            {
                items = items.OrderBy(x => x.GetCriticalRatio()).ToList(); // for critical ratio, the value needs to be continuously updated, since current time is part of the equation
            }
            else if (SchedulingStrat == "CR_alt")
            {
                items = items.OrderBy(x => x.GetCriticalRatioAlt()).ToList(); // for critical ratio, the value needs to be continuously updated, since current time is part of the equation
            }
            return lot;
        }
    }

    public class ODDComparer : IComparer<Lot>
    {
        public int Compare([AllowNull] Lot x, [AllowNull] Lot y)
        {
            if (x==null)
            {
                if (y == null) { return 0; }
                else { return -1; }
            }
            else
            {
                if (y == null) { return 1; }
                
                else
                { 
                    double xVal = x.GetCurrentODD;
                    double yVal = y.GetCurrentODD;
                    //if (xVal == yVal)
                    //{ 
                    //    return x.Id.CompareTo(y.Id); // secondary comparison method (tiebreaker)
                    //}
                    return xVal.CompareTo(yVal); 
                }
            }
        }
    }
    public class EDDComparer : IComparer<Lot>
    {
        public int Compare([AllowNull] Lot x, [AllowNull] Lot y)
        {
            if (x == null)
            {
                if (y == null) { return 0; }
                else { return -1; }
            }
            else
            {
                if (y == null) { return 1; }
                else
                {
                    return x.GetDueDate.CompareTo(y.GetDueDate);
                }
            }
        }
    }
    public class CRComparer : IComparer<Lot>
    {
        public int Compare([AllowNull] Lot x, [AllowNull] Lot y)
        {
            if (x == null)
            {
                if (y == null) { return 0; }
                else { return -1; }
            }
            else
            {
                if (y == null) { return 1; }
                else
                {
                    return x.GetCriticalRatio().CompareTo(y.GetCriticalRatio());
                }
            }
        }
    }
    public class CRaltComparer : IComparer<Lot>
    {
        public int Compare([AllowNull] Lot x, [AllowNull] Lot y)
        {
            if (x == null)
            {
                if (y == null) { return 0; }
                else { return -1; }
            }
            else
            {
                if (y == null) { return 1; }
                else
                {
                    return x.GetCriticalRatioAlt().CompareTo(y.GetCriticalRatioAlt());
                }
            }
        }
    }
}
