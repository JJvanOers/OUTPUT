using CSSL.Utilities.Distributions;
using LithographyAreaValidation.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LithographyAreaValidation.DataReader
{
    public class CSVReader
    {

        public CSVReader(DateTime startDate)
        {
            //InputDirectory = @"C:\Users\NX015127\OneDrive - Nexperia\1_Thesis\5_SimulationModel\2_LithographyAreaValidation\3_InputSimulation\";
            InputDirectory = $"{Directory.GetCurrentDirectory()}/Input";

            FileNameProcessingTimes = "DataProcessingTimes.csv"; // "ExampleProcessingTimes.csv";

            //FileNameNonProductiveTimes = "DataNonProductiveTimes_Deterministic.csv"; //TODO: Change

            //FileNameNonProductiveTimes = "DataNonProductiveTimes_01Jan2021_30days.csv"; //"DataNonProductiveTimes.csv"; 

            if (startDate == new DateTime(2021, 1, 1, 7, 0, 0))
            {
                FileNameNonProductiveTimes = "DataNonProductiveTimes_01Jan2021_30days.csv"; //"DataNonProductiveTimes.csv"; 
            }
            else if (startDate == new DateTime(2021, 1, 15, 7, 0, 0))
            {
                FileNameNonProductiveTimes = "DataNonProductiveTimes_15Jan2021_30days.csv"; //"DataNonProductiveTimes.csv"; 
            }
            else if (startDate == new DateTime(2021, 2, 1, 7, 0, 0))
            {
                FileNameNonProductiveTimes = "DataNonProductiveTimes_01Feb2021_30days.csv"; //"DataNonProductiveTimes.csv"; 
            }
            else if (startDate == new DateTime(2021, 3, 1, 7, 0, 0))
            {
                FileNameNonProductiveTimes = "DataNonProductiveTimes_01Mar2021_30days.csv"; //"DataNonProductiveTimes.csv"; 
            }
            else if (startDate == new DateTime(2021, 4, 1, 7, 0, 0))
            {
                FileNameNonProductiveTimes = "DataNonProductiveTimes_01Apr2021_30days.csv"; //"DataNonProductiveTimes.csv"; 
            }

            FileNameArrivalsAndStartRuns = "DataLotArrivals.csv"; //"ExampleArrivalsAndStartRuns.csv";

            //FileNameArrivalsAndStartRuns = "DataLotArrivalsInstance02.csv";

            FileNameMachineEligibilities = "DataMachineEligibilities.csv"; // "ExampleMachineEligibilities.csv";
            FileNameMachineStartStates = "DataStartStates.csv";
            FileNameUltratechTitans = "DataUltratechTitans.csv";
            FileNameWeightsWIPBalance = "Weights_WIPBalance.csv";
        }

        private string InputDirectory { get; }

        private string FileNameProcessingTimes { get; }

        private string FileNameNonProductiveTimes { get; }

        private string FileNameArrivalsAndStartRuns { get; }

        private string FileNameMachineEligibilities { get; }

        private string FileNameMachineStartStates { get; }

        private string FileNameUltratechTitans { get; }
        private string FileNameWeightsWIPBalance { get; }

        public Dictionary<string, double> ReadWeightsWIPBalance(DateTime startDate)
        {
            Dictionary<string, double> weightsWIPBalance = new Dictionary<string, double>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameWeightsWIPBalance)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');
                    try
                    {
                        DateTime timeSnapshot = DateTime.Parse(dataLine[0]);
                        string lotID = dataLine[1];

                        double weight = Convert.ToDouble(dataLine[4]);

                        if (timeSnapshot == startDate)
                        {
                            weightsWIPBalance.Add(lotID, weight);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return weightsWIPBalance;
        }

        public Dictionary<string, Distribution> ReadDeterministicProcessingTimes(string machineName)
        {
            Dictionary<string, Distribution> processingTimeDictionary = new Dictionary<string, Distribution>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameProcessingTimes)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    string reticleID = dataLine[1];
                    double deterministicTime = Convert.ToDouble(dataLine[2]);

                    if (equipmentID == machineName)
                    {
                        if (!processingTimeDictionary.ContainsKey(reticleID))
                        {
                            processingTimeDictionary.Add(reticleID, new ConstantDistribution(deterministicTime));
                        }
                    }
                }
            }
            return processingTimeDictionary;
        }

        public Dictionary<string, Distribution> ReadStochasticProcessingTimes(string machineName)
        {
            Dictionary<string, Distribution> processingTimeDictionary = new Dictionary<string, Distribution>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameProcessingTimes)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                List<double> sampleData = new List<double>();

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    string reticleID = dataLine[1];
                    double sampleTime = Convert.ToDouble(dataLine[3]);

                    if (equipmentID == machineName)
                    {
                        if (!processingTimeDictionary.ContainsKey(reticleID))
                        {
                            sampleData.Clear();
                            sampleData.Add(sampleTime);
                            double[] sampleDataArray = sampleData.ToArray();
                            EmpericalDistribution processingTimeDistribution = new EmpericalDistribution(sampleDataArray);
                            processingTimeDictionary.Add(reticleID, processingTimeDistribution);
                        }
                        else
                        {
                            sampleData.Add(sampleTime);
                            double[] sampleDataArray = sampleData.ToArray();
                            EmpericalDistribution processingTimeDistribution = new EmpericalDistribution(sampleDataArray);
                            processingTimeDictionary[reticleID] = processingTimeDistribution;
                        }
                    }
                }
            }
            return processingTimeDictionary;
        }

        public Dictionary<string, Distribution> ReadProcessingTimes(string machineName, string processingTimeDistributionType)
        {
            Dictionary<string, Distribution> processingTimeDictionary = new Dictionary<string, Distribution>();

            if (processingTimeDistributionType == "Deterministic")
            {
                using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameProcessingTimes)))
                {
                    string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                    while (!reader.EndOfStream)
                    {
                        string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                        string equipmentID = dataLine[0];
                        string reticleID = dataLine[1];
                        double deterministicTime = Convert.ToDouble(dataLine[2]);

                        if (equipmentID == machineName)
                        {
                            if (!processingTimeDictionary.ContainsKey(reticleID))
                            {
                                processingTimeDictionary.Add(reticleID, new ConstantDistribution(deterministicTime));
                            }
                        }
                    }
                }
                return processingTimeDictionary;
            }
            else if (processingTimeDistributionType == "Stochastic")
            {
                using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameProcessingTimes)))
                {
                    string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                    List<double> sampleData = new List<double>();

                    while (!reader.EndOfStream)
                    {
                        string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                        string equipmentID = dataLine[0];
                        string reticleID = dataLine[1];
                        double sampleTime = Convert.ToDouble(dataLine[3]);

                        if (equipmentID == machineName)
                        {
                            if (!processingTimeDictionary.ContainsKey(reticleID))
                            {
                                sampleData.Clear();
                                sampleData.Add(sampleTime);
                                double[] sampleDataArray = sampleData.ToArray();
                                EmpericalDistribution processingTimeDistribution = new EmpericalDistribution(sampleDataArray);
                                processingTimeDictionary.Add(reticleID, processingTimeDistribution);
                            }
                            else
                            {
                                sampleData.Add(sampleTime);
                                double[] sampleDataArray = sampleData.ToArray();
                                EmpericalDistribution processingTimeDistribution = new EmpericalDistribution(sampleDataArray);
                                processingTimeDictionary[reticleID] = processingTimeDistribution;
                            }
                        }
                    }
                }
                return processingTimeDictionary;
            }
            else
            {
                return processingTimeDictionary;
            }
        }

        public Dictionary<string, Distribution> ReadStochasticNonProductiveTimes(string machineName)
        {
            Dictionary<string, Distribution> nonProductiveTimeDictionary = new Dictionary<string, Distribution>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameNonProductiveTimes)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                List<double> sampleData = new List<double>();

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    string type = dataLine[1];
                    double sampleTime = Convert.ToDouble(dataLine[4]);

                    if (equipmentID == machineName)
                    {
                        if (!nonProductiveTimeDictionary.ContainsKey(type))
                        {
                            sampleData.Clear();
                            sampleData.Add(sampleTime);
                            double[] sampleDataArray = sampleData.ToArray();
                            EmpericalDistributionVariableSeed processingTimeDistribution = new EmpericalDistributionVariableSeed(sampleDataArray);
                            nonProductiveTimeDictionary.Add(type, processingTimeDistribution);
                        }
                        else
                        {
                            sampleData.Add(sampleTime);
                            double[] sampleDataArray = sampleData.ToArray();
                            EmpericalDistributionVariableSeed processingTimeDistribution = new EmpericalDistributionVariableSeed(sampleDataArray);
                            nonProductiveTimeDictionary[type] = processingTimeDistribution;
                        }
                    }
                }
            }
            return nonProductiveTimeDictionary;
        }

        public Dictionary<string, Distribution> ReadNonProductiveTimes(string machineName, string nonProductiveTimeDistributionType)
        {
            Dictionary<string, Distribution> nonProductiveTimeDictionary = new Dictionary<string, Distribution>();

            if (nonProductiveTimeDistributionType == "Deterministic")
            {
                using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameNonProductiveTimes)))
                {
                    string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                    while (!reader.EndOfStream)
                    {
                        string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                        string equipmentID = dataLine[0];
                        string type = dataLine[1];
                        double deterministicTime = Convert.ToDouble(dataLine[3]);

                        if (equipmentID == machineName)
                        {
                            if (!nonProductiveTimeDictionary.ContainsKey(type))
                            {
                                nonProductiveTimeDictionary.Add(type, new ConstantDistribution(deterministicTime));
                            }
                        }
                    }
                }
                return nonProductiveTimeDictionary;
            }
            else if (nonProductiveTimeDistributionType == "Stochastic")
            {
                using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameNonProductiveTimes)))
                {
                    string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                    List<double> sampleData = new List<double>();

                    while (!reader.EndOfStream)
                    {
                        string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                        string equipmentID = dataLine[0];
                        string type = dataLine[1];
                        double sampleTime = Convert.ToDouble(dataLine[4]);

                        if (equipmentID == machineName)
                        {
                            if (!nonProductiveTimeDictionary.ContainsKey(type))
                            {
                                sampleData.Clear();
                                sampleData.Add(sampleTime);
                                double[] sampleDataArray = sampleData.ToArray();
                                EmpericalDistributionVariableSeed processingTimeDistribution = new EmpericalDistributionVariableSeed(sampleDataArray);
                                nonProductiveTimeDictionary.Add(type, processingTimeDistribution);
                            }
                            else
                            {
                                sampleData.Add(sampleTime);
                                double[] sampleDataArray = sampleData.ToArray();
                                EmpericalDistributionVariableSeed processingTimeDistribution = new EmpericalDistributionVariableSeed(sampleDataArray);
                                nonProductiveTimeDictionary[type] = processingTimeDistribution;
                            }
                        }
                    }
                }
                return nonProductiveTimeDictionary;
            }
            else
            {
                return nonProductiveTimeDictionary;
            }
        }

        public Dictionary<string, double> ReadNonProductiveTimeProbabilities(string machineName)
        {
            Dictionary<string, double> nonProductiveTimeDictionary = new Dictionary<string, double>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameNonProductiveTimes)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    string type = dataLine[1];
                    double probability = Convert.ToDouble(dataLine[2]);

                    if (equipmentID == machineName)
                    {
                        if (!nonProductiveTimeDictionary.ContainsKey(type))
                        {
                            nonProductiveTimeDictionary.Add(type, probability);
                        }
                    }
                }
            }
            return nonProductiveTimeDictionary;
        }

        public List<Array> ReadLotArrivalsAndStartRuns(DateTime startDate, double lengthOfReplication)
        {
            DateTime endDate = startDate.AddSeconds(lengthOfReplication); // TODO: Check this value

            List<Array> lotArrivalsAndDepartures = new List<Array>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameArrivalsAndStartRuns)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string lotID = dataLine[0];
                    string irdName = dataLine[1];
                    DateTime arrivalTimeDate = DateTime.Parse(dataLine[2]);
                    double arrivalTimeSeconds = arrivalTimeDate.Subtract(startDate).TotalSeconds;
                    DateTime dueDate = DateTime.Parse(dataLine[3]);
                    string speed = dataLine[4];
                    int lotQty = Convert.ToInt32(dataLine[5]);
                    string recipeStepCluster = dataLine[6];
                    string recipeStandAlone = dataLine[7];
                    string masksetLayer = dataLine[8];
                    string reticleID1 = dataLine[9];
                    string reticleID2 = dataLine[10];
                    DateTime startRunTime = DateTime.Parse(dataLine[11]);

                    DateTime improvedDueDate = DateTime.Parse(dataLine[14]);

                    if (startRunTime >= startDate & arrivalTimeDate <= endDate)
                    {
                        Object[] ArrayOfObjects = {lotID, irdName, arrivalTimeSeconds, arrivalTimeDate, dueDate, speed, lotQty, recipeStepCluster, recipeStandAlone, masksetLayer, reticleID1, reticleID2, improvedDueDate};
                        lotArrivalsAndDepartures.Add(ArrayOfObjects);
                    }
                }
            }
            return lotArrivalsAndDepartures;
        }

        public Dictionary<string,string> ReadLotMachineEligibilities(DateTime startDate)
        {
            Dictionary<string, string> machineEligibilityDictionary = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameMachineEligibilities)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    string recipe = dataLine[1];
                    string enabled = dataLine[2];
                    DateTime daySnapshot = DateTime.Parse(dataLine[3]);
                    DateTime dayNextSnapshot = DateTime.Parse(dataLine[4]);
                    string key = equipmentID + "__" + recipe;

                    if (startDate>= daySnapshot & startDate < dayNextSnapshot)
                    {
                        if (!machineEligibilityDictionary.ContainsKey(key))
                        {
                            machineEligibilityDictionary.Add(key, enabled);
                        }
                        else
                        {
                            //TODO: Write error
                        }
                    }
                }
            }
            return machineEligibilityDictionary;
        }

        public List<Object> ReadRealStartState(string machineName, DateTime startDate)
        {
            List<Object> startStates = new List<Object>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameMachineStartStates)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    DateTime daySnapshot = DateTime.Parse(dataLine[1]);
                    string state = dataLine[2];
                    DateTime nextStart = DateTime.Parse(dataLine[3]);

                    if (equipmentID == machineName)
                    {
                        if (startDate == daySnapshot)
                        {
                            startStates.Add(state);
                            startStates.Add(nextStart);
                        }
                    }
                }
            }
            return startStates;
        }

        public List<Object> ReadRandomStartState(string machineName, DateTime startDate, int seed)
        {
            List<Object> startStates = new List<Object>();

            List<Array> allStartStates = new List<Array>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameMachineStartStates)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    DateTime daySnapshot = DateTime.Parse(dataLine[1]);
                    string state = dataLine[2];
                    DateTime nextStart = DateTime.Parse(dataLine[3]);

                    if (equipmentID == machineName)
                    {
                        Object[] ArrayOfObjects = { daySnapshot, state, nextStart };
                        allStartStates.Add(ArrayOfObjects);
                    }
                }
            }

            int possibleStartStates = allStartStates.Count;
            int randomStartState = new Random(seed).Next(0, possibleStartStates);
            Array thisStartState = allStartStates[randomStartState];

            DateTime thisDaySnapshot = (DateTime)thisStartState.GetValue(0);
            string thisState = (string)thisStartState.GetValue(1);
            DateTime thisNextStart = (DateTime)thisStartState.GetValue(2);

            TimeSpan delta = startDate.Subtract(thisDaySnapshot);
            DateTime nextStartWithDelta = thisNextStart.Add(delta);
            
            startStates.Add(thisState);
            startStates.Add(nextStartWithDelta);

            return startStates;
        }

        public List<Object> ReadRandomStartState(string machineName, DateTime startDate)
        {
            List<Object> startStates = new List<Object>();

            List<Array> allStartStates = new List<Array>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameMachineStartStates)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string equipmentID = dataLine[0];
                    DateTime daySnapshot = DateTime.Parse(dataLine[1]);
                    string state = dataLine[2];
                    DateTime nextStart = DateTime.Parse(dataLine[3]);

                    if (equipmentID == machineName)
                    {
                        Object[] ArrayOfObjects = { daySnapshot, state, nextStart };
                        allStartStates.Add(ArrayOfObjects);
                    }
                }
            }

            int possibleStartStates = allStartStates.Count;
            int randomStartState = new Random().Next(0, possibleStartStates);
            Array thisStartState = allStartStates[randomStartState];

            DateTime thisDaySnapshot = (DateTime)thisStartState.GetValue(0);
            string thisState = (string)thisStartState.GetValue(1);
            DateTime thisNextStart = (DateTime)thisStartState.GetValue(2);

            TimeSpan delta = startDate.Subtract(thisDaySnapshot);
            DateTime nextStartWithDelta = thisNextStart.Add(delta);

            startStates.Add(thisState);
            startStates.Add(nextStartWithDelta);

            return startStates;
        }



        public List<Array> ReadUltratechTitans(DateTime startDate, double lengthOfReplication)
        {
            List<Array> ultratechTitans = new List<Array>();

            using (StreamReader reader = new StreamReader(Path.Combine(InputDirectory, FileNameUltratechTitans)))
            {
                string[] headerLine = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Trim(',').Split(',');

                    string lotID = dataLine[0];
                    string irdName = dataLine[1];
                    int lotQty = Convert.ToInt32(dataLine[2]);
                    DateTime trackOut = DateTime.Parse(dataLine[4]);
                    double trackOutSimulationTime = trackOut.Subtract(startDate).TotalSeconds;
                    DateTime endDate = startDate.AddSeconds(lengthOfReplication);

                    if (trackOut >= startDate && trackOut<=endDate)
                    {
                        Object[] ArrayOfObjects = { lotID, irdName, lotQty, trackOutSimulationTime };
                        ultratechTitans.Add(ArrayOfObjects);
                    }
                }
            }
            return ultratechTitans;
        }
    }
}
