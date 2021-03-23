using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WaferFabSim.WaferFabElements;
using WaferFabSim.WaferFabElements.Utilities;
using static WaferFabSim.WaferFabElements.Utilities.EPTDistribution;
using static WaferFabSim.WaferFabElements.Utilities.OvertakingDistributionBase;

namespace WaferFabSim.Import.Distributions
{
    public class EPTDistributionReader : IWorkCenterDistributionReader
    {
        private string directory;

        private List<string> workcenters;

        private Dictionary<string, List<LotStep>> lotStepsPerWorkStation;

        public EPTDistributionReader(string directory, List<string> workcenters, Dictionary<string, List<LotStep>> lotStepsPerWorkStation) 
        {
            this.directory = directory;

            this.workcenters = workcenters;

            this.lotStepsPerWorkStation = lotStepsPerWorkStation;
        }

        public Dictionary<string, Distribution> GetServiceTimeDistributions(bool isFitted = true)
        {
            Dictionary<string, Distribution> dict = new Dictionary<string, Distribution>();

            Dictionary<string, WIPDepDistParameters> parameters = new Dictionary<string, WIPDepDistParameters>();

            // Read fitted WIP dependent EPT parameters from csv
            using (StreamReader reader = new StreamReader(Path.Combine(directory, isFitted ? "FittedEPTParameters.csv" : "OptimisedEPTParameters.csv")))
            {
                string[] headers = reader.ReadLine().Trim(',').Split(',');

                while (!reader.EndOfStream)
                {
                    WIPDepDistParameters par = new WIPDepDistParameters();

                    string[] data = reader.ReadLine().Trim(',').Split(',');

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (headers[i] == "WorkStation")
                        {
                            if (!workcenters.Contains(data[i]))
                            {
                                throw new Exception($"Waferfab does not contain workcenter {data[i]}");
                            }
                            par.WorkCenter = data[i];
                        }
                        if (headers[i] == "wip_min") { par.LBWIP = (int)double.Parse(data[i]); }
                        if (headers[i] == "wip_max") { par.UBWIP = (int)double.Parse(data[i]); }
                        if (headers[i] == "t_min") { par.Tmin = double.Parse(data[i]); }
                        if (headers[i] == "t_max") { par.Tmax = double.Parse(data[i]); }
                        if (headers[i] == "t_decay") { par.Tdecay = double.Parse(data[i]); }
                        if (headers[i] == "c_min") { par.Cmin = double.Parse(data[i]); }
                        if (headers[i] == "c_max") { par.Cmax = double.Parse(data[i]); }
                        if (headers[i] == "c_decay") { par.Cdecay = double.Parse(data[i]); }
                    }

                    parameters.Add(par.WorkCenter, par);
                }
            }

            foreach (string wc in workcenters)
            {
                dict.Add(wc, new EPTDistribution(parameters[wc]));
            }

            return dict;
        }


        public Dictionary<string, OvertakingDistributionBase> GetOvertakingDistributions()
        {
            Dictionary<string, OvertakingDistributionBase> distributions = new Dictionary<string, OvertakingDistributionBase>();

            // Read all overtaking records
            List<OvertakingRecord> records = new List<OvertakingRecord>();

            using (StreamReader reader = new StreamReader(Path.Combine(directory, "OvertakingRecords.csv")))
            {
                string headerline = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string dataline = reader.ReadLine();

                    OvertakingRecord rec = new OvertakingRecord();

                    string[] headers = headerline.Trim(',').Split(',');
                    string[] data = dataline.Trim(',').Split(',');

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (headers[i] == "WorkStation") { rec.WorkCenter = data[i]; }
                        else if (headers[i] == "IRDGroup") { rec.LotStep = data[i]; }
                        else if (headers[i] == "WIPIn") { rec.WIPIn = Convert.ToInt32(data[i]); }
                        else if (headers[i] == "OvertakenLots") { rec.OvertakenLots = Convert.ToInt32(data[i]); }
                        else { throw new Exception($"{headers[i]} is unknown"); }
                    }

                    records.Add(rec);
                }
            }

            // Read overtaking distribution parameters
            OvertakingDistributionParameters parameters = new OvertakingDistributionParameters(10, 100, 1);

            // Select records per workcenter per lotstep
            foreach (string wc in workcenters)
            {
                List<OvertakingRecord> recordsWC = records.Where(x => x.WorkCenter == wc).ToList();

                distributions.Add(wc, new LotStepOvertakingDistribution(recordsWC, parameters, lotStepsPerWorkStation[wc]));
            }

            return distributions;
        }

    }
}
