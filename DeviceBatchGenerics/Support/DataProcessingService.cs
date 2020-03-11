using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace DeviceBatchGenerics.Support
{
    public static class DataProcessingService
    {
        /// <summary>
        /// Handles calculations from raw data as well as processes for removing garbage data
        /// </summary>
        public static LEDCalculator LEDCalculator { get; set; } = new LEDCalculator();
        static public Task CreateLEDCalculatorAsync()
        {
            return Task.Run(async () =>
            {
                LEDCalculator = await LEDCalculator.CreateAsync();
            });
        }
        public static void WriteIENumberableToCSV(System.Collections.IEnumerable records, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                var writecsv = new CsvWriter(sw);
                writecsv.WriteRecords(records);
                sw.Dispose();
                fs.Dispose();
            }
        }
    }
}
