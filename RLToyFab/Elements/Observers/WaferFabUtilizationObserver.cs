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
    public class WaferFabUtilizationObserver : ModelElementObserverBase
    {
        public WaferFabUtilizationObserver(Simulation mySimulation, string name, ToyFab toyFab) : base(mySimulation, name)
        {
            utilizations = new Dictionary<WorkStation, Dictionary<Machine, Variable<int>>>();
            utilStatistics = new Dictionary<WorkStation, Dictionary<Machine, WeightedStatistic>>();
            wsCapacity = new Dictionary<WorkStation, Variable<int>>();
            wsCapacityStat = new Dictionary<WorkStation, WeightedStatistic>();

            orderedWorkstations = toyFab.WorkStations.Values.OrderBy(x => x.Id).ToList();
            maxMachines = 0;
            foreach (WorkStation ws in orderedWorkstations)
            {
                utilizations.Add(ws, new Dictionary<Machine, Variable<int>>());
                utilStatistics.Add(ws, new Dictionary<Machine, WeightedStatistic>());
                wsCapacity.Add(ws, new Variable<int>(this));
                wsCapacityStat.Add(ws, new WeightedStatistic(ws.Name + "_utilization"));

                foreach (Machine mach in ws.Machines)
                {
                    utilizations[ws].Add(mach, new Variable<int>(this));
                    utilStatistics[ws].Add(mach, new WeightedStatistic(mach.Name + "_utilization"));
                }

                if (ws.Machines.Count > maxMachines) maxMachines = ws.Machines.Count;
            }
        }

        private Dictionary<WorkStation, Dictionary<Machine, Variable<int>>> utilizations;

        private Dictionary<WorkStation, Dictionary<Machine, WeightedStatistic>> utilStatistics;

        private Dictionary<WorkStation, Variable<int>> wsCapacity;

        private Dictionary<WorkStation, WeightedStatistic> wsCapacityStat;

        private List<WorkStation> orderedWorkstations;

        //private Dictionary<WorkStation,List<Machine>> orderedMachines;

        private int maxMachines;

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            ToyFab toyFab = (ToyFab)modelElement;

            foreach (WorkStation ws in orderedWorkstations)
            {
                foreach (var queueLength in utilizations[ws].Values)
                {
                    queueLength.Reset();
                }

                foreach (var queueLengthStatistic in utilStatistics[ws].Values)
                {
                    queueLengthStatistic.Reset();
                }
                wsCapacity[ws].Reset();
                wsCapacityStat[ws].Reset();
            }
            headerToFile(toyFab);
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
        protected override void OnInitialized(ModelElementBase modelElement)
        {
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            ToyFab toyFab = (ToyFab)modelElement;
            if (toyFab.currentObserverType == ToyFab.GetObserverTypes.UtilizationUpdate.ToString())
            {
                Machine mach = toyFab.machineToUpdate;
                WorkStation ws = mach.WorkStation;

                wsCapacity[ws].UpdateValue(1 - ws.NrFreeMachines / ws.Machines.Count);
                utilizations[ws][mach].UpdateValue((mach.LotInService == null) ? 0 : 1);

                wsCapacityStat[ws].Collect(wsCapacity[ws].PreviousValue, wsCapacity[ws].Weight);
                utilStatistics[ws][mach].Collect(utilizations[ws][mach].PreviousValue, utilizations[ws][mach].Weight);

                //writeOutputToFile(ws);
            }
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            ToyFab toyFab = (ToyFab)modelElement;

            //// Final collection of data for work currently in progress
            foreach (WorkStation ws in toyFab.WorkStations.Values)
            {
                foreach (Machine mach in ws.Machines)
                {
                    wsCapacity[ws].UpdateValue(1 - ws.NrFreeMachines / ws.Machines.Count);
                    utilizations[ws][mach].UpdateValue((mach.LotInService == null) ? 0 : 1);

                    wsCapacityStat[ws].Collect(wsCapacity[ws].PreviousValue, wsCapacity[ws].Weight);
                    utilStatistics[ws][mach].Collect(utilizations[ws][mach].PreviousValue, utilizations[ws][mach].Weight);
                }
            }
            writeOutputToFile(toyFab); //throw new Exception("Order of updating is incorrect.");
        }
        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
        }


        private void headerToFile(ToyFab toyfab)
        {
            Writer?.Write("WorkStation,TotalCapacity,");
            for (int i=0; i < maxMachines; i++) 
            {
                Writer?.Write($"Machine{i+1},");
            }
            Writer?.Write("\n");
        }

        private void writeOutputToFile(ToyFab toyFab)
        {
            foreach (WorkStation ws in orderedWorkstations)
            {
                Writer?.Write(ws.Name.Split('_').Last() + ",");
                Writer?.Write(wsCapacityStat[ws].Average() + ",");
                foreach (Machine machine in ws.Machines)
                {
                    Writer?.Write(utilStatistics[ws][machine].Average() + ",");
                }
                if (ws.Machines.Count < maxMachines) Writer?.Write(new String(',', maxMachines - ws.Machines.Count));
                Writer?.Write("\n");
            }
        }

        private void writeOutputToConsole(WorkStation ws)
        {
            //Console.Write(ws.GetTime + "," + ws.GetWallClockTime + ",");

            //foreach (Machine machine in orderedMachines)
            //{
            //    Console.Write($"{machine.Name} " + utilizations[machine].Value + ",");
            //}
            //Console.Write("\n");
        }
    }
}
