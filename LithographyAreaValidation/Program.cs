using CSSL.Modeling;
using CSSL.Reporting;
using LithographyAreaValidation.Controls;
using LithographyAreaValidation.ModelElements;
using LithographyAreaValidation.Observers;
using LithographyAreaValidation.Solvers;
using System;
using System.Collections.Generic;
using System.IO;

namespace LithographyAreaValidation
{
    public class Program
    {
        static void Main(string[] args)
        {
            #region Simulation settings
            
            // CONTROLLER
            bool simulateFIFO = false;
            bool simulateEDD = false;
            bool simulateSPT = false;
            bool simulateCurrentProductionControl = false;
            bool simulateILPScheduling = false;
            bool simulateCPScheduling = true;

            // STOCHASTIC / DYNAMIC
            bool dynamic = true;
            bool stochastic = true;

            // START DATES
            List<DateTime> startDates = new List<DateTime>();

            //for (int i = 1; i < 29; i++)
            //{
            //    startDates.Add(new DateTime(2021, 2, i, 7, 0, 0));
            //}

            startDates.Add(new DateTime(2021, 2, 1, 7, 0, 0));
            //startDates.Add(new DateTime(2021, 3, 1, 7, 0, 0));
            //startDates.Add(new DateTime(2021, 4, 1, 7, 0, 0));

            // WEIGHTS
            List<double[]> weightsILPScheduler = new List<double[]>();
            List<double[]> weightsCPScheduler = new List<double[]>();

            if (dynamic)
            {
                weightsILPScheduler.Add(new double[] { 1 });

                weightsCPScheduler.Add(new double[] { 100, 1, 0 });
            }
            else
            {
                weightsILPScheduler.Add(new double[] { 0 });
                weightsILPScheduler.Add(new double[] { 0.25 });
                weightsILPScheduler.Add(new double[] { 0.5 });
                weightsILPScheduler.Add(new double[] { 0.75 });
                weightsILPScheduler.Add(new double[] { 1 });

                weightsCPScheduler.Add(new double[] { 1, 0, 0 });
                weightsCPScheduler.Add(new double[] { 0, 1, 0 });
                weightsCPScheduler.Add(new double[] { 0, 0, 1 });
                weightsCPScheduler.Add(new double[] { 100, 1, 1 });

                //List<double> weightsA = new List<double>();
                //weightsA.Add(0);

                //List<double> weightsB = new List<double>();
                //weightsB.Add(0);

                //List<double> weightsC = new List<double>();
                //weightsC.Add(0);

                //foreach (double weightA in weightsA)
                //{
                //    foreach (double weightB in weightsB)
                //    {
                //        foreach (double weightC in weightsC)
                //        {
                //            weightsCPScheduler.Add(new double[] { weightA, weightB, weightC });
                //        }
                //    }
                //}
            }

            #endregion

            #region Set up times
            Dictionary<string,double> deterministicNonProductiveTimesRMS = new Dictionary<string, double>
            {
                { "SameReticle", 17 },
                { "DifferentReticle", 75 },
                { "DifferentIRD", 195 }
            };
            Dictionary<string, double> deterministicNonProductiveTimesARMS = new Dictionary<string, double>
            {
                { "SameReticle", 17 },
                { "DifferentReticle", 60 },
                { "DifferentIRD", 120 }
            };
            #endregion

            #region Build simulations

            List<string> simulatedControls = new List<string>();
            if (simulateFIFO)
            {
                simulatedControls.Add("FIFO");
            }
            if (simulateEDD)
            {
                simulatedControls.Add("EDD");
            }
            if (simulateSPT)
            {
                simulatedControls.Add("SPT");
            }
            if (simulateCurrentProductionControl)
            {
                simulatedControls.Add("CurrentProductionControl");
            }
            if (simulateILPScheduling)
            {
                simulatedControls.Add("ILPScheduling");
            }
            if (simulateCPScheduling)
            {
                simulatedControls.Add("CPScheduling");
            }

            string simulationOutputDirectory = $"{Directory.GetCurrentDirectory()}/Output";

            foreach (DateTime date in startDates)
            {
                foreach (string control in simulatedControls)
                {
                    if (control == "ILPScheduling")
                    {
                        foreach (double[] weights in weightsILPScheduler)
                        {
                            string settingDirectory = $"sto={stochastic}_dyn={dynamic}";
                            string dayDirectory = $"{date.Year}_{date.Month}_{date.Day}";
                            string controlDirectory = $"{control}";
                            string weightDirectory = $"a={weights[0]}";
                            string experimentOutputDirectory = Path.Combine(simulationOutputDirectory,settingDirectory, dayDirectory, controlDirectory, weightDirectory);
                            Directory.CreateDirectory(experimentOutputDirectory);
                            Experiment(control, date, experimentOutputDirectory, dynamic, stochastic, weights[0], -1, -1, deterministicNonProductiveTimesRMS, deterministicNonProductiveTimesARMS);
                        }
                    }
                    else if (control == "CPScheduling")
                    {
                        foreach (double[] weights in weightsCPScheduler)
                        {
                            string settingDirectory = $"sto={stochastic}_dyn={dynamic}";
                            string dayDirectory = $"{date.Year}_{date.Month}_{date.Day}";
                            string weightDirectory = $"a={weights[0]}_b={weights[1]}_c={weights[2]}";
                            string controlDirectory = $"{control}";
                            string experimentOutputDirectory = Path.Combine(simulationOutputDirectory,settingDirectory, dayDirectory, controlDirectory, weightDirectory);
                            Directory.CreateDirectory(experimentOutputDirectory);
                            Experiment(control, date, experimentOutputDirectory, dynamic, stochastic, weights[0], weights[1], weights[2], deterministicNonProductiveTimesRMS, deterministicNonProductiveTimesARMS);
                        }
                    }
                    else
                    {
                        string settingDirectory = $"sto={stochastic}_dyn={dynamic}";
                        string dayDirectory = $"{date.Year}_{date.Month}_{date.Day}";
                        string controlDirectory = $"{control}";
                        string experimentOutputDirectory = Path.Combine(simulationOutputDirectory, settingDirectory, dayDirectory, controlDirectory);
                        Directory.CreateDirectory(experimentOutputDirectory);
                        Experiment(control, date, experimentOutputDirectory, dynamic, stochastic, -1, -1, -1, deterministicNonProductiveTimesRMS, deterministicNonProductiveTimesARMS);
                    }
                }
            }
            #endregion
        }

        private static void Experiment(string control, DateTime startDate, string experimentOutputDirectory, bool dynamic, bool stochastic, double weightA, double weightB, double weightC, Dictionary<string,double> deterministicNonProductiveTimesRMS, Dictionary<string, double> deterministicNonProductiveTimesARMS)
        {
            string outputDir = experimentOutputDirectory;
            Simulation sim = new Simulation("LithographyAreaSim", outputDir);

            // Parameters
            double simulationLength = 15 * 24 * 3600 + 1;
            string productionControl = control;
            int CPTimeLimit = 30;

            // The experiment part
            sim.MyExperiment.LengthOfReplication = simulationLength; 
            sim.MyExperiment.LengthOfWarmUp = 0;
            if (stochastic)
            {
                sim.MyExperiment.NumberOfReplications = 10;
            }
            else
            { 
                sim.MyExperiment.NumberOfReplications = 1;
            }
          
            // The model part

            // Create lithographyarea
            LithographyArea lithographyarea = new LithographyArea(sim.MyModel, "LithographyArea", startDate, simulationLength, dynamic, stochastic, weightA, weightB, weightC, deterministicNonProductiveTimesRMS, deterministicNonProductiveTimesARMS);

            // Property dispatcherBase
            DispatcherBase dispatcher = null;

            // Create chosen dispatcher
            if (productionControl == "FIFO")
            {
                dispatcher = new FIFODispatcher(lithographyarea, "FIFODispatcher");
            }
            else if (productionControl == "EDD")
            {
                dispatcher = new EDDDispatcher(lithographyarea, "EDDDispatcher");
            }
            else if (productionControl == "SPT")
            {
                dispatcher = new SPTDispatcher(lithographyarea, "SPTDispatcher");
            }
            else if (productionControl == "CurrentProductionControl")
            {
                dispatcher = new CurrentDispatcher(lithographyarea, "CurrentDispatcher");
            }
            else if (productionControl == "ILPScheduling")
            {
                dispatcher = new ILPSchedulingDispatcher(lithographyarea, "ILPSchedulingDispatcher");
            }
            else if (productionControl == "CPScheduling")
            {
                dispatcher = new CPDispatcher(lithographyarea, "CPDispatcher", CPTimeLimit);
            }

            // Set dispatcher
            lithographyarea.SetDispatcher(dispatcher);

            // Create and set lotGenerator
            LotGenerator lotGenerator = new LotGenerator(lithographyarea, "LotGenerator", dispatcher);
            lithographyarea.SetLotGenerator(lotGenerator);

            // Create and set machines
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#1", dispatcher, 1));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#2", dispatcher, 2));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#3", dispatcher, 3));
            lithographyarea.AddMachine(new Machine(lithographyarea, "ASML#4", dispatcher, 4));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#5", dispatcher, 5));
            lithographyarea.AddMachine(new Machine(lithographyarea, "ASML#6", dispatcher, 6));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#7", dispatcher, 7));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#8", dispatcher, 8));
            lithographyarea.AddMachine(new Machine(lithographyarea, "ASML#9", dispatcher, 9));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#10", dispatcher, 10));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#11", dispatcher, 11));
            lithographyarea.AddMachine(new Machine(lithographyarea, "StepCluster#13", dispatcher, 12));

            // The observer part

            LithographyAreaObserver lithographyAreaObserver = new LithographyAreaObserver(sim, startDate);
            lithographyarea.Subscribe(lithographyAreaObserver);

            foreach (Machine machine in lithographyarea.Machines)
            {
                MachineObserver machineObserver = new MachineObserver(sim, startDate);
                machine.Subscribe(machineObserver);
            }

            // Run

            sim.Run();

            // The reporting part

            SimulationReporter reporter = sim.MakeSimulationReporter();

            reporter.PrintSummaryToFile();
            reporter.PrintSummaryToConsole();
        }
    }
}
