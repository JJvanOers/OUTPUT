using CSSL.Utilities.Distributions;
using System;
using System.Collections.Generic;
using System.Text;
using WaferFabSim.WaferFabElements.Utilities;

namespace WaferFabSim.Import.Distributions
{
    public interface IWorkCenterDistributionReader
    {

        Dictionary<string, Distribution> GetServiceTimeDistributions();

        Dictionary<string, OvertakingDistributionBase> GetOvertakingDistributions();
    }
}
