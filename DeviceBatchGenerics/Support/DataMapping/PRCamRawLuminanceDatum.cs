using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public class PRCamRawLuminanceDatum
    {
        public decimal Luminance { get; set; }
        public decimal CIEx { get; set; }
        public decimal CIEy { get; set; }
    }
}
