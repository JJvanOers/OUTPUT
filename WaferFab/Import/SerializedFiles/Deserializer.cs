using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;

namespace WaferFabSim.Import
{
    /// <summary>
    /// Class to import serialized files (.dat) files. Serialized files are created by DataReader class.
    /// </summary>
    public static class Deserializer
    {

        public static WaferFabSettings DeserializeWaferFabSettings(string fileName)
        {
            Console.Write($"Reading {fileName} - ");
            WaferFabSettings waferFabSettings = Tools.ReadFromBinaryFile<WaferFabSettings>(fileName);
            if (waferFabSettings.WCDispatcherTypes == null) waferFabSettings.WCDispatcherTypes = new Dictionary<string, WaferFabElements.Dispatchers.DispatcherBase.Type>();
            Console.Write($"done.\n");
            return waferFabSettings;
        }

        public static List<Tuple<DateTime, RealLot>> DeserializeRealLotStarts(string fileName)
        {
            Console.Write($"Reading {fileName} - ");
            List<Tuple<DateTime, RealLot>> RealLotStarts = Tools.ReadFromBinaryFile<List<Tuple<DateTime, RealLot>>>(Path.Combine(fileName));
            Console.Write($"done.\n");
            return RealLotStarts;
        }

        public static List<WorkCenterLotActivities> DeserializeWorkCenterLotActivities(string fileName)
        {
            Console.Write($"Reading {fileName} - ");
            List<WorkCenterLotActivities> WorkCenterLotActivities = Tools.ReadFromBinaryFile<List<WorkCenterLotActivities>>(Path.Combine(fileName));
            Console.Write($"done.\n");
            return WorkCenterLotActivities;
        }

        public static LotTraces DeserializeLotTraces(string fileName)
        {
            Console.Write($"Reading {fileName} - ");
            LotTraces LotTraces = Tools.ReadFromBinaryFile<LotTraces>(Path.Combine(fileName));
            Console.Write($"done.\n");
            return LotTraces;

        }
    }
}
