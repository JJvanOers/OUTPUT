using WaferFabSim.WaferFabElements.Utilities;
using CSSL.Modeling.Elements;
using CSSL.Utilities;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Linq;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class WaferFab : EventGeneratorBase, IGetDateTime
    {
        public DateTime InitialDateTime { get; }

        public LotGenerator LotGenerator { get; private set; }

        public Dictionary<string, WorkCenter> WorkCenters { get; private set; }

        public Dictionary<string, Sequence> Sequences { get; private set; }

        public Dictionary<string, LotStep> LotSteps { get; set; }

        public Dictionary<string, int> ManualLotStarts { get; set; }

        public List<Tuple<DateTime, Lot>> LotStarts { get; set; }

        public Dictionary<LotStep, double> WIPTargets { get; private set; }

        /// <summary>
        /// Note: the ordering of these lists is used by some dispatchers to determine initial queue
        /// </summary>
        public List<Lot> InitialLots { get; set; }

        public WaferFab(ModelElementBase parent, string name, ConstantDistribution samplingDistribution, DateTime? initialTime = null)
            : base(parent, name, samplingDistribution)
        {
            WorkCenters = new Dictionary<string, WorkCenter>();
            Sequences = new Dictionary<string, Sequence>();
            LotSteps = new Dictionary<string, LotStep>();
            ManualLotStarts = new Dictionary<string, int>();
            InitialLots = new List<Lot>();
            WIPTargets = new Dictionary<LotStep, double>();
            InitialDateTime = initialTime == null ? DateTime.Now : (DateTime)initialTime;
        }

        public DateTime GetDateTime => InitialDateTime + new TimeSpan(0,0,(int)GetTime);

        public void SetLotGenerator(LotGenerator lotGenerator)
        {
            LotGenerator = lotGenerator;
        }

        public void AddWorkCenter(string name, WorkCenter workCenter)
        {
            WorkCenters.Add(name, workCenter);
        }

        public void AddSequence(string lotType, Sequence sequence)
        {
            Sequences.Add(lotType, sequence);
        }

        public void AddLotStart(string lotType, int quantity)
        {
            ManualLotStarts.Add(lotType, quantity);
        }

        public void AddWIPTargets(Dictionary<LotStep, double> targets)
        {
            if (LotSteps.Values.All(targets.Keys.Contains))
            {
                WIPTargets = targets;
            }
            else
            {
                List<string> missingLotSteps = new List<string>();
                foreach (LotStep lotStep in LotSteps.Values)
                {
                    if (!targets.Keys.Contains(lotStep))
                    {
                        missingLotSteps.Add(lotStep.Name);
                    }
                }

                throw new Exception($"WIP Targets are not complete for following LotSteps: {missingLotSteps}");
            }
        }

        public void UpdateBottleneck()
        {
            int maxBottlenecks = 1;
            List<WorkCenter> workLoadOrdered = WorkCenters.Values.OrderByDescending(x => x.GetTotalWorkload).ToList();
            foreach (WorkCenter workCenter in workLoadOrdered)
            {
                int i = workLoadOrdered.IndexOf(workCenter);
                workCenter.WCisBottleneck = i < maxBottlenecks;  // if current workcenter has highest total workload, then this is a bottleneck (allows for choosing multiple bottlenecks)
            }
        }

        public void ShiftLotStarts(double StartTimeShiftFactor, DateTime timeMinimum)
        {
            //List<Lot> newInitials = new List<Lot>();
            //newInitials = InitialLots;//.Select(x => x.ShiftStarts(StartTimeShiftFactor, InitialDateTime, InitialDateTime)).ToList();

            List<Lot> ShiftedLGenStarts = LotStarts.Select(x => x.Item2.ShiftStarts(StartTimeShiftFactor, InitialDateTime, InitialDateTime)).Where(x => x.StartTimeReal != null).ToList();
            List<Tuple<DateTime, Lot>> newStarts = ShiftedLGenStarts.Select(x => new Tuple<DateTime, Lot>((DateTime)x.StartTimeReal, x)).ToList();

            //if (StartTimeShiftFactor < 1)
            //{
            //    List<Tuple<DateTime, Lot>> moveLots = newStarts.Where(x => x.Item2.StartTimeReal < InitialDateTime).ToList();
            //    foreach (Tuple<DateTime, Lot> tuple in moveLots)
            //    {
            //        newStarts.Remove(tuple);
            //        Lot moveLot = tuple.Item2;
            //        newInitials.Add(moveLot.SetAsInitialLot(InitialDateTime));
            //    }
            //    newInitials = newInitials.Select(x => x.SetBestFittingStep()).Where(x => x != null).ToList();
            //}
            //else if (StartTimeShiftFactor > 1)
            //{
            //    List<Lot> moveLots = newInitials.Where(x => x.StartTimeReal > InitialDateTime && x.StartTimeReal != null).ToList();
            //    foreach (Lot lot in moveLots)
            //    {
            //        //lot.SetCurrentStepCount(-1); // kan dit veilig weg?
            //        //lot.StartTime = default; // " ?
            //        newStarts.Add(new Tuple<DateTime, Lot>((DateTime)lot.StartTimeReal, lot));
            //        newInitials.Remove(lot);
            //    }
            //    newStarts = newStarts.OrderBy(x => x.Item2.StartTimeReal).ToList();
            //}
            //InitialLots = newInitials;
            LotStarts = newStarts;
        }

        /// <summary>
        /// This is created to read out data on a sampled interval.
        /// </summary>
        /// <param name="e"></param>
        protected override void HandleGeneration(CSSLEvent e)
        {
            NotifyObservers(this);

            ScheduleEvent(NextEventTime(), HandleGeneration);
        }

        /// <summary>
        /// This reads out the data on t = 0;
        /// </summary>
        /// <param name="e"></param>
        private void HandleFirstSnapshot(CSSLEvent e)
        {
            NotifyObservers(this);
        }

        protected override void OnReplicationStart()
        {
            base.OnReplicationStart();

            ScheduleEvent(0, HandleFirstSnapshot);
        }
    }
}
