using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.RL;
using CSSL.Utilities.Distributions;
using RLToyFab.Elements;
using RLToyFab.Elements.Dispatchers;
using System;
using WaferFabSim;
using WaferFabSim.InputDataConversion;
using WaferFabSim.WaferFabElements;

namespace RLToyFab
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output\RLToyFab\";

            Settings.WriteOutput = true;

            // Fab settings
            ManualDataReader reader = new ManualDataReader(inputDir + @"\RLToyFab\");
            WaferFabSettings waferFabSettings = reader.ReadWaferFabSettings();
            waferFabSettings.SampleInterval = 1 * 60 * 60;  // seconds
            waferFabSettings.LotStartsFrequency = 1;        // hours

            // Build Fab
            //RLSimulation sim = new RLSimulation("RLToyFab", outputDir);
            Simulation sim = new Simulation("RLToyFab", outputDir);

            // Experiment settings
            sim.MyExperiment.NumberOfReplications = 1;
            sim.MyExperiment.LengthOfWarmUp = 0;
            sim.MyExperiment.LengthOfReplication = 121 * 24 * 60 * 60;

            //// RLLayer
            //RLLayer RLLayer = new RLLayer();

            // Build the model
            ToyFab toyFab = new ToyFab(sim.MyModel, "WaferFab")
            {
                //// LotStarts
                ManualLotStarts = waferFabSettings.ManualLotStartQtys,

                //// LotSteps
                LotSteps = waferFabSettings.LotSteps
            };

            //// WorkStations
            foreach (string wc in waferFabSettings.WorkCenters)
            {
                WorkStation workStation = new WorkStation(toyFab, $"WorkCenter_{wc}", waferFabSettings.LotStepsPerWorkStation[wc]);

                workStation.Dispatcher = new BQFDispatcher(workStation, workStation.Name + "_BQFDispatcher");


                toyFab.AddWorkCenter(workStation.Name, workStation);
            }

            //// Sequences
            foreach (var sequence in waferFabSettings.Sequences)
            {
                toyFab.AddSequence(sequence.Key, sequence.Value);
            }

            //// LotGenerator
            toyFab.SetLotGenerator(new LotGenerator(toyFab, "LotGenerator", new ConstantDistribution(waferFabSettings.LotStartsFrequency * 60 * 60), waferFabSettings.UseRealLotStartsFlag));

            sim.Run();

            // Report summary
            SimulationReporter reporter = sim.MakeSimulationReporter();

            reporter.PrintSummaryToFile();
            reporter.PrintSummaryToConsole();
        }
    }
}
