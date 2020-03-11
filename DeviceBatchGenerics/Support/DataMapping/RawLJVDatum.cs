using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public class RawLJVDatum
    {
        public decimal Voltage { get; set; }
        public decimal PhotoCurrentA { get; set; }
        public decimal Current { get; set; }
        public decimal PhotoCurrentB { get; set; }

        public Nullable<decimal> Resistance { get; set; }
        //public decimal PhotoCurrent { get; set; }
        public Nullable<decimal> CameraLuminance { get; set; }
        public Nullable<decimal> CameraCIEx { get; set; }
        public Nullable<decimal> CameraCIEy { get; set; }
        public decimal PhotoCurrentC { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
