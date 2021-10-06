using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WaferFabSim.InputDataConversion;

namespace WaferFabSim.SnapshotData
{
    public class RealSnapshotReader
    {
        public RealSnapshotReader()
        {
            RealLots = new List<RealLot>();
            RealSnapshots = new List<RealSnapshot>();
        }
        public string Filename { get; set; }

        public int WaferQtyThreshold { get; set; }

        public List<RealLot> RealLots { get; set; }

        public List<RealSnapshot> RealSnapshots { get; set; }

        public List<RealSnapshot> Read(string filename, int waferQtyThreshold)
        {
            RealLots.Clear();
            RealSnapshots.Clear();

            WaferQtyThreshold = waferQtyThreshold;
            Filename = filename;

            string type = Path.GetExtension(filename).ToLower();

            //if (type == ".csv")
            //{
            //    readCSV();
            //}
            if (type == ".dat")
            {
                ReadDAT();
            }
            else
            {
                throw new Exception($"Cannot read file type {type}");
            }

            return RealSnapshots;
        }

        private void ReadDAT()
        {
            RealSnapshots = Tools.ReadFromBinaryFile<List<RealSnapshot>>(Filename);
            RealLots = RealSnapshots.SelectMany(x => x.GetRealLots(WaferQtyThreshold)).ToList();
        }

        private List<RealLot> fillAllLots()
        {
            List<RealLot> allLots = new List<RealLot>();

            using (StreamReader reader = new StreamReader(Filename))
            {
                string headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string dataLine = reader.ReadLine();

                    allLots.Add(new RealLot(headerLine, dataLine));
                }
            }

            return allLots;
        }

        //Method that returns the correct RealSnapshot string based on DateTime
        public string GetRealSnapshotString(DateTime dateTime)
        {
            int startYear = dateTime.Year;
            int startMonth = dateTime.Month;
            int endYear, endMonth;

            if (startMonth != 12)
            {
                endMonth = startMonth + 1;
                endYear = startYear;
            }
            else
            {
                endMonth = 1;
                endYear = startYear + 1;
            }

            string snapshot = $"RealSnapshots_{startYear}-{startMonth}-1_{endYear}-{endMonth}-1_1h.dat";

            return snapshot;
        }
    }

}
