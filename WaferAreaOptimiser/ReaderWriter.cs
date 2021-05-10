using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements.Utilities;
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;

namespace WaferAreaOptimiser
{
    public class ReaderWriter
    {
        private string inputDirectory;

        private string outputDirectory;

        public ReaderWriter(string inputDirectory, string outputDirectory)
        {
            this.inputDirectory = inputDirectory;

            this.outputDirectory = outputDirectory;
        }


        public Dictionary<string, Tuple<double, double>> GetRealQueueLengths()
        {
            Dictionary<string, Tuple<double, double>> realQueueLengths = new Dictionary<string, Tuple<double, double>>();

            using (StreamReader reader = new StreamReader(Path.Combine(inputDirectory, "CSVs", "RealQueueLengths.csv")))
            {
                string[] headers = reader.ReadLine().Trim(',').Split(',');
                string workCenter = "";
                double mean = -1;
                double std = -1;

                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Trim(',').Split(',');

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (headers[i] == "WorkStation") { workCenter = data[i]; }
                        if (headers[i] == "Mean") { mean = double.Parse(data[i]); }
                        if (headers[i] == "Std") { std = double.Parse(data[i]); }
                    }
                    realQueueLengths.Add(workCenter, new Tuple<double, double>(mean, std));
                }
            }

            return realQueueLengths;
        }


        /// <summary>
        /// Write all evaluated paramater configurations to file
        /// </summary>
        public void WriteAllSolutions(Dictionary<WIPDepDistParameters, Tuple<double, double>> solutions, string wc)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_parameters.txt")))
            {
                writer.WriteLine("LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");

                foreach (KeyValuePair<WIPDepDistParameters, Tuple<double, double>> solution in solutions)
                {
                    WIPDepDistParameters pars = solution.Key;
                    Tuple<double, double> result = solution.Value;
                    writer.WriteLine(pars.LBWIP + "," + pars.UBWIP + "," + pars.Tmin + "," + pars.Tmax + "," + pars.Tdecay + "," + pars.Cmin + "," + pars.Cmax + "," + pars.Cdecay
                        + "," + result.Item1 + "," + result.Item2);
                }
            }
        }

        /// <summary>
        /// Write current and best parameter configurations to file.
        /// </summary>
        public void WriteFinalSolutions(Dictionary<string, Distribution> currentPar, Tuple<double, double> currentRes,
            Dictionary<string, Distribution> bestPar, Tuple<double, double> bestRes, string wc)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(outputDirectory, $"{wc}_best_parameters.txt")))
            {
                writer.WriteLine("Parameters,LBWIP,UBWIP,Tmin,Tmax,Tdecay,Cmin,Cmax,Cdecay,AverageQL,StdQL");
       
                EPTDistribution dist = (EPTDistribution)currentPar.First().Value;
                WIPDepDistParameters par = dist.Par;
                 

                writer.WriteLine($"Current,{par.LBWIP},{par.UBWIP},{par.Tmin},{par.Tmax},{par.Tdecay},{par.Cmin},{par.Cmax},{par.Cdecay},{currentRes.Item1},{currentRes.Item2}");

                dist = (EPTDistribution)bestPar.First().Value;
                par = dist.Par;

                writer.WriteLine($"Best,{par.LBWIP},{par.UBWIP},{par.Tmin},{par.Tmax},{par.Tdecay},{par.Cmin},{par.Cmax},{par.Cdecay},{bestRes.Item1},{bestRes.Item2}");
            }
        }
    }
}
