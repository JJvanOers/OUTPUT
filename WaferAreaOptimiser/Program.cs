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
            string inputDirectory = @"C:\Users\nx015953\OneDrive - Nexperia\CSSLWaferFab\Input";

            string outputDirectory = @"C:\CSSLWaferFab\OptimiserOutput";

            ReaderWriter readerWriter = new ReaderWriter(inputDirectory, outputDirectory);

            Dictionary<string, Tuple<double, double>> realQueueLengths = readerWriter.GetRealQueueLengths();

            List<string> workCenters = realQueueLengths.Keys.ToList(); // All work centers

            workCenters = new List<string>() // Remove to evaluate all work centers
            {"LPCVD"};

            foreach (string workCenter in workCenters)
            {
                #region Parameters
                // Model parameters
                string wc = workCenter;

                DateTime initialDateTime = new DateTime(2019, 8, 1);

                bool useInitialLots = true;

                Settings.WriteOutput = false;

                // Simulated annealing parameters
                double temp = 25;

                double cooldown = 0.99; //0.995 = 1102 solutions, 0.996 = 1378 solutions, 0.997 = 1834 solutions

                double meanObj = realQueueLengths[wc].Item1;

                double stdObj = realQueueLengths[wc].Item2;

                // Dictionary with parameters to optimise and optional weights
                Dictionary<string, Parameter> parameterConfiguration = new Dictionary<string, Parameter>()
                {
                    {"LBWIP",   new Parameter("LBWIP", false)},
                    {"UBWIP",   new Parameter("UBWIP", false)},
                    {"Tmin",    new Parameter("Tmin", false)},                   
                    {"Tmax",    new Parameter("Tmax", true)},
                    {"Tdecay",  new Parameter("Tdecay", true)},
                    {"Cmin",    new Parameter("Cmin", true)},
                    {"Cmax",    new Parameter("Cmax", true)},
                    {"Cdecay",  new Parameter("Cdecay", true)}
                };
                #endregion

                #region Variables and instances
                Optimiser optimiser = new Optimiser(wc, temp, parameterConfiguration);

                optimiser.SetBounds(inputDirectory);

                WaferAreaSim waferAreaSim = new WaferAreaSim(wc, inputDirectory, outputDirectory, initialDateTime, optimiser, useInitialLots);

                Dictionary<string, Distribution> currentPar, nextPar, bestPar;

                Tuple<double, double> currentRes, nextRes, bestRes;

                double currentCost, nextCost, bestCost, deltaCost;

                Dictionary<WIPDepDistParameters, Tuple<double, double>> results = new Dictionary<WIPDepDistParameters, Tuple<double, double>>(); // Save all solutions

                UniformDistribution uDist = new UniformDistribution(0, 1);
                #endregion

                #region Simulated annealing algorithm
                // Initial model parameters and results
                currentPar = waferAreaSim.InitialParameters;
                bestPar = optimiser.CopyParameters(currentPar);

                currentRes = waferAreaSim.RunSim(currentPar);
                bestRes = optimiser.CopyResults(currentRes);

                currentCost = Math.Abs(currentRes.Item1 - meanObj) + 0.5 * Math.Abs(currentRes.Item2 - stdObj);
                bestCost = currentCost;

                optimiser.AddResult(results, currentPar, currentRes);

                // Iterate and evaluate solutions until sufficiently cooled down
                int i = 0;
                while (temp > 0.1 && currentCost > Math.Min(1, 0.1 * (meanObj + stdObj))) // If a good solution is found, stop searching
                {
                    nextPar = optimiser.GenerateNeighbour(currentPar, temp);
                    nextRes = waferAreaSim.RunSim(nextPar);

                    nextCost = Math.Abs(nextRes.Item1 - meanObj) + 0.5 * Math.Abs(nextRes.Item2 - stdObj);

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
                    }
                    else
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

                    Console.WriteLine("\nResults for area {0}.", wc);
                    Console.WriteLine("Iteration: {0}. Temperature {1}", i, temp);
                    Console.WriteLine("Evaluated solution: {0}, {1}", nextRes.Item1, nextRes.Item2);
                    Console.WriteLine("Current solution:   {0}, {1}", currentRes.Item1, currentRes.Item2);
                    Console.WriteLine("Best solution:      {0}, {1}\n", bestRes.Item1, bestRes.Item2);
                }
                #endregion

                #region Write results to file
                // Write all results to a text file
                readerWriter.WriteAllSolutions(results, wc);

                // Write the best and current solution to a text file
                readerWriter.WriteFinalSolutions(currentPar, currentRes, bestPar, bestRes, wc);
                #endregion
            }
        }
    }
}
