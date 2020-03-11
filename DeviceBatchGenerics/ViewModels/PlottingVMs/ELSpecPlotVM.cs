using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using DeviceBatchGenerics.ViewModels.EntityVMs;

namespace DeviceBatchGenerics.ViewModels.PlottingVMs
{
    public class ELSpecPlotVM : OxyPlotVMBase
    {
        #region Construction
        public ELSpecPlotVM()
        {
            ThePlotModel = new PlotModel();
            ELSpecVMCollection = new ObservableCollection<ELSpecVM>();
            SelectedViewStyle = ViewStyle.Regular;
        }
        public ELSpecPlotVM(ObservableCollection<ELSpecVM> specs)
        {
            ThePlotModel = new PlotModel();
            ELSpecVMCollection = specs;
            SelectedViewStyle = ViewStyle.Regular;
            UpdatePlotModel();
        }
        #endregion
        #region Members
        private ObservableCollection<ELSpecVM> _ELSpecVMCollection;
        #endregion
        #region Properties

        public ObservableCollection<ELSpecVM> ELSpecVMCollection
        {
            get { return _ELSpecVMCollection; }
            set
            {
                _ELSpecVMCollection = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #region Methods
        public void UpdatePlotModel()
        {
            ThePlotModel = new PlotModel();
            ThePlotModel.LegendTitle = "Legend";
            ThePlotModel.LegendPosition = LegendPosition.TopLeft;
            ThePlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensity (a.u.)", Minimum = 0 });
            int colorCounter = 0;
            double min = 400;
            double max = 800;
            if (ELSpecVMCollection.Count > 0)
            {
                min = ELSpecVMCollection.First().MinLambdaCutoff;
                max = ELSpecVMCollection.First().MaxLambdaCutoff;
            }
            ThePlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Wavelength (nm)", Minimum = min, Maximum = max });
            foreach (ELSpecVM spec in ELSpecVMCollection)
            {
                LineSeries specSeries = new LineSeries();
                specSeries.Color = LineSeriesColors[colorCounter];
                colorCounter++;
                if (SelectedViewStyle == ViewStyle.Regular)
                    specSeries.Title = spec.TheELSpectrum.Pixel.Site;
                if (SelectedViewStyle == ViewStyle.Aging)
                    specSeries.Title = spec.TheELSpectrum.DeviceLJVScanSummary.TestCondition;
                foreach (Support.DataMapping.ELSpecDatum d in spec.ELSpecList)
                {
                    specSeries.Points.Add(new DataPoint(d.Wavelength, d.Intensity));
                }
                ThePlotModel.Series.Add(specSeries);
            }
            ThePlotModel.InvalidatePlot(true);//do this out of superstition/ignorance
        }
        public void SaveELSpeclotBitmap(string fp)
        {
            IsGeneratingPlotsForReporting = true;
            //SelectedRightAxisProperty = null;
            //SelectedLeftAxis = LeftAxisDict["Logarithmic"];
            //SelectedLeftAxisProperty = LJVScanPropertyDict["Current Density"];
            //SelectedBottomAxis = BottomAxisDict["Logarithmic"];
            //SelectedBottomAxisProperty = LJVScanPropertyDict["Voltage"];
            ExportPlotBitmap(fp);
        }
        #endregion
    }
}
