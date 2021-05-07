using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferAreaOptimiser
{
    public class Parameter
    {
        public Parameter(string name, bool isOptimised, double weight = 1)
        {
            Name = name;
            IsOptimised = isOptimised;
            if (isOptimised)
            {
                Weight = weight;
                TotalWeight += Weight;
                CumulativeWeight = TotalWeight;
            }
        }

        public string Name { get; set; }

        public bool IsOptimised { get; set; }

        public double LowerBound { get; set; } = double.NegativeInfinity;

        public double UpperBound { get; set; } = double.PositiveInfinity;

        public double Weight { get; private set; } = 0;

        public double CumulativeWeight { get; private set; }

        public static double TotalWeight { get; private set; }
    }
}
