﻿using WaferFabSim.WaferFabElements.Dispatchers;
using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using System;
using System.Collections.Generic;
using System.Text;

namespace WaferFabSim.WaferFabElements.Observers
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
            DispatcherBase dispatcher = (DispatcherBase)modelElement;

            Lot lot = dispatcher.DepartingLot;
            jobOutCounter++;

            Writer?.WriteLine($"{dispatcher.GetDateTime},{lot.EndTime - lot.StartTime},{lot.GetCurrentStep.Name},{lot.LotID},{lot.ProductType},{lot.EndTime},{lot.StartTime},{lot.Sequence.TPTPrediction},{lot.GetCurrentSchedDev},{lot.HasPlanDay}");
            //Writer?.WriteLine($"{dispatcher.GetDateTime},{lot.EndTime - lot.StartTime},{lot.GetCurrentStep.Name},{lot.LotID},{lot.ProductType},{lot.EndTime},{lot.StartTime},{lot.CycleTimeReal},{lot.WIPInReal},{lot.WIPIn}");
        }


        protected override void OnInitialized(ModelElementBase modelElement)
        {
            Writer?.WriteLine("DateTime,CycleTime,IRDGroup,LotID,ProductType,EndTime,StartTime,PredictedCycleTime,ScheduleDeviation,HasPlanDay");
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
