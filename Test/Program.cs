using CSSL.Calendar;
using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
using CSSL.Utilities.Statistics;
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
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            /*
            double currentCost, nextCost, temp, deltaCost;
            UniformDistribution uDist = new UniformDistribution(0, 1);
            currentCost = 10;
            nextCost = 20;
            deltaCost = nextCost - currentCost;
            temp = 10;
            double u = uDist.Next();
            double term = Math.Pow(Math.E, -deltaCost / temp);

            Console.WriteLine("{0} {1}", u, term);
            Console.WriteLine(u < term);
            */
           
            
            
            string wc = "PHOTOLITH";

            UniformDistribution uniDist = new UniformDistribution(0, 1);

            Dictionary<string, Distribution> dict = new Dictionary<string, Distribution>();

            WIPDepDistParameters Parameters;

            Parameters = new WIPDepDistParameters
            {
                WorkCenter = wc,
                LBWIP = (int)(uniDist.Next() * 60 + 194), // int
                UBWIP = (int)(uniDist.Next() * 400 + 1300), // int
                Tmin = uniDist.Next() * 400 + 1440, // double, Minimum flow time empty system
                Tmax = uniDist.Next() * 30 + 96, // EPT full system
                Tdecay = uniDist.Next() * 0.01 + 0.02,
                Cmin = uniDist.Next() * 0.075 + 0.1,
                Cmax = uniDist.Next() * 0.2 + 0.78,
                Cdecay = uniDist.Next() * 0.005 + 0.014,
            };

            dict.Add(wc, new EPTDistribution(Parameters));

            var first = dict.First();
            Distribution value = first.Value;

            EPTDistribution value2 = (EPTDistribution)value;

            Console.WriteLine(value2.Par.LBWIP);

            Console.WriteLine((int)0.0001);

            //Console.WriteLine(value.)



            /*#region Parameters
            string wc = "PHOTOLITH";

            string inputDirectory = @"C:\CSSLWaferFabArea\Input";

            string outputDirectory = @"C:\CSSLWaferFabArea\Output";

            //Settings.Output = false; // Observer data is not written to text files
            #endregion

            #region WaferFab settings
            WaferFabSettings waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

            EPTDistributionReader distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

            waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions();

            waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions();
            #endregion

            Dictionary<WIPDepDistParameters, WeightedStatistic> results = new Dictionary<WIPDepDistParameters, WeightedStatistic>();

            Optimiser optimiser = new Optimiser(wc);

            double delta = 15;

            double RealWip = 701.8146597028681;

            int i = 0;
            while (delta > 10 & i < 1000) // While distribution is not represenative of reality
            {
                if (i > 0)
                {
                    waferFabSettings.WCServiceTimeDistributions = optimiser.GenerateParameters();
                }
                i++;

                #region Initializing simulation
                Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);
                #endregion

                #region Experiment settings
                simulation.MyExperiment.NumberOfReplications = 10;
                simulation.MyExperiment.LengthOfReplication = 60 * 60 * 24 * 60;
                simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 30;
                DateTime intialDateTime = new DateTime(2019, 10, 30);
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
                //LotOutObserver lotOutObserver = new LotOutObserver(simulation, wc + "_LotOutObserver");
                //dispatcher.Subscribe(lotOutObserver);

                //WaferFabObserver waferFabObserver = new WaferFabObserver(simulation, "WaferFabObserver", waferFab);
                //waferFab.Subscribe(waferFabObserver); // Queue for each IRD stage for each workstation in the wafer fab

                OptimiserObserver optimiserObs = new OptimiserObserver(simulation, wc + "_TotalQueueObserver");
                //SeperateQueuesObserver seperateQueueObs = new SeperateQueuesObserver(simulation, workCenter, wc + "_SeperateQueuesObserver");

                workCenter.Subscribe(optimiserObs); // Total queue for workcenter
                //workCenter.Subscribe(seperateQueueObs); // Queue for each IRD stage at this workcenter
                #endregion

                simulation.Run();

                results.Add(distr.Par, optimiserObs.QueueLengthStatistic);

                delta = Math.Abs(optimiserObs.QueueLengthStatistic.Average() - RealWip);

                #region Reporting
                SimulationReporter reporter = simulation.MakeSimulationReporter();

                reporter.PrintSummaryToConsole();
                #endregion
            }

            using StreamWriter outputFile = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_parameters.txt"));

            outputFile.WriteLine("LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");

            foreach (KeyValuePair<WIPDepDistParameters, WeightedStatistic> entry in results)
            {
                WIPDepDistParameters x = entry.Key;
                WeightedStatistic y = entry.Value;
                outputFile.WriteLine(x.LBWIP + "," + x.UBWIP + "," + x.Tmin + "," + x.Tmax + "," + x.Tdecay + "," + x.Cmin + "," + x.Cmax + "," + x.Cdecay
                    + "," + y.Average() + "," + y.StandardDeviation());
            }
            */
        }
    }
}
