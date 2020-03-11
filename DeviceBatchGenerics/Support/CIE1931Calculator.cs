using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using DeviceBatchGenerics.Support.DataMapping;

namespace DeviceBatchGenerics.Support
{
    public static class CIE1931Calculator
    {
        public static Tuple<double, double> CalculateCIE1931CoordsFromFilePath(string fp)
        {
            Tuple<double, double> coords = new Tuple<double, double>(0, 0);
            //first, load the Electroluminescent spectrum from the filepath into a list
            var ELSpecList = new List<ELSpecDatum>();
            using (var sr = new StreamReader(fp))
            {
                var reader = new CsvReader(sr);
                ELSpecList = reader.GetRecords<ELSpecDatum>().ToList<ELSpecDatum>();
            }
            //normalize the spectrum
            var maxIntensity = ELSpecList.Max(x => x.Intensity);
            foreach (ELSpecDatum e in ELSpecList)
            {
                e.Intensity = e.Intensity / maxIntensity;
            }
            //load color matching curves
            var CIEColorMatchingCurveList = new List<CIEColorMatchingDatum>();
            using (var sr = new StreamReader(@"Z:\Data (LJV, Lifetime)\Numerical Calculation Curves\CIE color matching curves.csv"))
            {
                var reader = new CsvReader(sr);
                CIEColorMatchingCurveList = reader.GetRecords<CIEColorMatchingDatum>().ToList<CIEColorMatchingDatum>();
            }
            //perform integrations
            double bigX = 0;
            double bigY = 0;
            double bigZ = 0;
            foreach (ELSpecDatum e in ELSpecList)
            {
                var CIEDatumAtSameLambda = CIEColorMatchingCurveList.Where(x => x.Wavelength == e.Wavelength).First();
                bigX += e.Intensity * CIEDatumAtSameLambda.x_bar;
                bigY += e.Intensity * CIEDatumAtSameLambda.y_bar;
                bigZ += e.Intensity * CIEDatumAtSameLambda.z_bar;
            }
            var CIEx = bigX / (bigX + bigY + bigZ);
            var CIEy = bigY / (bigX + bigY + bigZ);
            coords = new Tuple<double, double>(CIEx, CIEy);
            return coords;
        }
    }

}
