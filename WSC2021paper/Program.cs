using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WaferAreaOptimiser;
using WaferFabSim;
using WaferFabSim.Import;
using WaferFabSim.Import.Distributions;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Observers;
using WaferFabSim.WaferFabElements.Utilities;

namespace WSC2021paper
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDirectory = @"C:\CSSLWaferFab\Input\WSC2021paper";
            string outputDirectory = @"C:\CSSLWaferFab\Output\WSC2021paper";
            string eptParameterFile = @"FittedEPTParameters - 2019-6-1.csv";

            List<string> workcenters = new List<string> { "PHOTOLITH", "FURNACING", "DRY ETCH" };
            List<string> SOvsLDO = new List<string> { "SO", "LDO" };
            //List<string> SOvsLDO = new List<string> { "LDO"};

            foreach (string wc in workcenters)
            {
                WaferFabSettings waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

                foreach (string overtaking in SOvsLDO)
                {
                    bool lotStepOvertaking = overtaking == "SO" ? false : true;

                    #region Initializing simulation
                    Simulation simulation = new Simulation(wc, outputDirectory);
                    #endregion 

                    #region Experiment settings
                    simulation.MyExperiment.NumberOfReplications = 10;
                    simulation.MyExperiment.LengthOfReplication = 60 * 60 * 24 * 91; // September and October
                    simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 30;
                    DateTime initialDateTime = new DateTime(2019, 08, 01);
                    #endregion

                    #region WaferFab settings

                    EPTDistributionReader distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

                    waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions(eptParameterFile);
                    waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions(lotStepOvertaking);
                    waferFabSettings.WCDispatcherTypes[wc] = DispatcherBase.Type.EPTOVERTAKING;


                    #endregion

                    #region Make starting lots
                    AutoDataReader dataReader = new AutoDataReader(Path.Combine(inputDirectory, "Auto"), Path.Combine(inputDirectory, "SerializedFiles"));

                    #endregion

                    #region Building the model
                    WaferFab waferFab = new WaferFab(simulation.MyModel, "WaferFab", new ConstantDistribution(60 * 60 * 24), initialDateTime);

                    WorkCenter workCenter = new WorkCenter(waferFab, $"WorkCenter_{wc}", waferFabSettings.WCServiceTimeDistributions[wc], waferFabSettings.LotStepsPerWorkStation[wc]);

                    // Connect workcenter to WIPDependentDistribution
                    EPTDistribution distr = (EPTDistribution)waferFabSettings.WCServiceTimeDistributions[wc];

                    distr.WorkCenter = workCenter;

                    EPTOvertakingDispatcher dispatcher = new EPTOvertakingDispatcher(workCenter, workCenter.Name + "_EPTOvertakingDispatcher", waferFabSettings.WCOvertakingDistributions[wc]);

                    workCenter.SetDispatcher(dispatcher);

                    // Connect workcenter to OvertakingDistribution
                    waferFabSettings.WCOvertakingDistributions[wc].WorkCenter = workCenter;

                    waferFab.AddWorkCenter(workCenter.Name, workCenter);

                    // Sequences
                    foreach (var sequence in waferFabSettings.Sequences)
                    {
                        waferFab.AddSequence(sequence.Key, sequence.Value);
                    }

                    // LotSteps
                    waferFab.LotSteps = waferFab.Sequences.Select(x => x.Value).Select(x => x.GetCurrentStep(0)).ToDictionary(x => x.Name);

                    // LotGenerator
                    waferFab.SetLotGenerator(new LotGenerator(waferFab, "LotGenerator", new ConstantDistribution(60), true));

                    // Add lotstarts
                    waferFab.LotStarts = waferFabSettings.LotStarts;

                    // Add observers
                    LotOutObserver lotOutObserver = new LotOutObserver(simulation, wc + "_" + overtaking + "LotOutObserver");
                    dispatcher.Subscribe(lotOutObserver);
                    OptimiserObserver optimiserObserver = new OptimiserObserver(simulation, "TotalQueueObserver");
                    workCenter.Subscribe(optimiserObserver);
                    #endregion

                    #region Read initial lots
                    //RealSnapshotReader reader = new RealSnapshotReader();

                    //List<RealSnapshot> realSnapshots = reader.Read(Path.Combine(inputDirectory, "SerializedFiles", reader.GetRealSnapshotString(initialDateTime)), 1);

                    //RealSnapshot realSnapShot = realSnapshots.Where(x => x.Time == initialDateTime).First();

                    //List<string> lotSteps = workCenter.LotSteps.Select(x => x.Name).ToList();

                    //List<RealLot> initialRealLots = realSnapShot.GetRealLots(1).Where(x => lotSteps.Contains(x.IRDGroup)).ToList();

                    //List<Lot> initialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences, initialDateTime)).ToList();

                    //waferFab.InitialLots = initialLots;
                    #endregion

                    simulation.Run();

                    #region Reporting
                    SimulationReporter reporter = simulation.MakeSimulationReporter();

                    reporter.PrintSummaryToConsole();
                    #endregion
                }
            }

           
        }
    }
}
