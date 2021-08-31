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
            ExperimentWriter.WriteLine($"Replication,Start DateTime,End DateTime,Total Lots Produced,Total Wafers Produced," +
                                       $"Total Production Target Fulfillment,Total Squared Lateness,Total Squared Earliness," +
                                       $"Total Squared Tardiness,Total Layer Switches,Total Reticle Switches,Total Squared Lateness Queue," +
                                       $"EndQueueLength," +
                                       $"Total Earliness,Total Tardiness,Total Score Throughput,Total Score Due Date,Total Score WIP Balance," +
                                       $"Total Earliness Queue,Total Tardiness Queue," +
                                       $"Rate Efficiency,Operational Efficiency");
            replication = 1;
        }

        protected override void OnInitialized(ModelElementBase modelElement)
        {
            //Write
            Writer.WriteLine($"Replication,Start DateTime,End DateTime,Total Lots Produced,Total Wafers Produced,Total Production Target Fulfillment," +
                             $"Total Squared Lateness,Total Squared Earliness,Total Squared Tardiness,Total Layer Switches,Total Reticle Switches," +
                             $"Total Squared Lateness Queue,EndQueueLength," +
                             $"Total Earliness,Total Tardiness,Total Score Throughput,Total Score Due Date,Total Score WIP Balance," +
                             $"Total Earliness Queue,Total Tardiness Queue," +
                             $"Rate Efficiency,Operational Efficiency");
            //Writer.WriteLine($"Start DateTime,End DateTime, Cumulative Weighted Throughput,Cumulative Weighted Underproduction,Cumulative Weighted Squared Lateness");
        }

        protected override void OnReplicationEnd(ModelElementBase modelElement)
        {
            replication += 1;
        }

        protected override void OnReplicationStart(ModelElementBase modelElement)
        {
            // Initialize startDateDay
            startDateDay = startDateRun;
        }

        protected override void OnUpdate(ModelElementBase modelElement)
        {
            // Get lithographyArea object
            LithographyArea lithographyArea = (LithographyArea)modelElement;

            // Write
            endDateDay = startDateRun.AddSeconds(GetTime);

            double operationalEfficiency;

            if (!lithographyArea.Dynamic)
            {
                operationalEfficiency = lithographyArea.TotalProductionTime / (lithographyArea.Machines.Count * lithographyArea.SchedulingHorizon);
            }
            else
            {
                operationalEfficiency = lithographyArea.TotalProductionTime / (lithographyArea.Machines.Count * lithographyArea.GetTime - lithographyArea.TotalDownTime);
            }

            Writer.WriteLine($"{replication},{startDateDay},{endDateDay},{lithographyArea.TotalLotsProduced},{lithographyArea.TotalWafersProduced}," +
                             $"{lithographyArea.TotalProductionTargetFulfillment},{lithographyArea.TotalSquaredLateness},{lithographyArea.TotalSquaredEarliness}," +
                             $"{lithographyArea.TotalSquaredTardiness},{lithographyArea.TotalLayerSwitches},{lithographyArea.TotalReticleSwitches}," +
                             $"{lithographyArea.Dispatcher.GetSquaredLatenessQueue()},{lithographyArea.Dispatcher.GetQueueLength()}," +
                             $"{lithographyArea.TotalEarliness},{lithographyArea.TotalTardiness},{lithographyArea.TotalScoreThroughput}," +
                             $"{lithographyArea.TotalScoreDueDate},{lithographyArea.TotalScoreWIPBalance}," +
                             $"{lithographyArea.Dispatcher.GetEarlinessQueue()},{lithographyArea.Dispatcher.GetTardinessQueue()}," +
                             $"{lithographyArea.TotalTheoreticalProductionTime/lithographyArea.TotalProductionTime},{operationalEfficiency}");

            ExperimentWriter.WriteLine($"{replication},{startDateDay},{endDateDay},{lithographyArea.TotalLotsProduced},{lithographyArea.TotalWafersProduced}," +
                                       $"{lithographyArea.TotalProductionTargetFulfillment},{lithographyArea.TotalSquaredLateness},{lithographyArea.TotalSquaredEarliness}," +
                                       $"{lithographyArea.TotalSquaredTardiness},{lithographyArea.TotalLayerSwitches},{lithographyArea.TotalReticleSwitches}," +
                                       $"{lithographyArea.Dispatcher.GetSquaredLatenessQueue()},{lithographyArea.Dispatcher.GetQueueLength()}," +
                                       $"{lithographyArea.TotalEarliness},{lithographyArea.TotalTardiness},{lithographyArea.TotalScoreThroughput}," +
                                       $"{lithographyArea.TotalScoreDueDate},{lithographyArea.TotalScoreWIPBalance}," +
                                       $"{lithographyArea.Dispatcher.GetEarlinessQueue()},{lithographyArea.Dispatcher.GetTardinessQueue()}," +
                                       $"{lithographyArea.TotalTheoreticalProductionTime / lithographyArea.TotalProductionTime},{operationalEfficiency}");

            // Update startDateDay
            startDateDay = endDateDay;
        }

        protected override void OnWarmUp(ModelElementBase modelElement)
        {
        }
    }
}
