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
            string wc = "PHOTOLITH";

            string inputDirectory = @"C:\CSSLWaferFabArea\Input";

            string outputDirectory = @"C:\CSSLWaferFabArea\Output";
            #endregion

            #region Initializing simulation
            Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);
            #endregion

            #region Experiment settings
            simulation.MyExperiment.NumberOfReplications = 10;
            simulation.MyExperiment.LengthOfReplication = 60 * 60 * 24 * 60;
            simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 30;
            DateTime intialDateTime = new DateTime(2019, 10, 30);
            #endregion

            #region WaferFab settings
            WaferFabSettings waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", "WaferFabSettings_PHOTOLITH_WithLotStarts.dat"));

            EPTDistributionReader distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

            waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions();

            waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions();
            #endregion

            #region Make starting lots
            AutoDataReader dataReader = new AutoDataReader(Path.Combine(inputDirectory, "Auto"), Path.Combine(inputDirectory, "SerializedFiles"));

            #endregion

            #region Building the model
            WaferFab waferFab = new WaferFab(simulation.MyModel, "WaferFab", new ConstantDistribution(60 * 60 * 24), intialDateTime);

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

            WaferFabObserver waferFabObserver = new WaferFabObserver(simulation, "WaferFabObserver", waferFab);
            waferFab.Subscribe(waferFabObserver);

            TotalQueueObserver totalQueueObs = new TotalQueueObserver(simulation, wc + "_TotalQueueObserver");
            SeperateQueuesObserver seperateQueueObs = new SeperateQueuesObserver(simulation, workCenter, wc + "_SeperateQueuesObserver");

            workCenter.Subscribe(totalQueueObs);
            workCenter.Subscribe(seperateQueueObs);
            #endregion

            simulation.Run();

            #region Reporting
            SimulationReporter reporter = simulation.MakeSimulationReporter();

            reporter.PrintSummaryToConsole();
            #endregion

        }
    }
}
