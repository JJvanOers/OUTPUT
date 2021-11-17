using CSSL.Modeling.CSSLQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Priority_Queue;

namespace WaferFabSim.WaferFabElements
{
    [Serializable]
    public class Lot : CSSLQueueObject<Lot>
    {
        public string LotID { get; set; }

        public string ProductType => Sequence.ProductType;

        public string ProductGroup => Sequence.ProductGroup;

        public Sequence Sequence { get; }

        /// <summary>
        /// Lot steps from 0 to number of steps. Initialized on -1 before released in fab.
        /// </summary>
        /// <param name="currentStepCount"></param>
        /// <returns></returns>
        public int CurrentStepCount { get; private set; }

        /// <summary>
        /// Simulation time which the lot got released in the fab.
        /// </summary>
        public double StartTime { get; set; }
        //public DateTime? TimeMinimum { get; set; }

        /// <summary>
        /// Wall clock date time when the lot got released in the fab.
        /// </summary>
        public DateTime? StartTimeReal { get; set; }
        public double EndTime { get; private set; }

        /// <summary>
        /// Temporary used for WaferAreaSim
        /// </summary>
        public int WIPIn { get; set; }

        #region Data from original real lot activity
        /// <summary>
        /// Arrival time stamp of original lot activity in workcenter. Used for initial lots, for non-initial lot this is null.
        /// </summary>
        public DateTime? ArrivalReal { get; set; }

        /// <summary>
        /// Departure time stamp of original lot activity in workcenter. Used for initial lots, for non-initial lot this is null.
        /// </summary>
        public DateTime? DepartureReal { get; set; }

        /// <summary>
        /// Overtaken lots of original Lotactivity in workcenter
        /// </summary>
        public int? OvertakenLotsReal { get; set; }

        /// <summary>
        /// WIP right before arrival of original lot activity in workcenter. Used for initial lots, for non-initial lot this is -1.
        /// </summary>
        public int WIPInReal { get; set; } = -1;

        /// <summary>
        /// Cycle time of original real lot activity
        /// </summary>
        public double CycleTimeReal
        {
            get
            {
                if (DepartureReal != null && ArrivalReal != null)
                {
                    TimeSpan cycle = (TimeSpan)(DepartureReal - ArrivalReal);

                    return cycle.TotalSeconds;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Wafer quantity of original real lot
        /// </summary>
        public int QtyReal { get; set; }

        public int? PlanDayReal { get; set; }

        public int ClipWeekReal { get; set; }
        #endregion

        public WorkCenter GetCurrentWorkCenter => Sequence.GetCurrentWorkCenter(CurrentStepCount);

        public WorkCenter GetNextWorkCenter => Sequence.GetNextWorkCenter(CurrentStepCount);

        public LotStep GetCurrentStep => Sequence.GetCurrentStep(CurrentStepCount);
        
        // TPT predictor is not set if it remains null. If no predictor is set, GetCurrentSchedDev is unusable.
        // public bool HasPlanDay => Sequence.GetCurrentStep(CurrentStepCount).PlanDay != null;
        // public bool HasProdPrediction => Sequence.GetCurrentStep(CurrentStepCount).ProdPrediction != null;
        // public bool HasTechPrediction => Sequence.GetCurrentStep(CurrentStepCount).TechPrediction != null;

        /// <summary>
        /// Current Schedule Deviation calculated as predicted days until this step, minus actual days passed. Negative if job is late to step. (Variable expression)
        /// </summary> 
        public int GetCurrentSchedDev()
        {
            if (StartTimeReal != null)
            {
                return (int)Math.Round((GetCurrentODD - GetCurrentWorkCenter.GetTime) / 24 / 60 / 60); // Schedule Deviation in days 
            }
            else
            {
                // Console.WriteLine($"WARNING: Start time not given for {ProductType}");
                return int.MinValue; // If starttime is not given, give this lot maximum lateness. (This not printed in the LotOutObserver)
            }
        }

        public double GetDueDate => (StartTimeReal != null) ? StartTime + Sequence.StandardTPT * 24 * 60 * 60 : double.MinValue;  // due date in seconds
        public double GetCurrentODD => (StartTimeReal != null) ? GetDueDate - Sequence.RemainingTimePredictor(CurrentStepCount) * 24 * 60 * 60 : double.MinValue; // operation due date in seconds

        public double GetCriticalRatio()
        {
            if (StartTimeReal != null)
            {
                double DaysTillDueDate = (GetDueDate - GetCurrentWorkCenter.GetTime) / 24 / 60 / 60;
                double CR;
                if (DaysTillDueDate >= 0)
                {
                    CR = (1 + DaysTillDueDate) / (1 + Sequence.RemainingTimePredictor(CurrentStepCount));
                }
                else
                {
                    CR = 1 / ((1 + Math.Abs(DaysTillDueDate)) * (1 + Sequence.RemainingTimePredictor(CurrentStepCount)));
                }
                return CR;
            }
            else { return 0; }
        }
        public double GetCriticalRatioAlt()
        {
            if (StartTimeReal != null)
            {
                double DaysTillDueDate = (GetDueDate - GetCurrentWorkCenter.GetTime) / 24 / 60 / 60;
                return DaysTillDueDate / (1 + Sequence.RemainingTimePredictor(CurrentStepCount));
            }
            else { return double.MinValue; }
        }

        /// <summary>
        /// Calculates schedule deviation upon leaving the factory, in days. Negative if job is late. (Constant expression)
        /// </summary>
        public int ClipDayDeviation { get; private set; } // Schedule deviation after 

        public LotStep GetNextStep => Sequence.GetNextStep(CurrentStepCount);

        public bool HasRelativeStep(int relativeStepCount) => Sequence.HasRelativeStep(CurrentStepCount, relativeStepCount);

        public LotStep GetRelativeStep(int relativeStepCount) => Sequence.GetRelativeStep(CurrentStepCount, relativeStepCount);

        public WorkCenter GetRelativeWorkCenter(int relativeStepCount) => Sequence.GetRelativeWorkCenter(CurrentStepCount, relativeStepCount);

        public bool HasNextStep => Sequence.HasNextStep(CurrentStepCount);

        public void SendToNextWorkCenter()
        {
            // If has next step, send to next work station. Otherwise, do nothing and lot will dissapear from system.
            if (Sequence.HasNextStep(CurrentStepCount))
            {
                WorkCenter nextWorkCenter = GetNextWorkCenter;

                CurrentStepCount++;

                if (CurrentStepCount == 0)
                { // Means it is the first step, it is now released in fab so start time can be saved.
                    StartTime = nextWorkCenter.GetTime;
                    StartTimeReal = nextWorkCenter.GetDateTime;
                }

                nextWorkCenter.HandleArrival(this);

            }
            else
            {
                EndTime = GetCurrentWorkCenter.GetTime;
                ClipDayDeviation = (int)Math.Round(StartTime / 24 / 60 / 60 + Sequence.StandardTPT - GetCurrentWorkCenter.GetTime / 24 / 60 / 60); // set final lateness
            }
        }

        public Lot ShiftStarts(double StartTimeShiftFactor, DateTime timeMinimum, DateTime InitialDateTime)
        {
            if (StartTimeReal != null)
            {
                TimeSpan timeStepMinimum = (DateTime)StartTimeReal - timeMinimum;
                DateTime newStartReal = timeMinimum + timeStepMinimum * StartTimeShiftFactor;
                StartTimeReal = newStartReal;
                if (StartTime != default)
                {
                    TimeSpan timeDelta = (DateTime)StartTimeReal - InitialDateTime;
                    StartTime = timeDelta.TotalSeconds;
                }
            }
            return this;
        }

        public Lot SetAsInitialLot(DateTime InitialDateTime)
        {
            if (StartTimeReal != null)
            {
                TimeSpan timeDelta = (DateTime)StartTimeReal - InitialDateTime;
                StartTime = timeDelta.TotalSeconds;
                SetBestFittingStep();
            }
            else throw new Exception($"Lot {Id} has no StartTimeReal, even though it came from the LotGenerator list.");
            return this;
        }

        public Lot SetBestFittingStep()
        {
            List<Tuple<double, int>> selectStep = new List<Tuple<double, int>>();
            for (int i = 0; i < Sequence.stepCount; i++)
            {
                selectStep.Add(new Tuple<double, int>(Math.Abs(StartTime + (Sequence.StandardTPT - Sequence.RemainingTimePredictor(i)) * 24 * 60 * 60), i));
            }
            int BestStep = selectStep.OrderBy(x => x.Item1).First().Item2;
            //Console.WriteLine($"Selecting step {BestStep} with Schedule Deviation {GetCurrentSchedDev()}");
            SetCurrentStepCount(BestStep);
            int maxLatenessForInitial = -15;
            if (!HasNextStep)
            {
                return (GetCurrentSchedDev() >= maxLatenessForInitial) ? this : null;  // if the job is fitted to final step and waiting for more than specified time, the lot leaves the system
            }
            return this;
        }

        public void SetCurrentStepCount(int i)
        {
            CurrentStepCount = i;
        }

        public Lot(double creationTime, Sequence sequence) : base(creationTime)
        {
            CurrentStepCount = -1;

            Sequence = sequence;

            // If a sequence with a Standard TPT of 0 is given, the schedule deviation prediction will not be valid
            if (Sequence.StandardTPT == 0){ throw new Exception($"Lot {LotID} of producttype {ProductType} has TPT set to 0. Check AllProductAttributes table."); }
        }

        /// <summary>
        /// Deep copy lot, such that original initial lot does not change its current step count in a replication.
        /// </summary>
        /// <param name="lotToDeepCopy">Original lot to make deep copy of.</param>
        public Lot(Lot lotToDeepCopy)
        {
            StartTimeReal = lotToDeepCopy.StartTimeReal;
            LotID = lotToDeepCopy.LotID;
            Sequence = lotToDeepCopy.Sequence;
            CurrentStepCount = lotToDeepCopy.CurrentStepCount;
            StartTime = lotToDeepCopy.StartTime;
            EndTime = lotToDeepCopy.EndTime; //
            ArrivalReal = lotToDeepCopy.ArrivalReal; //
            DepartureReal = lotToDeepCopy.DepartureReal; //
            WIPInReal = lotToDeepCopy.WIPInReal;
            OvertakenLotsReal = lotToDeepCopy.OvertakenLotsReal;
            PlanDayReal = lotToDeepCopy.PlanDayReal;
            ClipWeekReal = lotToDeepCopy.ClipWeekReal; //
            QtyReal = lotToDeepCopy.QtyReal;
        }

        public Lot()
        {

        }
    }
}
