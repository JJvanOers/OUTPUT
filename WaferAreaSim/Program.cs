using CSSL.Calendar;
using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
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

namespace WaferAreaSim
{
    public class Program
    {
        static void Main(string[] args)
        {
            #region Parameters
            string wc = "FURNACING";

            string inputDirectory = @"E:\OneDrive - Nexperia\CSSLWaferFab\Input";

            string outputDirectory = @"C:\CSSLWaferFab\Output";

            bool fittedParameters = false; // true = fitted, false = optimised

            bool lotStepOvertaking = false;
            #endregion

            #region Initializing simulation
            Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);
            #endregion

            #region Experiment settings
            simulation.MyExperiment.NumberOfReplications = 30;
            simulation.MyExperiment.LengthOfReplication = 60 * 60 * 24 * 61; // (2019, 8, 1) - (2019, 10, 1) = 61 days | (2019, 10, 1) - (2019, 12, 1) = 61 days
            simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 0;
            DateTime initialDateTime = new DateTime(2019, 10, 1);
            #endregion

            #region WaferFab settings
            WaferFabSettings waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

            EPTDistributionReader distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

            waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions(fittedParameters);

            waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions(lotStepOvertaking);
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

            // Add observers
            LotOutObserver lotOutObserver = new LotOutObserver(simulation, wc + "_LotOutObserver");
            dispatcher.Subscribe(lotOutObserver);

            TotalQueueObserver totalQueueObserver = new TotalQueueObserver(simulation, wc + "_TotalQueueObserver");
            workCenter.Subscribe(totalQueueObserver);

            #endregion        

            #region Read initial lots
            RealSnapshotReader reader = new RealSnapshotReader();

            List<RealSnapshot> realSnapshots = reader.Read(Path.Combine(inputDirectory, "SerializedFiles", reader.GetRealSnapshotString(initialDateTime)), 1);

            RealSnapshot realSnapShot = realSnapshots.Where(x => x.Time == initialDateTime).First();

            List<string> lotSteps = workCenter.LotSteps.Select(x => x.Name).ToList();

            List<RealLot> initialRealLots = realSnapShot.GetRealLots(1).Where(x => lotSteps.Contains(x.IRDGroup)).ToList();

            List<Lot> initialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences, initialDateTime)).ToList();
            
            waferFab.InitialLots = initialLots;
            #endregion

            simulation.Run();

            #region Reporting
            SimulationReporter reporter = simulation.MakeSimulationReporter();

            reporter.PrintSummaryToConsole();
            #endregion
        }
    }
}
