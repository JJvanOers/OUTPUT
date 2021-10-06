using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using WaferFabSim;
using WaferFabSim.Import;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;

namespace WaferFabDataReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDir = @"C:\Users\nx008314\OneDrive - Nexperia\Work\WaferFab\Auto";

            string outputDir = @"C:\Users\nx008314\OneDrive - Nexperia\Work\WaferFab\SerializedFiles";

            AutoDataReader reader = new AutoDataReader(inputDir, outputDir);

            //SerializeWorkCenterLotActivities(reader, "LotActivity2019_2020.csv", "WorkCenterLotActivities_SmallTestSet", true);

            //SerializeWaferFabSettings(reader, false);

            //SerializeLotTraces(reader, "LotActivity2019_2020_2021.csv", "LotTraces_2019_2020_2021");

            //WriteLotActivitiesWithEPTsToCSV(reader, "LotTraces_2019_2020_2021.dat", "AllLotActivitiesWithEPTs_201920202021.csv");

            SerializeLotStarts(reader, "LotTraces_2019_2020.dat");

            ////SerializeWaferFabSettings(reader, true);

            //foreach (string wc in reader.WaferFabSettings.WorkCenters)
            //{
            //    SerializeWaferFabSettings(reader, true, wc);
            //}

            //SerializeRealSnaphotsAll(reader, 1);

            SerializeRealSnapshotsPerMonth(reader, 1);

        }

        /// <summary>
        /// Serializes WorkCenterLotActivities. Uses waferfabsettings + raw lot activities (from Excel add-on Nexperia Tools > F/W Queries > LotsProdEqpt).
        /// </summary>
        /// <param name="lotActivitiesFilename"></param>
        public static void SerializeWorkCenterLotActivities(AutoDataReader reader, string filenameLotActivities, string filenameSerializedOutput, bool onlyProductionLots)
        {
            reader.ReadWaferFabSettings(false, true, DispatcherBase.Type.EPTOVERTAKING);

            reader.ReadWorkCenterLotActivities(filenameLotActivities, onlyProductionLots);

            reader.SerializeWorkCenterLotActivities($"{filenameSerializedOutput}");
        }

        /// <summary>
        /// Serialized WaferFabSettings without the Distributions (Random class cannot be serialized).
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="area">Area to serialize, if not specified complete fab will be build.</param>
        /// <param name="withLotStarts">With or without real lot starts</param>
        public static void SerializeWaferFabSettings(AutoDataReader reader, bool withLotStarts, string area = "COMPLETE")
        {
            reader.ReadWaferFabSettings(withLotStarts, false, DispatcherBase.Type.EPTOVERTAKING, area);

            string fileName = withLotStarts ? $"WaferFabSettings_{area}_WithLotStarts" : $"WaferFabSettings_{area}_NoLotStarts";

            reader.SerializeWaferFabSettings(fileName);
        }

        /// <summary>
        /// Serializes LotTraces. Gets the WorkCenterLotActivities from serialized files. Combines the individual activities from one lot into a trace.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="workCenterLotAcitivitiesSerializedFile"></param>
        public static void SerializeLotTraces(AutoDataReader reader, string filenameLotActivities, string filenameOutputFile)
        {
            if (reader.WaferFabSettings == null)
            {
                reader.ReadWaferFabSettings(false, false, DispatcherBase.Type.EPTOVERTAKING);
            }

            reader.ReadWorkCenterLotActivities(filenameLotActivities, true);

            reader.SerializeLotTraces(filenameOutputFile);
        }

        /// <summary>
        /// Serializes LotStarts. Reads the LotTraces from serialized file, gets from all traces the starts and transfers this into LotStarts.
        /// </summary>
        /// <param name="reader"></param>
        public static void SerializeLotStarts(AutoDataReader reader, string lotTracesSerializedFile)
        {
            if (reader.WaferFabSettings == null)
            {
                reader.ReadWaferFabSettings(false, false, DispatcherBase.Type.EPTOVERTAKING);
            }

            reader.LotTraces = Deserializer.DeserializeLotTraces(Path.Combine(reader.DirectorySerializedFiles, "LotTraces_2019_2020.dat"));

            reader.GetLotStarts();

            reader.SerializeLotStarts("LotStarts_2019_2020");
        }

        public static void WriteLotActivitiesWithEPTsToCSV(AutoDataReader reader, string serializedLotTracesFile, string filenameCSVOutput)
        {
            reader.LotTraces = Deserializer.DeserializeLotTraces(Path.Combine(reader.DirectorySerializedFiles, serializedLotTracesFile));

            reader.WriteLotActivitiesToCSV(filenameCSVOutput);
        }
        
        public static void SerializeRealSnaphotsAll(AutoDataReader reader, int waferQtyThreshold)
        {
            if (reader.LotTraces == null)
            {
                reader.LotTraces = Deserializer.DeserializeLotTraces(Path.Combine(reader.DirectorySerializedFiles, "LotTraces_2019_2020.dat"));
            }

            DateTime start = reader.LotTraces.StartDate;
            DateTime end = reader.LotTraces.EndDate;

            DateTime from = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, 0);
            DateTime until = new DateTime(end.Year, end.Month, end.Day, end.Hour, 0, 0, 0);
            TimeSpan frequency = new TimeSpan(1, 0, 0);

            reader.ReadRealSnapshots(from, until, frequency, waferQtyThreshold);

            reader.SerializeRealSnapshots($"{from.Year}-{from.Month}-{from.Day}_{until.Year}-{until.Month}-{until.Day}_{frequency.Hours}h");
        }

        public static void SerializeRealSnapshotsPerMonth(AutoDataReader reader, int waferQtyThreshold)
        {
            if (reader.LotTraces == null)
            {
                reader.LotTraces = Deserializer.DeserializeLotTraces(Path.Combine(reader.DirectorySerializedFiles, "LotTraces_2019_2020.dat"));
            }

            DateTime start = reader.LotTraces.StartDate;
            DateTime end = reader.LotTraces.EndDate;

            DateTime from = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, 0);
            DateTime until = new DateTime(start.AddMonths(1).Year, start.AddMonths(1).Month, 1, 0, 0, 0, 0); // Until first of next month
            TimeSpan frequency = new TimeSpan(1, 0, 0);

            while (until < end.AddMonths(1))
            {
                reader.ReadRealSnapshots(from, until, frequency, waferQtyThreshold);

                reader.SerializeRealSnapshots($"{from.Year}-{from.Month}-{from.Day}_{until.Year}-{until.Month}-{until.Day}_{frequency.Hours}h");

                from = until;
                until = until.AddMonths(1);
            }
        }

        public static void LoadManualData(string inputDir, string outputDir)
        {
            ManualDataReader reader = new ManualDataReader(inputDir + "CSVs");

            reader.ReadWaferFabSettings();

            reader.WaferFabSettings.SampleInterval = 12 * 60 * 60; // 12 hours

        }
    }
}
