using CSSL.Modeling;
using CSSL.Utilities.Distributions;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WaferFabSim;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Utilities;
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;

namespace WaferAreaOptimiser
{
    public class Optimiser
    {
        private string wc;

        private double maxTemp;

        public Optimiser(string wc, double maxTemp)
        {
            this.wc = wc;

            this.maxTemp = maxTemp;
        }

        UniformDistribution uDist = new UniformDistribution(0, 1);

        public Dictionary<string, Tuple<double, double>> GetRealQueueLengths(string directory)
        {
            Dictionary<string, Tuple<double, double>> realQueueLengths = new Dictionary<string, Tuple<double, double>>();

            using (StreamReader reader = new StreamReader(Path.Combine(directory, "CSVs", "RealQueueLengths.csv")))
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

        public Dictionary<string, Distribution> GenerateRandomParameters()
        {
            WIPDepDistParameters par = new WIPDepDistParameters
            {
                WorkCenter = wc,
                LBWIP = (int)(uDist.Next() * 60 + 194), // int
                UBWIP = (int)(uDist.Next() * 400 + 1300), // int
                Tmin = uDist.Next() * 400 + 1440, // double, Minimum flow time empty system
                Tmax = uDist.Next() * 30 + 96, // EPT full system
                Tdecay = uDist.Next() * 0.01 + 0.02,
                Cmin = uDist.Next() * 0.075 + 0.1,
                Cmax = uDist.Next() * 0.2 + 0.78,
                Cdecay = uDist.Next() * 0.005 + 0.014,
            };

            Dictionary<string, Distribution> dict = new Dictionary<string, Distribution> { { wc, new EPTDistribution(par) } };

            return dict;
        }

        public Dictionary<string, Distribution> GenerateNeighbour(Dictionary<string, Distribution> currentPar, double temp)
        {
            double pTmax = 0.5; // Probability to create neighbour by changing Tmax.

            double pOthers = (1 - pTmax) / 3;

            double u = uDist.Next();

            EPTDistribution dist = (EPTDistribution)currentPar.First().Value;
            WIPDepDistParameters x = dist.Par;

            WIPDepDistParameters par = new WIPDepDistParameters { WorkCenter = wc };

            // x is the original parameter set (input), par is a neighbouring parameter set
            // Change one parameter based on a probability
            if (/*u < pOthers*/ false)                              { par.LBWIP = (int)Math.Max(1, newValue(x.LBWIP)); } else { par.LBWIP = x.LBWIP; }
            if (/* u >= pOthers && u < 2 * pOthers */ false)        { par.UBWIP = (int)Math.Max(1, newValue(x.UBWIP)); } else { par.UBWIP = x.UBWIP; }
            if (/* u >= 0 * pOthers && u < 1 * pOthers */ false)    { par.Tmin = newValue(x.Tmin); } else { par.Tmin = x.Tmin; }
            if (u >= 0 * pOthers && u < 1 * pOthers)                { par.Tdecay = newValue(x.Tdecay); } else { par.Tdecay = x.Tdecay; }
            if (u >= 1 * pOthers && u < 2 * pOthers)                { par.Cmin = newValue(x.Cmin); } else { par.Cmin = x.Cmin; }
            if (/* u >= 3 * pOthers && u < 4 * pOthers */ false)    { par.Cmax = newValue(x.Cmax); } else { par.Cmax = x.Cmax; }
            if (u >= 2 * pOthers && u < 3 * pOthers)                { par.Cdecay = newValue(x.Cdecay); } else { par.Cdecay = x.Cdecay; }
            if (u >= 3 * pOthers)                                   { par.Tmax = newValue(x.Tmax); } else { par.Tmax = x.Tmax; }

            Dictionary<string, Distribution> neighbour = new Dictionary<string, Distribution> { { wc, new EPTDistribution(par) } };

            return neighbour;

            double newValue(double value) 
            {
                // Use Min-max feature scaling to determine the range of the new value
                // Large range at high temps, small range at low temps
                double range = ((0.5 - 0.1) * temp) / maxTemp + 0.1;
                
                double newValue = value - range * value + 2 * range * value * uDist.Next();
                
                newValue = Math.Max(0.0001, newValue);

                return newValue;
            }
        }

        public Dictionary<string, Distribution> CopyParameters(Dictionary<string, Distribution> parameters)
        {
            EPTDistribution dist = (EPTDistribution)parameters.First().Value;
            WIPDepDistParameters x = dist.Par;

            WIPDepDistParameters pars = new WIPDepDistParameters
            {
                LBWIP = x.LBWIP,
                UBWIP = x.UBWIP,
                Tmin = x.Tmin,
                Tmax = x.Tmax,
                Tdecay = x.Tdecay,
                Cmin = x.Cmin,
                Cmax = x.Cmax,
                Cdecay = x.Cdecay
            };

            Dictionary<string, Distribution> copiedParameters = new Dictionary<string, Distribution>
            {
                { wc, new EPTDistribution(pars) }
            };

            return copiedParameters;
        }

        public Tuple<double, double> CopyResults(Tuple<double, double> result)
        {
            Tuple<double, double> copiedResult = new Tuple<double, double>(result.Item1, result.Item2);

            return copiedResult;
        }

        public void AddResult(Dictionary<WIPDepDistParameters, Tuple<double, double>> results, Dictionary<string, Distribution> parameters, Tuple<double, double> result)
        {
            EPTDistribution dist = (EPTDistribution)parameters.First().Value;
            WIPDepDistParameters x = dist.Par;

            results.Add(x, result);
        }

        public List<Lot> CopyInitialLots(List<Lot> initialLots)
        {
            List<Lot> copiedInitialLots = new List<Lot>();

            Lot deepCopiedLot;

            foreach (Lot lot in initialLots)
            {
                deepCopiedLot = new Lot(lot);

                copiedInitialLots.Add(lot);
            }

            return copiedInitialLots;
        }

        public List<Lot> GetInitialLots(string wc, string inputDirectory, string outputDirectory, DateTime initialDateTime, WaferFabSettings waferFabSettings)
        {
            Simulation simulation = new Simulation("CSSLWaferFabArea", outputDirectory);

            WaferFab waferFab = new WaferFab(simulation.MyModel, "WaferFab", new ConstantDistribution(60 * 60 * 24), initialDateTime);

            WorkCenter workCenter = new WorkCenter(waferFab, $"WorkCenter_{wc}", waferFabSettings.WCServiceTimeDistributions[wc], waferFabSettings.LotStepsPerWorkStation[wc]);

            // Sequences
            foreach (var sequence in waferFabSettings.Sequences)
            {
                waferFab.AddSequence(sequence.Key, sequence.Value);
            }

            // LotSteps
            waferFab.LotSteps = waferFab.Sequences.Select(x => x.Value).Select(x => x.GetCurrentStep(0)).ToDictionary(x => x.Name);

            // Read initial lots
            RealSnapshotReader reader = new RealSnapshotReader();

            List<RealSnapshot> realSnapshots = reader.Read(Path.Combine(inputDirectory, "SerializedFiles", reader.GetRealSnapshotString(initialDateTime)), 1);

            RealSnapshot realSnapShot = realSnapshots.Where(x => x.Time == initialDateTime).First();

            List<string> lotSteps = workCenter.LotSteps.Select(x => x.Name).ToList();

            List<RealLot> initialRealLots = realSnapShot.GetRealLots(1).Where(x => lotSteps.Contains(x.IRDGroup)).ToList();

            List<Lot> initialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences, initialDateTime)).ToList();

            waferFab.InitialLots = initialLots;

            return initialLots;
        }
    }
}
