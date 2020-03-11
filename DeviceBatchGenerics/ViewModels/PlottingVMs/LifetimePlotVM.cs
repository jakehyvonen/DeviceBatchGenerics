using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Reflection;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using DeviceBatchGenerics.Support.DataMapping;
using DeviceBatchGenerics.Support;

namespace DeviceBatchGenerics.ViewModels.PlottingVMs
{
    public class LifetimePlotVM : OxyPlotVMBase
    {
        public LifetimePlotVM()
        {
            ThePlotModel = new PlotModel();
            TheLifetimeVM = new LifetimeVM();
            InitializeAxisDicts();
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            SelectedRightAxis = RightAxisDict["Linear"];
        }
        public LifetimePlotVM(ObservableCollection<LifetimeVM> lifetimes)
        {
            ThePlotModel = new PlotModel();
            LifetimeVMCollection = lifetimes;
            foreach (LifetimeVM life in lifetimes)
            {
                life.LoadLifetimeDataIntoList();
                life.TrimDataList();
            }
            InitializeAxisDicts();
            SelectedLeftAxis = LeftAxisDict["Linear"];
            SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            SelectedRightAxis = RightAxisDict["Linear"];
        }
        #region Members
        private string _rightAxisKey = "Right";
        private LifetimeVM _theLifetimeVM;
        private ObservableCollection<LifetimeVM> _lifetimeVMCollection;
        private Tuple<string, PropertyInfo> _selectedLeftAxisProperty = LifetimePropertyDict["Luminance"];
        private Tuple<string, PropertyInfo> _selectedBottomAxisProperty = LifetimePropertyDict["Time"];
        private Tuple<string, PropertyInfo> _selectedRightAxisProperty = LifetimePropertyDict["Voltage"];
        private Dictionary<string, Func<string, AxisPosition, Axis>> _leftAxisDict;
        private Dictionary<string, Func<string, AxisPosition, Axis>> _bottomAxisDict;
        private Dictionary<string, Func<string, AxisPosition, Axis>> _rightAxisDict;
        private Func<string, AxisPosition, Axis> _selectedLeftAxis;
        private Func<string, AxisPosition, Axis> _selectedBottomAxis;
        private Func<string, AxisPosition, Axis> _selectedRightAxis;
        #endregion
        #region Properties
        public LifetimeVM TheLifetimeVM
        {
            get { return _theLifetimeVM; }
            set
            {
                _theLifetimeVM = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<LifetimeVM> LifetimeVMCollection
        {
            get { return _lifetimeVMCollection; }
            set
            {
                _lifetimeVMCollection = value;
                OnPropertyChanged();
            }
        }
        public static Dictionary<string, Tuple<string, PropertyInfo>> LifetimePropertyDict
        {
            get
            {
                var _LJVScanPropertyDict = new Dictionary<string, Tuple<string, PropertyInfo>>()
                {
                    {"Time", Tuple.Create("Elapsed Time (hrs)", typeof(CryscoLifetimeDatum).GetProperty("ElapsedHours")) },//
                    {"Relative Intensity", Tuple.Create("Relative Intensity", typeof(CryscoLifetimeDatum).GetProperty("RelativeLuminance")) },//
                    {"Voltage", Tuple.Create("Voltage (V)", typeof(CryscoLifetimeDatum).GetProperty("PixelVoltage")) },
                    {"Luminance", Tuple.Create("Luminance (cd/m\xB2)", typeof(CryscoLifetimeDatum).GetProperty("Luminance")) },//
                    {"Current Efficiency", Tuple.Create("Current Eff. (cd/A)", typeof(CryscoLifetimeDatum).GetProperty("CurrentEfficiency")) },
                    {"Power Efficiency", Tuple.Create("Power Eff. (lm/W)", typeof(CryscoLifetimeDatum).GetProperty("PowerEfficiency")) },
                    {"External Quantum Efficiency", Tuple.Create("EQE (%)", typeof(CryscoLifetimeDatum).GetProperty("EQE")) },
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
                Debug.WriteLine("SelectedLeftAxisProperty changed");
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
                Debug.WriteLine("SelectedLeftAxis changed");
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
                Debug.WriteLine("SelectedBottomAxis changed");
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
                Debug.WriteLine("SelectedRightAxis changed");
                UpdatePlotModel();
            }
        }
        #endregion
        #region Methods
        private void SelectLifetimeData()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                TheLifetimeVM.TheLifetime.FilePath = dialog.FileName;
                Debug.WriteLine("Selected file: " + dialog.FileName);
            }
        }
        public void UpdatePlotModel()
        {
            ThePlotModel = new PlotModel();
            ThePlotModel.LegendTitle = "Legend";
            ThePlotModel.LegendPosition = LegendPosition.TopLeft;
            int colorCounter = 0;
            if (SelectedLeftAxis != null && SelectedBottomAxis != null && SelectedRightAxis != null)//assume we always want to plot two quantities
            {
                ThePlotModel.Axes.Add((Axis)SelectedLeftAxis.DynamicInvoke(SelectedLeftAxisProperty.Item1, AxisPosition.Left));
                ThePlotModel.Axes.Add((Axis)SelectedRightAxis.DynamicInvoke(SelectedRightAxisProperty.Item1, AxisPosition.Right));
                ThePlotModel.Axes.Add((Axis)SelectedBottomAxis.DynamicInvoke(SelectedBottomAxisProperty.Item1, AxisPosition.Bottom));
                if (TheLifetimeVM != null)//we are (probably) plotting without a link to the dataContext
                {
                    LineSeries lifetimeSeries1 = new LineSeries();
                    LineSeries lifetimeSeries2 = new LineSeries();
                    lifetimeSeries1.Color = LineSeriesColors[colorCounter];
                    lifetimeSeries2.Color = LineSeriesColors[colorCounter];
                    lifetimeSeries2.LineStyle = LineStyle.Dash;
                    lifetimeSeries2.YAxisKey = _rightAxisKey;
                    colorCounter++;
                    lifetimeSeries1.Title = TheLifetimeVM.CryscoFileName;
                    PropertyInfo xprop = SelectedBottomAxisProperty.Item2;
                    PropertyInfo y1prop = SelectedLeftAxisProperty.Item2;
                    PropertyInfo y2prop = SelectedRightAxisProperty.Item2;
                    foreach (CryscoLifetimeDatum cld in TheLifetimeVM.LifetimeDataList)
                    {
                        double xval = (double)xprop.GetValue(cld);
                        double y1val = (double)y1prop.GetValue(cld);
                        double y2val = (double)y2prop.GetValue(cld);
                        lifetimeSeries1.Points.Add(new DataPoint(xval, y1val));
                        lifetimeSeries2.Points.Add(new DataPoint(xval, y2val));
                    }
                    ThePlotModel.Series.Add(lifetimeSeries1);
                    ThePlotModel.Series.Add(lifetimeSeries2);
                }
                else if (LifetimeVMCollection.Count > 0)//we are plotting from a collection of lifetime data entities
                {
                    foreach (LifetimeVM life in LifetimeVMCollection)
                    {
                        LineSeries lifetimeSeries1 = new LineSeries();
                        LineSeries lifetimeSeries2 = new LineSeries();
                        lifetimeSeries1.Color = LineSeriesColors[colorCounter];
                        lifetimeSeries2.Color = LineSeriesColors[colorCounter];
                        lifetimeSeries2.LineStyle = LineStyle.Dash;
                        lifetimeSeries2.YAxisKey = _rightAxisKey;
                        colorCounter++;
                        lifetimeSeries1.Title = life.TheLifetime.Pixel.Site;
                        PropertyInfo xprop = SelectedBottomAxisProperty.Item2;
                        PropertyInfo y1prop = SelectedLeftAxisProperty.Item2;
                        PropertyInfo y2prop = SelectedRightAxisProperty.Item2;
                        foreach (CryscoLifetimeDatum cld in life.LifetimeDataList)
                        {
                            double xval = (double)xprop.GetValue(cld);
                            double y1val = (double)y1prop.GetValue(cld);
                            double y2val = (double)y2prop.GetValue(cld);
                            lifetimeSeries1.Points.Add(new DataPoint(xval, y1val));
                            lifetimeSeries2.Points.Add(new DataPoint(xval, y2val));
                        }
                        ThePlotModel.Series.Add(lifetimeSeries1);
                        ThePlotModel.Series.Add(lifetimeSeries2);
                    }
                }
                ThePlotModel.InvalidatePlot(true);//do this out of superstition/ignorance
            }
        }
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
        private LogarithmicAxis CreateNewLogAxis(string title, AxisPosition axpos)
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
        private LinearAxis CreateNullAxis(string title, AxisPosition axpos)
        {
            var newAxis = new LinearAxis
            {
                Title = "Null Axis"
            };
            return newAxis;
        }
        #endregion
        #region Commands
        private RelayCommand _selectAndPlotData;
        public ICommand SelectAndPlotData
        {
            get
            {
                if (_selectAndPlotData == null)
                {
                    _selectAndPlotData = new RelayCommand(param => this.SelectAndPlotDataExecute(param));
                }
                return _selectAndPlotData;
            }
        }
        public void SelectAndPlotDataExecute(object o)
        {
            SelectLifetimeData();
            TheLifetimeVM.PopulatePropertiesFromPath();
            UpdatePlotModel();
        }
        private RelayCommand _trimData;
        public ICommand TrimData
        {
            get
            {
                if (_trimData == null)
                {
                    _trimData = new RelayCommand(param => this.TrimDataExecute(param));
                }
                return _trimData;
            }
        }
        public void TrimDataExecute(object o)
        {
            TheLifetimeVM.TrimDataList();
            UpdatePlotModel();
        }
        #endregion
    }

}
