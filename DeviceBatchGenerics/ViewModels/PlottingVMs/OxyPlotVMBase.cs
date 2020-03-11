using OxyPlot;
using System.IO;
using System.Drawing;
using DeviceBatchGenerics.Support.Bases;

namespace DeviceBatchGenerics.ViewModels.PlottingVMs
{
    public class OxyPlotVMBase : NotifyUIBase
    {
        #region Members
        private PlotModel _thePlotModel;
        private bool _isGeneratingPlotsForReporting = false;
        #endregion
        #region Properties
        public PlotModel ThePlotModel
        {
            get { return _thePlotModel; }
            set
            {
                _thePlotModel = value;
                OnPropertyChanged();
            }
        }

        public ViewStyle SelectedViewStyle { get; set; }
        public bool IsGeneratingPlotsForReporting
        {
            get { return _isGeneratingPlotsForReporting; }
            set
            {
                _isGeneratingPlotsForReporting = value;
                OnPropertyChanged();
            }
        }
        public enum ViewStyle
        {
            Regular,
            Aging
        }
        public static OxyColor[] LineSeriesColors = new OxyColor[]
        {
            OxyColors.Blue,
            OxyColors.Red,
            OxyColors.BlueViolet,
            OxyColors.DarkGreen,
            OxyColors.DarkOrange,
            OxyColors.Brown,
            OxyColors.Magenta,
            OxyColors.Olive,
            OxyColors.Navy,
        };
        public void ExportPlotBitmap(string path)
        {
            MemoryStream ms = new MemoryStream();
            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 1024, Height = 768, Background = OxyColors.White };
            pngExporter.Export(ThePlotModel, ms);
            var newImage = new Bitmap(ms);
            newImage.Save(path);
        }
        #endregion
    }
}
