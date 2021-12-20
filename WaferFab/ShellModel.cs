﻿using CSSL.Examples.AccessController;
using CSSL.Modeling;
using CSSL.Utilities.Distributions;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WaferFabSim.Import;
using WaferFabSim.OutputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Observers;
using WaferFabSim.WaferFabElements.Utilities;

namespace WaferFabSim
{
    public class ShellModel : INotifyPropertyChanged
    {
        public ShellModel(string outputDir)
        {
            this.outputDir = outputDir;
        }

        private string outputDir { get; set; }

        public Simulation MySimulation { get; private set; }

        public DataReaderBase DataReader { get; set; }

        public WaferFabSettings MyWaferFabSettings { get; set; }

        public ExperimentSettings MyExperimentSettings { get; set; }

        public Results MySimResults { get; set; }

        public RealSnapshotReader RealSnapshotReader { get; set; }

        public void RunSimulation()
        {
            MySimulation = new Simulation("WaferFab", outputDir);

            buildWaferFab();

            setExperiment();

            MySimulation.Run();

            ReadSimulationResults();
        }

        public void ReadRealSnaphots(string filename)
        {
            try
            {
                RealSnapshotReader = new RealSnapshotReader();
                RealSnapshotReader.Read(filename, 1);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            NotifyOfPropertyChange(nameof(RealSnapshotReader));
        }

        private void buildWaferFab()
        {
            // Build the model
            WaferFab waferFab = new WaferFab(MySimulation.MyModel, "WaferFab", new ConstantDistribution(MyWaferFabSettings.SampleInterval), MyWaferFabSettings.InitialTime);

            //// LotStarts
            waferFab.ManualLotStarts = MyWaferFabSettings.ManualLotStartQtys;

            //// LotSteps
            waferFab.LotSteps = MyWaferFabSettings.LotSteps;

            //// Control
            if (MyWaferFabSettings.WIPTargets != null)
            {
                waferFab.AddWIPTargets(MyWaferFabSettings.WIPTargets);
            }

            //// WorkCenters
            foreach (string wc in MyWaferFabSettings.WorkCenters)
            {
                WorkCenter workCenter = new WorkCenter(waferFab, $"WorkCenter_{wc}", MyWaferFabSettings.WCServiceTimeDistributions[wc], MyWaferFabSettings.LotStepsPerWorkStation[wc]);

                // Connect workcenter to WIPDependentDistribution
                if (MyWaferFabSettings.WCServiceTimeDistributions[wc] is EPTDistribution)
                {
                    var distr = (EPTDistribution)MyWaferFabSettings.WCServiceTimeDistributions[wc];

                    distr.WorkCenter = workCenter;
                }

                // Choose dispatcher
                if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.BQF)
                {
                    workCenter.SetDispatcher(new BQFDispatcher(workCenter, workCenter.Name + "_BQFDispatcher"));
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.EPTOVERTAKING)
                {
                    workCenter.SetDispatcher(new EPTOvertakingDispatcher(workCenter, workCenter.Name + "_EPTOvertakingDispatcher", MyWaferFabSettings.WCOvertakingDistributions[wc]));

                    // Connect workcenter to OvertakingDistribution
                    MyWaferFabSettings.WCOvertakingDistributions[wc].WorkCenter = workCenter;
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.RANDOM)
                {
                    workCenter.SetDispatcher(new RandomDispatcher(workCenter, workCenter.Name + "_RandomDispatcher"));
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.FIFO)
                {
                    workCenter.SetDispatcher(new FIFODispatcher(workCenter, workCenter.Name + "_FIFODisptacher"));
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.EDD)
                {
                    workCenter.SetDispatcher(new LatenessDispatcher(workCenter, workCenter.Name + "_EDDDisptacher"));
                    workCenter.SetLatenessBasedQueue("EDD");
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.ODD)
                {
                    workCenter.SetDispatcher(new LatenessDispatcher(workCenter, workCenter.Name + "_ODDDisptacher"));
                    workCenter.SetLatenessBasedQueue("ODD");
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.CR)
                {
                    workCenter.SetDispatcher(new LatenessDispatcher(workCenter, workCenter.Name + "_CRDisptacher"));
                    workCenter.SetLatenessBasedQueue("CR");
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.CR_alt)
                {
                    workCenter.SetDispatcher(new LatenessDispatcher(workCenter, workCenter.Name + "_CRDisptacher"));
                    workCenter.SetLatenessBasedQueue("CR_alt");
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.MIVS)
                {
                    workCenter.SetDispatcher(new MIVSDispatcher(workCenter, workCenter.Name + "_MIVSDisptacher", MyWaferFabSettings.MIVSkStepAhead, MyWaferFabSettings.MIVSjStepBack));
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.WORKLOAD)
                {
                    workCenter.SetDispatcher(new WLDispatcher(workCenter, workCenter.Name + "_WLDisptacher", MyWaferFabSettings.MIVSkStepAhead, MyWaferFabSettings.MIVSjStepBack));
                }
                else if (MyWaferFabSettings.WCDispatcherTypes[wc] == DispatcherBase.Type.WL_ODD)
                {
                    workCenter.SetDispatcher(new WLDispatcher(workCenter, workCenter.Name + "_WL_ODDDisptacher", MyWaferFabSettings.MIVSkStepAhead, MyWaferFabSettings.MIVSjStepBack));
                    workCenter.SetLatenessBasedQueue("ODD");
                }

                waferFab.AddWorkCenter(workCenter.Name, workCenter);
            }

            //// Sequences
            foreach (var sequence in MyWaferFabSettings.Sequences)
            {
                waferFab.AddSequence(sequence.Key, sequence.Value);
            }

            //// LotGenerator
            waferFab.SetLotGenerator(new LotGenerator(waferFab, "LotGenerator", new ConstantDistribution(MyWaferFabSettings.LotStartsFrequency * 60 * 60), MyWaferFabSettings.UseRealLotStartsFlag));

            // Add real LotStarts, copied from fab data
            if (MyWaferFabSettings.UseRealLotStartsFlag)
            {
                waferFab.LotStarts = MyWaferFabSettings.GetLotStarts();
            }

            // Add intial lots (lots present at t = 0) by translating RealLots (from RealSnapshot) to Lots
            if (MyWaferFabSettings.InitialRealLots.Any() != default)
            {
                //waferFab.InitialLots = new System.Collections.Generic.List<Lot>(); // no initial lots
                waferFab.InitialLots = MyWaferFabSettings.InitialRealLots.Select(x => x.ConvertToLot(0, waferFab.Sequences, false, waferFab.InitialDateTime)).Where(x => x != null).ToList();
                if (MyWaferFabSettings.TimeMinimum != null)
                {
                    if (MyWaferFabSettings.StartTimeShiftFactor != 1) waferFab.ShiftLotStarts(MyWaferFabSettings.StartTimeShiftFactor, (DateTime)MyWaferFabSettings.TimeMinimum);
                }
                else throw new Exception("No RealLotStarts found in WaferfabSettings.");
            }

            // Add observers
            WaferFabLotsObserver waferFabObserver = new WaferFabLotsObserver(MySimulation, "WaferFabLotsObserver", waferFab);
            WaferFabWafersObserver waferFabObserverWafers = new WaferFabWafersObserver(MySimulation, "WaferFabWafersObserver", waferFab);
            WaferFabTotalQueueObserver waferFabTotalQueueObserver = new WaferFabTotalQueueObserver(MySimulation, "WaferFabTotalQueueObserver", waferFab);
            LotGeneratorStartsObserver startsObserver = new LotGeneratorStartsObserver(MySimulation, "WaferFabStartsObserver");
            waferFab.Subscribe(waferFabObserver);
            waferFab.Subscribe(waferFabObserverWafers);
            waferFab.Subscribe(waferFabTotalQueueObserver);
            waferFab.LotGenerator.Subscribe(startsObserver);

            foreach (var wc in waferFab.WorkCenters)
            {
                //TotalQueueObserver totalQueueObs = new TotalQueueObserver(MySimulation, wc.Key + "_TotalQueueObserver");
                //SeperateQueuesObserver seperateQueueObs = new SeperateQueuesObserver(MySimulation, wc.Value, wc.Key + "_SeperateQueuesObserver");
                LotOutObserver lotOutObserver = new LotOutObserver(MySimulation, wc.Key + "_LotOutObserver");

                //wc.Value.Subscribe(totalQueueObs);
                //wc.Value.Subscribe(seperateQueueObs);
                wc.Value.dispatcher.Subscribe(lotOutObserver);
            }
        }

        private void setExperiment()
        {
            MySimulation.MyExperiment.NumberOfReplications = MyExperimentSettings.NumberOfReplications;
            MySimulation.MyExperiment.LengthOfWarmUp = MyExperimentSettings.LengthOfWarmUp;
            MySimulation.MyExperiment.LengthOfReplication = MyExperimentSettings.LengthOfReplication;
            MySimulation.MyExperiment.LengthOfReplicationWallClock = MyExperimentSettings.LengthOfReplicationWallClock;
        }

        public void ReadSimulationResults()
        {
            // Get to last experiment directory
            string experimentDir = Directory.GetDirectories(outputDir).OrderBy(x => Directory.GetCreationTime(x)).Last();

            MySimResults = new Results(experimentDir);

            MySimResults.ReadResults();

            NotifyOfPropertyChange(nameof(MySimResults));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
