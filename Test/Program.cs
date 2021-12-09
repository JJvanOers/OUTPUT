using CSSL.Calendar;
using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WaferFabSim;
using WaferFabSim.Import;
using WaferFabSim.Import.Distributions;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Observers;
using WaferFabSim.WaferFabElements.Utilities;
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {

            LotArrivalAndDepartures();

            //SnapshotsWorksstationQueueLenthsJeroen();

        }

        static void LotArrivalAndDepartures()
        {
            string inputDir = @"C:\Users\nx008314\OneDrive - Nexperia\Work\WaferFab\Auto";

            string outputDir = @"C:\Users\nx008314\OneDrive - Nexperia\Work\WaferFab\SerializedFiles";

            AutoDataReader reader = new AutoDataReader(inputDir, outputDir);

            reader.LotTraces = Deserializer.DeserializeLotTraces(Path.Combine(reader.DirectorySerializedFiles, $"LotTraces_2019_2020_2021.dat"));

            using (StreamWriter writer = new StreamWriter(Path.Combine(outputDir, "LotStartsDeparturesFab_2019_2020_2021.txt")))
            {
                writer.WriteLine("LotID,ProductType,StartTime,EndTime,Status");

                foreach(LotTraces.LotTrace trace in reader.LotTraces.All)
                {
                    writer.WriteLine($"{trace.LotId},{trace.ProductType},{trace.StartDate},{trace.EndDate},{trace.Status}");
                }
            }
        }

        static void SnapshotsWorksstationQueueLenthsJeroen()
        {
            /// //////////////////////////////////////////////
            /// SnapShots Workstation Queue Lengths //////////
            /*
            DateTime date = new DateTime(2019, 6, 1);
            int monthsToEvaluate = 12;

            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output";

            string parameters = @"FittedEPTParameters - 2019-06-01.csv";

            AutoDataReader autoDataReader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");
            WaferFabSettings waferFabSettings = autoDataReader.ReadWaferFabSettings(parameters, true, false);

            List<string> workCenters = new List<string>();
            foreach (string workCenter in waferFabSettings.WorkCenters)
            {
                workCenters.Add(workCenter);
            }

            // Write headers
            StreamWriter writer = new StreamWriter(Path.Combine(outputDir, "Snapshots", $"Snapshots Workcenter - Complete - 06-2019.txt"));
            writer.Write("Time,");
            foreach (string workCenter in workCenters)
            {
                if (workCenter != workCenters.Last())
                {
                    writer.Write($"{workCenter},");
                }
                else
                {
                    writer.Write($"{workCenter}\n");
                }
            }

            RealSnapshotReader realSnapshotReader = new RealSnapshotReader();
            for (int j = 0; j < monthsToEvaluate; j++)
            {
                DateTime n = date.AddMonths(j);
                DateTime m = date.AddMonths(j + 1);

                string filename = inputDir + @$"SerializedFiles\RealSnapShots_{n.Year}-{n.Month}-{n.Day}_{m.Year}-{m.Month}-{m.Day}_1h.dat";
                realSnapshotReader.Read(filename, 1);

                List<RealSnapshot> realSnaphshots = new List<RealSnapshot>();
                if (j == 0)
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots;
                }
                else
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots.Skip(1).ToList();
                }

                // Write Data
                foreach (RealSnapshot realSnapshot in realSnaphshots)
                {
                    writer.Write($"{realSnapshot.Time},");
                    foreach (string workCenter in workCenters)
                    {
                        if (workCenter != workCenters.Last())
                        {
                            int count = realSnapshot.RealLots.Where(x => x.LotActivity.WorkStation == workCenter).Count();
                            writer.Write($"{count},");
                        }
                        else
                        {
                            int count = realSnapshot.RealLots.Where(x => x.LotActivity.WorkStation == workCenter).Count();
                            writer.Write($"{count}\n");
                        }
                    }
                }
            }

            writer.Close();
            */


            /// //////////////////////////////////////////////
            /// SnapShots IRD Queue Lengths //////////////////

            DateTime date = new DateTime(2019, 6, 1);
            int monthsToEvaluate = 12;

            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output";

            string parameters = @"FittedEPTParameters - 2019-06-01.csv";


            AutoDataReader autoDataReader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");
            WaferFabSettings waferFabSettings = autoDataReader.ReadWaferFabSettings(parameters, true, false);

            List<string> orderedLotSteps = new List<string>();
            foreach (var step in waferFabSettings.LotSteps.OrderBy(x => x.Value.Id))
            {
                orderedLotSteps.Add(step.Key);
            }

            //Write headers
            StreamWriter writer = new StreamWriter(Path.Combine(outputDir, "Snapshots", $"Snapshots IRD Wafers - {date.Year}-{date.Month}-{date.Day}.txt"));
            writer.Write("Time,");
            foreach (string lotStep in orderedLotSteps)
            {
                if (lotStep != orderedLotSteps.Last())
                {
                    writer.Write($"{lotStep},");
                }
                else
                {
                    writer.Write($"{lotStep}\n");
                }
            }

            RealSnapshotReader realSnapshotReader = new RealSnapshotReader();
            for (int j = 0; j < monthsToEvaluate; j++)
            {
                DateTime n = date.AddMonths(j);
                DateTime m = date.AddMonths(j + 1);

                string filename = inputDir + @$"SerializedFiles\RealSnapShots_{n.Year}-{n.Month}-{n.Day}_{m.Year}-{m.Month}-{m.Day}_1h.dat";
                realSnapshotReader.Read(filename, 1);

                List<RealSnapshot> realSnaphshots = new List<RealSnapshot>();
                if (j == 0)
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots;
                }
                else
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots.Skip(1).ToList();
                }

                //Write Data
                foreach (RealSnapshot realSnapshot in realSnaphshots)
                {
                    writer.Write($"{realSnapshot.Time},");
                    foreach (string lotStep in orderedLotSteps)
                    {
                        if (lotStep != orderedLotSteps.Last())
                        {
                            if (realSnapshot.LotSteps.Contains(lotStep))
                            {
                                List<RealLot> realLots = realSnapshot.RealLots.Where(x => x.IRDGroup == lotStep).ToList();
                                int nrOfWafers = 0;
                                for (int k = 0; k < realLots.Count(); k++)
                                {
                                    nrOfWafers += realLots[k].Qty;
                                }
                                writer.Write($"{nrOfWafers},");
                            }
                            else
                            {
                                writer.Write("0,");
                            }
                        }
                        else
                        {
                            if (realSnapshot.LotSteps.Contains(lotStep))
                            {
                                List<RealLot> realLots = realSnapshot.RealLots.Where(x => x.IRDGroup == lotStep).ToList();
                                int nrOfWafers = 0;
                                for (int k = 0; k < realLots.Count(); k++)
                                {
                                    nrOfWafers += realLots[k].Qty;
                                }
                                writer.Write($"{nrOfWafers}\n");
                            }
                            else
                            {
                                writer.Write("0\n");
                            }
                        }
                    }
                }
            }

            writer.Close();
            


            /// //////////////////////////////////////////////
            /// SnapShots IRD Queue Lengths //////////////////
            /*
            DateTime i = new DateTime(2019, 10, 1);

            string inputDir = @"E:\OneDrive - Nexperia\CSSLWaferFab\Input\";

            string outputDir = $@"C:\CSSLWaferFab\Output";

            
            AutoDataReader autoDataReader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");
            WaferFabSettings waferFabSettings = autoDataReader.ReadWaferFabSettings(true, false);
            List<string> orderedLotSteps = new List<string>();
            foreach (var step in waferFabSettings.LotSteps.OrderBy(x => x.Value.Id))
            {
                orderedLotSteps.Add(step.Key);
            }

            // Write headers
            StreamWriter writer = new StreamWriter(Path.Combine(outputDir, "Snapshots", $"Snapshots IRD Lots - {i.Year}-{i.Month}-{i.Day}.txt"));
            writer.Write("Time,");
            foreach (string lotStep in orderedLotSteps)
            {
                if (lotStep != orderedLotSteps.Last())
                {
                    writer.Write($"{lotStep},");
                }
                else
                {
                    writer.Write($"{lotStep}\n");
                }
            }

            RealSnapshotReader realSnapshotReader = new RealSnapshotReader();
            for (int j = 0; j < 2; j++)
            {                
                string filename = inputDir + @$"SerializedFiles\RealSnapShots_{i.Year}-{i.Month + j}-{i.Day}_{i.Year}-{i.Month + 1 + j}-{i.Day}_1h.dat";
                realSnapshotReader.Read(filename, 1);

                List<RealSnapshot> realSnaphshots = new List<RealSnapshot>();
                if (j == 0)
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots;
                }
                else
                {
                    realSnaphshots = realSnapshotReader.RealSnapshots.Skip(1).ToList();
                }

                // Write Data
                foreach (RealSnapshot realSnapshot in realSnaphshots)
                {
                    writer.Write($"{realSnapshot.Time},");
                    foreach (string lotStep in orderedLotSteps)
                    {
                        if (lotStep != orderedLotSteps.Last())
                        {
                            if (realSnapshot.LotSteps.Contains(lotStep))
                            {
                                int count = realSnapshot.RealLots.Where(x => x.IRDGroup == lotStep).Count();
                                writer.Write($"{count},");
                            }
                            else
                            {
                                writer.Write("0,");
                            }
                        }
                        else
                        {
                            if (realSnapshot.LotSteps.Contains(lotStep))
                            {
                                int count = realSnapshot.RealLots.Where(x => x.IRDGroup == lotStep).Count();
                                writer.Write($"{count}\n");
                            }
                            else
                            {
                                writer.Write("0\n");
                            }
                        }
                    }
                }
            }

            writer.Close();
            


            /// ///////////////////////////////////////////////////
            /// Gamma analysis ///////////////////////////////            
            /*
            bool write = true;            
            
            GammaDistribution gammaDistribution = new GammaDistribution(90, 72900);
            double alpha = 0.11;
            double beta = 0.001235;

            Console.WriteLine("{0}, {1}", gammaDistribution.Alpha, gammaDistribution.Beta);
            List<double> values = new List<double>();
            double sumx = 0;
            double sumxx = 0;
            double sumw = 0;

            for (int i = 0; i < 1000000; i++)
            {
                double value = gammaDistribution.Next();
                values.Add(value);
                sumx += value;
                sumxx += value * value;
                sumw += 1;
            }

            double mean = sumx / sumw;
            double variance = sumxx / sumw - sumx / sumw * sumx / sumw;
            double stdev = Math.Sqrt(variance);

            Console.WriteLine("{0}, {1}, {2}", mean, variance, stdev);
            Console.WriteLine("{0}, {1}", gammaDistribution.Alpha, gammaDistribution.Beta);
            if (write)
            {
                using (StreamWriter writer = new StreamWriter(@$"E:\OneDrive - Nexperia\OUTPUT\Python\Gamma Test\Alpha = {alpha}, Beta = {beta}.txt"))
                {
                    writer.WriteLine("Gamma rvs");

                    foreach (double i in values)
                    {
                        writer.WriteLine(i);
                    }
                }
            }
            */

            /// ///////////////////////////////////////////////////
            /// Read Queue lengths ///////////////////////////////
            /*
            DateTime initialDateTime = new DateTime(2019, 4, 1);

            DateTime finalDateTime = new DateTime(2022, 4, 1);

            string DirectorySerializedFiles = @"E:\OneDrive - Nexperia\CSSLWaferFab\Input\SerializedFiles";

            string outputDirectory = $@"C:\CSSLWaferFab\LotActivities\{initialDateTime.ToString("yyyy-MM-dd")}";

            List<WorkCenterLotActivities> workCenterLotActivities = Deserializer.DeserializeWorkCenterLotActivities(Path.Combine(DirectorySerializedFiles, "WorkCenterLotActivities_2019_2020.dat"));

            //string wc = "PHOTOLITH";

            List<string> workCenters = new List<string>()
            {"BACKGRIND", "BATCH UP", "CMP", "DICE", "DRY ETCH", "ELEC TEST", "EVAPORATION", "FURNACING", "IMPLANT",
                "INSPECTION", "LPCVD", "MERCURY", "NITRIDE DEP", "OFF LINE INK", "PACK", "PHOTOLITH", "PROBE", "REPORTING",
                "SAMPLE TEST", "SPUTTERING", "WET ETCH"};


            foreach (string wc in workCenters)
            {
                List<Tuple<DateTime, int>> queueLengths = workCenterLotActivities.Where(x => x.WorkCenter == wc).First()
                    .WIPTrace.Where(x => x.Item1 >= initialDateTime && x.Item1 <= finalDateTime).OrderBy(x => x.Item1).ToList();

                // Write all results to a text file
                using (StreamWriter writer = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_QueueLength.txt")))
                {
                    writer.WriteLine("Time,QueueLength");

                    foreach (Tuple<DateTime, int> queueLength in queueLengths)
                    {
                        writer.WriteLine(queueLength.Item1 + "," + queueLength.Item2);
                    }
                }
            }            
            */
        }
    }
}
