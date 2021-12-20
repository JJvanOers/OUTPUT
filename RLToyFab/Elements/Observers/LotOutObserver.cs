using WaferFabSim.WaferFabElements.Dispatchers;
using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;
using RLToyFab.Elements;

namespace RLToyFab.Observers
{
    /// <summary>
    /// Observer to report lot-out data when a lot is finished with production. Subscribe this to 
    /// an implementation of DispatcherBase.
    /// </summary>
    public class LotOutObserver : ModelElementObserverBase
    {
        public LotOutObserver(Simulation mySimulation, string name) : base(mySimulation, name)
        {
        }

        private int jobOutCounter = 0;

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            RLToyFab.Elements.Dispatchers.DispatcherBase dispatcher = (RLToyFab.Elements.Dispatchers.DispatcherBase)modelElement;
            WorkStation ws = (WorkStation)dispatcher.Parent;

            Lot lot = ws.DepartingLot;
            jobOutCounter++;

            //bool HasPlanDay = lot.Sequence.TimeTillLotOut[0][0] != null;
            
            //if (lot.StartTimeReal != null) // exclude initial lots without a starting time
            //{
                //if (!HasPlanDay) { Console.WriteLine($"WARNING: Lot {lot.LotID} of producttype {lot.ProductType} is not using the PlanDay prediction for CLIP-day. Continuing with product or technology prediction."); }
            Writer?.WriteLine($"{ws.GetDateTime},{lot.EndTime - lot.StartTime},{lot.GetCurrentStep.Name},{lot.LotID},{lot.ProductType},{lot.EndTime},{lot.StartTime},{lot.Sequence.StandardTPT}");//,{lot.ClipDayDeviation},{lot.GetCurrentSchedDev()},{HasPlanDay}"); 
            //}
            //Writer?.WriteLine($"{dispatcher.GetDateTime},{lot.EndTime - lot.StartTime},{lot.GetCurrentStep.Name},{lot.LotID},{lot.ProductType},{lot.EndTime},{lot.StartTime},{lot.CycleTimeReal},{lot.WIPInReal},{lot.WIPIn}");
        }


        protected override void OnInitialized(ModelElementBase modelElement)
        {
            Writer?.WriteLine("DateTime,CycleTime,IRDGroup,LotID,ProductType,EndTime,StartTime,PredictedCycleTime");//,ScheduleDeviation,SchedDevCurrent,HasPlanDay");
            //Writer?.WriteLine("DateTime,CycleTime,IRDGroup,LotID,ProductType,EndTime,StartTime,OriginalCycleTime,WIPInReal,WIPIn");
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }

        public override void OnError(Exception error)
        {
        }

        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }


        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            double totalTimeInDays = GetTime / 24 / 60 / 60;
            Writer?.WriteLine($"Throughput,{jobOutCounter/ totalTimeInDays},JobOuts,{jobOutCounter},Days,{totalTimeInDays}");
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            jobOutCounter = 0;
        }


    }
}
