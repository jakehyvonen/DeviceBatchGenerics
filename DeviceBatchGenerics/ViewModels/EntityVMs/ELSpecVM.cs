using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using CsvHelper;
using System.Diagnostics;
using MathNet.Numerics;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support.DataMapping;
using DeviceBatchGenerics.Support;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class ELSpecVM : CrudVMBase
    {
        #region Constructors
        public ELSpecVM()
        {
            TheELSpectrum = new ELSpectrum();
        }
        public ELSpecVM(string path)
        {
            TheELSpectrum = new ELSpectrum();
            try
            {
                LoadELSpecDataIntoList(path);
                if (ELSpecList.Count > 1)
                {
                    AssignCutoffLambdaValues();
                    PopulatePropertiesFromPath(path);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ELSpecVM Exception: " + e.ToString());
            }
        }
        public ELSpecVM(ELSpectrum spec)
        {
            TheELSpectrum = spec;
            try
            {
                if (ELSpecList.Count > 1)
                {
                    AssignCutoffLambdaValues();
                    PopulatePropertiesFromPath(spec.FilePath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ELSpecVM Exception: " + e.ToString());
            }
        }
        #endregion
        #region Members
        private List<ELSpecDatum> _ELSpecList = new List<ELSpecDatum>();
        private double _cutoffHeuristic = 0.003;
        private double _minLambdaCutoff = 420.0;//DUDE
        private double _maxLambdaCutoff = 800.0;
        #endregion
        #region Properties
        public ELSpectrum TheELSpectrum { get; set; }
        public List<ELSpecDatum> ELSpecList
        {
            get { return _ELSpecList; }
            set
            {
                _ELSpecList = value;
                OnPropertyChanged();
            }
        }
        public double MinLambdaCutoff
        {
            get { return _minLambdaCutoff; }
            set
            {
                _minLambdaCutoff = value;
                OnPropertyChanged();
            }
        }
        public double MaxLambdaCutoff
        {
            get { return _maxLambdaCutoff; }
            set
            {
                _maxLambdaCutoff = value;
                OnPropertyChanged();
            }
        }
        #endregion
        public string[,] ELSpecArray()
        {
            //ELSpecDatum has two properties and that's unlikely to change
            string[,] dataArray = new string[ELSpecList.Count, 2];
            for (int i = 0; i < ELSpecList.Count; i++)
            {
                dataArray[i, 0] = ELSpecList[i].Wavelength.ToString();
                dataArray[i, 1] = ELSpecList[i].Intensity.ToString();
            }
            return dataArray;
        }
        private void LoadELSpecDataIntoList(string fp)
        {
            try
            {
                using (var sr = new StreamReader(fp))
                {
                    var reader = new CsvReader(sr);
                    ELSpecList = reader.GetRecords<ELSpecDatum>().ToList<ELSpecDatum>();
                    var maxIntensity = ELSpecList.Max(i => i.Intensity);
                    foreach (ELSpecDatum d in ELSpecList)
                    {
                        d.Intensity = d.Intensity / maxIntensity;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        private void AssignCutoffLambdaValues()
        {
            try
            {
                var peakLambdaIndex = ELSpecList.FindIndex(x => x.Intensity == ELSpecList.Max(y => y.Intensity)); //find the index of the EL peak
                var presentIntensity = ELSpecList[peakLambdaIndex].Intensity; //find the peak intensity (should be 1.0 but this is safer)
                var indexCounter = peakLambdaIndex; //start counting at the peak 
                while (presentIntensity > _cutoffHeuristic && indexCounter > 0) //search for the first index where the intensity is less than the cutoffHeuristic
                {
                    indexCounter--;
                    presentIntensity = ELSpecList[indexCounter].Intensity;
                }
                MinLambdaCutoff = ELSpecList[indexCounter].Wavelength; //get the wavelength at the cutoff index
                indexCounter = peakLambdaIndex; //rinse and repeat but in the opposite direction
                presentIntensity = ELSpecList[peakLambdaIndex].Intensity;
                while (presentIntensity > _cutoffHeuristic && indexCounter < ELSpecList.Count - 1)
                {
                    indexCounter++;
                    presentIntensity = ELSpecList[indexCounter].Intensity;
                }
                MaxLambdaCutoff = ELSpecList[indexCounter].Wavelength;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception at ELSpecVM AssignCutoffLambdaValues(): " + e.ToString());
            }
        }
        public void PopulatePropertiesFromPath(string fp)
        {
            TheELSpectrum.FilePath = fp;
            try
            {
                Debug.WriteLine("ELSpec filepath: " + fp);
                //perform linear fits on both sides of the gaussian to approximate where the peak would be if the spectrophotometer had better resolution
                LoadELSpecDataIntoList(fp);
                var max = _ELSpecList.Max(x => x.Intensity);
                var maxListIndex = _ELSpecList.FindIndex(x => x.Intensity == max);
                Debug.WriteLine("Maximum ELSpec raw value is: " + max + " at list index: " + maxListIndex);
                foreach (ELSpecDatum e in _ELSpecList)
                {
                    e.Intensity = e.Intensity / max;
                }
                //find points between 0.42 and 0.9 on the high energy side of the peak and add to list
                var leftList = new List<ELSpecDatum>();
                for (int i = 0; i < maxListIndex; i++)
                {
                    if (_ELSpecList[i].Intensity > 0.42 && _ELSpecList[i].Intensity < 0.9)
                    {
                        leftList.Add(_ELSpecList[i]);
                    }
                }
                //same for lower energy side but in reverse
                var rightList = new List<ELSpecDatum>();
                for (int i = _ELSpecList.Count() - 1; i > maxListIndex; i--)
                {
                    if (_ELSpecList[i].Intensity > 0.42 && _ELSpecList[i].Intensity < 0.9)
                    {
                        rightList.Add(_ELSpecList[i]);
                    }
                }
                if (leftList.Count >= 2 && rightList.Count >= 2)
                {
                    //perform linear fits on these data
                    double[] leftXData = leftList.Select(x => x.Wavelength).ToArray();
                    double[] leftYData = leftList.Select(x => x.Intensity).ToArray();
                    Tuple<double, double> leftVals = Fit.Line(leftXData, leftYData);
                    //Debug.WriteLine("Left side line: Intensity = " + leftVals.Item2 + "x + " + leftVals.Item1);
                    double rsquared = GoodnessOfFit.RSquared(leftXData.Select(x => leftVals.Item1 + leftVals.Item2 * x), leftYData);
                    //Debug.WriteLine("R^2 = " + rsquared);
                    double[] rightXData = rightList.Select(x => x.Wavelength).ToArray();
                    double[] rightYData = rightList.Select(x => x.Intensity).ToArray();
                    Tuple<double, double> rightVals = Fit.Line(rightXData, rightYData);
                    //Debug.WriteLine("Right side line: Intensity = " + rightVals.Item2 + "x + " + rightVals.Item1);
                    rsquared = GoodnessOfFit.RSquared(rightXData.Select(x => rightVals.Item1 + rightVals.Item2 * x), rightYData);
                    //Debug.WriteLine("R^2 = " + rsquared);
                    double calculatedPeakLambda = (leftVals.Item1 - rightVals.Item1) / (rightVals.Item2 - leftVals.Item2);
                    //Debug.WriteLine("Calculated peak lambda to be: " + calculatedPeakLambda + "nm");
                    TheELSpectrum.ELPeakLambda = Math.Round(Convert.ToDecimal(calculatedPeakLambda), 1);
                    double leftLambdaAtHalfMax = (0.5 - leftVals.Item1) / leftVals.Item2;
                    double rightLambdaAtHalfMax = (0.5 - rightVals.Item1) / rightVals.Item2;
                    var calculatedFWHM = Math.Round(Convert.ToDecimal(rightLambdaAtHalfMax - leftLambdaAtHalfMax), 1);
                    //Debug.WriteLine("Calculated FWHM to be: " + calculatedFWHM + "nm");
                    TheELSpectrum.ELFWHM = calculatedFWHM;
                }
                else
                {
                    TheELSpectrum.ELPeakLambda = Convert.ToDecimal(_ELSpecList[maxListIndex].Wavelength);
                    //need to update ELSpectrum entity with nullables 
                }
                var CIEcoords = CIE1931Calculator.CalculateCIE1931CoordsFromFilePath(fp);
                TheELSpectrum.CIEx = Convert.ToDecimal(CIEcoords.Item1);
                TheELSpectrum.CIEy = Convert.ToDecimal(CIEcoords.Item2);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                MessageBox.Show(e.ToString());
            }
        }
    }

}
