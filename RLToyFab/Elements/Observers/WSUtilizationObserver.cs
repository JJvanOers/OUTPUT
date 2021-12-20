using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using RLToyFab.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements;

namespace RLToyFab.Observers
{
    public class WSUtilizationObserver : ModelElementObserverBase
    {
        public WSUtilizationObserver(Simulation mySimulation, string name, WorkStation ws) : base(mySimulation, name)
        {
            utilizations = new Dictionary<Machine, Variable<int>>();
            utilStatistics = new Dictionary<Machine, WeightedStatistic>();
            wsCapacity = new Variable<int>(this);
            wsCapacityStat = new WeightedStatistic("TotalUtilization");

            orderedMachines = ws.Machines.OrderBy(x => x.Id).ToList();

            foreach (Machine machine in orderedMachines)
            {
                utilizations.Add(machine, new Variable<int>(this));
                utilStatistics.Add(machine, new WeightedStatistic("Utilization_" + machine.Name));
            }
        }

        private Dictionary<Machine, Variable<int>> utilizations;

        private Dictionary<Machine, WeightedStatistic> utilStatistics;

        private Variable<int> wsCapacity;

        private WeightedStatistic wsCapacityStat; 

        private List<Machine> orderedMachines;

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            WorkStation ws = (WorkStation)modelElement;

            foreach (var queueLength in utilizations.Values)
            {
                queueLength.Reset();
            }

            foreach (var queueLengthStatistic in utilStatistics.Values)
            {
                queueLengthStatistic.Reset();
            }
            wsCapacity.Reset();
            wsCapacityStat.Reset();
            headerToFile(ws);
        }
        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
        protected override void OnInitialized(ModelElementBase modelElement)
        {
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            WorkStation ws = (WorkStation)modelElement;
            if (ws.GetTime < 5*24*3600) //temporary fix for warmup time
            {
                foreach (var queueLength in utilizations.Values)
                {
                    queueLength.Reset();
                }
                foreach (var queueLengthStatistic in utilStatistics.Values)
                {
                    queueLengthStatistic.Reset();
                }
                wsCapacity.Reset();
                wsCapacityStat.Reset();
            }
            else
            {
                foreach (Machine machine in orderedMachines)
                {
                    utilizations[machine].UpdateValue((machine.LotInService == null) ? 0 : 1);
                    utilStatistics[machine].Collect(utilizations[machine].PreviousValue, utilizations[machine].Weight);
                }
                //wsCapacity.UpdateValue((ws.NrFreeMachines > 0) ? 0 : 1);
                wsCapacity.UpdateValue(1 - ws.NrFreeMachines / ws.Machines.Count);
                wsCapacityStat.Collect(wsCapacity.PreviousValue, wsCapacity.Weight);
                //writeOutputToFile(ws);
            }
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            WorkStation ws = (WorkStation)modelElement;
            foreach (Machine machine in orderedMachines)
            {
                utilizations[machine].UpdateValue((machine.LotInService == null) ? 0 : 1);
                utilStatistics[machine].Collect(utilizations[machine].PreviousValue, utilizations[machine].Weight);
            }
            //wsCapacity.UpdateValue((ws.NrFreeMachines > 0) ? 0 : 1);
            wsCapacity.UpdateValue(1 - ws.NrFreeMachines / ws.Machines.Count);
            wsCapacityStat.Collect(wsCapacity.PreviousValue, wsCapacity.Weight);

            writeOutputToFile(ws);
            //writeUtilToWaferFab(ws);
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(WorkStation ws)
        {
            Writer?.Write("Simulation Time,");
            Writer?.Write("TotalCapacity,");
            foreach (Machine machine in orderedMachines)
            {
                string[] words = machine.Name.Split('_');
                Writer?.Write($"{words.Last()},");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToFile(WorkStation ws)
        {
            Writer?.Write(ws.GetTime + ",");
            Writer?.Write(wsCapacityStat.Average() + ",");
            foreach (Machine machine in orderedMachines)
            {
                //Writer?.Write(utilizations[machine].Value + ",");
                Writer?.Write(utilStatistics[machine].Average() + ",");
            }
            Writer?.Write("\n");
        }

        //private void writeUtilToWaferFab(WorkStation ws)
        //{
        //    ToyFab toyFab = (ToyFab)ws.Parent;
        //    int maxMachines = toyFab.WorkStations.Values.Select(x => x.Machines.Count).OrderByDescending(x=>x).First();
        //    string writeString = "";
        //    writeString += wsCapacityStat.Average() + ",";
        //    foreach (Machine machine in orderedMachines)
        //    {
        //        writeString += utilStatistics[machine].Average() + ",";
        //        if (utilStatistics.Count < maxMachines) writeString += new String(',', maxMachines-utilStatistics.Count);
        //    }
        //    toyFab.WSUtilizations.Add(ws, writeString);
        //}

        private void writeOutputToConsole(WorkStation ws)
        {
            Console.Write(ws.GetTime + "," + ws.GetWallClockTime + ",");

            foreach (Machine machine in orderedMachines)
            {
                Console.Write($"{machine.Name} " + utilizations[machine].Value + ",");
            }
            Console.Write("\n");
        }
    }
}
