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

        public Dictionary<string, Parameter> ParConfig;

        public Optimiser(string wc, double maxTemp, Dictionary<string, Parameter> parConfig)
        {
            this.wc = wc;

            this.maxTemp = maxTemp;

            ParConfig = parConfig;
        }

        public void SetBounds(string directory)
        {
            using (StreamReader reader = new StreamReader(Path.Combine(directory, "CSVs", "Bounds.csv")))
            {
                string[] headers = reader.ReadLine().Trim(',').Split(',');
                
                while (!reader.EndOfStream)
                {
                    string[] data = reader.ReadLine().Trim(',').Split(',');

                    if (data[0] == wc)
                    {
                        for (int i = 1; i < data.Length; i++)
                        {
                            if (headers[i] == "Tmin_lb") { ParConfig["Tmin"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Tmin_ub") { ParConfig["Tmin"].UpperBound = double.Parse(data[i]); }
                            if (headers[i] == "Tmax_lb") { ParConfig["Tmax"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Tmax_ub") { ParConfig["Tmax"].UpperBound = double.Parse(data[i]); }
                            if (headers[i] == "Tdecay_lb") { ParConfig["Tdecay"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Tdecay_ub") { ParConfig["Tdecay"].UpperBound = double.Parse(data[i]); }
                            if (headers[i] == "Cmin_lb") { ParConfig["Cmin"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Cmin_ub") { ParConfig["Cmin"].UpperBound = double.Parse(data[i]); }
                            if (headers[i] == "Cmax_lb") { ParConfig["Cmax"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Cmax_ub") { ParConfig["Cmax"].UpperBound = double.Parse(data[i]); }
                            if (headers[i] == "Cdecay_lb") { ParConfig["Cdecay"].LowerBound = double.Parse(data[i]); }
                            if (headers[i] == "Cdecay_ub") { ParConfig["Cdecay"].UpperBound = double.Parse(data[i]); }
                        }
                    }
                }
            }
        }

        public EPTDistribution CheckInitialDistBounds(EPTDistribution dist)
        {
            WIPDepDistParameters x = dist.Par;

            // For each initial value, check if it is within bounds. If not, set to closest bound
            if      (x.Tmin < ParConfig["Tmin"].LowerBound) { x.Tmin = ParConfig["Tmin"].LowerBound; }
            else if (x.Tmin > ParConfig["Tmin"].UpperBound) { x.Tmin = ParConfig["Tmin"].UpperBound; }

            if      (x.Tmax < ParConfig["Tmax"].LowerBound) { x.Tmax = ParConfig["Tmax"].LowerBound; }
            else if (x.Tmax > ParConfig["Tmax"].UpperBound) { x.Tmax = ParConfig["Tmax"].UpperBound; }

            if      (x.Tdecay < ParConfig["Tdecay"].LowerBound) { x.Tdecay = ParConfig["Tdecay"].LowerBound; }
            else if (x.Tdecay > ParConfig["Tdecay"].UpperBound) { x.Tdecay = ParConfig["Tdecay"].UpperBound; }


            if      (x.Cmin < ParConfig["Cmin"].LowerBound) { x.Cmin = ParConfig["Cmin"].LowerBound; }
            else if (x.Cmin > ParConfig["Cmin"].UpperBound) { x.Cmin = ParConfig["Cmin"].UpperBound; }

            if      (x.Cmax < ParConfig["Cmax"].LowerBound) { x.Cmax = ParConfig["Cmax"].LowerBound; }
            else if (x.Cmax > ParConfig["Cmax"].UpperBound) { x.Cmax = ParConfig["Cmax"].UpperBound; }

            if      (x.Cdecay < ParConfig["Cdecay"].LowerBound) { x.Cdecay = ParConfig["Cdecay"].LowerBound; }
            else if (x.Cdecay > ParConfig["Cdecay"].UpperBound) { x.Cdecay = ParConfig["Cdecay"].UpperBound; }

            EPTDistribution newDist = new EPTDistribution(x);

            return newDist;
        }

        public Dictionary<string, Distribution> GenerateNeighbour(Dictionary<string, Distribution> currentPar, double temp)
        {
            EPTDistribution dist = (EPTDistribution)currentPar.First().Value;
            WIPDepDistParameters x = dist.Par;

            WIPDepDistParameters par = new WIPDepDistParameters { WorkCenter = wc };

            UniformDistribution parDist = new UniformDistribution(0, Parameter.TotalWeight);
            double u = parDist.Next();

            // x is the original parameter set (input), par is a neighbouring parameter set
            // Change one parameter based on a probability
            if (isInRange("LBWIP", u))  { par.LBWIP = (int)Math.Max(1, newValue("LBWIP", x.LBWIP)); }    else { par.LBWIP = x.LBWIP; }
            if (isInRange("UBWIP", u))  { par.UBWIP = (int)Math.Max(1, newValue("UBWIP", x.UBWIP)); }    else { par.UBWIP = x.UBWIP; }
            if (isInRange("Tmin", u))   { par.Tmin = newValue("Tmin", x.Tmin); }                         else { par.Tmin = x.Tmin; }
            if (isInRange("Tmax", u))   { par.Tmax = newValue("Tmax", x.Tmax); }                         else { par.Tmax = x.Tmax; }
            if (isInRange("Tdecay", u)) { par.Tdecay = newValue("Tdecay", x.Tdecay); }                   else { par.Tdecay = x.Tdecay; }
            if (isInRange("Cmin", u))   { par.Cmin = newValue("Cmin", x.Cmin); }                         else { par.Cmin = x.Cmin; }
            if (isInRange("Cmax", u))   { par.Cmax = newValue("Cmax", x.Cmax); }                         else { par.Cmax = x.Cmax; }
            if (isInRange("Cdecay", u)) { par.Cdecay = newValue("Cdecay", x.Cdecay); }                   else { par.Cdecay = x.Cdecay; }

            Dictionary<string, Distribution> neighbour = new Dictionary<string, Distribution> { { wc, new EPTDistribution(par) } };

            return neighbour;

            bool isInRange(string parName, double u)
            {
                Parameter parameter = ParConfig[parName];
                double pLower = parameter.CumulativeWeight - parameter.Weight;
                double pUpper = parameter.CumulativeWeight;

                if (u > pLower && u <= pUpper) { return true; } else { return false; }
            }

            double newValue(string parName, double value)
            {                
                // Use Min-max feature scaling to determine the half width size of the new value
                // Large range at high temps (50%), small range at low temps (10%)
                double halfWidth = (0.5 - 0.1) * temp / maxTemp + 0.1;

                Parameter parameter = ParConfig[parName];

                double lowerBound = Math.Max(parameter.LowerBound, value - value * halfWidth);
                double upperBound = Math.Min(parameter.UpperBound, value + value * halfWidth);

                UniformDistribution valueDist = new UniformDistribution(lowerBound, upperBound);

                return valueDist.Next();
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

        public List<Lot> GetInitialLots(string wc, string inputDirectory, string outputDirectory, DateTime initialDateTime, WaferFabSettings waferFabSettings)
        {
            // Create an instance of a simulation class to retrieve lot steps
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

            List<RealLot> initialRealLots = realSnapShot.GetRealLots(1).Where(x => x.LotActivity.WorkStation == wc).ToList();

            List<Lot> initialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences, initialDateTime)).ToList();

            return initialLots;
        }      
    }
}
