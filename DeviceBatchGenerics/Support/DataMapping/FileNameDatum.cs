using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.DataMapping
{
    public class FileNameDatum
    {
        public int DeviceBatchIndex { get; set; }
        public string TestCondition { get; set; }
        public string FilePath { get; set; }
        public string MeasurementSite { get; set; }
        public string Extension { get; set; }
    }
}
