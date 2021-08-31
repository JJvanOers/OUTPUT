using CSSL.Utilities;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Distributions
{
    public class EmpericalDistributionVariableSeed : EmpericalDistribution
    {
        public EmpericalDistributionVariableSeed(double[] records) : base(records)
        {
            
        }

        public void SetSeed(int seed)
        {
            rnd = new ExtendedRandom(seed);
        }

        public override double Next()
        {
            return Records[rnd.Next(0, Records.Length)];
        }

    }
}
