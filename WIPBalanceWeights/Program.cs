using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WaferFabSim;
using WaferFabSim.Import;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;

namespace WIPBalanceWeights
{
    class Program
    {
        static void Main(string[] args)
        {
            List<DateTime> DateTimes = ReadDateTimes(@"C:\CSSLWaferFab\Input\WIPBalanceWeights\DateTimes.csv");

            int jStepBack = 0;
            int kStepAhead = 1;

            string inputDir = @"C:\CSSLWaferFab\Input\";
            string eptParameterFile = @"FittedEPTParameters - 2019-06-01.csv";

            // Get WaferFabSettings
            AutoDataReader reader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");
            WaferFabSettings waferFabSettings = reader.ReadWaferFabSettings(eptParameterFile, false, false, DispatcherBase.Type.MIVS);
            waferFabSettings.WIPTargets = reader.ReadWIPTargets(waferFabSettings.LotSteps, "WIPTargets.csv");


            // Get all lots from snapshot
            LotTraces lotTraces = Deserializer.DeserializeLotTraces(inputDir + @"SerializedFiles\LotTraces_2019_2020_2021.dat");

            foreach (DateTime time in DateTimes)
            {
                RealSnapshot realSnapshot = lotTraces.GetWIPSnapshot(time, 0);
                List<Lot> lots = realSnapshot.GetRealLots(0).Select(x => x.ConvertToLot(0, waferFabSettings.Sequences, false, time)).Where(x => x != null).ToList();

                // Construct current WIP levels and WIP targets
                Dictionary<string, int> WIPlevels = realSnapshot.WIPlevelsInWafers;
                Dictionary<LotStep, double> WIPtargets = waferFabSettings.WIPTargets;

                // Construct weights per lot
                Dictionary<Lot, double> weights = new Dictionary<Lot, double>();

                foreach (Lot lot in lots)
                {
                    double weight = 0.0;

                    for (int relStep = -jStepBack; relStep <= 0; relStep++)
                    {
                        if (lot.HasRelativeStep(relStep))
                        {
                            LotStep step = lot.GetRelativeStep(relStep);

                            if (WIPlevels.ContainsKey(step.Name))
                            {
                                weight += WIPlevels[step.Name] - WIPtargets[step];
                            }
                            else // WIP level of step is not know in snapshot, so it has to be 0
                            {
                                weight -= WIPtargets[step];
                            }
                        }
                    }

                    for (int relStep = 1; relStep <= kStepAhead; relStep++)
                    {
                        if (lot.HasRelativeStep(relStep))
                        {
                            LotStep step = lot.GetRelativeStep(relStep);

                            if (WIPlevels.ContainsKey(step.Name))
                            {
                                weight += WIPtargets[step] - WIPlevels[step.Name];
                            }
                            else // WIP level of step is not know in snapshot, so it has to be 0
                            {
                                weight += WIPtargets[step];
                            }
                        }
                    }

                    weights.Add(lot, weight);
                }

                WriteWeightsToCSV(weights, time);
            }
        }

        public static void WriteWeightsToCSV(Dictionary<Lot, double> weights, DateTime time)
        {
            using (StreamWriter writer = new StreamWriter(@$"C:\CSSLWaferFab\Output\WIPBalanceWeights\Weights.csv", true))
            {
                writer.WriteLine("Time,LotID,Qty,IRD,Weight");

                foreach (var item in weights)
                {
                    Lot lot = item.Key;
                    double weight = item.Value;

                    writer.WriteLine($"{time},{lot.LotID},{lot.QtyReal},{lot.GetCurrentStep.Name},{weight}");
                }
            }
        }

        public static List<DateTime> ReadDateTimes(string fileName)
        {
            List<DateTime> dateTimes = new List<DateTime>();

            using (StreamReader reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    dateTimes.Add(Convert.ToDateTime(line));
                }
            }

            return dateTimes;
        }
    }
}
