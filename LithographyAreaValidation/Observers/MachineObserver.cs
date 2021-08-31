using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Observers
{
    public class MachineObserver : ModelElementObserverBase
    {
        public MachineObserver(Simulation mySimulation, DateTime startDate) : base(mySimulation)
        {
            this.startDateRun = startDate;
        }

        private DateTime startDateRun;

        public override void OnError(Exception error)
        {
        }

        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnInitialized(ModelElementBase modelElement)
        {
            Writer.WriteLine($"LotID,StartRun,EndRun,Resource,IRDName,Maskset_Layer,Lateness");
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            // Get machine object
            Machine machine = (Machine)modelElement;

            // Get values
            string lotID = machine.CurrentLot.LotID;
            string irdName = machine.CurrentLot.IrdName;
            string reticle = machine.CurrentLot.MasksetLayer;

            DateTime startRun = startDateRun.AddSeconds(machine.CurrentStartRun);
            string startRunString = startRun.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime endRun = startDateRun.AddSeconds(machine.CurrentEndRun);
            string endRunString = endRun.ToString("yyyy-MM-dd HH:mm:ss");

            double lateness = Math.Ceiling((machine.CurrentLot.ImprovedDueDate.Subtract(endRun)).TotalDays);
            string resource = machine.Name;

            // Write
            Writer.WriteLine($"{lotID},{startRunString},{endRunString},{resource},{irdName},{reticle},{lateness}");
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
    }
}
