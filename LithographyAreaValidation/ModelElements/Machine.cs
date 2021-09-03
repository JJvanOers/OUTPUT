using CSSL.Modeling;
using CSSL.Modeling.CSSLQueue;
using CSSL.Modeling.Elements;
using CSSL.Utilities.Distributions;
using LithographyAreaValidation.DataReader;
using LithographyAreaValidation.Distributions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LithographyAreaValidation.ModelElements
{
    public class Machine : SchedulingElementBase
    {
        public Machine(ModelElementBase parent, string name, DispatcherBase dispatcher, int seedNumber) : base(parent, name)
        {
            // Get lithographyArea object
            LithographyArea = (LithographyArea)parent;

            // Get dispatcher object
            Dispatcher = dispatcher;

            // Create new queue object
            Queue = new CSSLQueue<Lot>(this, name + "_Queue");

            List<string> RMSTools = new List<string> { "StepCluster#5", "StepCluster#7", "ASML#9" };

            if (RMSTools.Contains(name))
            {
                DeterministicNonProductiveTimeDictionary = new Dictionary<string, double>
                {
                    { "SameReticle", LithographyArea.DeterministicNonProductiveTimesRMS["SameReticle"] },
                    { "DifferentReticle", LithographyArea.DeterministicNonProductiveTimesRMS["DifferentReticle"] },
                    { "DifferentIRD", LithographyArea.DeterministicNonProductiveTimesRMS["DifferentIRD"] }
                };
            }
            else
            {
                DeterministicNonProductiveTimeDictionary = new Dictionary<string, double>
                {
                    { "SameReticle", LithographyArea.DeterministicNonProductiveTimesARMS["SameReticle"] },
                    { "DifferentReticle", LithographyArea.DeterministicNonProductiveTimesARMS["DifferentReticle"] },
                    { "DifferentIRD", LithographyArea.DeterministicNonProductiveTimesARMS["DifferentIRD"] }
                };
            }

            // Create processingTimeDictionary
            DeterministicProcessingTimeDictionary = LithographyArea.Reader.ReadDeterministicProcessingTimes(Name);
            

            if (LithographyArea.Stochastic)
            {
                // Create processingTimeDictionary
                StochasticProcessingTimeDictionary = LithographyArea.Reader.ReadStochasticProcessingTimes(Name);
                // Create nonProductiveTimeProbabilityDictionary
                NonProductiveTimeProbabilityDictionary = LithographyArea.Reader.ReadNonProductiveTimeProbabilities(Name);
                // Create nonProductiveTimeDictionary
                StochasticNonProductiveTimeDictionary = LithographyArea.Reader.ReadStochasticNonProductiveTimes(Name);
            }

            startState = LithographyArea.Reader.ReadRealStartState(Name,LithographyArea.StartDate);

            SeedNumber = seedNumber;

            MachineDownRandomNumber = new Random();
        }

        // Properties
        private LithographyArea LithographyArea { get; }

        private DispatcherBase Dispatcher { get; }

        public CSSLQueue<Lot> Queue { get; }

        private List<Object> startState;
        public Dictionary<string, Distribution> DeterministicProcessingTimeDictionary;
        public Dictionary<string, Distribution> StochasticProcessingTimeDictionary;
        private Dictionary<string, Distribution> StochasticNonProductiveTimeDictionary { get; }
        private Dictionary<string, double> NonProductiveTimeProbabilityDictionary { get; }
        public Dictionary<string, double> DeterministicNonProductiveTimeDictionary { get; }

        public string PreviousLotIRDName { get; set; }

        public string PreviousReticleID { get; set; }

        public bool EndedOnError { get; set; } 

        public Lot CurrentLot { get; private set; }

        public double CurrentStartRun { get; private set; }

        public double CurrentEndRun { get; private set; }   

        public double StartMachineDown { get; set; }

        public double EndMachineDown { get; set; }

        private Random MachineDownRandomNumber { get; set; }

        private int SeedNumber { get; set; }

        protected override void OnReplicationStart()
        {
            PreviousLotIRDName = null;
            PreviousReticleID = null;
            EndedOnError = false;

            ScheduleEvent(0, HandleStartReplication);
        }

        public void HandleStartReplication(CSSLEvent e)
        {
            //string state = "Up"; //(string)startState[0];
            //DateTime startDateTime = (DateTime)startState[1];

            //ouble startFirstLot = GetTime;//startDateTime.Subtract(LithographyArea.StartDate).TotalSeconds;

            //if (state == "Down")
            //{
            //    LithographyArea.MachineStates[Name] = "Down";

            //    Dispatcher.HandleMachineDown(this);

            //    ScheduleEvent(GetTime + startFirstLot, HandleEndMachineDown);
            //}

            ScheduleEvent(GetTime, DispatchFirstLot);  //ScheduleEvent(GetTime + startFirstLot, DispatchFirstLot);
        }

        private void HandleStartRun(CSSLEvent e)
        {
            // Get dispatched lot
            Lot lot = Queue.PeekFirst();

            // Get LotID
            CurrentLot = lot;

            // Get startRun timestamp
            CurrentStartRun = GetTime;

            double processingTime;
            if (LithographyArea.Stochastic)
            {
                // Get processing time of lot
                processingTime = GetStochasticProcessingTime(lot);
            }
            else
            {
                // Get processing time of lot
                processingTime = GetDeterministicProcessingTime(lot);
            }

            // Schedule endRun
            ScheduleEvent(GetTime + processingTime, HandleEndRun);
        }

        private void HandleEndRun(CSSLEvent e)
        {
            // Get finished lot
            Lot finishedLot = Queue.DequeueFirst();

            // Make Resource Available
            Dispatcher.MakeResourceAvailable(finishedLot);

            // Send reticle back to dispatcher
            Dispatcher.HandleReticleArrival(finishedLot.ReticleID1, Name);

            // Get endRun timestamp
            CurrentEndRun = GetTime;

            // HandleEndRun in LithographyArea
            LithographyArea.HandleEndRun(finishedLot,this);

            // Notify machine observer
            NotifyObservers(this);

            // Check if machine goes Down
            double rnd = MachineDownRandomNumber.NextDouble();
            double total = 0;
            string nonProductiveTimeType = null;

            if (LithographyArea.Stochastic)
            {
                foreach (KeyValuePair<string, double> entry in NonProductiveTimeProbabilityDictionary)
                {
                    total += entry.Value;
                    if (rnd < total)
                    {
                        nonProductiveTimeType = entry.Key;
                        break;
                    }
                }
            }

            if (nonProductiveTimeType == "Down")
            {
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;

                StartMachineDown = GetTime;

                double nonProductiveTime = StochasticNonProductiveTimeDictionary[nonProductiveTimeType].Next();

                Dispatcher.HandleMachineDown(this);

                ScheduleEvent(GetTime + nonProductiveTime, HandleEndMachineDown);
            }
            else
            {
                DispatchNextLot();
                TriggerMachinesWaiting(); //TODO: Change
            }
        }

        private void DispatchFirstLot(CSSLEvent e)
        {
            Lot lot;

            // Get lot by dispatching in the dispatcher
            lot = Dispatcher.Dispatch(this);

            if (lot != null)
            {
                if (PreviousLotIRDName == null)
                {
                    PreviousLotIRDName = lot.IrdName;
                    PreviousReticleID = lot.ReticleID1;
                }
                else
                {
                    if (PreviousLotIRDName != lot.IrdName)
                    {
                        LithographyArea.TotalLayerSwitches += 1;
                    }
                    if (PreviousReticleID != lot.ReticleID1)
                    {
                        LithographyArea.TotalReticleSwitches += 1;
                    }
                    PreviousLotIRDName = lot.IrdName;
                    PreviousReticleID = lot.ReticleID1;

                }

                // Place lot in queue of machine
                Queue.EnqueueLast(lot);

                // If machine was waiting
                if (LithographyArea.WaitingMachines.Contains(this))
                {
                    // Remove machine from waiting list
                    LithographyArea.WaitingMachines.Remove(this);
                }

                // First nonProductiveTime = 0
                double nonProductiveTime = 0;

                // Schedule next startRun
                ScheduleEvent(GetTime + nonProductiveTime, HandleStartRun);
            }
            else //no lot is in queue which can be produced
            {
                // let machine wait
                if (!LithographyArea.WaitingMachines.Contains(this))
                {
                    LithographyArea.WaitingMachines.Add(this);
                }
            }
        }

        public void DispatchNextLotEvent(CSSLEvent e)
        {
            DispatchNextLot();
        }

        public void DispatchNextLot()
        {
            Lot lot;

            // Get lot by dispatching in the dispatcher
            lot = Dispatcher.Dispatch(this);

            if (lot != null)
            {
                Boolean layerChange = false;
                Boolean reticleChange = false;

                if (PreviousLotIRDName == null)
                {
                    PreviousLotIRDName = lot.IrdName;
                    PreviousReticleID = lot.ReticleID1;
                }
                else
                {
                    if (PreviousLotIRDName != lot.IrdName)
                    {
                        LithographyArea.TotalLayerSwitches += 1;
                        layerChange = true;
                    }
                    if (PreviousReticleID != lot.ReticleID1)
                    {
                        LithographyArea.TotalReticleSwitches += 1;
                        reticleChange = true;
                    }
                    PreviousLotIRDName = lot.IrdName;
                    PreviousReticleID = lot.ReticleID1;
                }

                // Place lot in queue of machine
                Queue.EnqueueLast(lot);

                // If machine was waiting
                if (LithographyArea.WaitingMachines.Contains(this))
                {
                    // Remove machine from waiting list
                    LithographyArea.WaitingMachines.Remove(this);
                }

                // Get NonProductiveTime of machine
                double nonProductiveTime;
                if (LithographyArea.Stochastic)
                {
                    if (EndMachineDown == GetTime)
                    {
                        nonProductiveTime = 0;
                    }
                    else
                    {
                        nonProductiveTime = GetNonStochasticProductiveTime(layerChange, reticleChange);
                    }
                    
                }
                else
                {
                    nonProductiveTime = GetDeterministicNonProductiveTime(layerChange, reticleChange);
                }

                if (!LithographyArea.Dynamic && Dispatcher.Name == "CPDispatcher")
                {
                    nonProductiveTime = 0;
                }
                
                // Schedule next startRun
                ScheduleEvent(GetTime + nonProductiveTime, HandleStartRun);
            }
            else //no lot is in queue which can be produced
            {
                // let machine wait
                if (!LithographyArea.WaitingMachines.Contains(this))
                {
                    LithographyArea.WaitingMachines.Add(this);
                }
            }
        }

        private void TriggerMachinesWaiting()
        {
            // If machines are waiting
            if (LithographyArea.WaitingMachines.Count > 0)
            {
                List<Machine> copyList = new List<Machine>(LithographyArea.WaitingMachines);
                // Trigger HandleStartRun for machines waiting
                foreach (Machine machine in copyList)
                {
                    if (machine!=this)
                    {
                        machine.DispatchNextLot();
                    }
                }
            }
        }

        public double GetDeterministicProcessingTime(Lot lot)
        {
            double processingTime;
            int lotQty = lot.LotQty;

            // Get processing time
            if (DeterministicProcessingTimeDictionary.ContainsKey(lot.MasksetLayer)) // If processing time is known for reticle
            {
                processingTime = DeterministicProcessingTimeDictionary[lot.MasksetLayer].Next();
            }
            else // If processing time is not known for reticle, but is known for recipe
            {
                if (Name.Contains("StepCluster"))
                {
                    if (DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStepCluster))
                    {
                        processingTime = DeterministicProcessingTimeDictionary[lot.RecipeStepCluster].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        Console.WriteLine($"No Processing Time known of Lot: {lot.LotID} on Machine: {this.Name}");
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }

                }
                else
                {
                    if (DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStandAlone))
                    {
                        processingTime = DeterministicProcessingTimeDictionary[lot.RecipeStandAlone].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        Console.WriteLine($"No Processing Time known of Lot: {lot.LotID} on Machine: {this.Name}");
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }

                }
            }

            processingTime = (processingTime / 25) * lotQty;

            processingTime = Math.Ceiling(processingTime);

            return processingTime;
        }

        public double GetDeterministicProcessingTimeFullLot(Lot lot)
        {
            double processingTime;
            int lotQty = lot.LotQty;

            // Get processing time
            if (DeterministicProcessingTimeDictionary.ContainsKey(lot.MasksetLayer)) // If processing time is known for reticle
            {
                processingTime = DeterministicProcessingTimeDictionary[lot.MasksetLayer].Next();
            }
            else // If processing time is not known for reticle, but is known for recipe
            {
                if (Name.Contains("StepCluster"))
                {
                    if (DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStepCluster))
                    {
                        processingTime = DeterministicProcessingTimeDictionary[lot.RecipeStepCluster].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }
                }
                else
                {
                    if (DeterministicProcessingTimeDictionary.ContainsKey(lot.RecipeStandAlone))
                    {
                        processingTime = DeterministicProcessingTimeDictionary[lot.RecipeStandAlone].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }
                }
            }

            return processingTime;
        }

        public double GetStochasticProcessingTime(Lot lot)
        {
            double processingTime;
            int lotQty = lot.LotQty;

            // Get processing time
            if (StochasticProcessingTimeDictionary.ContainsKey(lot.MasksetLayer)) // If processing time is known for reticle
            {
                processingTime = StochasticProcessingTimeDictionary[lot.MasksetLayer].Next();
            }
            else // If processing time is not known for reticle, but is known for recipe
            {
                if (Name.Contains("StepCluster"))
                {
                    if (StochasticProcessingTimeDictionary.ContainsKey(lot.RecipeStepCluster))
                    {
                        processingTime = StochasticProcessingTimeDictionary[lot.RecipeStepCluster].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        Console.WriteLine($"No Processing Time known of Lot: {lot.LotID} on Machine: {this.Name}");
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }

                }
                else
                {
                    if (StochasticProcessingTimeDictionary.ContainsKey(lot.RecipeStandAlone))
                    {
                        processingTime = StochasticProcessingTimeDictionary[lot.RecipeStandAlone].Next();
                    }
                    else
                    {
                        processingTime = 30 * 60; //TODO: Validate this assumption
                        Console.WriteLine($"No Processing Time known of Lot: {lot.LotID} on Machine: {this.Name}");
                        EndedOnError = true;
                        NotifyObservers(this);
                        ScheduleEndEvent(GetTime);
                    }

                }
            }

            processingTime = (processingTime / 25) * lotQty;

            processingTime = Math.Ceiling(processingTime);

            return processingTime;
        }

        private double GetDeterministicNonProductiveTime(Boolean layerChange, Boolean reticleChange)
        {
            string nonProductiveTimeType = null;

            if (layerChange)
            {
                nonProductiveTimeType = "DifferentIRD";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = DeterministicNonProductiveTimeDictionary[nonProductiveTimeType];
                return nonProductiveTime;
            }
            else if (reticleChange)
            {
                nonProductiveTimeType = "DifferentReticle";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = DeterministicNonProductiveTimeDictionary[nonProductiveTimeType];
                return nonProductiveTime;
            }
            else
            {
                nonProductiveTimeType = "SameReticle";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = DeterministicNonProductiveTimeDictionary[nonProductiveTimeType];
                return nonProductiveTime;
            }
        }

        private double GetNonStochasticProductiveTime(Boolean layerChange, Boolean reticleChange)
        {
            string nonProductiveTimeType = null;

            if (layerChange)
            {
                nonProductiveTimeType = "DifferentIRD";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = StochasticNonProductiveTimeDictionary[nonProductiveTimeType].Next();
                return nonProductiveTime;
            }
            else if (reticleChange)
            {
                nonProductiveTimeType = "DifferentReticle";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = StochasticNonProductiveTimeDictionary[nonProductiveTimeType].Next();
                return nonProductiveTime;
            }
            else
            {
                nonProductiveTimeType = "SameReticle";
                LithographyArea.MachineStates[Name] = nonProductiveTimeType;
                double nonProductiveTime = StochasticNonProductiveTimeDictionary[nonProductiveTimeType].Next();
                return nonProductiveTime;
            }
        }

        private void HandleEndMachineDown(CSSLEvent e)
        {
            LithographyArea.MachineStates[Name] = "Up";
            Dispatcher.HandleEndMachineDown(); // Reschedule if needed
            DispatchNextLot();
            TriggerMachinesWaiting(); // Maybe not needed

            EndMachineDown = GetTime;
            LithographyArea.HandleEndMachineDown(this);
        }
    }
}
