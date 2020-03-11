using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using DeviceBatchGenerics.Controls.Plotting;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using DeviceBatchGenerics.Support;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.PlottingVMs
{
    public class DevicePlotVM : DeviceVM
    {
        public DevicePlotVM(Device dev) : base(dev)
        {
            PopulateControlsDict();
            SetInitialState();
        }
        #region Members
        LJVPlotVM _LJVPlotVM1;
        ELSpecPlotVM _theELSpecPlotVM;
        LifetimePlotVM _theLifetimePlotVM;
        private string _selectedTestCondition;
        private Dictionary<string, UserControl> _controlsDict;
        UserControl _activeUserControl;
        LifetimeViewControl _lifetimeViewControl;
        private Pixel _selectedPixel;
        bool _lifetimeHasBeenPlotted = false;
        private ObservableCollection<ImageVM> _imageVMCollection;
        #endregion
        #region Properties
        public LJVPlotVM LJVPlotVM1
        {
            get { return _LJVPlotVM1; }
            set
            {
                _LJVPlotVM1 = value;
                OnPropertyChanged();
            }
        }
        public ELSpecPlotVM TheELSpecPlotVM
        {
            get { return _theELSpecPlotVM; }
            set
            {
                _theELSpecPlotVM = value;
                OnPropertyChanged();
            }
        }
        public LifetimePlotVM TheLifetimePlotVM
        {
            get { return _theLifetimePlotVM; }
            set
            {
                _theLifetimePlotVM = value;
                OnPropertyChanged();
            }
        }
        public string SelectedTestCondition
        {
            get { return _selectedTestCondition; }
            set
            {
                _selectedTestCondition = value;
                OnPropertyChanged();
                ConstructAllPixelsPlotVMs();
            }
        }
        public override Pixel SelectedPixel
        {
            get { return _selectedPixel; }
            set
            {
                _selectedPixel = value;
                OnPropertyChanged();
                if (ActiveUserControl.GetType() == typeof(PixelAgingControl))
                    ConstructPixelAgingPlotVMs();
            }
        }
        public UserControl ActiveUserControl
        {
            get { return _activeUserControl; }
            set
            {
                {
                    _activeUserControl = value;
                    OnPropertyChanged();
                    OnActiveUserControlChanged();
                }
            }
        }
        public LifetimeViewControl TheLifetimeViewControl
        {
            get { return _lifetimeViewControl; }
            set
            {
                _lifetimeViewControl = value;
                OnPropertyChanged();
            }
        }
        public Dictionary<string, UserControl> ControlsDict
        {

            get { return _controlsDict; }
            set
            {
                _controlsDict = value;
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
        #endregion
        #region Methods
        private void SetInitialState()
        {
            ConstructAllPixelsPlotVMs();
            SelectedTestCondition = TestConditionsCollection.FirstOrDefault();
            SelectedPixel = PixelsDict[PixelsDict.First().Key];//not sure if this can be simplified to PixelsDict.First() and don't care to test
            OnPropertyChanged();
            ConstructLifetimePlotVMs();
        }
        private void PopulateControlsDict()
        {
            ControlsDict = new Dictionary<string, UserControl>();
            ControlsDict.Add("Regular", new SingleTestConditionControl());
            ControlsDict.Add("Aging", new PixelAgingControl());
            ActiveUserControl = ControlsDict["Regular"];
        }
        private void ConstructLifetimePlotVMs()
        {
            ObservableCollection<LifetimeVM> lifeVMs = new ObservableCollection<LifetimeVM>();
            foreach (Pixel p in TheDevice.Pixels)
            {
                if (p.Lifetime != null)
                {
                    lifeVMs.Add(new LifetimeVM(p.Lifetime));
                }
            }
            if (TheLifetimePlotVM == null)
            {
                TheLifetimePlotVM = new LifetimePlotVM(lifeVMs);
            }
        }
        private void ConstructAllPixelsPlotVMs()
        {
            ObservableCollection<LJVScanVM> scanVMs = new ObservableCollection<LJVScanVM>();
            ObservableCollection<ELSpecVM> ELSpecVMs = new ObservableCollection<ELSpecVM>();
            ImageVMCollection = new ObservableCollection<ImageVM>();
            foreach (Pixel p in TheDevice.Pixels)
            {
                if (SelectedTestCondition != null)
                {
                    foreach (LJVScan scan in p.LJVScans)
                    {
                        if (scan.DeviceLJVScanSummary.TestCondition == SelectedTestCondition)//parse for LJVScans that correspond to selected value
                        {
                            scanVMs.Add(new LJVScanVM(scan));
                        }
                    }
                    foreach (ELSpectrum spec in p.ELSpectrums)
                    {
                        if (spec.DeviceLJVScanSummary != null && spec.DeviceLJVScanSummary.TestCondition == SelectedTestCondition)
                        {
                            ELSpecVMs.Add(new ELSpecVM(spec));
                        }
                    }
                    foreach (EFDeviceBatchCodeFirst.Image pic in p.Images)
                    {
                        if (pic.DeviceLJVScanSummary != null && pic.DeviceLJVScanSummary.TestCondition == SelectedTestCondition)
                        {
                            var thisVM = new ImageVM(pic);
                            thisVM.Label = p.Site;
                            ImageVMCollection.Add(thisVM);
                        }
                    }
                }
            }

            if (LJVPlotVM1 == null)
            {
                LJVPlotVM1 = new LJVPlotVM(scanVMs);
                TheELSpecPlotVM = new ELSpecPlotVM(ELSpecVMs);
            }
            else//do this instead of new PlotVMs to retain selected axes
            {
                LJVPlotVM1.SelectedViewStyle = LJVPlotVM.ViewStyle.Regular;
                TheELSpecPlotVM.SelectedViewStyle = ELSpecPlotVM.ViewStyle.Regular;
                LJVPlotVM1.LJVScanVMCollection = scanVMs;
                LJVPlotVM1.UpdatePlotModel();
                TheELSpecPlotVM.ELSpecVMCollection = ELSpecVMs;
                TheELSpecPlotVM.UpdatePlotModel();
            }
            Debug.WriteLine("constructed AllPixelsPlotVMs for: " + TheDevice.Label);
        }
        private void ConstructPixelAgingPlotVMs()
        {
            var scanVMsList = new List<LJVScanVM>();
            var ELSpecVMsList = new List<ELSpecVM>();
            var imageVMList = new List<ImageVM>();
            foreach (Pixel p in TheDevice.Pixels)
            {
                if (p.Site == SelectedPixel.Site)
                {
                    foreach (LJVScan scan in p.LJVScans)
                    {
                        scanVMsList.Add(new LJVScanVM(scan));
                    }
                    foreach (ELSpectrum spec in p.ELSpectrums)
                    {
                        ELSpecVMsList.Add(new ELSpecVM(spec));
                    }
                    foreach (EFDeviceBatchCodeFirst.Image pic in p.Images)
                    {
                        var thisVM = new ImageVM(pic);
                        thisVM.Label = pic.DeviceLJVScanSummary.TestCondition;
                        imageVMList.Add(thisVM);
                    }
                }
            }
            ObservableCollection<LJVScanVM> scanVMs = new ObservableCollection<LJVScanVM>();
            if (scanVMsList.Count > 0)
                scanVMs = new ObservableCollection<LJVScanVM>(scanVMsList.OrderBy(o => o.TheLJVScan.DeviceLJVScanSummary.TestCondition));
            ObservableCollection<ELSpecVM> ELSpecVMs = new ObservableCollection<ELSpecVM>();
            if(ELSpecVMsList.Count>0)
                ELSpecVMs = new ObservableCollection<ELSpecVM>(ELSpecVMsList.OrderBy(o => o.TheELSpectrum.DeviceLJVScanSummary.TestCondition));
            if(imageVMList.Count>0)
                ImageVMCollection = new ObservableCollection<ImageVM>(imageVMList.OrderBy(o => o.TheImage.DeviceLJVScanSummary.TestCondition));

            if (LJVPlotVM1 == null)
            {
                LJVPlotVM1 = new LJVPlotVM(scanVMs);
                LJVPlotVM1.SelectedViewStyle = LJVPlotVM.ViewStyle.Aging;
                LJVPlotVM1.SelectedViewStyle = ELSpecPlotVM.ViewStyle.Aging;
                TheELSpecPlotVM = new ELSpecPlotVM(ELSpecVMs);
            }
            else
            {
                LJVPlotVM1.SelectedViewStyle = LJVPlotVM.ViewStyle.Aging;
                TheELSpecPlotVM.SelectedViewStyle = ELSpecPlotVM.ViewStyle.Aging;
                LJVPlotVM1.LJVScanVMCollection = scanVMs;
                LJVPlotVM1.UpdatePlotModel();
                TheELSpecPlotVM.ELSpecVMCollection = ELSpecVMs;
                TheELSpecPlotVM.UpdatePlotModel();
            }
            Debug.WriteLine("constructed PixelAgingPlotVMs for: " + TheDevice.Label);
        }
        private void OnActiveUserControlChanged()
        {
            if (ActiveUserControl.GetType() == typeof(SingleTestConditionControl))
                ConstructAllPixelsPlotVMs();
            if (ActiveUserControl.GetType() == typeof(PixelAgingControl))
                ConstructPixelAgingPlotVMs();
            else
                Debug.WriteLine("ActiveUserControl is set to something weird");
        }



        #endregion
        #region Commands
        private RelayCommand _GoToNextDeviceInBatch;
        public ICommand GoToNextDeviceInBatch
        {
            get
            {
                if (_GoToNextDeviceInBatch == null)
                {
                    _GoToNextDeviceInBatch = new RelayCommand(param => this.GoToNextDeviceInBatchExecute(param));
                }
                return _GoToNextDeviceInBatch;
            }
        }
        private void GoToNextDeviceInBatchExecute(object o)
        {
            Debug.WriteLine("going to next device in batch");
            int maxBatchIndex = TheDevice.DeviceBatch.Devices.Max(x => x.BatchIndex);
            Device nextDevice = null;
            if (TheDevice.BatchIndex == maxBatchIndex)//if this is the final device in the batch, go back to the first
                nextDevice = TheDevice.DeviceBatch.Devices.Where(x => x.BatchIndex == TheDevice.DeviceBatch.Devices.Min(y => y.BatchIndex)).First();
            else
            {
                var deviceList = TheDevice.DeviceBatch.Devices.ToList();
                int TheDeviceIndex = deviceList.IndexOf(TheDevice);
                nextDevice = deviceList[TheDeviceIndex + 1];
            }
            if (nextDevice != null)
            {
                TheDevice = nextDevice;
                OnActiveUserControlChanged();
            }
        }
        private RelayCommand _GoToPreviousDeviceInBatch;
        public ICommand GoToPreviousDeviceInBatch
        {
            get
            {
                if (_GoToPreviousDeviceInBatch == null)
                {
                    _GoToPreviousDeviceInBatch = new RelayCommand(param => this.GoToPreviousDeviceInBatchExecute(param));
                }
                return _GoToPreviousDeviceInBatch;
            }
        }
        private void GoToPreviousDeviceInBatchExecute(object o)
        {
            int minBatchIndex = TheDevice.DeviceBatch.Devices.Min(x => x.BatchIndex);
            Device nextDevice = null;
            if (TheDevice.BatchIndex == minBatchIndex)//if this is the first device in the batch, go to the final
                nextDevice = TheDevice.DeviceBatch.Devices.Where(x => x.BatchIndex == TheDevice.DeviceBatch.Devices.Max(y => y.BatchIndex)).First();
            else
            {
                var deviceList = TheDevice.DeviceBatch.Devices.ToList();
                int TheDeviceIndex = deviceList.IndexOf(TheDevice);
                nextDevice = deviceList[TheDeviceIndex - 1];
            }
            if (nextDevice != null)
            {
                TheDevice = nextDevice;
                OnActiveUserControlChanged();
            }
        }
        #endregion
    }

}
