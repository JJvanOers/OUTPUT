using System;
using CSSL.Modeling;
using CSSL.Reporting;
using System.Linq;
using WaferFabSim.InputDataConversion;
using WaferFabSim.WaferFabElements.Dispatchers;
using System.Collections.Generic;
using WaferFabSim.WaferFabElements;

namespace WaferFabSim
{
    public class Program
    {
        static void Main(string[] args)
        {
            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output\WaferFabSim\";

            string parameters = @"FittedEPTParameters - 2019-8-1_4months.csv";

            DateTime initialDateTime = new DateTime(2019, 8, 1);

            Settings.WriteOutput = true;
            Settings.FixSeed = true;

            ShellModel WaferFabSim = new ShellModel(outputDir);

            // Load WaferFab settings
            AutoDataReader reader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");

            WaferFabSettings waferFabSettings = reader.ReadWaferFabSettings(parameters, true, true, DispatcherBase.Type.EPTOVERTAKING);

            waferFabSettings.SampleInterval = 1 * 60 * 60;  // seconds
            waferFabSettings.LotStartsFrequency = 1;        // hours
            waferFabSettings.UseRealLotStartsFlag = true;

            // MIVS Settings
            //waferFabSettings.WIPTargets = reader.ReadWIPTargets(waferFabSettings.LotSteps, "WIPTargets.csv");
            //waferFabSettings.MIVSjStepBack = 2;
            //waferFabSettings.MIVSkStepAhead = 2;

            // Read Initial Lots
            WaferFabSim.ReadRealSnaphots(inputDir + @$"SerializedFiles\RealSnapShots_2019-{initialDateTime.Month}-1_2019-{initialDateTime.Month + 1}-1_1h.dat");
            waferFabSettings.InitialRealLots = WaferFabSim.RealSnapshotReader.RealSnapshots.Where(x => x.Time.Date == initialDateTime).OrderBy(x => x.Time).First().GetRealLots(1);

            // Experiment settings
            ExperimentSettings experimentSettings = new ExperimentSettings();

            int count = waferFabSettings.InitialRealLots.Where(x => x.StartTime == null).Count();

            experimentSettings.NumberOfReplications = 30;
            experimentSettings.LengthOfReplication = 121 * 24 * 60 * 60; // seconds
            experimentSettings.LengthOfWarmUp = 0 * 60 * 60;  // seconds

            // Connect settings
            WaferFabSim.MyWaferFabSettings = waferFabSettings;

            WaferFabSim.MyExperimentSettings = experimentSettings;

            // Run simulation
            WaferFabSim.RunSimulation();

            // Report summary
            SimulationReporter reporter = WaferFabSim.MySimulation.MakeSimulationReporter();

            reporter.PrintSummaryToFile();
            reporter.PrintSummaryToConsole();
        }
    }
}
