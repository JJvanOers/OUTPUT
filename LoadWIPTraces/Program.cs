using System;
using System.Collections.Generic;
using WaferFabSim.Import;
using WaferFabSim.InputDataConversion;

namespace LoadWIPTraces
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDirectory = @"C:\CSSLWaferFab\Input";

            string outputDirectory = @"C:\CSSLWaferFab\Output\WaferAreaSim";

            List<WorkCenterLotActivities> activities = Deserializer.DeserializeWorkCenterLotActivities(inputDirectory + @"\SerializedFiles");



        }
    }
}
