using CSSL.Examples.AccessController;
using CSSL.Modeling;
using CSSL.Modeling.Elements;
using CSSL.Observer;
using CSSL.Utilities.Statistics;
using LithographyAreaValidation.ModelElements;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.Observers
{
    public class LithographyAreaObserver : ModelElementObserverBase
    {
        public LithographyAreaObserver(Simulation mySimulation, DateTime startDate) : base(mySimulation)
        {
            // Get startDateRun
            this.startDateRun = startDate;
        }

        // Properties
        private DateTime startDateRun;

        private DateTime startDateDay;

        private DateTime endDateDay;

        private int replication;

        public override void OnError(Exception error)
        {
        }

        protected override void OnExperimentEnd(ModelElementBase modelElement)
        {
            //ObserverCounter = 0;
        }

        protected override void OnExperimentStart(ModelElementBase modelElement)
        {
            ExperimentWriter.WriteLine($"Replication,Start DateTime,End DateTime,Lots Produced,Wafers Produced," +
                       $"Rate Efficiency,Operational Efficiency,Performance Efficiency," +
                       $"Tot Squared Lateness,Tot Squared Earliness,Tot Squared Tardiness," +
                       $"Layer Switches,Reticle Switches,EndQueueLength," +
                       $"Prod Squared Lateness,Prod Squared Earliness,Prod Squared Tardiness," +
                       $"Queue Squared Lateness,Queue Squared Earliness,Queue Squared Tardiness," +
                       $"Prod Lateness,Prod Earliness,Prod Tardiness," +
                       $"Queue Lateness,Queue Earliness,Queue Tardiness," +
                       $"Production Target Fulfillment,Throughput Score,Due Date Score, WIP Balance score");


            replication = 1;
        }

        protected override void OnInitialized(ModelElementBase modelElement)
        {
            //Write
            Writer.WriteLine($"Replication,Start DateTime,End DateTime,Lots Produced,Wafers Produced," +
                       $"Rate Efficiency,Operational Efficiency,Performance Efficiency," +
                       $"Tot Squared Lateness,Tot Squared Earliness,Tot Squared Tardiness," +
                       $"Layer Switches,Reticle Switches,EndQueueLength," +
                       $"Prod Squared Lateness,Prod Squared Earliness,Prod Squared Tardiness," +
                       $"Queue Squared Lateness,Queue Squared Earliness,Queue Squared Tardiness," +
                       $"Prod Lateness,Prod Earliness,Prod Tardiness," +
                       $"Queue Lateness,Queue Earliness,Queue Tardiness," +
                       $"Production Target Fulfillment,Throughput Score,Due Date Score, WIP Balance score");
            //Writer.WriteLine($"Start DateTime,End DateTime, Cumulative Weighted Throughput,Cumulative Weighted Underproduction,Cumulative Weighted Squared Lateness");
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            replication += 1;
            ExperimentWriter.Flush();
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            // Initialize startDateDay
            startDateDay = startDateRun;
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            // Get lithographyArea object
            LithographyArea l = (LithographyArea)modelElement;

            endDateDay = startDateRun.AddSeconds(GetTime);


            if (l.EndedOnDispatcherNoSolution)
            {
                Writer.WriteLine($"Ended on error, CP dispatcher did not find a solution for {endDateDay}");
            }
            else
            {
                // Write
                double operationalEfficiency;
                double rateEffiency = l.TotalTheoreticalProductionTime / l.TotalProductionTime;

                if (!l.Dynamic)
                {
                    operationalEfficiency = l.TotalProductionTime / (l.Machines.Count * l.SchedulingHorizon);
                }
                else
                {
                    operationalEfficiency = l.TotalProductionTime / (l.Machines.Count * l.GetTime - l.TotalDownTime);
                }

                double performanceEfficiency = operationalEfficiency * rateEffiency;


                Writer.WriteLine($"{replication},{startDateDay},{endDateDay},{l.TotalLotsProduced},{l.TotalWafersProduced}," +
                                 $"{rateEffiency},{operationalEfficiency},{performanceEfficiency}," +
                                 $"{l.TotalSquaredLateness + l.Dispatcher.GetSquaredLatenessQueue()},{l.TotalSquaredEarliness + l.Dispatcher.GetEarlinessQueue(true)},{l.TotalSquaredTardiness + l.Dispatcher.GetTardinessQueue(true)}," +
                                 $"{l.TotalLayerSwitches},{l.TotalReticleSwitches},{l.Dispatcher.GetQueueLength()}," +
                                 $"{l.TotalSquaredLateness},{l.TotalSquaredEarliness},{l.TotalSquaredTardiness}," +
                                 $"{l.Dispatcher.GetSquaredLatenessQueue()},{l.Dispatcher.GetEarlinessQueue(true)},{l.Dispatcher.GetTardinessQueue(true)}," +
                                 $"{l.TotalEarliness},{l.TotalEarliness},{l.TotalTardiness}," +
                                 $"{l.Dispatcher.GetTardinessQueue(false) - l.Dispatcher.GetEarlinessQueue(false)},{l.Dispatcher.GetEarlinessQueue(false)},{l.Dispatcher.GetTardinessQueue(false)}," +
                                 $"{l.TotalProductionTargetFulfillment},{l.TotalScoreThroughput},{l.TotalScoreDueDate},{l.TotalScoreWIPBalance}");
                

                ExperimentWriter.WriteLine($"{replication},{startDateDay},{endDateDay},{l.TotalLotsProduced},{l.TotalWafersProduced}," +
                                 $"{rateEffiency},{operationalEfficiency},{performanceEfficiency}," +
                                 $"{l.TotalSquaredLateness + l.Dispatcher.GetSquaredLatenessQueue()},{l.TotalSquaredEarliness + l.Dispatcher.GetEarlinessQueue(true)},{l.TotalSquaredTardiness + l.Dispatcher.GetTardinessQueue(true)}," +
                                 $"{l.TotalLayerSwitches},{l.TotalReticleSwitches},{l.Dispatcher.GetQueueLength()}," +
                                 $"{l.TotalSquaredLateness},{l.TotalSquaredEarliness},{l.TotalSquaredTardiness}," +
                                 $"{l.Dispatcher.GetSquaredLatenessQueue()},{l.Dispatcher.GetEarlinessQueue(true)},{l.Dispatcher.GetTardinessQueue(true)}," +
                                 $"{l.TotalEarliness},{l.TotalEarliness},{l.TotalTardiness}," +
                                 $"{l.Dispatcher.GetTardinessQueue(false) - l.Dispatcher.GetEarlinessQueue(false)},{l.Dispatcher.GetEarlinessQueue(false)},{l.Dispatcher.GetTardinessQueue(false)}," +
                                 $"{l.TotalProductionTargetFulfillment},{l.TotalScoreThroughput},{l.TotalScoreDueDate},{l.TotalScoreWIPBalance}");

                // Update startDateDay
                startDateDay = endDateDay;
            }
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
    }
}
