using CSSL.Modeling.CSSLQueue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.ModelElements
{
    public class Lot : CSSLQueueObject<Lot>
    {
        public Lot(string lotID, string irdName, double arrivalTime, DateTime arrivalTimeDate, DateTime dueDate, string speed, int lotQty, string recipeStepCluster, string recipeStandAlone, string masksetLayer, string reticleID1, string reticleID2, DateTime improvedDueDate) : base(arrivalTime)
        {
            // Set dueDate of lot
            
            LotID = lotID;
            IrdName = irdName;
            ArrivalTime = arrivalTimeDate;
            DueDate = dueDate;
            Speed = speed;
            LotQty = lotQty;
            RecipeStepCluster = recipeStepCluster;
            RecipeStandAlone = recipeStandAlone;
            MasksetLayer = masksetLayer;
            ReticleID1 = reticleID1;
            ReticleID2 = reticleID2;
            ImprovedDueDate = improvedDueDate;
            ArrivalTimeSeconds = arrivalTime;
            ScheduledTime = 0;

            WeightDueDate = 0;
            WeightWIPBalance = 0;
        }

        public double WeightDueDate { get; set; }
        public double WeightWIPBalance { get; set; }
        public double ScheduledTime { get; set; }
        public DateTime DueDate { get; }

        public DateTime ImprovedDueDate { get; }

        public string LotID { get; }
        public DateTime ArrivalTime { get; }

        public int LotQty { get; }

        public string IrdName { get; }

        public string RecipeStandAlone { get; }

        public string RecipeStepCluster { get; }

        public string MasksetLayer { get; }

        public string MasksetLayer_RecipeStepCluster
        {
            get
            {
                return $"{MasksetLayer}_{RecipeStepCluster}";
            }
        }

        public string ReticleID1 { get; }

        public string ReticleID2 { get; }

        public string Speed { get; }

        public double ArrivalTimeSeconds { get; }
    }
}
