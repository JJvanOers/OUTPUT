using WaferFabSim.WaferFabElements.Dispatchers;
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
        protected override void OnUpdate(ModelElementBase modelElement)
        {
            DispatcherBase dispatcher = (DispatcherBase)modelElement;

            Lot lot = dispatcher.DepartingLot;

            if (lot.StartTime > 0 &&  lot.EndTime == 0)
            {
                int stop = 0;
            }
            Writer?.WriteLine($"{dispatcher.GetDateTime},{lot.EndTime - lot.StartTime},{lot.GetCurrentStep.Name},{lot.LotID},{lot.ProductType},{lot.EndTime},{lot.StartTime},{lot.CycleTimeTotalReal},{lot.WIPInReal},{lot.WIPIn}");
        }


        protected override void OnInitialized(ModelElementBase modelElement)
        {
            Writer?.WriteLine("DateTime,CycleTime,IRDGroup,LotID,ProductType,EndTime,StartTime,CycleTimeReal,WIPInReal,WIPIn");
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
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
        }


    }
}
