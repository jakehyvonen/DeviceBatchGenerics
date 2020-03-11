using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using System.IO;
using System.Windows;
using System.Diagnostics;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support.DataMapping;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class LifetimeVM : CrudVMBase
    {
        #region Constructors
        public LifetimeVM()
        {
            TheLifetime = new Lifetime();
            _lifetimeDataList = new List<CryscoLifetimeDatum>();
        }
        public LifetimeVM(Lifetime life)
        {
            TheLifetime = life;
            _lifetimeDataList = new List<CryscoLifetimeDatum>();
            if (life.FilePath != null)
            {
                //LoadLifetimeDataIntoList(life.FilePath);
                PopulatePropertiesFromContext();
            }
        }
        #endregion
        #region Members
        private List<CryscoLifetimeDatum> _lifetimeDataList;
        private Lifetime _theLifetime;
        private string _cryscoFileName = "unnamed";
        //private double _totalHoursElapsed;
        //private double _setCurrentDensity;
        private double _daysBetweenDeviceFabAndLifetimeTest;
        private string _label;
        #endregion
        #region Properties
        public double DaysBetweenDeviceFabAndLifetimeTest
        {
            get { return _daysBetweenDeviceFabAndLifetimeTest; }
            set
            {
                _daysBetweenDeviceFabAndLifetimeTest = value;
                OnPropertyChanged();
            }
        }
        public Lifetime TheLifetime
        {
            get { return _theLifetime; }
            set
            {
                _theLifetime = value;
                OnPropertyChanged();
            }
        }
        public List<CryscoLifetimeDatum> LifetimeDataList
        {
            get { return _lifetimeDataList; }
            set
            {
                _lifetimeDataList = value;
                OnPropertyChanged();
            }
        }
        public string CryscoFileName
        {
            get { return _cryscoFileName; }
            set
            {
                _cryscoFileName = value;
                OnPropertyChanged();
            }
        }
        public string Label
        {
            get { return _label; }
            set
            {
                _label = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #region Methods
        private void GetLifetimeValuesFromCryscoHeader()
        {
            using (var sr = new StreamReader(TheLifetime.FilePath))
            {
                int numberOfPreHeaderRows = 7;// Crysco .csvs include 7 rows before the header row
                for (int i = 0; i < numberOfPreHeaderRows; i++)
                {
                    var line = sr.ReadLine();//read through the rows until the header row
                    string[] fields = line.Split(',');
                    Debug.WriteLine("Read line: " + line);
                    if (fields[0] == "Init Luminance (cd/m2):")//get the initial luminance value from the preheader row
                    {
                        TheLifetime.InitialLuminance = Math.Round(Convert.ToDecimal(fields[1]), 2);//assign it to the Lifetime entity
                        Debug.WriteLine("Initial Luminance: " + TheLifetime.InitialLuminance);
                    }
                    if (fields[0] == "ID:")//get the ID value
                        CryscoFileName = fields[1];
                    if (fields[0] == "Start Time:")
                        TheLifetime.StartDateTime = Convert.ToDateTime(fields[1]);
                }
            }
        }
        public void LoadLifetimeDataIntoList()
        {
            using (var sr = new StreamReader(TheLifetime.FilePath))
            {
                int numberOfPreHeaderRows = 7;// Crysco .csvs include 7 rows before the header row that we need to remove before passing to CsvReader
                for (int i = 0; i < numberOfPreHeaderRows; i++)
                {
                    var line = sr.ReadLine();//read through the rows until the header row                   
                }
                using (var csv = new CsvReader(sr))
                {
                    csv.Configuration.RegisterClassMap<CryscoLifetimeDataMap>();//map datum values to Crysco .csv headers
                    LifetimeDataList = csv.GetRecords<CryscoLifetimeDatum>().ToList<CryscoLifetimeDatum>();//load data from .csv into list                  
                }
            }
            //calculate efficiencies
            foreach (CryscoLifetimeDatum cld in LifetimeDataList)
            {
                cld.CurrentEfficiency = Math.Round(((4E-3 * cld.Luminance) / cld.Current), 3);
                cld.PowerEfficiency = cld.CurrentEfficiency * Math.PI / cld.PixelVoltage;
            }
        }
        public void TrimDataList()//trim data points for log scale plotting to improve plotting performance
        {
            int desiredNumberOfDataPoints = 6000;//heuristic based on trial and error
            while (LifetimeDataList.Count > desiredNumberOfDataPoints)
            {
                var modulusValue = 12;//we want to remove fewer data points initially since LED properties change most rapidly at first
                var decileTracker = 1;//every 10% of the list we increase the frequency of data removal by decreasing the modulus value
                //i.e., 1 corresponds to the first 10% of data points, 2 to the first 20%...
                for (int i = 0; i < LifetimeDataList.Count; i++)
                {
                    if (((double)i * 10 / (double)LifetimeDataList.Count) > decileTracker)//*10 because all values are otherwise < 1
                    {
                        decileTracker++;
                        modulusValue--;
                        //Debug.WriteLine("Mod value is now" + modulusValue);
                    }
                    if (i % modulusValue == 0 && i != LifetimeDataList.Count - 1)//remove data point every modulusValue except for the final
                        LifetimeDataList.RemoveAt(i);
                }
                //Debug.WriteLine("LifetimeDataList.Count is now: " + LifetimeDataList.Count);
            }
        }
        private void PopulatePropertiesFromContext()
        {
            try
            {
                //LoadLifetimeDataIntoList();
                Label = TheLifetime.Pixel.Site;
                var lifeStartDate = TheLifetime.StartDateTime ?? default(DateTime);
                var timespan = lifeStartDate - TheLifetime.Pixel.Device.DeviceBatch.FabDate;
                DaysBetweenDeviceFabAndLifetimeTest = Math.Round(Convert.ToDouble(timespan.TotalDays), 1);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                MessageBox.Show(e.ToString());
            }
        }
        public void PopulatePropertiesFromPath()
        {
            try
            {
                //TheLifetime = new Lifetime();
                //TheLifetime.FilePath = fp;
                GetLifetimeValuesFromCryscoHeader();
                LoadLifetimeDataIntoList();
                TheLifetime.SetCurrent = Math.Round(Convert.ToDecimal(LifetimeDataList.First().Current), 3); //this is in milliamps
                TheLifetime.SetCurrentDensity = Math.Round(Convert.ToDecimal(LifetimeDataList.First().CurrentDensity), 3);
                TheLifetime.InitialCE = Math.Round((4E-3m * TheLifetime.InitialLuminance) / TheLifetime.SetCurrent, 1);
                var lowestRelativeLuminance = LifetimeDataList.Last().RelativeLuminance;//find how far the test went and then populate values
                TheLifetime.TotalHoursElapsed = Math.Round(Convert.ToDecimal(LifetimeDataList.Last().ElapsedHours), 3);
                var lifeStartDate = TheLifetime.StartDateTime ?? default(DateTime);
                var timespan = lifeStartDate - TheLifetime.Pixel.Device.DeviceBatch.FabDate;
                DaysBetweenDeviceFabAndLifetimeTest = Math.Round(Convert.ToDouble(timespan.TotalDays), 1);
                if (lowestRelativeLuminance <= 0.97)
                    TheLifetime.TimeUntil97Percent = Math.Round(Convert.ToDecimal(LifetimeDataList.Where(x => x.RelativeLuminance <= 0.97d).First().ElapsedHours), 3);//find the first datum where relativeLuminance is less than 97% and get the elapsedHours
                if (lowestRelativeLuminance <= 0.90)
                    TheLifetime.TimeUntil90Percent = Math.Round(Convert.ToDecimal(LifetimeDataList.Where(x => x.RelativeLuminance <= 0.90d).First().ElapsedHours), 3);//find the first datum where relativeLuminance is less than 97% and get the elapsedHours
                if (lowestRelativeLuminance <= 0.50)
                    TheLifetime.TimeUntil50Percent = Math.Round(Convert.ToDecimal(LifetimeDataList.Where(x => x.RelativeLuminance <= 0.50d).First().ElapsedHours), 3);//find the first datum where relativeLuminance is less than 97% and get the elapsedHours
                foreach (CryscoLifetimeDatum cld in LifetimeDataList)
                {
                    cld.CurrentEfficiency = Math.Round(((4E-3 * cld.Luminance) / cld.Current), 3);
                    cld.PowerEfficiency = cld.CurrentEfficiency * Math.PI / cld.PixelVoltage;
                }
                TrimDataList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                MessageBox.Show(e.ToString());
            }
        }
        #endregion
    }
}
