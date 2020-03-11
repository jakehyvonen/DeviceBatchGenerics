using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using DeviceBatchGenerics.Support.DataMapping;

namespace DeviceBatchGenerics.ViewModels.PlottingVMs
{
    public class LJVPlotVM : OxyPlotVMBase
    {
        public LJVPlotVM()
        {
            this.ThePlotModel = new PlotModel();
            LJVScanVMCollection = new ObservableCollection<LJVScanVM>();
            InitializeAxisDicts();
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            SelectedRightAxis = RightAxisDict["None"];
            SelectedViewStyle = ViewStyle.Regular;
        }
        public LJVPlotVM(ObservableCollection<LJVScanVM> scans)
        {
            ThePlotModel = new PlotModel();
            LJVScanVMCollection = scans;
            InitializeAxisDicts();
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            SelectedRightAxis = RightAxisDict["None"];
            SelectedViewStyle = ViewStyle.Regular;
        }
        #region Members
        private string _rightAxisKey = "Right";
        private ObservableCollection<LJVScanVM> _LJVScanVMCollection;
        private Tuple<string, PropertyInfo> _selectedLeftAxisProperty = LJVScanPropertyDict["External Quantum Efficiency"];
        private Tuple<string, PropertyInfo> _selectedBottomAxisProperty = LJVScanPropertyDict["Luminance"];
        private Tuple<string, PropertyInfo> _selectedRightAxisProperty = null;
        private Dictionary<string, Func<string, AxisPosition, Axis>> _leftAxisDict;
        private Dictionary<string, Func<string, AxisPosition, Axis>> _bottomAxisDict;
        private Dictionary<string, Func<string, AxisPosition, Axis>> _rightAxisDict;
        private Func<string, AxisPosition, Axis> _selectedLeftAxis;
        private Func<string, AxisPosition, Axis> _selectedBottomAxis;
        private Func<string, AxisPosition, Axis> _selectedRightAxis;
        #endregion
        #region Properties

        public ObservableCollection<LJVScanVM> LJVScanVMCollection
        {
            get { return _LJVScanVMCollection; }
            set
            {
                _LJVScanVMCollection = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public static Dictionary<string, Tuple<string, PropertyInfo>> LJVScanPropertyDict
        {
            get
            {
                var _LJVScanPropertyDict = new Dictionary<string, Tuple<string, PropertyInfo>>()
                {
                    {"Voltage", Tuple.Create("Voltage (V)", typeof(ProcessedLJVDatum).GetProperty("Voltage")) },
                    {"Current Density", Tuple.Create("Current Density (mA/cm\xB2)", typeof(ProcessedLJVDatum).GetProperty("CurrentDensity")) },
                    {"Photocurrent", Tuple.Create("Photocurrent (A)", typeof(ProcessedLJVDatum).GetProperty("PhotoCurrent")) },
                    {"Luminance", Tuple.Create("Luminance (cd/m\xB2)", typeof(ProcessedLJVDatum).GetProperty("Luminance")) },
                    {"Current Efficiency", Tuple.Create("Current Eff. (cd/A)", typeof(ProcessedLJVDatum).GetProperty("CurrentEff")) },
                    {"Power Efficiency", Tuple.Create("Power Eff. (lm/W)", typeof(ProcessedLJVDatum).GetProperty("PowerEff")) },
                    {"External Quantum Efficiency", Tuple.Create("EQE (%)", typeof(ProcessedLJVDatum).GetProperty("EQE")) },
                };
                return _LJVScanPropertyDict;
            }
        }
        public Dictionary<string, Func<string, AxisPosition, Axis>> LeftAxisDict
        {
            get
            {
                return _leftAxisDict;
            }
            set
            {
                _leftAxisDict = value;
                OnPropertyChanged();
            }
        }
        public Tuple<string, PropertyInfo> SelectedLeftAxisProperty
        {
            get { return _selectedLeftAxisProperty; }
            set
            {
                _selectedLeftAxisProperty = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public Func<string, AxisPosition, Axis> SelectedLeftAxis
        {
            get { return _selectedLeftAxis; }
            set
            {
                _selectedLeftAxis = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public Dictionary<string, Func<string, AxisPosition, Axis>> BottomAxisDict
        {
            get
            {
                return _bottomAxisDict;
            }
            set
            {
                _bottomAxisDict = value;
                OnPropertyChanged();
            }
        }
        public Tuple<string, PropertyInfo> SelectedBottomAxisProperty
        {
            get { return _selectedBottomAxisProperty; }
            set
            {
                _selectedBottomAxisProperty = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public Func<string, AxisPosition, Axis> SelectedBottomAxis
        {
            get { return _selectedBottomAxis; }
            set
            {
                _selectedBottomAxis = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public Dictionary<string, Func<string, AxisPosition, Axis>> RightAxisDict
        {
            get
            {
                return _rightAxisDict;
            }
            set
            {
                _rightAxisDict = value;
                OnPropertyChanged();
            }
        }
        public Tuple<string, PropertyInfo> SelectedRightAxisProperty
        {
            get { return _selectedRightAxisProperty; }
            set
            {
                _selectedRightAxisProperty = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        public Func<string, AxisPosition, Axis> SelectedRightAxis
        {
            get { return _selectedRightAxis; }
            set
            {
                _selectedRightAxis = value;
                OnPropertyChanged();
                UpdatePlotModel();
            }
        }
        #endregion
        #region Methods
        private void InitializeAxisDicts()
        {
            LeftAxisDict = new Dictionary<string, Func<string, AxisPosition, Axis>>()
                {
                    {
                        "Linear",
                        new Func<string, AxisPosition, Axis>(CreateNewLinearAxis)
                    },
                    {
                        "Logarithmic",
                         new Func<string, AxisPosition, Axis>(CreateNewLogAxis)
                    }
                };
            BottomAxisDict = new Dictionary<string, Func<string, AxisPosition, Axis>>()
                {
                    {
                        "Linear",
                        new Func<string, AxisPosition, Axis>(CreateNewLinearAxis)
                    },
                    {
                        "Logarithmic",
                         new Func<string, AxisPosition, Axis>(CreateNewLogAxis)
                    }
                };
            RightAxisDict = new Dictionary<string, Func<string, AxisPosition, Axis>>()
            {

                    {
                        "Linear",
                        new Func<string, AxisPosition, Axis>(CreateNewLinearAxis)
                    },
                    {
                        "Logarithmic",
                         new Func<string, AxisPosition, Axis>(CreateNewLogAxis)
                    },
                {
                    "None",
                    new Func<string, AxisPosition, Axis>(CreateNullAxis)
                }

            };
        }
        private LinearAxis CreateNewLinearAxis(string title, AxisPosition axpos)
        {
            if (!IsGeneratingPlotsForReporting)
            {
                var newAxis = new LinearAxis
                {
                    Position = axpos,
                    Minimum = 0,
                    Title = title
                };
                if (axpos == AxisPosition.Right)
                    newAxis.Key = _rightAxisKey;
                return newAxis;
            }
            else
            {
                var newAxis = new LinearAxis
                {
                    Position = axpos,
                    Minimum = 0,
                    Title = title
                };
                if (axpos == AxisPosition.Right)
                    newAxis.Key = _rightAxisKey;
                return newAxis;
            }
        }
        private LogarithmicAxis CreateNewLogAxis(string title, AxisPosition axpos)
        {
            if (!IsGeneratingPlotsForReporting)
            {
                var newAxis = new LogarithmicAxis
                {
                    Position = axpos,
                    Title = title
                };
                if (axpos == AxisPosition.Right)
                    newAxis.Key = _rightAxisKey;
                return newAxis;
            }
            else
            {
                var newAxis = new LogarithmicAxis
                {
                    Position = axpos,
                    Title = title
                };
                if (axpos == AxisPosition.Right)
                    newAxis.Key = _rightAxisKey;
                return newAxis;
            }
        }
        private LinearAxis CreateNullAxis(string title, AxisPosition axpos)
        {
            var newAxis = new LinearAxis
            {
                Title = "Null Axis"
            };
            return newAxis;
        }

        public void UpdatePlotModel()
        {
            ThePlotModel = new PlotModel();
            ThePlotModel.LegendTitle = "Legend";
            ThePlotModel.LegendPosition = LegendPosition.TopLeft;
            int colorCounter = 0;
            if (SelectedLeftAxis != null && SelectedBottomAxis != null)
            {
                if (SelectedRightAxisProperty == null || SelectedRightAxis == RightAxisDict["None"]) //we are not plotting a 2nd property
                {
                    ThePlotModel.Axes.Add((Axis)SelectedLeftAxis.DynamicInvoke(SelectedLeftAxisProperty.Item1, AxisPosition.Left));
                    ThePlotModel.Axes.Add((Axis)SelectedBottomAxis.DynamicInvoke(SelectedBottomAxisProperty.Item1, AxisPosition.Bottom));
                    foreach (LJVScanVM scan in LJVScanVMCollection)
                    {

                        LineSeries scanSeries = new LineSeries();
                        scanSeries.Color = LineSeriesColors[colorCounter];
                        colorCounter++;
                        if (SelectedViewStyle == ViewStyle.Regular)
                            scanSeries.Title = scan.TheLJVScan.Pixel.Site;
                        if (SelectedViewStyle == ViewStyle.Aging)
                            scanSeries.Title = scan.TheLJVScan.DeviceLJVScanSummary.TestCondition;
                        PropertyInfo xprop = SelectedBottomAxisProperty.Item2;
                        PropertyInfo yprop = SelectedLeftAxisProperty.Item2;
                        foreach (ProcessedLJVDatum pd in scan.ProcLJVDataList)
                        {
                            double xval = Convert.ToDouble((decimal)xprop.GetValue(pd));
                            double yval = Convert.ToDouble((decimal)yprop.GetValue(pd));
                            scanSeries.Points.Add(new DataPoint(xval, yval));
                        }
                        ThePlotModel.Series.Add(scanSeries);
                    }
                }
                else //plot left and right axis properties vs bottom
                {
                    ThePlotModel.Axes.Add((Axis)SelectedLeftAxis.DynamicInvoke(SelectedLeftAxisProperty.Item1, AxisPosition.Left));
                    ThePlotModel.Axes.Add((Axis)SelectedRightAxis.DynamicInvoke(SelectedRightAxisProperty.Item1, AxisPosition.Right));
                    ThePlotModel.Axes.Add((Axis)SelectedBottomAxis.DynamicInvoke(SelectedBottomAxisProperty.Item1, AxisPosition.Bottom));
                    foreach (LJVScanVM scan in LJVScanVMCollection)
                    {
                        LineSeries scanSeries1 = new LineSeries();
                        LineSeries scanSeries2 = new LineSeries();
                        scanSeries2.LineStyle = LineStyle.DashDot;
                        scanSeries1.Color = LineSeriesColors[colorCounter];
                        scanSeries2.Color = LineSeriesColors[colorCounter];
                        colorCounter++;
                        scanSeries2.YAxisKey = _rightAxisKey;
                        if (SelectedViewStyle == ViewStyle.Regular)
                            scanSeries1.Title = scan.TheLJVScan.Pixel.Site;
                        if (SelectedViewStyle == ViewStyle.Aging)
                            scanSeries1.Title = scan.TheLJVScan.DeviceLJVScanSummary.TestCondition;
                        PropertyInfo xprop = SelectedBottomAxisProperty.Item2;
                        PropertyInfo y1prop = SelectedLeftAxisProperty.Item2;
                        PropertyInfo y2prop = SelectedRightAxisProperty.Item2;
                        foreach (ProcessedLJVDatum pd in scan.ProcLJVDataList)
                        {
                            double xval = Convert.ToDouble((decimal)xprop.GetValue(pd));
                            double y1val = Convert.ToDouble((decimal)y1prop.GetValue(pd));
                            double y2val = Convert.ToDouble((decimal)y2prop.GetValue(pd));
                            scanSeries1.Points.Add(new DataPoint(xval, y1val));
                            scanSeries2.Points.Add(new DataPoint(xval, y2val));
                        }
                        ThePlotModel.Series.Add(scanSeries1);
                        ThePlotModel.Series.Add(scanSeries2);
                    }
                }
            }
            ThePlotModel.InvalidatePlot(true);//do this out of superstition/ignorance
        }
        public void SaveLJVPlotBitmap(string fp)
        {
            IsGeneratingPlotsForReporting = true;
            SelectedLeftAxis = LeftAxisDict["Logarithmic"];
            SelectedLeftAxisProperty = LJVScanPropertyDict["Current Density"];
            SelectedBottomAxis = BottomAxisDict["Linear"];
            SelectedBottomAxisProperty = LJVScanPropertyDict["Voltage"];
            SelectedRightAxis = RightAxisDict["Logarithmic"];
            SelectedRightAxisProperty = LJVScanPropertyDict["Luminance"];
            ExportPlotBitmap(fp);
        }
        public void SaveEQELPlotBitmap(string fp)
        {
            IsGeneratingPlotsForReporting = true;
            SelectedRightAxisProperty = null;
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedLeftAxisProperty = LJVScanPropertyDict["External Quantum Efficiency"];
            SelectedBottomAxis = BottomAxisDict["Linear"];
            SelectedBottomAxisProperty = LJVScanPropertyDict["Luminance"];
            ExportPlotBitmap(fp);
        }
        public void SaveEQEJPlotBitmap(string fp)
        {
            IsGeneratingPlotsForReporting = true;
            SelectedRightAxisProperty = null;
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedLeftAxisProperty = LJVScanPropertyDict["External Quantum Efficiency"];
            SelectedBottomAxis = BottomAxisDict["Linear"];
            SelectedBottomAxisProperty = LJVScanPropertyDict["Current Density"];
            ExportPlotBitmap(fp);
        }
        public void SaveJVPlotBitmap(string fp)
        {
            IsGeneratingPlotsForReporting = true;
            SelectedRightAxisProperty = null;
            SelectedLeftAxis = LeftAxisDict["Logarithmic"];
            SelectedLeftAxisProperty = LJVScanPropertyDict["Current Density"];
            SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            SelectedBottomAxisProperty = LJVScanPropertyDict["Voltage"];
            ExportPlotBitmap(fp);
        }
        #endregion
    }

}
