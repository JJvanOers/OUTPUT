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
            string wc = "PHOTOLITH";

            string inputDirectory = @"C:\CSSLWaferFabArea\Input";

            string outputDirectory = @"C:\CSSLWaferFabArea\Output";
                    
            WaferAreaSim waferAreaSim = new WaferAreaSim(wc, inputDirectory, outputDirectory);

            Optimiser optimiser = new Optimiser(wc);

            // Simulated Annealing
            // SA parameters
            double temp = 25;

            double cooldown = 0.999;

            double objective = 701.8146597028681;

            // Variables and functions
            Dictionary<string, Distribution> currentPar, nextPar, bestPar;

            WeightedStatistic currentRes, nextRes, bestRes;

            double currentCost, nextCost, bestCost, deltaCost;

            Dictionary<WIPDepDistParameters, WeightedStatistic> results = new Dictionary<WIPDepDistParameters, WeightedStatistic>(); // Save all solutions

            UniformDistribution uDist = new UniformDistribution(0, 1);

            // Initial model parameters and results
            currentPar = bestPar = waferAreaSim.initialParameters;

            currentRes = bestRes = waferAreaSim.RunSim(currentPar);

            currentCost = bestCost = Math.Abs(currentRes.Average() - objective);

            optimiser.AddResult(results, currentPar, currentRes);
                      
            // Iterate and evaluate solutions until sufficiently cooled down
            while (temp > 0.1)
            {
                nextPar = optimiser.GenerateNeighbour(currentPar);
                
                nextRes = waferAreaSim.RunSim(nextPar);

                nextCost = Math.Abs(nextRes.Average() - objective);

                optimiser.AddResult(results, nextPar, nextRes);

                if (nextCost < currentCost) // New solution is better than current, accept new solution
                {
                    currentPar = nextPar;
                    currentRes = nextRes;
                    currentCost = nextCost;

                    if (nextCost < bestCost) // New solution is better best, accept new best solution
                    {
                        bestPar = nextPar;
                        bestRes = nextRes;
                        bestCost = nextCost;
                    }
                } else
                {
                    deltaCost = nextCost - currentCost;

                    if (uDist.Next() < Math.Pow(Math.E, -deltaCost / temp)) // Accept solution if u ~ U[0,1] < e^-(dC/T)
                    {
                        currentPar = nextPar;
                        currentRes = nextRes;
                        currentCost = nextCost;
                    }
                }

                temp = temp * cooldown; // Reduce temperature
            }

            //Dictionary<string, Distribution> parameters = optimiser.GenerateParameters();

            //Console.WriteLine("Average: {0} and standard deviation: {1}", results.Average(), results.StandardDeviation());

            using StreamWriter outputFile = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_parameters.txt"));

            outputFile.WriteLine("LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");

            foreach (KeyValuePair<WIPDepDistParameters, WeightedStatistic> entry in results)
            {
                WIPDepDistParameters x = entry.Key;
                WeightedStatistic y = entry.Value;
                outputFile.WriteLine(x.LBWIP + "," + x.UBWIP + "," + x.Tmin + "," + x.Tmax + "," + x.Tdecay + "," + x.Cmin + "," + x.Cmax + "," + x.Cdecay
                    + "," + y.Average() + "," + y.StandardDeviation());
            }
            
        }     
    }
}
