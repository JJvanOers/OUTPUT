using CSSL.Modeling;
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

        Optimiser optimiser;

        public WaferAreaSim(string wc, string inputDirectory, string outputDirectory, DateTime initialDateTime, Optimiser optimiser)
        {
            this.wc = wc;

            this.inputDirectory = inputDirectory;

            this.outputDirectory = outputDirectory;

            this.initialDateTime = initialDateTime;

            this.optimiser = optimiser;

            #region WaferFab settings
            waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

            distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

            // Get initial parameters
            waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions();

            EPTDistribution initialDist = (EPTDistribution)waferFabSettings.WCServiceTimeDistributions[wc];
            InitialParameters = new Dictionary<string, Distribution> { { wc, initialDist } };

            waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions();
            #endregion

            initialLots = optimiser.GetInitialLots(wc, inputDirectory, outputDirectory, initialDateTime, waferFabSettings);
        }

        public Tuple<double, double> RunSim(Dictionary<string, Distribution> dict)
        {
            waferFabSettings.WCServiceTimeDistributions = dict;

            #region Initializing simulation
            Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);
            #endregion

            #region Experiment settings
            simulation.MyExperiment.NumberOfReplications = 5;
            simulation.MyExperiment.LengthOfReplication = 60 * 60 * 24 * 61;
            simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 0;
            #endregion

            #region Make starting lots
            AutoDataReader dataReader = new AutoDataReader(Path.Combine(inputDirectory, "Auto"), Path.Combine(inputDirectory, "SerializedFiles"));
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
            List<Lot> copiedInitialLots = optimiser.CopyInitialLots(initialLots);
            waferFab.InitialLots = initialLots;

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
