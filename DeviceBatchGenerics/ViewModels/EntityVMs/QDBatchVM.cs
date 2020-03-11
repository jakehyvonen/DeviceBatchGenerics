using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeviceBatchGenerics.Support.Bases;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class QDBatchVM : CrudVMBase
    {
        public QDBatchVM()
        {
            TheQDBatch = new QDBatch();
        }
        public QDBatchVM(QDBatch qdb)
        {
            TheQDBatch = qdb;
            PopulatePropertiesFromQDBatch();
        }
        #region Members
        private StatsBase _peakEQE = new StatsBase();
        private StatsBase _peakLambda = new StatsBase();
        private StatsBase _FWHM = new StatsBase();
        private StatsBase _CIEx = new StatsBase();
        private StatsBase _CIEy = new StatsBase();
        #endregion
        #region Properties
        public StatsBase PeakEQE
        {
            get { return _peakEQE; }
            set
            {
                _peakEQE = value;
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
        public int NumberOfDevices { get; set; }
        public decimal BestEQE { get; set; }
        public QDBatch TheQDBatch { get; set; }
        #endregion
        #region Methods
        private void UpdateEntityPropsFromName()
        {
            if(TheQDBatch.Color==null)
            {
                var firstChar = TheQDBatch.Name.Substring(0, 1);
                if (firstChar == "R")
                    TheQDBatch.Color = "Red";
                if (firstChar == "G")
                    TheQDBatch.Color = "Green";
                if (firstChar == "B")
                    TheQDBatch.Color = "Blue";
            }
            if(TheQDBatch.DateReceivedOrSynthesized==null && TheQDBatch.Name.Length > 6)
            {
                Debug.WriteLine("TheQDBatch named: " + TheQDBatch.Name);
                var monthString = TheQDBatch.Name.Substring(1, 2);
                Debug.WriteLine("monthString: " + monthString);
                var dayString = TheQDBatch.Name.Substring(3, 2);
                Debug.WriteLine("dayString: " + dayString);
                var yearString = TheQDBatch.Name.Substring(5, 2);
                Debug.WriteLine("yearString: " + yearString);
                int monthInt, dayInt, yearInt;
                if(Int32.TryParse(monthString, out monthInt) && Int32.TryParse(dayString, out dayInt) && Int32.TryParse(yearString, out yearInt))
                {
                    var date = new DateTime(yearInt, monthInt, dayInt);
                    TheQDBatch.DateReceivedOrSynthesized = date;
                }
            }
            ctx.SaveChanges();
        }
        public void PopulatePropertiesFromQDBatch()
        {
            UpdateEntityPropsFromName();
            var ELSpecs = TheQDBatch.ELSpectrums.ToList();
            var PeakLambdasArray = ELSpecs.Select(x => Convert.ToDouble(x.ELPeakLambda)).ToArray();
            PeakLambda.PopulateStatsFromArrayAndRound(PeakLambdasArray, 1);
            var FWHMArray = ELSpecs.Select(x => Convert.ToDouble(x.ELFWHM)).ToArray();
            FWHM.PopulateStatsFromArrayAndRound(FWHMArray, 1);
            var CIExArray = ELSpecs.Select(x => Convert.ToDouble(x.CIEx)).ToArray();
            CIEx.PopulateStatsFromArrayAndRound(CIExArray, 3);
            var CIEyArray = ELSpecs.Select(x => Convert.ToDouble(x.CIEy)).ToArray();
            CIEy.PopulateStatsFromArrayAndRound(CIEyArray, 3);
            var DevicesWithThisQDBatch = TheQDBatch.Devices.ToList();
            NumberOfDevices = DevicesWithThisQDBatch.Count;
            BestEQE = 0;
            List<double> peakEQEs = new List<double>();
            foreach (Device d in DevicesWithThisQDBatch)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    if (ss.MaxEQE > BestEQE)
                        BestEQE = ss.MaxEQE;
                    foreach (LJVScan scan in ss.LJVScans)
                    {
                        peakEQEs.Add(Convert.ToDouble(scan.MaxEQE));
                    }
                }
            }
            var peakEQEArray = peakEQEs.ToArray();
            PeakEQE.PopulateStatsFromArrayAndRound(peakEQEArray, 2);
        }
        #endregion
    }

}
