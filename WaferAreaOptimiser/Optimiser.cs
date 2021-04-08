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
        
        public Optimiser(string wc)
        {
            this.wc = wc;
        }        

        UniformDistribution uDist = new UniformDistribution(0, 1);

        public Dictionary<string, Distribution> GenerateRandomParameters()
        {
            Dictionary<string, Distribution> dict = new Dictionary<string, Distribution>();

            WIPDepDistParameters Parameters = new WIPDepDistParameters
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

            dict.Add(wc, new EPTDistribution(Parameters));

            return dict;
        }

        public Dictionary<string, Distribution> GenerateNeighbour(Dictionary<string, Distribution> currentPar)
        {
            double pTmax = 0.3; // Probability to create neighbour by changing Tmax. Equal prob, pTmax = 1/8

            double pOthers = (1 - pTmax) / 7;

            double u = uDist.Next();

            var first = currentPar.First();
            Distribution value = first.Value;
            EPTDistribution dist = (EPTDistribution)value;
            WIPDepDistParameters x = dist.Par;

            WIPDepDistParameters par = new WIPDepDistParameters { WorkCenter = wc };

            // x is the original set of parameters (input), par is a neighbouring set of parameters
            // Change one parameter based on a probability
            if (u < pOthers)                            { par.LBWIP = (int)Math.Max(1, newValue(x.LBWIP)); } else { par.LBWIP = x.LBWIP; }
            if (u >= pOthers && u < 2 * pOthers)        { par.UBWIP = (int)Math.Max(1, newValue(x.UBWIP)); } else { par.UBWIP = x.UBWIP; }
            if (u >= 2 * pOthers && u < 3 * pOthers)    { par.Tmin = newValue(x.Tmin); }                     else { par.Tmin = x.Tmin; }
            if (u >= 3 * pOthers && u < 4 * pOthers)    { par.Tdecay = newValue(x.Tdecay); }                 else { par.Tdecay = x.Tdecay; }
            if (u >= 4 * pOthers && u < 5 * pOthers)    { par.Cmin = newValue(x.Cmin); }                     else { par.Cmin = x.Cmin; }
            if (u >= 5 * pOthers && u < 6 * pOthers)    { par.Cmax = newValue(x.Cmax); }                     else { par.Cmax = x.Cmax; }
            if (u >= 6 * pOthers && u < 7 * pOthers)    { par.Cdecay = newValue(x.Cdecay); }                 else { par.Cdecay = x.Cdecay; }
            if (u >= 7 * pOthers)                       { par.Tmax = newValue(x.Tmax); }                     else { par.Tmax = x.Tmax; }

            Dictionary<string, Distribution> nextPar = new Dictionary<string, Distribution>();

            nextPar.Add(wc, new EPTDistribution(par));

            return nextPar;
        }

        private double newValue(double value)
        {
            double newValue = value - 0.1 * value + 0.2 * value * uDist.Next();

            newValue = Math.Max(0.0001, newValue);

            return newValue;
        }

        public Dictionary<string, Distribution> CopyParameters(Dictionary<string, Distribution> parameters)
        {
            Distribution value = parameters.First().Value;
            EPTDistribution dist = (EPTDistribution)value;
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

            Dictionary<string, Distribution> copiedParameters = new Dictionary<string, Distribution>();

            copiedParameters.Add(wc, new EPTDistribution(pars));

            return copiedParameters;
        }

        public Tuple<double, double> CopyResults(Tuple<double, double> result)
        {
            Tuple<double, double> copiedResult = new Tuple<double, double>(result.Item1, result.Item2);

            return copiedResult;
        }

        public void AddResult(Dictionary<WIPDepDistParameters, Tuple<double, double>> results, Dictionary<string, Distribution> parameters, Tuple<double, double> result)
        {
            Distribution value = parameters.First().Value;
            EPTDistribution dist = (EPTDistribution)value;
            WIPDepDistParameters x = dist.Par;

            results.Add(x, result);
        }

        public List<Lot> copyInitialLots(List<Lot> initialLots)
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

            #region Read initial lots
            RealSnapshotReader reader = new RealSnapshotReader();

            List<RealSnapshot> realSnapshots = reader.Read(Path.Combine(inputDirectory, "SerializedFiles", reader.GetRealSnapshotString(initialDateTime)), 25);

            RealSnapshot realSnapShot = realSnapshots.Where(x => x.Time == initialDateTime).First();

            List<string> lotSteps = workCenter.LotSteps.Select(x => x.Name).ToList();
            //List<string> lotSteps = waferFabSettings.Sequences.Select(x => x.Value.GetCurrentStep(0).Name).ToList();

            List<RealLot> initialRealLots = realSnapShot.RealLots.Where(x => lotSteps.Contains(x.IRDGroup)).ToList();

            List<Lot> initialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences)).ToList();
            #endregion

            return initialLots;
        }
    }
}
