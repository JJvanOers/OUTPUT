using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaferFabSim.Import;
using WaferFabSim.WaferFabElements;
using static WaferFabSim.Import.LotTraces;

namespace WaferFabSim.SnapshotData
{
    [Serializable]
    public class RealLot
    {
        public DateTime? SnapshotTime { get; private set; } = null;
        public DateTime? StartTime { get; private set; } = null;
        public DateTime? EndTime { get; private set; } = null;
        public string IRDGroup { get; private set; }
        public string StepName { get; private set; }
        public string LotID { get; private set; }
        public int Qty { get; private set; }
        public string Location { get; private set; }
        public string DeviceType { get; private set; }
        public string MasksetID { get; private set; }
        public string Technology { get; private set; }
        public DateTime TrackOutInTime { get; private set; }
        public int? PlanDay { get; private set; }
        public string Status { get; private set; }
        public string Speed { get; private set; }
        public int ClipWeek { get; private set; }
        private DateTime? arrival => LotActivity.Arrival;
        private DateTime? departure => LotActivity.Departure;
        private int wipIn => LotActivity.WIPIn;
        public LotActivity LotActivity { get; }

        public RealLot(LotActivity activity, LotActivityRaw raw, DateTime? startTime, DateTime? endTime, DateTime? snapshotTime = null)
        {
            SnapshotTime = snapshotTime;
            StartTime = startTime;
            EndTime = endTime;
            IRDGroup = activity.IRDGroup;
            StepName = raw.Stepname;
            LotID = activity.LotId;
            Qty = activity.QtyIn;
            Location = raw.Location;
            DeviceType = raw.ProductType;

            Status = raw.Status;

            LotActivity = activity;
        }

        public RealLot(string headerLine, string dataLine)
        {
            string[] headers = headerLine.Trim(',').Split(',');
            string[] data = dataLine.Trim(',').Split(',');

            for (int i = 0; i < data.Length; i++)
            {
                if (headers[i] == "IRDGroup") { IRDGroup = data[i]; }
                if (headers[i] == "StepName") { StepName = data[i]; }
                if (headers[i] == "LotID") { LotID = data[i]; }
                if (headers[i] == "Qty") { Qty = Convert.ToInt32(data[i]); }
                if (headers[i] == "Location") { Location = data[i]; }
                if (headers[i] == "DeviceType") { DeviceType = data[i]; }
                if (headers[i] == "MasketID") { MasksetID = data[i]; }
                if (headers[i] == "Technology") { Technology = data[i]; }
                if (headers[i] == "TrackOut/InTime") { TrackOutInTime = DateTime.Parse(data[i]); }
                if (headers[i] == "PlanDay") { if (data[i] == " " || data[i] == "") { PlanDay = null; } else { PlanDay = int.Parse(data[i]); } }
                if (headers[i] == "MasketID") { MasksetID = data[i]; }
                if (headers[i] == "Status") { Status = data[i]; }
                if (headers[i] == "Speed") { Speed = data[i]; }
                if (headers[i] == "ClipWeek") { ClipWeek = Convert.ToInt32(data[i]); }
                if (headers[i] == "MasketID") { MasksetID = data[i]; }
            }
        }

        public Lot ConvertToLot(double creationTime, Dictionary<string, Sequence> sequences, bool lotYetToStart, DateTime? initialTime) // lotYetToStart false = isInitialLot
        {
            if (sequences.ContainsKey(DeviceType))
            {
                Sequence sequence = sequences[DeviceType];

                Lot lot = new Lot(creationTime, sequence);

                lot.StartTimeReal = StartTime;
                lot.EndTimeReal = EndTime;
                lot.LotID = LotID;
                lot.PlanDayReal = PlanDay;
                lot.ClipWeekReal = ClipWeek;
                lot.ArrivalReal = arrival;
                lot.WIPInReal = wipIn;
                lot.QtyReal = Qty;

                // Add calculate negative simulation start time for initial lots
                if (!lotYetToStart && StartTime != null && initialTime != null)
                {
                    TimeSpan timeDelta = (DateTime)StartTime - (DateTime)initialTime;
                    lot.StartTime = timeDelta.TotalSeconds;
                }

                if (!lotYetToStart)
                {   // For intial lots, these have a initial position. Stepcount is set to according step based on Real lot's IRD group.

                    for (int i = 0; i < lot.Sequence.stepCount; i++)
                    {
                        if (lot.Sequence.GetCurrentStep(i).Name == IRDGroup)
                        {
                            lot.SetCurrentStepCount(i);
                            break;
                        }
                        if (i == lot.Sequence.stepCount - 1)
                        {
                            //throw new Exception($"{IRDGroup} not found in {DeviceType} for Lot {LotID} i");
                            return null;
                        }
                    }
                }

                return lot;
            }
            else
            {
                Console.WriteLine($"WARNING: Process plans does not contain {DeviceType} for lot {LotID}");
                return null;
                throw new Exception($"Process plans does not contain {DeviceType}");
            }
        }

        /// <summary>
        /// Use this for initial lots only, not for yet-to-start lots, in WaferAreaSim
        /// </summary>
        /// <param name="creationTime"></param>
        /// <param name="sequences"></param>
        /// <returns></returns>
        public Lot ConvertToLotArea(double creationTime, Dictionary<string, Sequence> sequences, DateTime initialDateTime)
        {
            Lot lot = new Lot(creationTime, sequences[IRDGroup]);

            lot.LotID = LotID;
            lot.PlanDayReal = PlanDay;
            lot.ClipWeekReal = ClipWeek;
            lot.ArrivalReal = LotActivity.Arrival;
            lot.DepartureReal = LotActivity.Departure;
            lot.OvertakenLotsReal = LotActivity?.OvertakenLots;
            lot.WIPInReal = wipIn;

            // Initial lots have start time < 0
            lot.StartTime = lot.ArrivalReal != null ? ((DateTime)lot.ArrivalReal - initialDateTime).TotalSeconds : 0;

            lot.SetCurrentStepCount(0);

            return lot;
        }
    }
}
