using CSSL.Modeling.Elements;
using LithographyAreaValidation.DataReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LithographyAreaValidation.ModelElements
{
    public class LithographyArea : SchedulingElementBase
    {
        public LithographyArea(ModelElementBase parent, string name, DateTime startDate, double lengthOfReplication, Boolean dynamic, Boolean stochastic, double weightA, double weightB, double weightC, Dictionary<string, double> deterministicNonProductiveTimesRMS, Dictionary<string, double> deterministicNonProductiveTimesARMS) : base(parent, name)
        {
            // Get startDate of replication
            StartDate = startDate;
            LengthOfReplication = lengthOfReplication;

            SchedulingHorizon = 2 * 3600;

            Reader = new CSVReader(startDate);

            Dynamic = dynamic;
            Stochastic = stochastic;
            WeightA = weightA;
            WeightB = weightB;
            WeightC = weightC;
            DeterministicNonProductiveTimesRMS = deterministicNonProductiveTimesRMS;
            DeterministicNonProductiveTimesARMS = deterministicNonProductiveTimesARMS;
        }

        // Properties
        public double SchedulingHorizon { get; }
        public Boolean Dynamic { get; }
        public Boolean Stochastic { get; }
        public double WeightA { get; }
        public double WeightB { get; }
        public double WeightC { get; }

        public Dictionary<string, double> DeterministicNonProductiveTimesRMS { get; }
        public Dictionary<string, double> DeterministicNonProductiveTimesARMS { get; }

        public DateTime StartDate { get; private set; }

        public double LengthOfReplication { get; private set; }

        public CSVReader Reader { get; private set; }

        public LotGenerator LotGenerator { get; private set; }

        public DispatcherBase Dispatcher { get; private set; }

        public List<Machine> Machines { get; set; }

        public List<Machine> WaitingMachines { get; set; }

        public Dictionary<string, string> MachineStates { get; set; }

        public Dictionary<string, int> LayerActivities { get; set; }

        public Dictionary<string, int> LayerTargets { get; set; }

        private List<Array> UltratechTitanLotEnds { get; set; }

        // NEW:
        public bool EndedOnDispatcherNoSolution { get; set; }
        public int TotalWafersProduced { get; set; }
        public int TotalLotsProduced { get; set; }

        public int TotalValidationLotsProduced { get; set; }
        public double TotalCompletionTimeValidationLots { get; set; }
        public double TotalSquaredLatenessValidationLots { get; set; }

        public double TotalProductionTargetFulfillment { get; set; }
        public double TotalSquaredLateness { get; set; }
        public double TotalSquaredEarliness { get; set; }
        public double TotalSquaredTardiness { get; set; }

        public double TotalEarliness { get; set; }
        public double TotalTardiness { get; set; }

        public double TotalScoreThroughput { get; set; }
        public double TotalScoreDueDate { get; set; }
        public double TotalScoreWIPBalance { get; set; }

        public double TotalTheoreticalProductionTime { get; set; }
        public double TotalProductionTime { get; set; }

        public double TotalDownTime { get; set; }

        public int TotalLayerSwitches { get; set; }
        public int TotalReticleSwitches { get; set; }

        public void SetLotGenerator(LotGenerator lotGenerator)
        {
            LotGenerator = lotGenerator;
        }

        public void SetDispatcher(DispatcherBase dispatcher)
        {
            Dispatcher = dispatcher;
        }

        public void AddMachine(Machine machine)
        {
            if (Machines == null)
            {
                Machines = new List<Machine>();
            }
            Machines.Add(machine);
        }

        protected override void OnReplicationStart()
        {
            EndedOnDispatcherNoSolution = false;

            MachineStates = new Dictionary<string, string>();

            foreach (Machine machine in Machines)
            {
                MachineStates.Add(machine.Name, "Up");
            }

            WaitingMachines = new List<Machine>();

            TotalLotsProduced = 0;
            TotalWafersProduced = 0;

            TotalValidationLotsProduced = 0;
            TotalCompletionTimeValidationLots = 0;
            TotalSquaredLatenessValidationLots = 0;

            TotalProductionTargetFulfillment = 0;
            TotalSquaredLateness = 0;
            TotalSquaredEarliness = 0;
            TotalSquaredTardiness = 0;

            TotalEarliness = 0;
            TotalTardiness = 0;

            TotalScoreThroughput = 0;
            TotalScoreDueDate = 0;
            TotalScoreWIPBalance = 0;

            TotalTheoreticalProductionTime = 0;
            TotalProductionTime = 0;

            TotalDownTime = 0;

            TotalLayerSwitches = 0;
            TotalReticleSwitches = 0;

            LayerActivities = new Dictionary<string, int>
            {
                { "OW Photo", 0 },
                { "ZL Photo", 0 },
                { "DC Photo", 0 },
                { "DP Photo", 0 },
                { "OD Photo", 0 },
                { "OC Photo", 0 },
                { "TR Photo", 0 },
                { "PS Photo", 0 },
                { "AP T3 Photo", 0 },
                { "SN Photo", 0 },
                { "CO Photo", 0 },
                { "IN Photo", 0 },
                { "CB Photo", 0 },
                { "VI Photo", 0 },
                { "TC Photo", 0 },
                { "Other", 0 }
            };

            LayerTargets = new Dictionary<string, int>
            {
                { "OW Photo", 275 },
                { "ZL Photo", 1375 },
                { "DC Photo", 300 },
                { "DP Photo", 1375 },
                { "OD Photo", 1375 },
                { "OC Photo", 300 },
                { "TR Photo", 1950 },
                { "PS Photo", 1950 },
                { "AP T3 Photo", 650 },
                { "SN Photo", 1950 },
                { "CO Photo", 1950 },
                { "IN Photo", 1950 },
                { "CB Photo", 800 },
                { "VI Photo", 1150 },
                { "TC Photo", 1150 },
                { "Other", 800 }
            };

            //{
            //    { "OW Photo", 250 },
            //    { "ZL Photo", 1350 },
            //    { "DC Photo", 275 },
            //    { "DP Photo", 1350 },
            //    { "OD Photo", 1350 },
            //    { "OC Photo", 275 },
            //    { "TR Photo", 1875 },
            //    { "PS Photo", 1875 },
            //    { "AP T3 Photo", 650 },
            //    { "SN Photo", 1875 },
            //    { "CO Photo", 1875 },
            //    { "IN Photo", 1875 },
            //    { "CB Photo", 775 },
            //    { "VI Photo", 1100 },
            //    { "TC Photo", 1100 },
            //    { "Other", 800 }
            //};



            // Schedule event to read and reset the underproduction after 24h
            ScheduleEvent(GetTime + 24 * 3600, HandleEndDay);

            // Get events on the UltraTechTitans
            UltratechTitanLotEnds = Reader.ReadUltratechTitans(StartDate, LengthOfReplication);

            // Schedule first event on the UltratechTitan
            Array nextLotEndUltratechTitan = UltratechTitanLotEnds[0];
            double nextLotEndUltratechTitanTime = (double)nextLotEndUltratechTitan.GetValue(3);
            ScheduleEvent(nextLotEndUltratechTitanTime, HandleLotEndUltratechTitan);
        }

        public void HandleDispatcherError()
        {
            EndedOnDispatcherNoSolution = true;
            NotifyObservers(this);
            ScheduleEndEvent(GetTime);
        }

        public void HandleEndMachineDown(Machine machine)
        {
            double downTime = machine.EndMachineDown - machine.StartMachineDown;

            TotalDownTime += downTime;
        }

        public void HandleEndDay(CSSLEvent e)
        {
            double totalFulfillment = 0;
            double totalLayers = 0;
            foreach (KeyValuePair<string, int> layer in LayerTargets)
            {
                double fulfillment = (double)LayerActivities[layer.Key] / (double)LayerTargets[layer.Key];

                if (fulfillment > 1)
                {
                    fulfillment = 1;
                }

                totalFulfillment += fulfillment;
                totalLayers += 1;
            }

            TotalProductionTargetFulfillment += totalFulfillment / totalLayers;

            // Read value
            NotifyObservers(this);

            LayerActivities = new Dictionary<string, int>
            {
                { "OW Photo", 0 },
                { "ZL Photo", 0 },
                { "DC Photo", 0 },
                { "DP Photo", 0 },
                { "OD Photo", 0 },
                { "OC Photo", 0 },
                { "TR Photo", 0 },
                { "PS Photo", 0 },
                { "AP T3 Photo", 0 },
                { "SN Photo", 0 },
                { "CO Photo", 0 },
                { "IN Photo", 0 },
                { "CB Photo", 0 },
                { "VI Photo", 0 },
                { "TC Photo", 0 },
                { "Other", 0 }
            };

            // Schedule event to read and reset the underproduction after 24h
            ScheduleEvent(GetTime + 24 * 3600, HandleEndDay);
        }

        public void HandleEndRun(Lot finishedLot, Machine machine)
        {
            // Add Nr. of produced lots to LayerActivity

            if (LayerActivities.ContainsKey(finishedLot.IrdName))
            {
                LayerActivities[finishedLot.IrdName] += finishedLot.LotQty;
            }
            else
            {
                LayerActivities["Other"] += finishedLot.LotQty;
            }

            TotalLotsProduced += 1;
            TotalWafersProduced += finishedLot.LotQty;

            // Calculate and add lateness of lot
            double lateness = (this.StartDate.AddSeconds(GetTime).Subtract(finishedLot.ImprovedDueDate)).TotalDays;
            TotalSquaredLateness += lateness * lateness;

            double earliness = Math.Max(0,(finishedLot.ImprovedDueDate.Subtract(this.StartDate.AddSeconds(GetTime))).TotalDays);
            TotalSquaredEarliness += earliness * earliness;
            TotalEarliness += earliness;

            double tardiness = Math.Max(0,(this.StartDate.AddSeconds(GetTime).Subtract(finishedLot.ImprovedDueDate)).TotalDays);
            TotalSquaredTardiness += tardiness * tardiness;
            TotalTardiness += tardiness;

            double fractionInSchedulingHorizon = Math.Min(Math.Max(((SchedulingHorizon - machine.CurrentStartRun) / (machine.CurrentEndRun - machine.CurrentStartRun)), 0.0), 1.0);

            if (!Dynamic)
            {
                double theoreticalProductionTime = GetTheoreticalProductionTime(finishedLot);
                double weightDueDate = finishedLot.WeightDueDate;
                double weightWIPBalance = finishedLot.WeightWIPBalance;

                TotalScoreThroughput += fractionInSchedulingHorizon * theoreticalProductionTime / (Machines.Count*SchedulingHorizon);
                TotalScoreDueDate += fractionInSchedulingHorizon * weightDueDate * (finishedLot.LotQty / 25.0);
                TotalScoreWIPBalance += fractionInSchedulingHorizon * weightWIPBalance * (finishedLot.LotQty / 25.0);

                TotalTheoreticalProductionTime += fractionInSchedulingHorizon * theoreticalProductionTime;
                TotalProductionTime += fractionInSchedulingHorizon * (machine.CurrentEndRun - machine.CurrentStartRun);
            }
            else
            {
                double theoreticalProductionTime = GetTheoreticalProductionTime(finishedLot);
                //double weightDueDate = finishedLot.WeightDueDate;
                //double weightWIPBalance = finishedLot.WeightWIPBalance;

                TotalScoreThroughput += theoreticalProductionTime / (Machines.Count * LengthOfReplication);
                //TotalScoreDueDate += fractionInSchedulingHorizon * weightDueDate * (finishedLot.LotQty / 25.0);
                //TotalScoreWIPBalance += fractionInSchedulingHorizon * weightWIPBalance * (finishedLot.LotQty / 25.0);

                TotalTheoreticalProductionTime += theoreticalProductionTime;
                TotalProductionTime += machine.CurrentEndRun - machine.CurrentStartRun;
            }
        }

        private double GetTheoreticalProductionTime(Lot lot)
        {
            double theoreticalProductionTime = Double.PositiveInfinity;

            foreach (Machine machine in Machines)
            {
                // Get needed recipe for the lot
                string recipe = Dispatcher.GetRecipe(lot, machine);
                string recipeKey = machine.Name + "__" + recipe;

                // Check if needed recipe is eligible on machine
                Boolean recipeEligible = Dispatcher.CheckMachineEligibility(recipeKey);

                // Check if processingTime is known
                Boolean processingTimeKnown = Dispatcher.CheckProcessingTimeKnown(lot, machine, recipe);

                if (recipeEligible && processingTimeKnown)
                {
                    double thisProductionTime = machine.GetDeterministicProcessingTime(lot);

                    if (thisProductionTime < theoreticalProductionTime)
                    {
                        theoreticalProductionTime = thisProductionTime;
                    }
                }
            }
            return theoreticalProductionTime;
        }

        public void HandleLotEndUltratechTitan(CSSLEvent e)
        {
            // Get information of Scheduled Lot
            Array thisLotEndUltratechTitan = UltratechTitanLotEnds[0];
            string irdName = (string)thisLotEndUltratechTitan.GetValue(1);
            int lotQty = (int)thisLotEndUltratechTitan.GetValue(2);

            // Remove lot from List
            UltratechTitanLotEnds.Remove(thisLotEndUltratechTitan);

            // Add Nr. of produced lots to LayerActivity
            if (LayerActivities.ContainsKey(irdName))
            {
                LayerActivities[irdName] += lotQty;
            }
            else
            {
                LayerActivities["Other"] += lotQty;
            }

            //TotalWafersProducedDay += lotQty;
            //TotalLotsProducedDay += 1;

            //TotalLotsProduced += 1;
            //TotalWafersProduced += lotQty;

            // Schedule next event on the UltratechTitan
            if (UltratechTitanLotEnds.Count > 0 )
            {
                Array nextLotEndUltratechTitan = UltratechTitanLotEnds[0];
                double nextLotEndUltratechTitanTime = (double)nextLotEndUltratechTitan.GetValue(3);
                ScheduleEvent(nextLotEndUltratechTitanTime, HandleLotEndUltratechTitan);
            }
        }
    }
}
