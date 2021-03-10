using CSSL.Utilities.Distributions;
using CSSL.Utilities.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            double pOthers = (1 - pTmax ) / 7;

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

        public void AddResult(Dictionary<WIPDepDistParameters, WeightedStatistic> results, Dictionary<string, Distribution> parameters, WeightedStatistic result)
        {
            var first = parameters.First();
            Distribution value = first.Value;
            EPTDistribution dist = (EPTDistribution)value;
            WIPDepDistParameters x = dist.Par;

            results.Add(x, result);
        }
    }
}
