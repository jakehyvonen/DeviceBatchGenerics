using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public class CIEColorMatchingDatum
    {
        public double Wavelength { get; set; }
        public double x_bar { get; set; }
        public double y_bar { get; set; }
        public double z_bar { get; set; }
    }
}
