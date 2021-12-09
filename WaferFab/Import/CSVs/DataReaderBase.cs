using System;
using System.Collections.Generic;
using System.IO;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements.Dispatchers;

namespace WaferFabSim.Import
{
    /// <summary>
    /// This is the base class for reading data from CSV files. This data can either directly be used in the ShellModel or be saved it into serialized .dat files.
    /// Serialized .dat files can be read by the DataImporter class.
    /// </summary>
    public abstract class DataReaderBase
    {

        public DataReaderBase(string directory)
        {
            DirectoryInputCSVs = directory;
            DirectorySerializedFiles = directory;
        }


        public DataReaderBase(string directoryCSVs, string directorySerialized)
        {
            DirectoryInputCSVs = directoryCSVs;
            DirectorySerializedFiles = directorySerialized;
        }

        public string DirectoryInputCSVs { get; set; }
        public string DirectorySerializedFiles { get; set; }

        public ExperimentSettings ExperimentSettings { get; set; }

        public WaferFabSettings WaferFabSettings { get; set; }

        public List<RealSnapshot> RealSnapshots { get; set; }

        public List<Tuple<DateTime, RealLot>> RealLotStarts { get; set; }

        public List<WorkCenterLotActivities> WorkCenterLotActivities { get; set; }

        public LotTraces LotTraces { get; set; }

        public virtual ExperimentSettings ReadExperimentSettings(string fileName)
        {
            throw new NotImplementedException();
        }


        public virtual WaferFabSettings ReadWaferFabSettings(string eptParameterFile, bool includeLotstarts, bool includeDistributions, DispatcherBase.Type dispatcherType, string area = "COMPLETE")
        {
            throw new NotImplementedException();
        }

        public virtual List<WorkCenterLotActivities> ReadWorkCenterLotActivities(string fileName, bool productionLots)
        {
            throw new NotImplementedException();
        }

        public virtual WaferFabSettings DeserializeWaferFabSettings(string eptParameterFile, string serializedFileName)
        {
            throw new NotImplementedException();
        }


        public virtual List<RealSnapshot> ReadRealSnapshots(DateTime from, DateTime until, TimeSpan interval, int waferQtyThreshold)
        {
            throw new NotImplementedException();
        }

        public void SerializeExperimentSettings(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<ExperimentSettings>($@"{DirectorySerializedFiles}\ExperimentSettings_{filename}.dat", ExperimentSettings);
            Console.Write($"done.\n");
        }

        public void SerializeWaferFabSettings(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<WaferFabSettings>($@"{DirectorySerializedFiles}\{filename}.dat", WaferFabSettings);
            Console.Write($"done.\n");

        }

        public void SerializeRealSnapshots(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<List<RealSnapshot>>($@"{DirectorySerializedFiles}\RealSnapshots_{filename}.dat", RealSnapshots);
            Console.Write($"done.\n");
        }

        public void SerializeWorkCenterLotActivities(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<List<WorkCenterLotActivities>>($@"{DirectorySerializedFiles}\{filename}.dat", WorkCenterLotActivities);
            Console.Write($"done.\n");
        }

        public void SerializeLotTraces(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<LotTraces>($@"{DirectorySerializedFiles}\{filename}.dat", LotTraces);
            Console.Write($"done.\n");
        }

        public void SerializeLotStarts(string filename)
        {
            Console.Write($"Saving {filename} - ");
            Tools.WriteToBinaryFile<List<Tuple<DateTime, RealLot>>>($@"{DirectorySerializedFiles}\{filename}.dat", RealLotStarts);
            Console.Write($"done.\n");
        }
    }
}
