using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support.DataMapping;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using CsvHelper;

namespace DeviceBatchGenerics.Support
{
    public class LEDCalculator : NotifyUIBase
    {
        /// <summary>
        /// Handles raw data calculations as well as data hygiene activities
        /// </summary>
        /*
        public LEDCalculator()
        {
            LoadResponsivityCurves();
        }
        */
        private async Task<LEDCalculator> InitializeAsync()
        {
            return await Task.Run(async() =>
            {
                Debug.WriteLine("LEDCalculator InitializeAsync");
                await LoadResponsivityCurves();
                return this;
            }).ConfigureAwait(false);
        }
        public static Task<LEDCalculator> CreateAsync()
        {
            var ret = new LEDCalculator();
            return ret.InitializeAsync();
        }
        #region Members
        string _spectrumPath = "Path not set";
        List<double> _spectrumPathXVar;
        List<double> _spectrumPathYVar;

        string _PDResponsivityPath = @"Z:\Data (LJV, Lifetime)\Numerical Calculation Curves\PD1-Responsivity(A_per_W).csv";
        List<double> _PDResponsivityXVar;
        List<double> _PDResponsivityYVar;

        string _PhotopicResponsePath = @"Z:\Data (LJV, Lifetime)\Numerical Calculation Curves\Photopic_response_1nm.csv";
        List<double> _PhotopicXVar;
        List<double> _PhotopicYVar;

        List<double> _productXVar;
        List<double> _productYVar;
        double _intGS = 0;
        double _intRS = 0;
        double _phiNot = 683; //peak value of photopic response curve [lm/W]
        double _deviceArea = 4E-6; //2x2 mm QLED area = 4E-6 m^2
        #endregion       
        #region Methods
        private async Task LoadResponsivityCurves()
        {
            await Task.Run(() =>
            {
                //load the photodiode responsivity curve
                _PDResponsivityXVar = new List<double>();
                _PDResponsivityYVar = new List<double>();
                using (var reader = new StreamReader(_PDResponsivityPath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        try
                        {
                            _PDResponsivityXVar.Add(Convert.ToDouble(values[0]));
                            _PDResponsivityYVar.Add(Convert.ToDouble(values[1]));
                        }
                        catch (Exception e)
                        {
                            //MessageBox.Show(e.ToString());
                            Debug.WriteLine("wasn't a double");
                        }
                    }
                }
                //load the photopic response curve
                _PhotopicXVar = new List<double>();
                _PhotopicYVar = new List<double>();
                using (var reader = new StreamReader(_PhotopicResponsePath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        try
                        {
                            _PhotopicXVar.Add(Convert.ToDouble(values[0]));
                            _PhotopicYVar.Add(Convert.ToDouble(values[1]));
                        }
                        catch (Exception e)
                        {
                            //MessageBox.Show(e.ToString());
                            Debug.WriteLine("wasn't a double");
                        }
                    }
                }
            }).ConfigureAwait(false);
        }
        private async Task LoadELSpectrumCurve()
        {
            await Task.Run(() =>
            {
                _spectrumPathXVar = new List<double>();
                _spectrumPathYVar = new List<double>();
                using (var reader = new StreamReader(_spectrumPath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        try
                        {
                            _spectrumPathXVar.Add(Convert.ToDouble(values[0]));
                            _spectrumPathYVar.Add(Convert.ToDouble(values[1]));
                        }
                        catch (Exception e)
                        {
                            //MessageBox.Show(e.ToString());
                            Debug.WriteLine("wasn't a double");
                        }
                    }
                }
            }).ConfigureAwait(false);
        }
        private async Task IntegrateGtimesS()
        {
            await Task.Run(() =>
            {

                // this only works if the 1st curve.Count >= Spectrum curve.Count
                List<double> _newSpecXVar = new List<double>();
                List<double> _newSpecYVar = new List<double>();
                //use the x variables from the photopic response curve and interpolate y values from the spectrum curve to match up with it
                for (int i = 0; i < _PhotopicXVar.Count; i++)
                {
                    //find the 2nd curve data points that surround the 1st curve data point (or equal it)
                    int j = 0;
                    //Debug.WriteLine("2nd curve array size is: " + _path2XVar.Count);
                    while (_spectrumPathXVar[j] < _PhotopicXVar[i] && j < _spectrumPathXVar.Count - 1)
                    {

                        //Debug.WriteLine("j = " + j);
                        //Debug.WriteLine(_path2XVar[j] + "<" + _spectrumPathXVar[i]);
                        j++;
                    }
                    //if x points are equal, don't need to interpolate
                    if (_spectrumPathXVar[j] == _PhotopicXVar[i])
                    {
                        _newSpecXVar.Add(_PhotopicXVar[i]);

                        _newSpecYVar.Add(_spectrumPathYVar[j]);
                        //Debug.WriteLine(_spectrumPathXVar[i] + " same value as " + _path2XVar[j]);
                        //Debug.WriteLine("so y[i] is the same and = " + _path2YVar[j]);
                    }

                    else //interpolate the new y value
                    {

                        if (_PhotopicXVar[i] < _spectrumPathXVar[j]) //don't make up data points outside curve 2's range
                        {
                            _newSpecXVar.Add(_PhotopicXVar[i]);

                            double _interpolatedValue = _spectrumPathYVar[j - 1] + (_PhotopicXVar[i] - _spectrumPathXVar[j - 1]) * (_spectrumPathYVar[j] - _spectrumPathYVar[j - 1]) / (_spectrumPathXVar[j] - _spectrumPathXVar[j - 1]);
                            _newSpecYVar.Add(_interpolatedValue);
                            //Debug.WriteLine("y0= " + _path2YVar[j - 1] + " y1= " + _path2YVar[j] + "interpolated value = " + _interpolatedValue);
                        }

                    }
                }
                List<double> dumbList = new List<double>();
                dumbList.AddRange(_newSpecXVar);
                dumbList.AddRange(_newSpecYVar);
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\InterpolatedGSOutput.txt", dumbList.Select(x => string.Join(",", x)));

                //normalize the dataset
                double maxValue = _newSpecYVar.Max();
                for (int i = 0; i < _newSpecYVar.Count; i++)
                {
                    _newSpecYVar[i] = _newSpecYVar[i] / maxValue;
                }
                dumbList.Clear();
                dumbList.AddRange(_newSpecXVar);
                dumbList.AddRange(_newSpecYVar);
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\NormalizedGSOutput.txt", dumbList.Select(x => string.Join(",", x)));

                _productXVar = new List<double>();
                _productYVar = new List<double>();
                //multiply two curves together
                for (int i = 0; i < _newSpecYVar.Count; i++)
                {
                    _productXVar.Add(_newSpecXVar[i]);
                    _productYVar.Add(_newSpecYVar[i] * _PhotopicYVar[i]);

                }
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\ProductGSOutput.txt", _productYVar.Select(x => string.Join(",", x)));

                double productIntegral = 0;
                //perform the integration
                for (int i = 1; i < _productYVar.Count; i++)
                {
                    productIntegral += (_productYVar[i] + _productYVar[i - 1]) / 2 * (_productXVar[i] - _productXVar[i - 1]);
                }
                Debug.WriteLine("Integral value is: " + productIntegral);
                _intGS = productIntegral;
            }).ConfigureAwait(false);
        }
        private async Task IntegrateRtimesS()
        {
            await Task.Run(() =>
            {
                // this only works if the 1st curve.Count >= Spectrum curve.Count
                List<double> _newSpecXVar = new List<double>();
                List<double> _newSpecYVar = new List<double>();
                //use the x variables from the photopic response curve and interpolate y values from the spectrum curve to match up with it
                for (int i = 0; i < _PDResponsivityXVar.Count; i++)
                {
                    //find the 2nd curve data points that surround the 1st curve data point (or equal it)
                    int j = 0;
                    //Debug.WriteLine("2nd curve array size is: " + _path2XVar.Count);
                    while (_spectrumPathXVar[j] < _PDResponsivityXVar[i] && j < _spectrumPathXVar.Count - 1)
                    {

                        //Debug.WriteLine("j = " + j);
                        //Debug.WriteLine(_path2XVar[j] + "<" + _spectrumPathXVar[i]);
                        j++;
                    }
                    //if x points are equal, don't need to interpolate
                    if (_spectrumPathXVar[j] == _PDResponsivityXVar[i])
                    {
                        _newSpecXVar.Add(_PDResponsivityXVar[i]);

                        _newSpecYVar.Add(_spectrumPathYVar[j]);
                        //Debug.WriteLine(_spectrumPathXVar[i] + " same value as " + _path2XVar[j]);
                        //Debug.WriteLine("so y[i] is the same and = " + _path2YVar[j]);
                    }

                    else //interpolate the new y value
                    {

                        if (_PDResponsivityXVar[i] < _spectrumPathXVar[j]) //don't make up data points outside curve 2's range
                        {
                            _newSpecXVar.Add(_PDResponsivityXVar[i]);

                            double _interpolatedValue = _spectrumPathYVar[j - 1] + (_PDResponsivityXVar[i] - _spectrumPathXVar[j - 1]) * (_spectrumPathYVar[j] - _spectrumPathYVar[j - 1]) / (_spectrumPathXVar[j] - _spectrumPathXVar[j - 1]);
                            _newSpecYVar.Add(_interpolatedValue);
                            //Debug.WriteLine("y0= " + _path2YVar[j - 1] + " y1= " + _path2YVar[j] + "interpolated value = " + _interpolatedValue);
                        }

                    }
                }
                List<double> dumbList = new List<double>();
                dumbList.AddRange(_newSpecXVar);
                dumbList.AddRange(_newSpecYVar);
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\InterpolatedRSOutput.txt", dumbList.Select(x => string.Join(",", x)));

                //normalize the dataset
                double maxValue = _newSpecYVar.Max();
                for (int i = 0; i < _newSpecYVar.Count; i++)
                {
                    _newSpecYVar[i] = _newSpecYVar[i] / maxValue;
                }
                dumbList.Clear();
                dumbList.AddRange(_newSpecXVar);
                dumbList.AddRange(_newSpecYVar);
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\NormalizedRSOutput.txt", dumbList.Select(x => string.Join(",", x)));

                _productXVar = new List<double>();
                _productYVar = new List<double>();
                //multiply two curves together
                for (int i = 0; i < _newSpecYVar.Count; i++)
                {
                    _productXVar.Add(_newSpecXVar[i]);
                    _productYVar.Add(_newSpecYVar[i] * _PDResponsivityYVar[i]);

                }
                File.WriteAllLines(@"C:\Users\Jake\Documents\PhotoCalc\ProductRSOutput.txt", _productYVar.Select(x => string.Join(",", x)));

                double productIntegral = 0;
                //perform the integration
                for (int i = 1; i < _productYVar.Count; i++)
                {
                    productIntegral += (_productYVar[i] + _productYVar[i - 1]) / 2 * (_productXVar[i] - _productXVar[i - 1]);
                }
                Debug.WriteLine("Integral value is: " + productIntegral);
                _intRS = productIntegral;
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// Calculate geometry factor from an empirical measurement of the conversion factor alpha
        /// </summary>
        /// <param name="path"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public async Task<double> GeometryFactorFromELSpectrum(string path, double alpha)
        {
            return await Task.Run(async () =>
            {
                double geofactor = 1E7;
                _spectrumPath = path;
                await LoadELSpectrumCurve();
                await IntegrateGtimesS();
                await IntegrateRtimesS();
                geofactor = (_phiNot * _intGS) / (_deviceArea * Math.PI * alpha * _intRS);
                return geofactor;
            }).ConfigureAwait(false);

        }
        public async Task<Tuple<double, double>> AlphaFromRawData(List<RawLJVDatum> data)
        {
            return await Task.Run(() =>
            {
                Tuple<double, double> alphaAndR2 = new Tuple<double, double>(-1, 0.001);
                List<double> pdBCData = new List<double>();
                List<double> camData = new List<double>();
                foreach (RawLJVDatum raw in data)
                {
                    if (raw.CameraLuminance != null)
                    {
                        //interpolate readings to account for device instability
                        pdBCData.Add(Convert.ToDouble((raw.PhotoCurrentB+raw.PhotoCurrentC))/2.0f);
                        camData.Add(Convert.ToDouble(raw.CameraLuminance));
                    }
                }
                if (camData.Count > 2)
                {
                    double[] xdata = pdBCData.ToArray();
                    double[] ydata = camData.ToArray();
                    Tuple<double, double> p = Fit.Line(xdata, ydata);
                    Debug.WriteLine("Measured Alpha = " + p.Item2);
                    double rsquared = GoodnessOfFit.RSquared(xdata.Select(x => p.Item1 + p.Item2 * x), ydata);
                    alphaAndR2 = new Tuple<double, double>(p.Item2, rsquared);
                    Debug.WriteLine("R^2 was: " + rsquared);
                }
                //take the ratio from a single measurement if not enough data for linear regression
                else if(camData.Count <=2 && camData.Count >=1)
                {
                    double ratioAlpha = camData.Last() / pdBCData.Last();
                    alphaAndR2 = new Tuple<double, double>(ratioAlpha, 0);
                }
                return alphaAndR2;
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// Calculate # of photons from radiance(lambda) curve
        /// </summary>
        /// <param name="eLSpecData"></param>
        /// <returns></returns>
        public async Task<double> NumberOfPhotons(List<ELSpecDatum> eLSpecData)
        {
            return await Task.Run(() =>
            {
                double photons = -1;
                double h = 6.626e-34;//planck's constant (J*s)
                double c = 2.998e8;//speed of light(m/s)
                double[] wavelength = eLSpecData.Select(e => e.Wavelength).ToArray();//units = nm
                double[] intensity = eLSpecData.Select(e => e.Intensity).ToArray();//units = W*m^-2*sr-1*nm-1
                double[] photonsCount = new double[eLSpecData.Count];//# of photons/m^2 at each wavelength
                for (int j = 0; j < eLSpecData.Count; j++)
                {
                    photonsCount[j] = intensity[j] / (h * c / (wavelength[j] * 1e-9)) * Math.PI;//divide by energy of wavelength to count # of photons, multiply by pi for hemisphere emission integral
                }
                for (int j = 1; j < eLSpecData.Count; j++)
                {
                    photons += (photonsCount[j] + photonsCount[j - 1]) / 2 * (wavelength[j] - wavelength[j - 1]);//integrate over photonsCount curve 
                }
                return photons;
            }).ConfigureAwait(false);
        }
        public async Task<List<ProcessedLJVDatum>> ProcessRawData(List<RawLJVDatum> rawLJVData, double specCurrent, double specPhotoCurrent, List<ELSpecDatum> ELSpecData, double activeArea = 4E-6)
        {
            return await Task.Run(async () =>
            {
                Debug.WriteLine("ProcessRawData");
                List<ProcessedLJVDatum> processedLJVData = new List<ProcessedLJVDatum>();

                var alphaAndR2 = await AlphaFromRawData(rawLJVData).ConfigureAwait(false);
                double alpha = alphaAndR2.Item1;
                //double finalCurrent = Convert.ToDouble(rawLJVData.Select(r => r.Current).Last());

                double electronsCount = specCurrent / 1.602E-19 / activeArea;//electron charge = 1.602E-19C
                var numPhotons = await NumberOfPhotons(ELSpecData).ConfigureAwait(false);
                double specEQE = numPhotons * 100 / electronsCount;//external quantum eff. definition (*100 to get value in %)
                double EQECF = specEQE / (activeArea * specPhotoCurrent * alpha / specCurrent);//use fact that current eff. is directly proportional to EQE to generate conversion factor
                                                                                               //find the baseline photocurrent 
                List<double> darkPCurrentList = new List<double>();
                foreach (var datum in rawLJVData)
                {
                    //assume no emission below 1V
                    if (datum.Voltage < 1)
                        darkPCurrentList.Add(Convert.ToDouble(datum.PhotoCurrentA));
                }
                decimal averageDarkCurrent = 0;
                if (darkPCurrentList.Count > 0)
                    averageDarkCurrent = Convert.ToDecimal(darkPCurrentList.Average());
                foreach (RawLJVDatum rawDatum in rawLJVData)
                {
                    ProcessedLJVDatum tempProcDatum = new ProcessedLJVDatum();
                    tempProcDatum.Voltage = rawDatum.Voltage;
                    tempProcDatum.CurrentDensity = 0.1m * rawDatum.Current / Convert.ToDecimal(activeArea); //mA/cm^2: 1000 mA/A 10000 cm^2/m^2 --> 0.1 conversion factor
                    tempProcDatum.PhotoCurrent = (rawDatum.PhotoCurrentA+rawDatum.PhotoCurrentB)/2.0m;//interpolate to account for device instability
                    if (tempProcDatum.PhotoCurrent < 1.8m * averageDarkCurrent)//write zeros to Luminance if photocurrent is below 2x baseline
                        tempProcDatum.Luminance = 0;
                    else
                        tempProcDatum.Luminance = Convert.ToDecimal(Math.Round(Convert.ToDouble(tempProcDatum.PhotoCurrent) * alpha, 1));
                    if (tempProcDatum.Luminance > 10.0m && rawDatum.Voltage > 0 && rawDatum.Current > 0)//we only care about efficiency over 10 nits
                    {
                        tempProcDatum.CurrentEff = Math.Round(4E-6m * tempProcDatum.Luminance / rawDatum.Current, 2);//cd/A --> multiply by area in m^2 to cancel out cd/m^2
                        tempProcDatum.PowerEff = Math.Round(Convert.ToDecimal(Math.PI) * tempProcDatum.CurrentEff / tempProcDatum.Voltage, 2);
                        tempProcDatum.EQE = Math.Round(Convert.ToDecimal(EQECF) * tempProcDatum.CurrentEff, 2);
                    }
                    processedLJVData.Add(tempProcDatum);
                }
                Debug.WriteLine("ProcessRawData completed");

                return processedLJVData;
            }).ConfigureAwait(false);

        }
        public List<LJVStatsDatum> LJVStatsData(List<List<FullLJVDatum>> dataLists)
        {
            List<LJVStatsDatum> statsDataList = new List<LJVStatsDatum>();
            //find each unique voltage point
            HashSet<decimal> voltagePoints = new HashSet<decimal>();
            foreach (List<FullLJVDatum> list in dataLists)
            {
                foreach (FullLJVDatum d in list)
                    voltagePoints.Add(d.Voltage);
            }
            //loop through ever list and calculate mean/stdDev for each voltage (if it exists)
            foreach (decimal voltage in voltagePoints)
            {
                LJVStatsDatum statsDatum = new LJVStatsDatum();
                List<FullLJVDatum> dataAtVoltage = new List<FullLJVDatum>();
                foreach (List<FullLJVDatum> list in dataLists)
                {
                    int index = list.FindIndex(x => x.Voltage == voltage);
                    if (index >= 0)//FindIndex returns -1 if element doesn't exist
                        dataAtVoltage.Add(list[index]);
                }
            }

            return statsDataList;
        }
        #endregion
    }
}
