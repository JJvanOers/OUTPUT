using CSSL.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class LotStep : IIdentity, IName
    {
        public WorkCenter WorkCenter { get; private set; }
        public double PlanDay { get; private set; }
        public double ProdPrediction { get; private set; }
        public double TechPrediction { get; private set; }

        public int Id { get; }

        public string Name { get; }

        public void SetWorkCenter(WorkCenter workCenter)
        {
            WorkCenter = workCenter;
        }

        public void SetTPTPredictions(double planDay, double prodPrediction, double techPrediction)
        {
            PlanDay = planDay; //days
            ProdPrediction = prodPrediction; //days (with decimals)
            TechPrediction = techPrediction; //days (with decimals)
        }

        public LotStep(int id, string name)
        {
            Id = id;
            Name = name;
            PlanDay = 0;
            ProdPrediction = double.NaN;
            TechPrediction = double.NaN;
        }

        public LotStep()
        {
        }
    }
}
