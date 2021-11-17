using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class LotGenerator : EventGeneratorBase
    {
        public LotGenerator(ModelElementBase parent, string name, Distribution interEventTimeDistribution, bool useRealLotStartsFlag) : base(parent, name, interEventTimeDistribution)
        {
            waferFab = (WaferFab)parent;
            UseRealLotStartsFlag = useRealLotStartsFlag;
            previousEventTime = waferFab.InitialDateTime;
            sw1 = new Stopwatch();
            sw2 = new Stopwatch();
            sw3 = new Stopwatch();
        }

        private List<DateTime> dates { get; set; }

        private WaferFab waferFab { get; }

        private DateTime previousEventTime { get; set; }

        private int indexFrom { get; set; } = 0;

        private int indexUntil { get; set; } = 0;

        private Stopwatch sw1;

        private Stopwatch sw2;

        private Stopwatch sw3;

        public bool UseRealLotStartsFlag { get; }

        public int NJobsToStart { get; private set; }

        public int GetInitialWIP => waferFab.InitialLots.Count();

        protected override void HandleGeneration(CSSLEvent e)
        {
            // Schedule next generation
            ScheduleEvent(NextEventTime(), HandleGeneration);

            if (!UseRealLotStartsFlag)
            {
                StartManualLotStarts();
            }
            else
            {
                StartRealLotStarts();
            }
        }

        private void StartManualLotStarts()
        {
            NJobsToStart = 0;
            // Create lots according to preset quantities in LotStarts and send all lots to first workstation
            foreach (KeyValuePair<string, int> lotStart in waferFab.ManualLotStarts)
            {
                Sequence sequence = waferFab.Sequences[lotStart.Key];

                for (int i = 0; i < lotStart.Value; i++)
                {
                    Lot newLot = new Lot(GetTime, sequence);

                    newLot.SendToNextWorkCenter();
                    NJobsToStart++;
                }
            }
            NotifyObservers(this);
        }

        private void StartRealLotStarts()
        {
            DateTime currentEventTime = waferFab.GetDateTime;

            sw2.Start();

            indexFrom = dates.BinarySearch(previousEventTime);
            if (indexFrom < 0) indexFrom = ~indexFrom;

            indexUntil = dates.BinarySearch(currentEventTime);
            if (indexUntil < 0) indexUntil = ~indexUntil;

            sw2.Stop();
            sw3.Start();

            var selectedLotstarts = waferFab.LotStarts.Skip(indexFrom).Take(indexUntil - indexFrom);

            NJobsToStart = selectedLotstarts.Count();
            NotifyObservers(this);

            // Create lots according to preset quantities in LotStarts and send all lots to first workstation
            foreach (Tuple<DateTime, Lot> lot in selectedLotstarts)
            {
                sw1.Start();
                Lot newLot = lot.Item2;

                Lot deepCopiedLot = new Lot(newLot);

                deepCopiedLot.SendToNextWorkCenter();

                sw1.Stop();
            }

            sw3.Stop();
            previousEventTime = currentEventTime;

            
        }

        protected override void OnExperimentStart()
        {
            // Order lot starts on date because StartRealLots utilizes an ordered list to speed up process
            if (waferFab.LotStarts != null)
            {
                waferFab.LotStarts = waferFab.LotStarts.OrderBy(x => x.Item1).ToList();

                dates = waferFab.LotStarts.Select(x => x.Item1).ToList();
            }
        }

    }
}
