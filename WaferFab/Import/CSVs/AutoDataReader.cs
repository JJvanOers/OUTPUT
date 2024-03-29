﻿using CSSL.Examples.AccessController;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using WaferFabSim.Import;
using WaferFabSim.Import.Distributions;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Utilities;
using static WaferFabSim.Import.LotTraces;
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;
using static WaferFabSim.WaferFabElements.Utilities.OvertakingDistributionBase;

namespace WaferFabSim.InputDataConversion
{
    public class AutoDataReader : DataReaderBase
    {

        public AutoDataReader(string csvsDirectory, string serializedDirectory) : base(csvsDirectory, serializedDirectory)
        {
            lotStepsRaw = new List<SingleStep>();
            irdMappings = new List<IRDMapping>();
            irdNumbering = new Dictionary<string, int>();
            lotActivitiesRaw = new List<LotActivityRaw>();
        }


        /// <summary>
        /// Reading waferfabsettings
        /// </summary>
        /// <param name="includeLotstarts">True to include lot starts which will be read from serialized file</param>
        /// <param name="includeDistributions">True to include workcenter service time and overtaking distributions. Do not include this if waferfabsettings have to be serialized,
        /// because Random class in distributions cannot be serialized.</param>
        /// <param name="area"></param>
        /// <returns></returns>
        public override WaferFabSettings ReadWaferFabSettings(string eptParameterFile, bool includeLotstarts, bool includeDistributions, DispatcherBase.Type dispatcherType = DispatcherBase.Type.EPTOVERTAKING,
            string area = "COMPLETE")
        {
            Console.Write("Reading waferfabsettings -");

            ReadLotStepsRawAndIRDs();

            lotSteps = fillStepsWithIRDs();

            WaferFabSettings = new WaferFabSettings();

            WaferFabSettings.LotStartsFrequency = 12;

            WaferFabSettings.LotTypes = getProductTypes();

            WaferFabSettings.ManualLotStartQtys = WaferFabSettings.LotTypes.ToDictionary(x => x, x => 0);

            WaferFabSettings.LotSteps = lotSteps;

            WaferFabSettings.WorkCenters = irdMappings.Select(x => x.WorkStation).Distinct().ToList();

            WaferFabSettings.LotStepsPerWorkStation = getLotStepsPerWorkstation();

            WaferFabSettings.WCDispatcherTypes = getDispatchers(dispatcherType);

            WaferFabSettings.Sequences = getSequencesPerIRDGroup();

            processPlans = getProcessPlans();

            if (includeLotstarts)
            {
                if (area == "COMPLETE")
                {
                    WaferFabSettings.RealLotStarts = Deserializer.DeserializeRealLotStarts(Path.Combine(DirectorySerializedFiles, "LotStarts_2019_2020_2021.dat"));
                }
                else
                {
                    WaferFabSettings.LotStarts = GetLotStartsOneWorkCenter(area);
                }
            }

            if (includeDistributions)
            {

                EPTDistributionReader reader = new EPTDistributionReader(DirectoryInputCSVs, WaferFabSettings.WorkCenters, WaferFabSettings.LotStepsPerWorkStation);

                WaferFabSettings.WCServiceTimeDistributions = reader.GetServiceTimeDistributions(eptParameterFile);

                WaferFabSettings.WCOvertakingDistributions = reader.GetOvertakingDistributions();
            }

            return WaferFabSettings;
        }

        public override WaferFabSettings DeserializeWaferFabSettings(string serializedFileName, string eptParameterFile)
        {
            Console.Write("Reading waferfabsettings -");

            WaferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(DirectorySerializedFiles, serializedFileName));

            EPTDistributionReader reader = new EPTDistributionReader(DirectoryInputCSVs, WaferFabSettings.WorkCenters, WaferFabSettings.LotStepsPerWorkStation);

            WaferFabSettings.WCServiceTimeDistributions = reader.GetServiceTimeDistributions(eptParameterFile);

            WaferFabSettings.WCOvertakingDistributions = reader.GetOvertakingDistributions();

            Console.Write(" done.\n");

            return WaferFabSettings;
        }

        public override List<WorkCenterLotActivities> ReadWorkCenterLotActivities(string fileName, bool onlyProductionLots)
        {
            Console.Write("Reading lot activities raw - ");

            WorkCenterLotActivities = new List<WorkCenterLotActivities>();

            // Fill lot activites raw
            using (StreamReader reader = new StreamReader(Path.Combine(DirectoryInputCSVs, fileName)))
            {
                string headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string dataLine = reader.ReadLine();

                    lotActivitiesRaw.Add(new LotActivityRaw(headerLine, dataLine));
                }
            }

            Console.Write("done. \n");

            // Filter only production lots, order by track-in time and group activities from same lot
            if (onlyProductionLots)
            {
                lotActivitiesRaw = lotActivitiesRaw.Where(x => x.LotId.StartsWith("M1")).OrderBy(x => x.TrackIn).OrderBy(x => x.LotId).ToList();
            }

            // Map IRD groups on LotActivitiesRaw
            Dictionary<string, IRDMapping> irdDict = irdMappings.ToDictionary(x => $"{x.Techstage} {x.Subplan}", x => x);

            List<string> lotids = lotActivitiesRaw.Select(x => x.LotId).ToList();

            foreach (LotActivityRaw lotActivity in lotActivitiesRaw)
            {
                if (irdDict.ContainsKey($"{lotActivity.Techstage} {lotActivity.Subplan}"))
                {
                    lotActivity.IRDGroup = irdDict[$"{lotActivity.Techstage} {lotActivity.Subplan}"].IRDGroup;
                    lotActivity.WorkStation = irdDict[$"{lotActivity.Techstage} {lotActivity.Subplan}"].WorkStation;
                }
            }

            Console.Write("Grouping lot activities per lot into lotraces - ");

            // Group lotactivities per Lot into LotTraces and map StepSequences on the LotActitiesRaw
            List<LotTrace> lotTraces = new List<LotTrace>();
            string currentId = "";
            LotTrace trace = new LotTrace("");

            foreach (LotActivityRaw activity in lotActivitiesRaw)
            {
                if (activity.LotId != currentId) // Start new lot trace
                {
                    if (currentId != "")
                    {
                        lotTraces.Add(trace);
                        trace.MapPPStepSequencesOnActivites(processPlans);
                        trace.CheckCompleteness();
                        trace.CalculateTimeInSteps();
                    }

                    currentId = activity.LotId;

                    trace = new LotTrace(activity.LotId);
                    trace.ProductType = activity.ProductType;
                }

                trace.LotActivitiesRaw.Add(activity);
            }

            Console.Write("done. \n");

            DateTime begin = lotActivitiesRaw.Select(x => x.TrackIn).Min();
            DateTime end = lotActivitiesRaw.Select(x => x.TrackIn).Max();

            LotTraces = new LotTraces(lotTraces, begin, end);

            int count = 1;

            Console.WriteLine($"Calculating EPTs for {irdMappings.Select(x => x.WorkStation).Distinct().Count()} workstations. Done for workstations:");

            // Make LotActivitiesPerWorkCenter and calculate EPTs
            foreach (string workCenter in irdMappings.Select(x => x.WorkStation).Distinct())
            {
                WorkCenterLotActivities.Add(new WorkCenterLotActivities(LotTraces, workCenter));

                Console.Write($"{count++} ");
            }

            Console.Write("- done. \n");

            return WorkCenterLotActivities;
        }

        public override List<RealSnapshot> ReadRealSnapshots(DateTime from, DateTime until, TimeSpan interval, int waferQtyThreshold)
        {
            Console.Write("Reading RealSnaphots -");

            if (LotTraces == null)
            {
                throw new Exception("LotTraces is still empty, first use ReadLotactivityHistories");
            }

            RealSnapshots = new List<RealSnapshot>();

            DateTime current = from;

            while (current <= until)
            {
                if (current >= LotTraces.StartDate && current <= LotTraces.EndDate)
                {
                    RealSnapshots.Add(LotTraces.GetWIPSnapshot(current, waferQtyThreshold));
                }

                current += interval;
            }

            Console.Write(" done.\n");

            return RealSnapshots;
        }

        public Dictionary<LotStep, double> ReadWIPTargets(Dictionary<string, LotStep> lotSteps, string fileName)
        {
            Dictionary<LotStep, double> wipTargets = lotSteps.ToDictionary(x => x.Value, x => -1.0);

            // Read steps from process plans
            using (StreamReader reader = new StreamReader(Path.Combine(DirectoryInputCSVs, fileName)))
            {
                string[] headers = reader.ReadLine().Trim(',').Split(',');
                int irdIndex = -1; int WIPindex = -1;

                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i] == "IRD") { irdIndex = i; }
                    if (headers[i] == "Optimum WIP") { WIPindex = i; }
                }

                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Trim(',').Split(',');

                    wipTargets[lotSteps[data[irdIndex]]] = Convert.ToDouble(data[WIPindex]);
                }
            }

            // Check if all wip targets have been set
            if (wipTargets.Where(x => x.Value < 0.0).Any())
            {
                throw new Exception("Not all lot steps (IRDs) have WIP targets.");
            }

            return wipTargets;
        }

        public List<Tuple<DateTime, RealLot>> GetLotStarts()
        {
            RealLotStarts = LotTraces.GetRealLotStarts();

            return RealLotStarts;
        }

        public List<Tuple<DateTime, Lot>> GetLotStartsOneWorkCenter(string workcenter)
        {
            List<Tuple<DateTime, Lot>> starts = new List<Tuple<DateTime, Lot>>();

            if (WorkCenterLotActivities == null || !WorkCenterLotActivities.Any())
            {
                WorkCenterLotActivities = Deserializer.DeserializeWorkCenterLotActivities(Path.Combine(DirectorySerializedFiles, "WorkCenterLotActivities_2019_2020.dat"));
            }

            // Make sequences per lotstep. Each sequence contains just 1 lotstep.
            Dictionary<string, Sequence> sequences = new Dictionary<string, Sequence>();

            foreach (var step in lotSteps)
            {
                sequences.Add(step.Key, new Sequence(step.Value));
            }

            WorkCenterLotActivities selected = WorkCenterLotActivities.Where(x => x.WorkCenter == workcenter).First();

            var tmp = selected.LotActivities.Where(x => x.Arrival != null && x.IRDGroup != null).Count();


            var tmp2 = selected.LotActivities.Count();

            Console.WriteLine($"{tmp} {tmp2} {tmp2 - tmp}");

            foreach (LotActivity activity in selected.LotActivities.Where(x => x.Arrival != null && x.IRDGroup != null))
            {
                starts.Add(new Tuple<DateTime, Lot>((DateTime)activity.Arrival, activity.ConvertToLot(0, sequences[activity.IRDGroup])));
            }

            WaferFabSettings.Sequences = sequences;

            WaferFabSettings.LotStarts = starts;

            return starts;
        }

        public void WriteLotActivitiesToCSV(string fileName)
        {
            LotTraces.WriteLotActivitiesToCSV(Path.Combine(DirectorySerializedFiles, fileName));
        }

        /// <summary>
        /// Used to read LotActivityHistories
        /// </summary>
        private List<LotActivityRaw> lotActivitiesRaw { get; set; }
        /// <summary>
        /// Used to read waferfabsettings
        /// </summary>
        private List<SingleStep> lotStepsRaw { get; set; }
        private Dictionary<string, LotStep> lotSteps { get; set; }
        private List<IRDMapping> irdMappings { get; set; }
        private Dictionary<string, int> irdNumbering { get; set; }
        private Dictionary<string, ProcessPlan> processPlans { get; set; }




        private void ReadLotStepsRawAndIRDs()
        {
            lotStepsRaw.Clear();
            irdMappings.Clear();
            irdNumbering.Clear();

            // Read steps from process plans
            using (StreamReader reader = new StreamReader(Path.Combine(DirectoryInputCSVs, "ProcessPlans.csv")))
            {
                string headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string dataLine = reader.ReadLine();

                    lotStepsRaw.Add(new SingleStep(headerLine, dataLine));
                }
            }

            // Read IRD Mappings
            using (StreamReader reader = new StreamReader(Path.Combine(DirectoryInputCSVs, "IRDMapping.csv")))
            {
                string headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string dataLine = reader.ReadLine();

                    irdMappings.Add(new IRDMapping(headerLine, dataLine));
                }
            }



            // Map IRDs on Steps
            //List<string> missingIRDmappings = new List<string>();

            foreach (var step in lotStepsRaw)
            {
                for (int i = 0; i < irdMappings.Count; i++)
                {
                    var mapping = irdMappings[i];

                    if (step.Techstage == mapping.Techstage && step.Subplan == mapping.Subplan)
                    {
                        step.IRDGroup = mapping.IRDGroup;
                        step.ToolGroup = mapping.WorkStation;
                        break;
                    }
                    else if (i == irdMappings.Count - 1)
                    {
                        throw new Exception($"Did not find IRDGroup for {step.Productname} in {step.Techstage} {step.Subplan}");
                        //Console.WriteLine($"Did not find IRDGroup for {step.Productname} in {step.Techstage} {step.Subplan}");
                        //if (!missingIRDmappings.Contains($"{step.Techstage} {step.Subplan}"))
                        //{
                        //    missingIRDmappings.Add($"{step.Techstage} {step.Subplan}");
                        //}
                    }
                }
            }

            //foreach (var missing in missingIRDmappings)
            //{
            //    Console.WriteLine(missing);
            //}

            // Read IRD Numbering
            using (StreamReader reader = new StreamReader(Path.Combine(DirectoryInputCSVs, "IRDNumbering.csv")))
            {
                string headerLine = reader.ReadLine(); // not used

                while (!reader.EndOfStream)
                {
                    string[] dataLine = reader.ReadLine().Split(',');

                    irdNumbering.Add(dataLine[0], Convert.ToInt32(dataLine[1]));
                }

                // Order the lists on IDs
                irdNumbering = irdNumbering.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            }
        }
        private Dictionary<string, LotStep> fillStepsWithIRDs()
        {
            Dictionary<string, LotStep> steps = new Dictionary<string, LotStep>();

            foreach (var IRD in irdNumbering)
            {
                steps.Add(IRD.Key, new LotStep(IRD.Value, IRD.Key));
            }

            return steps;
        }
        private Dictionary<string, List<LotStep>> getLotStepsPerWorkstation()
        {
            Dictionary<string, List<LotStep>> lotStepsPerWorkstation = new Dictionary<string, List<LotStep>>();

            // Initialize Lists for each workcenter
            foreach (string wc in WaferFabSettings.WorkCenters)
            {
                lotStepsPerWorkstation.Add(wc, new List<LotStep>());
            }

            // Add all steps to corresponding lists
            foreach (var irdMapping in irdMappings)
            {
                // TO DO: REMOVE THIS if SAOP Process plans is up to date

                if (WaferFabSettings.LotSteps.ContainsKey(irdMapping.IRDGroup))
                {
                    lotStepsPerWorkstation[irdMapping.WorkStation].Add(WaferFabSettings.LotSteps[irdMapping.IRDGroup]);
                }
            }

            // Order lists per workcenter
            foreach (string wc in WaferFabSettings.WorkCenters)
            {
                lotStepsPerWorkstation[wc] = lotStepsPerWorkstation[wc].Distinct().OrderBy(x => x.Id).ToList();
            }

            return lotStepsPerWorkstation;
        }
        private Dictionary<string, Sequence> getSequencesPerIRDGroup()
        {
            List<Sequence> sequences = new List<Sequence>();

            foreach (string product in lotStepsRaw.Select(x => x.Productname).Distinct())
            {
                List<SingleStep> stepsThisProduct = lotStepsRaw.Where(x => x.Productname == product).OrderBy(x => x.StepSequence).ToList();

                Sequence seq = new Sequence(stepsThisProduct.First().Productname, stepsThisProduct.First().Plangroup);

                string currentIRD = "";

                // NOTE. Property of lists: foreach loop on List loops in correct order (from first to last index)
                foreach (var step in stepsThisProduct)
                {
                    if (currentIRD != step.IRDGroup)
                    {
                        currentIRD = step.IRDGroup;

                        seq.AddStep(lotSteps[currentIRD]);
                    }
                }
                sequences.Add(seq);
            }

            return sequences.ToDictionary(x => x.ProductType);
        }
        private Dictionary<string, ProcessPlan> getProcessPlans()
        {
            Dictionary<string, ProcessPlan> ProcessPlans = new Dictionary<string, ProcessPlan>();

            foreach (string product in getProductTypes())
            {
                ProcessPlan plan = new ProcessPlan(product, lotStepsRaw.Where(x => x.Productname == product).OrderBy(x => x.StepSequence).ToList());

                ProcessPlans.Add(product, plan);
            }

            return ProcessPlans;
        }

        private Dictionary<string, DispatcherBase.Type> getDispatchers(DispatcherBase.Type dispatcherType)
        {
            Dictionary<string, DispatcherBase.Type> dict = new Dictionary<string, DispatcherBase.Type>();

            foreach (string wc in WaferFabSettings.WorkCenters)
            {
                dict.Add(wc, dispatcherType);
            }

            return dict;
        }

        private List<string> getProductTypes()
        {
            return lotStepsRaw.Select(x => x.Productname).Distinct().ToList();
        }

        [Serializable]
        public class ProcessPlan
        {
            public string Productname { get; private set; }
            public List<SingleStep> Steps { get; private set; }

            public Dictionary<int, SingleStep> StepsDict { get; set; }

            public ProcessPlan(string productName, List<SingleStep> steps)
            {
                Productname = productName;
                Steps = steps;
                StepsDict = steps.ToDictionary(x => x.StepSequence, x => x);
            }
        }
        [Serializable]
        public class SingleStep
        {
            public string Productname { get; private set; }
            public string Plangroup { get; private set; }
            public string Techstage { get; private set; }
            public string Subplan { get; private set; }
            public string Stepname { get; private set; }
            public string Recipe { get; private set; }
            public int StepSequence { get; private set; }
            public string IRDGroup { get; set; }

            public string ToolGroup { get; set; }

            private readonly char[] digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            public string IRDStep => Techstage.TrimEnd(digits) + " " + Subplan.TrimEnd(digits);

            public SingleStep(string headerLine, string dataLine)
            {
                string[] headers = headerLine.Trim(',').Split(',');
                string[] data = dataLine.Trim(',').Split(',');

                for (int i = 0; i < data.Length; i++)
                {
                    if (headers[i] == "PRODUCTNAME" || headers[i] == "PRODUCT") { Productname = data[i]; }
                    if (headers[i] == "PLANGROUP") { Plangroup = data[i]; }
                    if (headers[i] == "TECHSTAGE") { Techstage = data[i]; }
                    if (headers[i] == "SUBPLAN") { Subplan = data[i]; }
                    if (headers[i] == "STEPNAME") { Stepname = data[i]; }
                    if (headers[i] == "RECIPE") { Recipe = data[i]; }
                    if (headers[i] == "STEPSEQUENCE") { StepSequence = int.Parse(data[i]); }
                }
            }
        }
        [Serializable]
        private class IRDMapping
        {
            public string Techstage { get; set; }
            public string Subplan { get; set; }
            public string IRDGroup { get; set; }
            public string WorkStation { get; set; }

            public IRDMapping(string headerLine, string dataLine)
            {
                string[] headers = headerLine.Trim(',').Split(',');
                string[] data = dataLine.Trim(',').Split(',');

                for (int i = 0; i < data.Length; i++)
                {
                    if (headers[i] == "TECHSTAGE") { Techstage = data[i]; }
                    if (headers[i] == "SUBPLAN") { Subplan = data[i]; }
                    if (headers[i] == "IRDNAME") { IRDGroup = data[i]; }
                    if (headers[i] == "SUMMARYGROUP") { WorkStation = data[i]; }
                }
            }
        }
    }
}
