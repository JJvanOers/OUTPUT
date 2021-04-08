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

namespace WaferAreaOptimiser
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Paramters
            // Simulation parameters
            string wc = "PHOTOLITH";

            string inputDirectory = @"C:\CSSLWaferFab\Input";

            string outputDirectory = @"C:\CSSLWaferFab\OptimiserOutput";

            DateTime initialDateTime = new DateTime(2019, 9, 1);

            Settings.WriteOutput = false;

            // Simulated Annealing parameters
            double temp = 25;

            double cooldown = 0.995;

            double meanObj = 671.6519999278748;

            double stdObj = 205.05027868604603;


            Optimiser optimiser = new Optimiser(wc);

            WaferAreaSim waferAreaSim = new WaferAreaSim(wc, inputDirectory, outputDirectory, initialDateTime, optimiser);            

            // Variables and functions
            Dictionary<string, Distribution> currentPar, nextPar, bestPar;

            Tuple<double, double> currentRes, nextRes, bestRes;

            double currentCost, nextCost, bestCost, deltaCost;

            Dictionary<WIPDepDistParameters, Tuple<double, double>> results = new Dictionary<WIPDepDistParameters, Tuple<double, double>>(); // Save all solutions

            UniformDistribution uDist = new UniformDistribution(0, 1);

            // Initial model parameters and results
            currentPar = waferAreaSim.initialParameters;
            bestPar = optimiser.CopyParameters(currentPar);

            currentRes = waferAreaSim.RunSim(currentPar);
            bestRes = optimiser.CopyResults(currentRes);

            currentCost = Math.Abs(currentRes.Item1 - meanObj) + Math.Abs(currentRes.Item2 - stdObj);
            bestCost = currentCost;

            optimiser.AddResult(results, currentPar, currentRes);

            // Iterate and evaluate solutions until sufficiently cooled down
            int i = 0;
            while (temp > 0.1)
            {
                nextPar = optimiser.GenerateNeighbour(currentPar);
                
                nextRes = waferAreaSim.RunSim(nextPar);

                nextCost = Math.Abs(nextRes.Item1 - meanObj) + Math.Abs(nextRes.Item2 - stdObj);

                optimiser.AddResult(results, nextPar, nextRes);

                if (nextCost < currentCost) // New solution is better than current, accept new solution
                {
                    currentPar = optimiser.CopyParameters(nextPar);
                    currentRes = optimiser.CopyResults(nextRes);
                    currentCost = nextCost;

                    if (nextCost < bestCost) // New solution is better best, accept new best solution
                    {
                        bestPar = optimiser.CopyParameters(nextPar);
                        bestRes = optimiser.CopyResults(nextRes);
                        bestCost = nextCost;
                    }
                } else
                {
                    deltaCost = nextCost - currentCost;

                    if (uDist.Next() < Math.Pow(Math.E, -deltaCost / temp)) // Accept solution if u ~ U[0,1] < e^-(dC/T)
                    {
                        currentPar = optimiser.CopyParameters(nextPar);
                        currentRes = optimiser.CopyResults(nextRes);
                        currentCost = nextCost;
                    }
                }

                temp = temp * cooldown; // Reduce temperature
                i++;

                Console.WriteLine("\nIteration: {0}. Temperature {1}", i, temp);
                Console.WriteLine("Evaluated solution: {0}, {1}", nextRes.Item1, nextRes.Item2);
                Console.WriteLine("Current solution:   {0}, {1}", currentRes.Item1, currentRes.Item2);
                Console.WriteLine("Best solution:      {0}, {1}\n", bestRes.Item1, bestRes.Item2);
            }

            #region Write results to file
            // Write all results to a text file
            using StreamWriter outputFile = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_parameters.txt"));

            outputFile.WriteLine("LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");

            foreach (KeyValuePair<WIPDepDistParameters, Tuple<double, double>> entry in results)
            {
                WIPDepDistParameters pars = entry.Key;
                Tuple<double, double> result = entry.Value;
                outputFile.WriteLine(pars.LBWIP + "," + pars.UBWIP + "," + pars.Tmin + "," + pars.Tmax + "," + pars.Tdecay + "," + pars.Cmin + "," + pars.Cmax + "," + pars.Cdecay
                    + "," + result.Item1 + "," + result.Item2);
            }

            // Write the best and current solution to a text file
            using StreamWriter outputFileBest = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_best_parameters.txt"));

            outputFileBest.WriteLine("Solution,LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");

            var first = currentPar.First();
            Distribution value = first.Value;
            EPTDistribution dist = (EPTDistribution)value;
            WIPDepDistParameters par = dist.Par;

            outputFileBest.WriteLine("Current," + par.LBWIP + "," + par.UBWIP + "," + par.Tmin + "," + par.Tmax + "," + par.Tdecay + "," + par.Cmin + "," + par.Cmax + "," + par.Cdecay
                    + "," + currentRes.Item1 + "," + currentRes.Item2);

            first = bestPar.First();
            value = first.Value;
            dist = (EPTDistribution)value;
            par = dist.Par;

            outputFileBest.WriteLine("Best," + par.LBWIP + "," + par.UBWIP + "," + par.Tmin + "," + par.Tmax + "," + par.Tdecay + "," + par.Cmin + "," + par.Cmax + "," + par.Cdecay
                    + "," + bestRes.Item1 + "," + bestRes.Item2);
            #endregion
        }
    }
}
