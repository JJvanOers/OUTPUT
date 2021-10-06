﻿using System;
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
            //  INCLUDE THIS IF SYSTEM IS SET TO DUTCH DECIMAL FORMATTING (comma separator)
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            

            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output\WaferFabSim\";

            DateTime initialDateTime = new DateTime(2019, 6, 1);

            Settings.WriteOutput = true;
            Settings.FixSeed = false;

            ShellModel WaferFabSim = new ShellModel(outputDir);

            // Load WaferFab settings
            AutoDataReader reader = new AutoDataReader(inputDir + @"CSVs\", inputDir + @"SerializedFiles\");

            WaferFabSettings waferFabSettings = reader.ReadWaferFabSettings(true, true, DispatcherBase.Type.RANDOM);

            waferFabSettings.SampleInterval = 1 * 60 * 60;  // seconds
            waferFabSettings.LotStartsFrequency = 1;        // hours
            waferFabSettings.UseRealLotStartsFlag = true;

            // MIVS Settings
            waferFabSettings.WIPTargets = reader.ReadWIPTargets(waferFabSettings.LotSteps, "WIPTargets.csv");
            waferFabSettings.MIVSjStepBack = 5;
            waferFabSettings.MIVSkStepAhead = 5;

            // Read Initial Lots
            WaferFabSim.ReadRealSnaphots(inputDir + @$"SerializedFiles\RealSnapShots_2019-{initialDateTime.Month}-1_2019-{initialDateTime.Month + 1}-1_1h.dat");
            waferFabSettings.InitialRealLots = WaferFabSim.RealSnapshotReader.RealSnapshots.First().GetRealLots(1);

            // Experiment settings
            ExperimentSettings experimentSettings = new ExperimentSettings();

            experimentSettings.NumberOfReplications = 10;
            experimentSettings.LengthOfReplication = 61 * 24 * 60 * 60; // seconds
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
