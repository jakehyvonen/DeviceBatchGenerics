using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    /// <summary>
    /// Contains all information for each datapoint from voltage sweeps
    /// </summary>
    public class FullLJVDatum
    {
        public decimal Voltage { get; set; }
        public decimal PhotoCurrentA { get; set; }
        public decimal Current { get; set; }
        public decimal CurrentDensity { get; set; }
        public Nullable<decimal> Resistance { get; set; }
        public decimal PhotoCurrentB { get; set; }
        public Nullable<decimal> CameraLuminance { get; set; }
        public Nullable<decimal> CameraCIEx { get; set; }
        public Nullable<decimal> CameraCIEy { get; set; }
        public decimal PhotoCurrentC { get; set; }
        public decimal Luminance { get; set; }//calculated from Alpha*PhotoCurrentA+B/2 to account for device instability
        public decimal CurrentEff { get; set; }
        public decimal PowerEff { get; set; }
        public decimal EQE { get; set; }
        public decimal PCurrChangePercent { get; set; }
        public DateTime TimeStamp { get; set; }
        public string ELSpecPath { get; set; }
        public List<ELSpecDatum> ELSpecData { get; set; }
    }
}
