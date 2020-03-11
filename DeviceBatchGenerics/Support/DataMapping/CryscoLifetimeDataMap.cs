using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public sealed class CryscoLifetimeDataMap : ClassMap<CryscoLifetimeDatum>
    {
        public CryscoLifetimeDataMap()
        {
            Map(m => m.ElapsedHours).Name("[Hour Pass (hrs)]");
            Map(m => m.PixelVoltage).Name("[Voltage (V)]");
            Map(m => m.Current).Name("[Current (mA)]");
            Map(m => m.CurrentDensity).Name("[Current Density (mA/cm2)]");
            Map(m => m.PDVoltage).Name("[PDVoltage (V)]");
            Map(m => m.Luminance).Name("[Luminance (cd/m2)]");
            Map(m => m.RelativeLuminance).Name("[Relative Luminance (%)]");
        }
    }
}
