using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace DeviceBatchGenerics.Support.Bases
{
    public class StatsBase
    {
        public double Max { get; set; }
        public double Min { get; set; }
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public void PopulateStatsFromArrayAndRound(double[] array, int roundDigits)
        {
            var ArrayStats = new DescriptiveStatistics(array);
            Max = Math.Round(ArrayStats.Maximum, roundDigits);
            Min = Math.Round(ArrayStats.Minimum, roundDigits);
            Mean = Math.Round(ArrayStats.Mean, roundDigits);
            //StdDev = Math.Round(ArrayStats.StandardDeviation, roundDigits); uses Bessel's correction (N-1 pop size)
            StdDev = Math.Round(Statistics.PopulationStandardDeviation(array), roundDigits);
            
        }       
    }
}
