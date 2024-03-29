﻿using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

namespace WaferAreaOptimiser
{
    public class WaferAreaSim
    {
        private string wc;

        private string inputDirectory;

        private string outputDirectory;

        private DateTime initialDateTime;

        WaferFabSettings waferFabSettings;

        EPTDistributionReader distributionReader;

        public Dictionary<string, Distribution> InitialParameters { get; }

        private List<Lot> initialLots;

        private Optimiser optimiser;

        private bool useInitialLots;

        public WaferAreaSim(string wc, string eptParameterFile, string inputDirectory, string outputDirectory, DateTime initialDateTime, Optimiser optimiser, bool useInitialLots = false)
        {
            this.wc = wc;

            this.inputDirectory = inputDirectory;

            this.outputDirectory = outputDirectory;

            this.initialDateTime = initialDateTime;

            this.optimiser = optimiser;

            this.useInitialLots = useInitialLots;

            #region WaferFab settings
            waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

            distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

            // Get initial parameters
            waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions(eptParameterFile);

            EPTDistribution initialDist = (EPTDistribution)waferFabSettings.WCServiceTimeDistributions[wc];
            InitialParameters = new Dictionary<string, Distribution> { { wc, optimiser.CheckInitialDistBounds(initialDist) } };

            waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions();
            #endregion

            if (useInitialLots) { initialLots = optimiser.GetInitialLots(wc, inputDirectory, outputDirectory, initialDateTime, waferFabSettings); }            
        }

        public Tuple<double, double> RunSim(Dictionary<string, Distribution> dict)
        {
            EPTDistribution dist = (EPTDistribution)dict.First().Value;
            WIPDepDistParameters x = dist.Par;

            waferFabSettings.WCServiceTimeDistributions = new Dictionary<string, Distribution> { { wc, new EPTDistribution(x) } };

            #region Initializing simulation
            Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);
            #endregion

            #region Experiment settings
            simulation.MyExperiment.NumberOfReplications = 10;

            DateTime finalDateTime = new DateTime(2019, initialDateTime.Month + 2, 1);
            simulation.MyExperiment.LengthOfReplication = (finalDateTime - initialDateTime).TotalSeconds; // Number of seconds between two months

            simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 0;
            #endregion

            #region Building the model
            WaferFab waferFab = new WaferFab(simulation.MyModel, "WaferFab", new ConstantDistribution(60 * 60 * 24), initialDateTime);

            WorkCenter workCenter = new WorkCenter(waferFab, $"WorkCenter_{wc}", waferFabSettings.WCServiceTimeDistributions[wc], waferFabSettings.LotStepsPerWorkStation[wc]);

            // Connect workcenter to WIPDependentDistribution
            EPTDistribution distr = (EPTDistribution)waferFabSettings.WCServiceTimeDistributions[wc];

            distr.WorkCenter = workCenter;

            EPTOvertakingDispatcher dispatcher = new EPTOvertakingDispatcher(workCenter, workCenter.Name + "_EPTOvertakingDispatcher", waferFabSettings.WCOvertakingDistributions[wc]);
            workCenter.SetDispatcher(dispatcher);

            // Connect workcenter to OvertakingDistribution
            waferFabSettings.WCOvertakingDistributions[wc].WorkCenter = workCenter;

            waferFab.AddWorkCenter(workCenter.Name, workCenter);

            // Sequences
            foreach (var sequence in waferFabSettings.Sequences)
            {
                waferFab.AddSequence(sequence.Key, sequence.Value);
            }

            // LotSteps
            waferFab.LotSteps = waferFab.Sequences.Select(x => x.Value).Select(x => x.GetCurrentStep(0)).ToDictionary(x => x.Name);

            // LotGenerator
            waferFab.SetLotGenerator(new LotGenerator(waferFab, "LotGenerator", new ConstantDistribution(60), true));

            // Add lotstarts
            waferFab.LotStarts = waferFabSettings.LotStarts;

            // Add initial lots
            if (useInitialLots)
            {
                List<Lot> initialLotsDeepCopy = initialLots.ConvertAll(x => new Lot(x));
                waferFab.InitialLots = initialLotsDeepCopy;
            }

            // Add observers
            OptimiserObserver optimiserObs = new OptimiserObserver(simulation, wc + "_TotalQueueObserver");
            workCenter.Subscribe(optimiserObs); // Total queue for workcenter
            #endregion

            simulation.Run();

            #region Reporting
            SimulationReporter reporter = simulation.MakeSimulationReporter();

            reporter.PrintSummaryToConsole();
            #endregion

            Tuple<double, double> results = new Tuple<double, double>(optimiserObs.QueueLengthStatistic.Average(), optimiserObs.QueueLengthStatistic.StandardDeviation());

            return results;
        }
    }
}
