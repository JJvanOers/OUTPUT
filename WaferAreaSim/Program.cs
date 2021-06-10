using CSSL.Calendar;
using CSSL.Modeling;
using CSSL.Reporting;
using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WaferFabSim;
using WaferFabSim.Import;
using WaferFabSim.Import.Distributions;
using WaferFabSim.InputDataConversion;
using WaferFabSim.SnapshotData;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Dispatchers;
using WaferFabSim.WaferFabElements.Observers;
using WaferFabSim.WaferFabElements.Utilities;

namespace WaferAreaSim
{
    public class Program
    {
        static void Main(string[] args)
        {
            List<string> workcenters = new List<string>()
            {"BACKGRIND", "BATCH UP", "CMP", "DICE", "DRY ETCH", "ELEC TEST", "EVAPORATION", "FURNACING", "IMPLANT",
                "INSPECTION", "LPCVD", "MERCURY", "NITRIDE DEP", "OFF LINE INK", "PACK", "PHOTOLITH", "PROBE", "REPORTING",
                "SAMPLE TEST", "SPUTTERING", "WET ETCH"};
            
            DateTime initialDateTime = new DateTime(2019, 10, 1);

            string inputDirectory = @"E:\OneDrive - Nexperia\CSSLWaferFab\Input";

            string outputDirectory = @"C:\CSSLWaferFab\Output\WaferFabArea";

            RealSnapshotReader reader = new RealSnapshotReader();

            List<RealSnapshot> realSnapshots = reader.Read(Path.Combine(inputDirectory, "SerializedFiles", reader.GetRealSnapshotString(initialDateTime)), 1);

            RealSnapshot realSnapShot = realSnapshots.Where(x => x.Time == initialDateTime).First();

            //workcenters = new List<string>() {"INSPECTION"};
            foreach (string workcenter in workcenters)
            {
                #region Parameters

                string wc = workcenter;

                bool isFitted = false; // true = fitted, false = optimised

                bool lotStepOvertaking = true;
                #endregion

                #region Initializing simulation
                Simulation simulation = new Simulation(wc, outputDirectory);
                #endregion

                #region Experiment settings
                simulation.MyExperiment.NumberOfReplications = 30;

                DateTime finalDateTime = new DateTime(2019, initialDateTime.Month + 2, 1);
                simulation.MyExperiment.LengthOfReplication = (finalDateTime - initialDateTime).TotalSeconds; // Number of seconds between two months

                simulation.MyExperiment.LengthOfWarmUp = 60 * 60 * 24 * 0;                
                #endregion

                #region WaferFab settings
                WaferFabSettings waferFabSettings = Deserializer.DeserializeWaferFabSettings(Path.Combine(inputDirectory, "SerializedFiles", $"WaferFabSettings_{wc}_WithLotStarts.dat"));

                EPTDistributionReader distributionReader = new EPTDistributionReader(Path.Combine(inputDirectory, "CSVs"), waferFabSettings.WorkCenters, waferFabSettings.LotStepsPerWorkStation);

                waferFabSettings.WCServiceTimeDistributions = distributionReader.GetServiceTimeDistributions(isFitted);

                waferFabSettings.WCOvertakingDistributions = distributionReader.GetOvertakingDistributions(lotStepOvertaking);
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

                // Add initial lots             
                List<RealLot> initialRealLots = realSnapShot.GetRealLots(1).Where(x => x.LotActivity.WorkStation == wc).ToList();

                waferFab.InitialLots = initialRealLots.Select(x => x.ConvertToLotArea(0, waferFabSettings.Sequences, initialDateTime)).ToList();

                // Add observers
                LotOutObserver lotOutObserver = new LotOutObserver(simulation, wc + "_LotOutObserver");
                dispatcher.Subscribe(lotOutObserver);

                TotalQueueObserver totalQueueObserver = new TotalQueueObserver(simulation, wc + "_TotalQueueObserver");
                workCenter.Subscribe(totalQueueObserver);
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
