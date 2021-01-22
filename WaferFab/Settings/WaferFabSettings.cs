using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Utilities;

namespace WaferFabSim
{
    [Serializable]
    public class WaferFabSettings
    {
        public WaferFabSettings()
        {
            SampleInterval = 10 * 60;

            ManualLotStartQtys = new Dictionary<string, int>();
            InitialRealLots = new List<RealLot>();
            LotTypes = new List<string>();
            WorkCenters = new List<string>();
            LotSteps = new Dictionary<string, LotStep>();
            WCServiceTimeDistributions = new Dictionary<string, Distribution>();
            WCDispatchers = new Dictionary<string, string>();
            WCOvertakingDistributions = new Dictionary<string, OvertakingDistributionBase>();
            LotStepsPerWorkStation = new Dictionary<string, List<LotStep>>();
            Sequences = new Dictionary<string, Sequence>();
        }

        // Initial time
        public DateTime? InitialTime => InitialRealLots.Any() ? (DateTime?)InitialRealLots.First().SnapshotTime : null;

        // Initial Lots. These are the lots which are already present in the waferfab at initial time of the simulation
        public List<RealLot> InitialRealLots { get; set; }
        public bool UseRealLotStartsFlag { get; set; }

        // Observers
        public double SampleInterval { get; set; }

        // Lot starts
        /// <summary>
        /// Lot start frequency in hours
        /// </summary>
        public int LotStartsFrequency { get; set; }
        public Dictionary<string, int> ManualLotStartQtys { get; set; }
        public List<Tuple<DateTime, Lot>> LotStarts { get; set; }
        public List<Tuple<DateTime, RealLot>> RealLotStarts { get; set; }
        public List<Tuple<DateTime, Lot>> GetLotStarts()
        {
            if (!Sequences.Any())
            {
                throw new Exception("Sequences has to be filled before getting lot starts");
            }
            else if (RealLotStarts == null)
            {
                throw new Exception("WaferFabSettings does not contain real lot starts. Either run simulation without real lot starts or load them from a serialized file.");
            }

            List<Tuple<DateTime, Lot>> lotStarts = new List<Tuple<DateTime, Lot>>();

            DateTime time = InitialTime == null ? DateTime.MinValue : (DateTime)InitialTime;

            foreach (var real in RealLotStarts.Where(x => x.Item1 >= InitialTime))
            {
                lotStarts.Add(new Tuple<DateTime, Lot>(real.Item1, real.Item2.ConvertToLot(0, Sequences, true)));
            }

            return lotStarts.Where(x => x.Item2 != null).ToList();
        }

        // Model
        public List<string> LotTypes { get; set; }
        public Dictionary<string, LotStep> LotSteps { get; set; }
        public List<string> WorkCenters { get; set; }
        public Dictionary<string, Distribution> WCServiceTimeDistributions { get; set; }
        public Dictionary<string, OvertakingDistributionBase> WCOvertakingDistributions { get; set; }
        public Dictionary<string, string> WCDispatchers { get; set; }
        public Dictionary<string, List<LotStep>> LotStepsPerWorkStation { get; set; }
        public Dictionary<string, Sequence> Sequences { get; set; }

        // Processing Time History
        public Dictionary<string, WorkCenterLotActivities> ProcessingTimeHistories { get; set; }

    }

}
