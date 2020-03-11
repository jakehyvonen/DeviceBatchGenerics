using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support.DataMapping;
using DeviceBatchGenerics.Support;
using EFDeviceBatchCodeFirst;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using MathNet.Numerics.Statistics;
using CsvHelper;
using System.Reflection;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class LJVScanSummaryVM : CrudVMBase
    {
        #region Constructors
        public LJVScanSummaryVM()
        {
            TheLJVScanSummary = new DeviceLJVScanSummary();
        }
        public LJVScanSummaryVM(DeviceBatchContext context) : base(context)
        {
            System.Diagnostics.Debug.WriteLine("LJVScanSummaryVM instantiated");
            TheLJVScanSummary = new DeviceLJVScanSummary();
        }
        public LJVScanSummaryVM(DeviceLJVScanSummary scansummary)
        {
            //reselect it to avoid multiple references with IEntityChangeTracker thing
            TheLJVScanSummary = ctx.DeviceLJVScanSummaries.Where(x => x.DeviceLJVScanSummaryId == scansummary.DeviceLJVScanSummaryId).FirstOrDefault();
            PopulateVMPropertiesFromChildren();
            if (TheLJVScanSummary.StatsDataPath != null)
                LoadStatsDataIntoList(TheLJVScanSummary.StatsDataPath);
        }
        #endregion
        #region Members
        private int _deviceIndex = 4;
        private string _statsDataPath = "path not set";
        private string _spreadsheetPath = "path not set";
        private StatsBase _peakEQE = new StatsBase();
        private StatsBase _EQEat1k = new StatsBase();
        private StatsBase _peakLambda = new StatsBase();
        private StatsBase _FWHM = new StatsBase();
        private StatsBase _CIEx = new StatsBase();
        private StatsBase _CIEy = new StatsBase();
        private ObservableCollection<LJVScanVM> _LJVScanVMCollection;
        private ObservableCollection<ELSpecVM> _ELSpecVMCollection;
        private ObservableCollection<ImageVM> _imageVMCollection;
        private List<LJVStatsDatum> _statsDataList;
        private List<Tuple<string, string>> _uniqueTCRawAndProcDatPaths;
        #endregion
        #region Properties
        public int DeviceIndex
        {
            get
            {
                if (TheLJVScanSummary.Device != null)
                    return TheLJVScanSummary.Device.BatchIndex;
                else
                    return _deviceIndex;
            }
        }
        public StatsBase PeakEQE
        {
            get { return _peakEQE; }
            set
            {
                _peakEQE = value;
                OnPropertyChanged();
            }
        }
        public StatsBase EQEat1k
        {
            get { return _EQEat1k; }
            set
            {
                _EQEat1k = value;
                OnPropertyChanged();
            }
        }
        public StatsBase PeakLambda
        {
            get { return _peakLambda; }
            set
            {
                _peakLambda = value;
                OnPropertyChanged();
            }
        }
        public StatsBase FWHM
        {
            get { return _FWHM; }
            set
            {
                _FWHM = value;
                OnPropertyChanged();
            }
        }
        public StatsBase CIEx
        {
            get { return _CIEx; }
            set
            {
                _CIEx = value;
                OnPropertyChanged();
            }
        }
        public StatsBase CIEy
        {
            get { return _CIEy; }
            set
            {
                _CIEy = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<LJVScanVM> LJVScanVMCollection
        {
            get { return _LJVScanVMCollection; }
            set
            {
                _LJVScanVMCollection = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ELSpecVM> ELSpecVMCollection
        {
            get { return _ELSpecVMCollection; }
            set
            {
                _ELSpecVMCollection = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ImageVM> ImageVMCollection
        {
            get { return _imageVMCollection; }
            set
            {
                _imageVMCollection = value;
                OnPropertyChanged();
            }
        }
        public List<LJVStatsDatum> StatsDataList
        {
            get { return _statsDataList; }
            set
            {
                _statsDataList = value;
                OnPropertyChanged();
            }
        }
        public DeviceLJVScanSummary TheLJVScanSummary { get; set; }
        #endregion
        #region Methods
        private void LoadStatsDataIntoList(string fp)
        {
            using (var sr = new StreamReader(fp))
            {
                var reader = new CsvReader(sr);
                StatsDataList = reader.GetRecords<LJVStatsDatum>().ToList<LJVStatsDatum>();
            }
        }
        public void ConstructChildVMs()
        {
            LJVScanVMCollection = new ObservableCollection<LJVScanVM>();
            foreach (LJVScan scan in TheLJVScanSummary.LJVScans)
            {
                LJVScanVMCollection.Add(new LJVScanVM(scan));
            }
            ELSpecVMCollection = new ObservableCollection<ELSpecVM>();
            foreach (ELSpectrum spec in TheLJVScanSummary.ELSpectrums)
            {
                ELSpecVMCollection.Add(new ELSpecVM(spec));
            }
            ImageVMCollection = new ObservableCollection<ImageVM>();
            foreach (Image image in TheLJVScanSummary.Images)
            {
                ImageVMCollection.Add(new ImageVM(image));
            }
        }
        private void PopulateVMPropertiesFromChildren()
        {
            LJVScanVMCollection = new ObservableCollection<LJVScanVM>();
            var LJVScansList = TheLJVScanSummary.LJVScans.ToList();
            foreach (LJVScan scan in LJVScansList)
                LJVScanVMCollection.Add(new LJVScanVM(scan));
            var peakEQEsArray = LJVScansList.Select(x => Convert.ToDouble(x.MaxEQE)).ToArray();
            PeakEQE.PopulateStatsFromArrayAndRound(peakEQEsArray, 1);
            var EQEsAt1kArray = LJVScansList.Select(x => Convert.ToDouble(x.At1kNitsEQE)).ToArray();
            EQEat1k.PopulateStatsFromArrayAndRound(EQEsAt1kArray, 1);

            var ELSpecsList = TheLJVScanSummary.ELSpectrums.ToList();
            var PeakLambdasArray = ELSpecsList.Select(x => Convert.ToDouble(x.ELPeakLambda)).ToArray();
            PeakLambda.PopulateStatsFromArrayAndRound(PeakLambdasArray, 1);
            var FWHMArray = ELSpecsList.Select(x => Convert.ToDouble(x.ELFWHM)).ToArray();
            FWHM.PopulateStatsFromArrayAndRound(FWHMArray, 1);
            var CIExArray = ELSpecsList.Select(x => Convert.ToDouble(x.CIEx)).ToArray();
            CIEx.PopulateStatsFromArrayAndRound(CIExArray, 3);
            var CIEyArray = ELSpecsList.Select(x => Convert.ToDouble(x.CIEy)).ToArray();
            CIEy.PopulateStatsFromArrayAndRound(CIEyArray, 3);
        }
        public void PopulateEntityPropertiesFromChildren()
        {
            TheLJVScanSummary.MaxEQE = TheLJVScanSummary.LJVScans.Max(x => x.MaxEQE);
            TheLJVScanSummary.MaxCE = TheLJVScanSummary.LJVScans.Max(x => x.MaxCE);
            TheLJVScanSummary.Max1kNitsEQE = TheLJVScanSummary.LJVScans.Max(x => x.At1kNitsEQE);
            TheLJVScanSummary.MaxAt1kNitsCE = TheLJVScanSummary.LJVScans.Max(x => x.At1kNitsCE);
            //Debug.WriteLine("Peak EQE for device " + TheLJVScanSummary.Device.Label + " across all pixels is: " + TheLJVScanSummary.MaxEQE);
            decimal MaxEQESum = 0;
            decimal MaxCESum = 0;
            decimal At1kNitsEQESum = 0;
            decimal At1kNitsCESum = 0;
            decimal LumAtMaxEQESum = 0;
            decimal scanCounter = TheLJVScanSummary.LJVScans.Count;
            if (scanCounter > 0)
            {
                //calculate mean values
                foreach (LJVScan scan in TheLJVScanSummary.LJVScans)
                {
                    MaxEQESum += scan.MaxEQE;
                    MaxCESum += scan.MaxCE;
                    At1kNitsEQESum += scan.At1kNitsEQE;
                    At1kNitsCESum += scan.At1kNitsCE;
                    LumAtMaxEQESum += scan.LuminanceAtMaxEQE ?? default(int);
                }
                TheLJVScanSummary.MeanOfMaxEQE = Math.Round((MaxEQESum / scanCounter), 1);
                TheLJVScanSummary.MeanOfMaxCE = Math.Round((MaxCESum / scanCounter), 1);
                TheLJVScanSummary.MeanAt1kNitsEQE = Math.Round((At1kNitsEQESum / scanCounter), 1);
                TheLJVScanSummary.MeanAt1kNitsCE = Math.Round((At1kNitsCESum / scanCounter), 1);
                TheLJVScanSummary.MeanLuminanceAtMaxEQE = Math.Round(LumAtMaxEQESum / scanCounter);
            }
            //Debug.WriteLine("Mean peak EQE for device " + TheLJVScanSummary.Device.Label + " across all pixels is: " + TheLJVScanSummary.MeanOfMaxEQE + " at luminance: " + TheLJVScanSummary.MeanLuminanceAtMaxEQE);

            //calculate standard deviations
            //standard deviation = sqrt((sum(x_i - x_mean)^2)/count)
            decimal sdMaxEQESum = 0;
            decimal sdMaxCESum = 0;
            decimal sdAt1kNitsEQESum = 0;
            decimal sdAt1kNitsCESum = 0;
            //sum(x_i - x_mean)^2 and I don't feel like using Math.Pow because it requires type conversion to double
            foreach (LJVScan scan in TheLJVScanSummary.LJVScans)
            {
                sdMaxEQESum += (scan.MaxEQE - TheLJVScanSummary.MeanOfMaxEQE) * (scan.MaxEQE - TheLJVScanSummary.MeanOfMaxEQE);
                sdMaxCESum += (scan.MaxCE - TheLJVScanSummary.MeanOfMaxCE) * (scan.MaxCE - TheLJVScanSummary.MeanOfMaxCE);
                sdAt1kNitsEQESum += (scan.At1kNitsEQE - TheLJVScanSummary.MeanAt1kNitsEQE) * (scan.At1kNitsEQE - TheLJVScanSummary.MeanAt1kNitsEQE);
                sdAt1kNitsCESum += (scan.At1kNitsCE - TheLJVScanSummary.MeanAt1kNitsCE) * (scan.At1kNitsCE - TheLJVScanSummary.MeanAt1kNitsCE);
            }
            TheLJVScanSummary.StdDevOfMaxEQE = Convert.ToDecimal(Math.Round(Math.Sqrt(Convert.ToDouble(sdMaxEQESum / scanCounter)), 2));
            TheLJVScanSummary.StdDevOfMaxCE = Convert.ToDecimal(Math.Round(Math.Sqrt(Convert.ToDouble(sdMaxCESum / scanCounter)), 2));
            TheLJVScanSummary.StdDevAt1kNitsEQE = Convert.ToDecimal(Math.Round(Math.Sqrt(Convert.ToDouble(sdAt1kNitsEQESum / scanCounter)), 2));
            TheLJVScanSummary.StdDevAt1kNitsCE = Convert.ToDecimal(Math.Round(Math.Sqrt(Convert.ToDouble(sdAt1kNitsCESum / scanCounter)), 2));
            //Debug.WriteLine("EQE standard deviation for device " + TheLJVScanSummary.Device.Label + " across all pixels is: " + TheLJVScanSummary.StdDevOfMaxEQE);
            //find mean values for EL Spectrum peak lambda and FWHM
            decimal peakLambdaSum = 0;
            decimal FWHMSum = 0;
            scanCounter = 0;
            foreach (ELSpectrum spec in TheLJVScanSummary.ELSpectrums)
            {
                scanCounter++;
                peakLambdaSum += spec.ELPeakLambda;
                FWHMSum += spec.ELFWHM;
            }
            if (scanCounter > 0)
            {
                TheLJVScanSummary.ELPeakLambda = Math.Round(peakLambdaSum / scanCounter, 1);
                TheLJVScanSummary.ELFWHM = Math.Round(FWHMSum / scanCounter, 1);
            }
            //Debug.WriteLine("Mean EL Peak Lambda for device " + TheLJVScanSummary.Device.Label + " across all pixels is: " + TheLJVScanSummary.ELPeakLambda);
            //Debug.WriteLine("Mean FWHM for device " + TheLJVScanSummary.Device.Label + " across all pixels is: " + TheLJVScanSummary.ELFWHM);

        }
        /// <summary>
        /// Finds desired files and checks to make sure data is in the correct format to proceed
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        public bool FindDataFilesFromPath(string fp)
        {
            bool ShouldProceedToDataProcessing = false;
            //find directories for desired files
            var selectedRawDATFiles = Directory.GetFiles(fp, "*.rawDAT", SearchOption.AllDirectories).ToList<string>();
            var selectedProcDATFiles = Directory.GetFiles(fp, "*.procDAT", SearchOption.AllDirectories).ToList<string>();
            var selectedELSpecFiles = Directory.GetFiles(fp, "*.ELSpectrum", SearchOption.AllDirectories).ToList<string>();
            var selectedImageFiles = Directory.GetFiles(fp, "*.jpg", SearchOption.AllDirectories).ToList<string>();
            //extract information from file names
            var indexedRawDATS = IndexAListOfPaths(selectedRawDATFiles);
            var indexedProcDATs = IndexAListOfPaths(selectedProcDATFiles);
            var indexedELSpecs = IndexAListOfPaths(selectedELSpecFiles);
            var indexedImages = IndexAListOfPaths(selectedImageFiles);
            //filter out files that aren't for the selected Device by DeviceIndex (Tuple Item1)
            indexedRawDATS = indexedRawDATS.Where(x => x.DeviceBatchIndex == DeviceIndex).ToList();
            indexedProcDATs = indexedProcDATs.Where(x => x.DeviceBatchIndex == DeviceIndex).ToList();
            indexedELSpecs = indexedELSpecs.Where(x => x.DeviceBatchIndex == DeviceIndex).ToList();
            indexedImages = indexedImages.Where(x => x.DeviceBatchIndex == DeviceIndex).ToList();
            //make sure there is only one TestCondition
            ShouldProceedToDataProcessing = IsOnlyOneTestCondition(indexedProcDATs) && IsOnlyOneTestCondition(indexedRawDATS)
                && IsOnlyOneTestCondition(indexedELSpecs) && IsOnlyOneTestCondition(indexedImages);
            if (ShouldProceedToDataProcessing)
            {
                LJVScanVMCollection = new ObservableCollection<LJVScanVM>();
                string firstRawDat = indexedRawDATS.First().FilePath;
                _statsDataPath = firstRawDat.Remove(firstRawDat.LastIndexOf("_"));
                _statsDataPath = _statsDataPath.Replace("Raw Data", "Statistical Data");
                string directoryCheck = _statsDataPath.Remove(_statsDataPath.LastIndexOf(@"\"));
                Directory.CreateDirectory(directoryCheck);
                _statsDataPath = string.Concat(_statsDataPath, ".statsDAT");
                Debug.WriteLine("_statsDataPath = " + _statsDataPath);
                _uniqueTCRawAndProcDatPaths = new List<Tuple<string, string>>();
                foreach (FileNameDatum rawDatFile in indexedRawDATS)
                {
                    FileNameDatum correspondingProcDat = indexedProcDATs.Where(x => x.MeasurementSite == rawDatFile.MeasurementSite).First();//find procDat for same MeasurementSite
                    LJVScanVMCollection.Add(new LJVScanVM(rawDatFile.FilePath, correspondingProcDat.FilePath));
                }
            }
            return ShouldProceedToDataProcessing;
        }
        /// <summary>
        /// Returns a list of FileNameData from a list of paths
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<FileNameDatum> IndexAListOfPaths(List<string> list)
        {
            var indexedList = new List<FileNameDatum>();
            try
            {
                foreach (string s in list)
                {
                    var fileNameDeviceIndex = s.Substring(s.LastIndexOf("-") + 1, (s.IndexOf("_Site") - s.LastIndexOf("-")) - 1);
                    int batchIndex = 0;
                    Int32.TryParse(fileNameDeviceIndex, out batchIndex);
                    Debug.WriteLine("Found a file that corresponds to device #: " + fileNameDeviceIndex);
                    var fileNameTestCondition = s.Substring(s.IndexOf("_") + 1, (s.LastIndexOf("-") - s.IndexOf("_") - 1));
                    Debug.WriteLine("with TestCondition: " + fileNameTestCondition);
                    var fileNameMeasurementSite = s.Substring(s.LastIndexOf("_") + 1, (s.LastIndexOf(".") - s.LastIndexOf("_") - 1));
                    Debug.WriteLine(" and Measurement Site/Pixel: " + fileNameMeasurementSite);
                    if (!s.Contains("Backup"))//ignore files in backup directories
                        indexedList.Add(new FileNameDatum()
                        {
                            DeviceBatchIndex = batchIndex,
                            TestCondition = fileNameTestCondition,
                            FilePath = s,
                            MeasurementSite = fileNameMeasurementSite
                        });
                }
                return indexedList;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return indexedList;
            }
        }
        private bool IsOnlyOneTestCondition(List<FileNameDatum> list)
        {
            bool OnlyOneTC = true;
            foreach (FileNameDatum item in list)
            {
                if (list.Exists(x => x.TestCondition != item.TestCondition))//check for any dissimilar TestConditions
                {
                    OnlyOneTC = false;//if .Exists() returns true, there are multiple TCs in the directory and everything will fail
                    Debug.WriteLine("Found more than one TestCondition in directory");
                }
            }
            return OnlyOneTC;
        }
        /// <summary>
        /// Step through and calculate means+std devs for each voltage in .fullDATs 
        /// </summary>
        public void GenerateStatsData()
        {
            StackTrace st = new StackTrace();
            Debug.WriteLine("GenerateStatsData() caller name: " + st.GetFrame(1).GetMethod().Name);
            StatsDataList = new List<LJVStatsDatum>();
            //remove scans where pixel did not light up
            for (int i = 0; i < LJVScanVMCollection.Count; i++)
            {
                if (!LJVScanVMCollection[i].TheLJVScan.PixelLitUp ?? false)
                {
                    Debug.WriteLine(LJVScanVMCollection[i].TheLJVScan.DeviceLJVScanSummary.Device.Label + LJVScanVMCollection[i].TheLJVScan.Pixel.Site + " did not light up");
                    LJVScanVMCollection.Remove(LJVScanVMCollection[i]);
                }
            }
            //first find the maximum voltage across all LJVScans
            decimal maxVoltage = 0;
            LJVScanVM maxScan = new LJVScanVM();
            foreach (LJVScanVM scan in LJVScanVMCollection)
            {
                try
                {
                    if (scan.FullLJVDataList.Count != scan.RawLJVDataList.Count)
                        scan.GenerateFullLJVData();
                    var scanMax = scan.FullLJVDataList.Max(x => x.Voltage);
                    if (scanMax > maxVoltage)
                    {
                        maxVoltage = scanMax;
                        maxScan = scan;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("LJVScanSummaryVM GenerateStatsData error: " + e.ToString());
                }
            }
            //assume that the StepSize is constant and loop through each step in maxScan
            for (int i = 0; i < maxScan.FullLJVDataList.Count; i++)
            {
                List<FullLJVDatum> dataAtVoltage = new List<FullLJVDatum>();
                foreach (LJVScanVM scan in LJVScanVMCollection)
                {
                    if (scan.FullLJVDataList.Count >= i)
                    {
                        FullLJVDatum datumAtIndex = scan.FullLJVDataList.Where(x => x.Voltage == maxScan.FullLJVDataList[i].Voltage).FirstOrDefault();
                        if (datumAtIndex != null)
                            dataAtVoltage.Add(datumAtIndex);
                    }
                }
                StatsDataList.Add(StatsDatumFromFullLJVList(dataAtVoltage));
            }
            SetStatsDataPath();
            DataProcessingService.WriteIENumberableToCSV(StatsDataList, _statsDataPath);
        }
        /// <summary>
        /// XD
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        private LJVStatsDatum StatsDatumFromFullLJVList(List<FullLJVDatum> dataList)
        {
            LJVStatsDatum statsDatum = new LJVStatsDatum();
            List<double> currentDensities = new List<double>();
            List<double> resistances = new List<double>();
            List<double> photoCurrents = new List<double>();
            List<double> luminances = new List<double>();
            List<double> currentEffs = new List<double>();
            List<double> powerEffs = new List<double>();
            List<double> EQEs = new List<double>();
            List<double> CIExs = new List<double>();
            List<double> CIEys = new List<double>();
            foreach (FullLJVDatum fd in dataList)
            {
                currentDensities.Add(Convert.ToDouble(fd.CurrentDensity));
                //Debug.WriteLine("currentDensity = " + fd.CurrentDensity);
                resistances.Add(Convert.ToDouble(fd.Resistance));
                photoCurrents.Add(Convert.ToDouble((fd.PhotoCurrentA + fd.PhotoCurrentB) / 2.0m));
                luminances.Add(Convert.ToDouble(fd.Luminance));
                currentEffs.Add(Convert.ToDouble(fd.CurrentEff));
                powerEffs.Add(Convert.ToDouble(fd.PowerEff));
                EQEs.Add(Convert.ToDouble(fd.EQE));
                CIExs.Add(Convert.ToDouble(fd.CameraCIEx));
                CIEys.Add(Convert.ToDouble(fd.CameraCIEy));
            }
            statsDatum.Voltage = dataList.First().Voltage;
            StatsBase calculator = new StatsBase();
            calculator.PopulateStatsFromArrayAndRound(currentDensities.ToArray(), 13);
            statsDatum.MeanCurrentDensity = Convert.ToDecimal(calculator.Mean);
            statsDatum.CurrentDensityStdDev = Convert.ToDecimal(calculator.StdDev);
            //Debug.WriteLine("statsDatum.MeanCurrentDensity = " + statsDatum.MeanCurrentDensity);
            //Debug.WriteLine("statsDatum.CurrentDensityStdDev = " + statsDatum.CurrentDensityStdDev);
            calculator.PopulateStatsFromArrayAndRound(resistances.ToArray(), 13);
            statsDatum.MeanResistance = Convert.ToDecimal(calculator.Mean);
            statsDatum.ResistanceStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(photoCurrents.ToArray(), 13);
            statsDatum.MeanPhotoCurrent = Convert.ToDecimal(calculator.Mean);
            statsDatum.PhotoCurrentStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(luminances.ToArray(), 13);
            statsDatum.MeanLuminance = Convert.ToDecimal(calculator.Mean);
            statsDatum.LuminanceStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(currentEffs.ToArray(), 13);
            statsDatum.MeanCurrentEff = Convert.ToDecimal(calculator.Mean);
            statsDatum.CurrentEffStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(powerEffs.ToArray(), 13);
            statsDatum.MeanPowerEff = Convert.ToDecimal(calculator.Mean);
            statsDatum.PowerEffStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(EQEs.ToArray(), 13);
            statsDatum.MeanEQE = Convert.ToDecimal(calculator.Mean);
            statsDatum.EQEStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(CIExs.ToArray(), 13);
            statsDatum.MeanCameraCIEx = Convert.ToDecimal(calculator.Mean);
            statsDatum.CameraCIExStdDev = Convert.ToDecimal(calculator.StdDev);
            calculator.PopulateStatsFromArrayAndRound(CIEys.ToArray(), 13);
            statsDatum.MeanCameraCIEy = Convert.ToDecimal(calculator.Mean);
            statsDatum.CameraCIEyStdDev = Convert.ToDecimal(calculator.StdDev);
            return statsDatum;
        }
        private void SetStatsDataPath()
        {
            string firstRawDat = LJVScanVMCollection.First().TheLJVScan.RawDATFilePath;
            _statsDataPath = firstRawDat.Remove(firstRawDat.LastIndexOf("_"));
            _statsDataPath = _statsDataPath.Replace("Raw Data", "Statistical Data");
            string directoryCheck = _statsDataPath.Remove(_statsDataPath.LastIndexOf(@"\"));
            Directory.CreateDirectory(directoryCheck);
            _statsDataPath = string.Concat(_statsDataPath, ".statsDAT");
            TheLJVScanSummary.StatsDataPath = _statsDataPath;
            //ctx.Entry(TheLJVScanSummary).State = System.Data.Entity.EntityState.Modified;
            ctx.SaveChanges();
            Debug.WriteLine("_statsDataPath = " + _statsDataPath);
        }
        public float[,] StatsDatArray()
        {
            if (this.StatsDataList == null && this.TheLJVScanSummary.StatsDataPath == null)
                GenerateStatsData();
            else
                LoadStatsDataIntoList(TheLJVScanSummary.StatsDataPath);
            var datum = new LJVStatsDatum();
            int propertiesCount = datum.GetType().GetProperties().Count();
            var propertiesArray = datum.GetType().GetProperties();
            float[,] dataArray = new float[1, propertiesCount];
            dataArray = new float[StatsDataList.Count, propertiesCount];
            for (int i = 0; i < StatsDataList.Count; i++)
            {
                datum = StatsDataList[i];
                for (int j = 0; j < propertiesCount; j++)
                {
                    float arrval = 0;
                    var datPropType = datum.GetType().GetProperty(propertiesArray[j].Name);
                    var datPropInstance = datPropType.GetValue(datum) as Nullable<decimal>;
                    arrval = (float)(datPropInstance ?? 0);
                    dataArray[i, j] = arrval;
                }
            }
            return dataArray;
        }

        #endregion
        #region Commands
        private RelayCommand _calculateStatsDataForSingleTestCondition;
        public ICommand CalculateStatsDataForSingleTestCondition
        {
            get
            {
                if (_calculateStatsDataForSingleTestCondition == null)
                {
                    _calculateStatsDataForSingleTestCondition = new RelayCommand(param => this.CalculateStatsDataForSingleTestConditionExecute(param));
                }
                return _calculateStatsDataForSingleTestCondition;
            }
        }
        public void CalculateStatsDataForSingleTestConditionExecute(object o)
        {
            FolderBrowserDialog folderBrowswer = new FolderBrowserDialog();
            if (folderBrowswer.ShowDialog() == DialogResult.OK)
            {
                if (FindDataFilesFromPath(folderBrowswer.SelectedPath))
                {
                    GenerateStatsData();
                }
            }
        }
        #endregion
    }
}
