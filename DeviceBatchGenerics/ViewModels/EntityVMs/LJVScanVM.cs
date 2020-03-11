using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CsvHelper;
using System.IO;
using System.Windows;
using DeviceBatchGenerics.Support;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support.DataMapping;
using EFDeviceBatchCodeFirst;
using System.Reflection;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class LJVScanVM : CrudVMBase
    {
        #region Constructors
        public LJVScanVM()
        {
            TheLJVScan = new LJVScan();
            _ProcLJVDataList = new List<ProcessedLJVDatum>();
        }
        public LJVScanVM(string procPath)
        {
            TheLJVScan = new LJVScan();
            LoadProcLJVDataIntoList(procPath);
            string rawDatPath = procPath.Replace("Processed Data", "Raw Data");
            rawDatPath = rawDatPath.Replace(".procDAT", ".rawDAT");
            if (File.Exists(rawDatPath))
            {
                TheLJVScan.RawDATFilePath = rawDatPath;
                LoadRawLJVDataIntoList(TheLJVScan.RawDATFilePath);
                GenerateFullLJVData();
            }
        }
        public LJVScanVM(string rawPath, string procPath)
        {
            TheLJVScan = new LJVScan();
            LoadRawLJVDataIntoList(rawPath);
            LoadProcLJVDataIntoList(procPath);
            string fullDataPath = rawPath.Replace("Raw Data", "Full Data");
            string folderPath = fullDataPath.Remove(fullDataPath.LastIndexOf(@"\"));
            fullDataPath = fullDataPath.Replace(".rawDAT", ".fullDAT");
            Directory.CreateDirectory(folderPath);
            ConstructFullLJVDataListAndCSV(fullDataPath);
        }
        public LJVScanVM(LJVScan scan)
        {
            //do this to avoid IEChangeTracker conflict
            TheLJVScan = ctx.LJVScans.Where(x => x.LJVScanId == scan.LJVScanId).FirstOrDefault();
            if (scan.ProcDATFilePath != null)
            {
                LoadProcLJVDataIntoList(scan.ProcDATFilePath);
                if (scan.RawDATFilePath != null)
                {
                    LoadRawLJVDataIntoList(scan.RawDATFilePath);
                    if (!File.Exists(scan.FullDATFilePath))
                        GenerateFullLJVData();
                    else
                        LoadFullDATIntoList(scan.FullDATFilePath);
                }
                else
                {
                    string rawDatPath = scan.ProcDATFilePath.Replace("Processed Data", "Raw Data");
                    rawDatPath = rawDatPath.Replace(".procDAT", ".rawDAT");
                    if (File.Exists(rawDatPath))
                    {
                        scan.RawDATFilePath = rawDatPath;
                        LoadRawLJVDataIntoList(scan.RawDATFilePath);
                        if (!File.Exists(scan.FullDATFilePath))
                            GenerateFullLJVData();
                        else
                            LoadFullDATIntoList(scan.FullDATFilePath);
                    }
                }

            }
        }
        #endregion
        #region Members
        private List<ProcessedLJVDatum> _ProcLJVDataList = new List<ProcessedLJVDatum>();
        private List<RawLJVDatum> _rawLJVDataList = new List<RawLJVDatum>();
        private List<FullLJVDatum> _fullLJVDataList = new List<FullLJVDatum>();
        private decimal _luminanceCutoffValue = 10.0m;
        #endregion
        #region Properties
        public List<ProcessedLJVDatum> ProcLJVDataList
        {
            get { return _ProcLJVDataList; }
            set
            {
                _ProcLJVDataList = value;
                OnPropertyChanged();
            }
        }
        public List<RawLJVDatum> RawLJVDataList
        {
            get { return _rawLJVDataList; }
            set
            {
                _rawLJVDataList = value;
                OnPropertyChanged();
            }
        }
        public List<FullLJVDatum> FullLJVDataList
        {
            get { return _fullLJVDataList; }
            set
            {
                _fullLJVDataList = value;
                OnPropertyChanged();
            }
        }
        public LJVScan TheLJVScan { get; set; }
        #endregion
        #region Methods
        public float[,] ProcDatArray()
        {
            float[,] dataArray = new float[1, 7];
            dataArray = new float[ProcLJVDataList.Count, 7];//assume procDATs will always have 7 columns
            for (int i = 0; i < ProcLJVDataList.Count; i++)
            {
                dataArray[i, 0] = (float)ProcLJVDataList[i].Voltage;
                dataArray[i, 1] = (float)ProcLJVDataList[i].CurrentDensity;
                dataArray[i, 2] = (float)ProcLJVDataList[i].PhotoCurrent;
                dataArray[i, 3] = (float)ProcLJVDataList[i].Luminance;
                dataArray[i, 4] = (float)ProcLJVDataList[i].CurrentEff;
                dataArray[i, 5] = (float)ProcLJVDataList[i].PowerEff;
                dataArray[i, 6] = (float)ProcLJVDataList[i].EQE;
            }
            return dataArray;
        }
        public string[,] FullDatArray()
        {
            var datum = new FullLJVDatum();
            var type = datum.GetType();
            var datumProps = new List<PropertyInfo>(type.GetProperties());
            List<PropertyInfo> ignoredProps = new List<PropertyInfo>()
            {
                typeof(FullLJVDatum).GetProperty("ELSpecPath"),
                typeof(FullLJVDatum).GetProperty("ELSpecData")
            };
            //remove ignored props from datumProps
            foreach (PropertyInfo p in ignoredProps)
            {
                if (p != null)
                {
                    for (int i = datumProps.Count - 1; i >= 0; i--)
                    {
                        if (datumProps[i].Name == p.Name)
                            datumProps.RemoveAt(i);
                    }
                }
            }
            string[,] dataArray = new string[FullLJVDataList.Count, datumProps.Count()];
            //load values into array by property name with reflection
            for (int i = 0; i < FullLJVDataList.Count; i++)
            {
                int propsCounter = 0;
                foreach (PropertyInfo p in datumProps)
                {
                    //Debug.WriteLine("datumProps.Name: " + p.Name);
                    if (FullLJVDataList[i].GetType().GetProperty(p.Name) != null)
                    {
                        var value = (FullLJVDataList[i].GetType().GetProperty(p.Name).GetValue(FullLJVDataList[i], null) ?? "").ToString();
                        dataArray[i, propsCounter] = value;
                        propsCounter++;
                    }
                }
            }
            return dataArray;
        }
        private void LoadProcLJVDataIntoList(string fp)
        {
            try
            {
                using (var sr = new StreamReader(fp))
                {
                    var reader = new CsvReader(sr);
                    ProcLJVDataList = reader.GetRecords<ProcessedLJVDatum>().ToList<ProcessedLJVDatum>();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception!!!$$!: " + e.ToString());
            }

        }
        private void LoadRawLJVDataIntoList(string fp)
        {
            using (var sr = new StreamReader(fp))
            {
                var reader = new CsvReader(sr);
                reader.Configuration.MissingFieldFound = null;
                reader.Configuration.HeaderValidated = null;
                RawLJVDataList = reader.GetRecords<RawLJVDatum>().ToList<RawLJVDatum>();
            }
        }
        private void LoadFullDATIntoList(string fp)
        {
            using (var sr = new StreamReader(fp))
            {
                var reader = new CsvReader(sr);
                reader.Configuration.MissingFieldFound = null;
                reader.Configuration.HeaderValidated = null;
                FullLJVDataList = reader.GetRecords<FullLJVDatum>().ToList<FullLJVDatum>();
            }
        }
        private void ConstructFullLJVDataListAndCSV(string fp)
        {
            FullLJVDataList = new List<FullLJVDatum>();
            foreach (RawLJVDatum rawdat in RawLJVDataList)
            {
                try
                {
                    ProcessedLJVDatum procdat = ProcLJVDataList.Where(x => x.Voltage == rawdat.Voltage).First();
                    FullLJVDatum newDatum = new FullLJVDatum();
                    if (rawdat.PhotoCurrentA != 0)
                    {
                        newDatum = new FullLJVDatum()
                        {
                            Voltage = rawdat.Voltage,
                            Current = rawdat.Current,
                            CurrentDensity = procdat.CurrentDensity,
                            Resistance = rawdat.Resistance,
                            PhotoCurrentA = rawdat.PhotoCurrentA,
                            PhotoCurrentB = rawdat.PhotoCurrentB,
                            PhotoCurrentC = rawdat.PhotoCurrentC,
                            CameraCIEx = rawdat.CameraCIEx,
                            CameraCIEy = rawdat.CameraCIEy,
                            CameraLuminance = rawdat.CameraLuminance,
                            Luminance = procdat.Luminance,
                            CurrentEff = procdat.CurrentEff,
                            PowerEff = procdat.PowerEff,
                            EQE = procdat.EQE,
                            PCurrChangePercent = ((rawdat.PhotoCurrentC / rawdat.PhotoCurrentA) - 1) * 100,
                            TimeStamp = rawdat.TimeStamp
                            //generate method to find ELSpecPaths from SpectrumAtEachStep function
                        };
                    }

                    FullLJVDataList.Add(newDatum);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("error at LJVScanVM ConstructFullLJVDataListAndCSV: " + e.ToString());
                }

            }
            DataProcessingService.WriteIENumberableToCSV(FullLJVDataList, fp);
        }
        public void GenerateFullLJVData()
        {
            try
            {
                if (TheLJVScan.FullDATFilePath == null)
                {
                    string fullDataPath = TheLJVScan.RawDATFilePath.Replace("Raw Data", "Full Data");
                    string folderPath = fullDataPath.Remove(fullDataPath.LastIndexOf(@"\"));
                    fullDataPath = fullDataPath.Replace(".rawDAT", ".fullDAT");
                    Directory.CreateDirectory(folderPath);
                    TheLJVScan.FullDATFilePath = fullDataPath;
                }
                ConstructFullLJVDataListAndCSV(TheLJVScan.FullDATFilePath);
                ctx.SaveChanges();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception at GenerateFullLJVData(): " + e.ToString());
            }

        }
        private void TrimEfficiencies()
        {
            foreach (ProcessedLJVDatum pd in ProcLJVDataList)
            {
                if (pd.Luminance < _luminanceCutoffValue)
                {
                    pd.CurrentEff = 0;
                    pd.PowerEff = 0;
                    pd.EQE = 0;
                }
            }
        }
        private double FindInterpolatedValueForTargetLuminance(int i, string property, double targetLuminance)
        {
            //x=(y-y1)*((x2-x1)/(y2-y1))+x1
            //where i = 1, i + 1 = 2 and y = targetLuminance and x is the desired property
            double interpolatedValue = 0;
            try
            {
                double y1 = Convert.ToDouble(ProcLJVDataList[i].GetType().GetProperty("Luminance").GetValue(ProcLJVDataList[i], null));
                double y2 = Convert.ToDouble(ProcLJVDataList[i + 1].GetType().GetProperty("Luminance").GetValue(ProcLJVDataList[i + 1], null));
                double x1 = Convert.ToDouble(ProcLJVDataList[i].GetType().GetProperty(property).GetValue(ProcLJVDataList[i], null));
                double x2 = Convert.ToDouble(ProcLJVDataList[i + 1].GetType().GetProperty(property).GetValue(ProcLJVDataList[i + 1], null));
                interpolatedValue = (targetLuminance - y1) * ((x2 - x1) / (y2 - y1)) + x1;
                //Debug.WriteLine("Interpolated value for " + property + " at luminance target: " + targetLuminance+ "nits is: " + interpolatedValue);
                return interpolatedValue;
            }
            catch (Exception e)
            {
                Debug.WriteLine("this one threw an exception");
                MessageBox.Show(e.ToString());
                return interpolatedValue;
            }
        }
        public void PopulatePropertiesFromPath(string fp)
        {
            TheLJVScan.ProcDATFilePath = fp;
            LoadProcLJVDataIntoList(fp);
            TrimEfficiencies();
            TheLJVScan.TimeWhenAcquired = File.GetCreationTime(fp);
            TheLJVScan.MaxEQE = Math.Round(Convert.ToDecimal(ProcLJVDataList.Max(x => x.EQE)), 1);
            TheLJVScan.MaxCE = Math.Round(Convert.ToDecimal(ProcLJVDataList.Max(x => x.CurrentEff)), 1);
            TheLJVScan.MaxPE = Math.Round(Convert.ToDecimal(ProcLJVDataList.Max(x => x.PowerEff)), 1);
            TheLJVScan.MaxVoltage = Math.Round(Convert.ToDecimal(ProcLJVDataList.Max(x => x.Voltage)), 1);
            int maxEQEIndex = ProcLJVDataList.IndexOf(ProcLJVDataList.Where(x => x.EQE == ProcLJVDataList.Max(y => y.EQE)).First());
            TheLJVScan.LuminanceAtMaxEQE = Math.Round(Convert.ToDecimal(ProcLJVDataList[maxEQEIndex].Luminance));
            Debug.WriteLine("Found max EQE to be: " + TheLJVScan.MaxEQE + " with luminance: " + TheLJVScan.LuminanceAtMaxEQE);
            //find indices of data for luminance before and after 500 nits
            var listMaxLuminance = ProcLJVDataList.Max(x => x.Luminance);
            if (listMaxLuminance < 10.0m)
                TheLJVScan.PixelLitUp = false;
            else
                TheLJVScan.PixelLitUp = true;
            if (listMaxLuminance > 1000.0m)
            {
                int i = ProcLJVDataList.Count() - 1;
                if (listMaxLuminance > ProcLJVDataList.Last().Luminance) //special case where the luminance reaches a peak value and drops off
                {
                    i = ProcLJVDataList.IndexOf(ProcLJVDataList.Where(x => x.Luminance == listMaxLuminance).First());
                }
                while (ProcLJVDataList[i].Luminance > 1000.0m)
                {
                    i--;
                }
                TheLJVScan.At1kNitsEQE = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "EQE", 1000.0)), 1);
                TheLJVScan.At1kNitsCE = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "CurrentEff", 1000.0)), 1);
                TheLJVScan.At1kNitsCurrentDensity = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "CurrentDensity", 1000.0)), 2);
                TheLJVScan.At1kNitsVoltage = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "Voltage", 1000.0)), 2);
            }
            if (listMaxLuminance > 500.0m)
            {
                int i = ProcLJVDataList.Count() - 1;
                if (listMaxLuminance > ProcLJVDataList.Last().Luminance) //special case where the luminance reaches a peak value and drops off
                {
                    i = ProcLJVDataList.IndexOf(ProcLJVDataList.Where(x => x.Luminance == listMaxLuminance).First());
                }
                while (ProcLJVDataList[i].Luminance > 500.0m)
                {
                    i--;
                }
                TheLJVScan.At500NitsEQE = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "EQE", 500.0)), 1);
                TheLJVScan.At500NitsCE = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "CurrentEff", 500.0)), 1);
                TheLJVScan.At500NitsCurrentDensity = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "CurrentDensity", 500.0)), 2);
                TheLJVScan.At500NitsVoltage = Math.Round(Convert.ToDecimal(FindInterpolatedValueForTargetLuminance(i, "Voltage", 500.0)), 2);
            }
            if (listMaxLuminance > 500.0m && listMaxLuminance < 1000.0m)
            {
                TheLJVScan.At1kNitsEQE = 0;
                TheLJVScan.At1kNitsCE = 0;
                TheLJVScan.At1kNitsCurrentDensity = 0;
                TheLJVScan.At1kNitsVoltage = 0;
            }
            if (listMaxLuminance < 500.0m)
            {
                TheLJVScan.At500NitsEQE = 0;
                TheLJVScan.At500NitsCE = 0;
                TheLJVScan.At500NitsCurrentDensity = 0;
                TheLJVScan.At500NitsVoltage = 0;
                TheLJVScan.At1kNitsEQE = 0;
                TheLJVScan.At1kNitsCE = 0;
                TheLJVScan.At1kNitsCurrentDensity = 0;
                TheLJVScan.At1kNitsVoltage = 0;
            }
        }
        #endregion
    }

}
