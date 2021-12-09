using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Elements
{
    public class Machine : SchedulingElementBase
    {
        public Machine(ModelElementBase parent, string name, Distribution serviceTimeDistribution, WorkStation workstation) : base(parent, name)
        {
            Eligibilities = new List<LotStep>();
            ServiceTimeDistribution = serviceTimeDistribution;
            WorkStation = workstation;
        }

        public WorkStation WorkStation { get;  }

        public List<LotStep> Eligibilities { get; set; }

        public Distribution ServiceTimeDistribution { get; }

        public Lot LotInService { get; set; }

        public void HandleArrival(Lot lot)
        {
            if (LotInService != null)
            {
                throw new Exception($"Lot arrived at Machine {Name}, while it is not idle.");
            }

            LotInService = lot;

            ScheduleEvent(GetTime + ServiceTimeDistribution.Next(), HandleDeparture);
            
        }

        public void HandleDeparture(CSSLEvent e)
        {
            LotInService.SendToNextWorkCenter();

            LotInService = null;
        }
    }
}
