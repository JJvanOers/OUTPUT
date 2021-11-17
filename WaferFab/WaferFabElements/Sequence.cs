using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class Sequence
    {
        public Sequence(string productType, string productGroup, List<LotStep> lotSteps)
        {
            ProductType = productType;
            ProductGroup = productGroup;
            this.lotSteps = lotSteps;
            TimeTillLotOut = new List<double?[]>();
        }

        public Sequence(string productType, string productGroup)
        {
            ProductType = productType;
            ProductGroup = productGroup;
            lotSteps = new List<LotStep>();
            TimeTillLotOut = new List<double?[]>();
        }

        public Sequence (LotStep lotStep)
        {
            ProductGroup = "SingleStep_" + lotStep.Name;
            ProductType = "SingleStep_" + lotStep.Name;
            lotSteps = new List<LotStep>();
            lotSteps.Add(lotStep);
            TimeTillLotOut = new List<double?[]>();
        }

        public string ProductGroup { get; }

        public string ProductType { get; }

        public List<LotStep> lotSteps { get; set; }

        public int stepCount => lotSteps.Count;

        // If available, use Plan day of first step after batching for TPT prediction. Otherwise use tech prediction. If nothing is given, throw error. 
        public double StandardTPT { get; private set; }
        public List<double?[]> TimeTillLotOut { get; private set; }

        public double RemainingTimePredictor(int currStepCount)
        {
            double? PlanDayBasedPredictor = TimeTillLotOut[currStepCount][0];
            double? TechBasedPredictor = TimeTillLotOut[currStepCount][1];
            double? ProductBasedPredictor = TimeTillLotOut[currStepCount][2];
            return ProductBasedPredictor ?? TechBasedPredictor ?? PlanDayBasedPredictor ?? throw new Exception($"No Remaining Process Time predictor given for {ProductType}");
        }


        public bool HasNextStep(int currentStepCount)
        {
            return currentStepCount + 1 < lotSteps.Count;
        }

        public bool HasRelativeStep(int currentStepCount, int relativeStepCount)
        {
            return currentStepCount + relativeStepCount >= 0 && currentStepCount + relativeStepCount < lotSteps.Count;
        }

        public LotStep GetCurrentStep(int currentStepCount)
        {
            return lotSteps[currentStepCount];
        }

        public LotStep GetNextStep(int currentStepCount)
        {
            if (HasNextStep(currentStepCount))
            {
                return lotSteps[currentStepCount + 1];
            }
            else
            {
                return null;
            }
        }

        public LotStep GetRelativeStep(int currentStepCount, int relativeStepCount)
        {
            if (HasRelativeStep(currentStepCount, relativeStepCount))
            {
                return lotSteps[currentStepCount + relativeStepCount];
            }
            else
            {
                return null;
            }
        }

        public WorkCenter GetCurrentWorkCenter(int currentStepCount)
        {
            return GetCurrentStep(currentStepCount).WorkCenter;
        }

        public WorkCenter GetNextWorkCenter(int currentStepCount)
        {
            if (HasNextStep(currentStepCount))
            {
                return GetNextStep(currentStepCount).WorkCenter;
            }
            else
            {
                return null;
            }
        }
        public WorkCenter GetRelativeWorkCenter(int currentStepCount, int relativeStepCount)
        {
            if (HasRelativeStep(currentStepCount, relativeStepCount))
            {
                return GetRelativeStep(currentStepCount, relativeStepCount).WorkCenter;
            }
            else
            {
                return null;
            }
        }

        public void AddStep(LotStep lotstep, double? PlanDay, double? TechPredictor, double? ProductPredictor, double? standardTPT =null)
        {
            TimeTillLotOut.Add(new double?[3] { PlanDay, TechPredictor, ProductPredictor });

            if (!lotSteps.Any())  // If this is the first step added
            {
                StandardTPT = standardTPT ?? RemainingTimePredictor(0);  // first chooses Plan Day to predict total Throughput Time, otherwise selects product or technology prediction of first step
            }
            if (standardTPT != TimeTillLotOut[0][0]) 
            { 
                Console.WriteLine($"WARNING: Standard TPT and first in Plan Day do not match for {ProductType}"); 
            }
            lotSteps.Add(lotstep);
            
            
        }


    }
}
