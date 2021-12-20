using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.RL;
using CSSL.Utilities.Distributions;
using RLToyFab.Elements;
using RLToyFab.Elements.Dispatchers;
using RLToyFab.Observers;
using System;
using System.Collections.Generic;
using WaferFabSim;
using WaferFabSim.InputDataConversion;
using WaferFabSim.WaferFabElements;
//using WaferFabSim.WaferFabElements.Observers;

namespace RLToyFab
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDir = @"C:\CSSLWaferFab\Input\";

            string outputDir = @"C:\CSSLWaferFab\Output\RLToyFab\";

            Settings.WriteOutput = true;
            Settings.FixSeed = true;

            // Fab settings
            ManualDataReader reader = new ManualDataReader(inputDir + @"\RLToyFab\");
            WaferFabSettings waferFabSettings = reader.ReadWaferFabSettings();
            waferFabSettings.SampleInterval = 0.25 * 60 * 60;  // seconds
            //int inputEvery = 1;
            //foreach (var techStarts in waferFabSettings.ManualLotStartQtys) 
            //{
            //    techStarts[techStarts.Key] /= (int)Math.Round((double)techStarts.Value * inputEvery / waferFabSettings.LotStartsFrequency);
            //}
            //waferFabSettings.LotStartsFrequency = 1;        // hours

            // Build Fab
            //RLSimulation sim = new RLSimulation("RLToyFab", outputDir);
            Simulation sim = new Simulation("RLToyFab", outputDir);

            // Experiment settings
            sim.MyExperiment.NumberOfReplications = 1;
            sim.MyExperiment.LengthOfWarmUp = 3 * 24 * 60 * 60;
            sim.MyExperiment.LengthOfReplication = 30 * 24 * 60 * 60;

            //// RLLayer
            //RLLayer RLLayer = new RLLayer();

            // Build the model
            ToyFab toyFab = new ToyFab(sim.MyModel, "WaferFab", new ConstantDistribution(waferFabSettings.SampleInterval))
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

                workStation.dispatcher = new BQFDispatcher(workStation, workStation.Name + "_BQFDispatcher");

                //// Add machines (5 identical machines everywhere)
                //List<Machine> wcMachines = new List<Machine>();
                int NumberOfMachines = (int)waferFabSettings.WCMachines[wc];
                for (int i=0; i < NumberOfMachines; i++)
                {
                    Machine machine = new Machine(toyFab, $"{workStation.Name}_Machine{i}", new ExponentialDistribution(1 / waferFabSettings.WCServiceTimeDistributions[wc].Mean / NumberOfMachines), workStation); 
                    foreach (LotStep lotStep in workStation.LotSteps)
                    {
                        if (reader.eligibilityMap[lotStep][i] == 1) machine.Eligibilities.Add(lotStep);
                    }
                    workStation.Machines.Add(machine);
                }
                //workStation.Machines = wcMachines;

                toyFab.AddWorkCenter(workStation.Name, workStation);
            }

            //// Sequences
            foreach (var sequence in waferFabSettings.Sequences)
            {
                toyFab.AddSequence(sequence.Key, sequence.Value);
            }

            //// LotGenerator
            toyFab.SetLotGenerator(new LotGenerator(toyFab, "LotGenerator", new ConstantDistribution(waferFabSettings.LotStartsFrequency * 60 * 60), waferFabSettings.UseRealLotStartsFlag));

            //// Observers
            LotGeneratorStartsObserver startsObserver = new LotGeneratorStartsObserver(sim, "WaferFabStartsObserver");
            WaferFabLotsObserver lotsObserver = new WaferFabLotsObserver(sim, "WaferfabLotsObserver", toyFab);
            WaferFabTotalWIPObserver totalQueueObserver = new WaferFabTotalWIPObserver(sim, "WaferFabTotalQueueObserver", toyFab);
            WaferFabUtilizationObserver wfUtilObserver = new WaferFabUtilizationObserver(sim, "WaferFabUtilizationObserver", toyFab);
            toyFab.LotGenerator.Subscribe(startsObserver);
            toyFab.Subscribe(lotsObserver);
            toyFab.Subscribe(totalQueueObserver);
            toyFab.Subscribe(wfUtilObserver);
            foreach (var wc in toyFab.WorkStations)
            {
                LotOutObserver lotOutObserver = new LotOutObserver(sim, wc.Key + "_LotOutObserver");
                //WSUtilizationObserver utilizationObserver = new WSUtilizationObserver(sim, wc.Key + "_UtilObserver", wc.Value);
                wc.Value.dispatcher.Subscribe(lotOutObserver);
                //wc.Value.Subscribe(utilizationObserver);
            }

            //// Run
            sim.Run();

            // Report summary
            SimulationReporter reporter = sim.MakeSimulationReporter();

            reporter.PrintSummaryToFile();
            reporter.PrintSummaryToConsole();
        }
    }
}
