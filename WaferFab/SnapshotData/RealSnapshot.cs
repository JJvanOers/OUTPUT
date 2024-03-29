﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.SnapshotData
{
    [Serializable]
    public class RealSnapshot : WIPSnapshotBase
    {
        public DateTime Time { get; private set; }

        public List<RealLot> RealLots { get; set; }

        public List<RealLot> GetRealLots(int waferQtyThreshold)
        {
            return RealLots.Where(x => x.Qty >= waferQtyThreshold).ToList();
        }

        public RealSnapshot(DateTime time, List<RealLot> lots, int waferQtyThreshold)
        {
            RealLots = lots;

            LotSteps = lots.Where(x => x.IRDGroup != null).Select(x => x.IRDGroup).Distinct().ToArray();

            Time = time;

            foreach(var lotStep in LotSteps)
            {
                WIPlevels.Add(lotStep, lots.Where(x => x.IRDGroup == lotStep).Where(x => x.Qty >= waferQtyThreshold).Count());
                WIPlevelsInWafers.Add(lotStep, lots.Where(x => x.IRDGroup == lotStep).Where(x => x.Qty >= waferQtyThreshold).Sum(x => x.Qty));
            }
        }


    }
}
