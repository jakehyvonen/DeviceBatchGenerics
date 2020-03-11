using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public class CryscoLifetimeDatum
    {
        public double ElapsedHours { get; set; }
        public double PixelVoltage { get; set; }
        public double Current { get; set; }
        public double CurrentDensity { get; set; }
        public double PDVoltage { get; set; }
        public double Luminance { get; set; }
        public double RelativeLuminance { get; set; }
        public double CurrentEfficiency { get; set; }
        public double PowerEfficiency { get; set; }
        public double EQE { get; set; }
    }
}
