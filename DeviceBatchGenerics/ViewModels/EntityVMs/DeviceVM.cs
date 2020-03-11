using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using DeviceBatchGenerics.Support;
using DeviceBatchGenerics.Support.Bases;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class DeviceVM : CrudVMBase
    {
        #region Construction
        public DeviceVM()
        {
            TheDevice = new Device();
            TheDevice.Label = "MMDD-CMMDDYY-0";
            TheDevice.BatchIndex = 0;
            TheDevice.Pixels.Add(new Pixel() { Site = "SiteA" });
            TheDevice.Pixels.Add(new Pixel() { Site = "SiteB" });
            TheDevice.Pixels.Add(new Pixel() { Site = "SiteC" });
            TheDevice.Pixels.Add(new Pixel() { Site = "SiteD" });
            //TheDevice.QDBatch = new QDBatch() { Name = "CMMDDYY" };
            PopulatePixelsDict();
        }
        public DeviceVM(Device dev)
        {
            TheDevice = dev;
            PopulateTestConditions();
            GenerateMaterialStructureFromLayerNames();
            PopulatePixelsDict();
            GenerateDataVMsFromChildren();
        }
        public DeviceVM(Device dev, DeviceBatchContext context):base(context)
        {
            TheDevice = dev;
            PopulateTestConditions();
            PopulatePixelsDict();
            GenerateDataVMsFromChildren();
        }
        #endregion
        #region Members
        private HashSet<string> _testConditions;
        private ObservableCollection<string> _testConditionsCollection;
        private Device _theDevice;
        private string _materialStructure = "/ ";
        private ObservableCollection<LJVScanSummaryVM> _scanSummaryVMCollection;
        private ObservableCollection<LifetimeVM> _lifetimeVMCollection;
        private Dictionary<string, Pixel> _pixelsDict;
        private Pixel _selectedPixel;

        private LifetimeVM _theLifetimeVM = new LifetimeVM();
        #endregion
        #region Properties
        //public AssignLifetimeDataToPixelWindow ParentWindow { get; set; }
        public ObservableCollection<string> TestConditionsCollection
        {
            get { return _testConditionsCollection; }
            set
            {
                _testConditionsCollection = value;
                OnPropertyChanged();
            }
        }
        public Device TheDevice
        {
            get { return _theDevice; }
            set
            {
                _theDevice = value;
                OnPropertyChanged();
            }
        }
        public string MaterialStructure
        {
            get { return _materialStructure; }
            set
            {
                _materialStructure = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<LJVScanSummaryVM> ScanSummaryVMCollection
        {
            get { return _scanSummaryVMCollection; }
            set
            {
                _scanSummaryVMCollection = value;
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
        public Dictionary<string, Pixel> PixelsDict
        {

            get { return _pixelsDict; }
            set
            {
                _pixelsDict = value;
                OnPropertyChanged();
            }
        }
        public virtual Pixel SelectedPixel
        {
            get { return _selectedPixel; }
            set
            {
                _selectedPixel = value;
                OnPropertyChanged();
            }
        }
        public LifetimeVM TheLifetimeVM
        {
            get { return _theLifetimeVM; }
            set
            {
                _theLifetimeVM = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #region Methods
        private void PopulatePixelsDict()
        {
            PixelsDict = new Dictionary<string, Pixel>();
            foreach (Pixel p in TheDevice.Pixels)
            {
                PixelsDict.Add(p.Site, p);
            }
        }
        private void PopulateTestConditions()
        {
            _testConditions = new HashSet<string>();//find all testConditions linked to this device
            foreach (DeviceLJVScanSummary ss in TheDevice.DeviceLJVScanSummaries)
            {
                _testConditions.Add(ss.TestCondition);
            }
            TestConditionsCollection = new ObservableCollection<string>();
            foreach (string cond in _testConditions)
            {
                TestConditionsCollection.Add(cond);
            }
            TestConditionsCollection = new ObservableCollection<string>(TestConditionsCollection.OrderByDescending(i => i));//sort the collection
        }
        private void GenerateMaterialStructureFromLayerNames()
        {
            var layerList = TheDevice.Layers.ToList().OrderBy(o => o.PositionIndex);
            foreach (Layer l in layerList)
            {
                if (l.Solution != null)
                    MaterialStructure = string.Concat(MaterialStructure, l.Solution.Material.Name, " / ");
                if (l.Solution == null && l.Material != null)
                    MaterialStructure = string.Concat(MaterialStructure, l.Material.Name, " / ");
            }
            //Debug.WriteLine("MaterialStructure is: " + MaterialStructure);
        }
        private void GenerateDataVMsFromChildren()
        {
            var summariesList = TheDevice.DeviceLJVScanSummaries.ToList().OrderBy(o => o.TestCondition);
            ScanSummaryVMCollection = new ObservableCollection<LJVScanSummaryVM>();
            foreach (DeviceLJVScanSummary ss in summariesList)
            {
                ScanSummaryVMCollection.Add(new LJVScanSummaryVM(ss));
            }
            LifetimeVMCollection = new ObservableCollection<LifetimeVM>();
            foreach (Pixel p in TheDevice.Pixels)
            {
                if (p.Lifetime != null)
                    LifetimeVMCollection.Add(new LifetimeVM(p.Lifetime));
            }
        }
        private void SelectLifetimeData()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                TheLifetimeVM = new LifetimeVM();
                ctx.Lifetimes.Add(TheLifetimeVM.TheLifetime);
                TheLifetimeVM.TheLifetime.FilePath = dialog.FileName;
                TheLifetimeVM.TheLifetime.Pixel = TheDevice.Pixels.First();//assign it a pixel even if incorrect to satisfy LifetimePixel FK 
                //ctx.SaveChanges();
                //TheLifetimeVM.TheLifetime.FilePath = dialog.FileName;
                Debug.WriteLine("Selected file: " + dialog.FileName);
            }
        }

        #endregion
        #region Commands
        private RelayCommand _selectLifetimeDataToAdd;
        public ICommand SelectLifetimeDataToAdd
        {
            get
            {
                if (_selectLifetimeDataToAdd == null)
                {
                    _selectLifetimeDataToAdd = new RelayCommand(param => this.SelectLifetimeDataToAddExecute(param));
                }
                return _selectLifetimeDataToAdd;
            }
        }
        public void SelectLifetimeDataToAddExecute(object o)
        {
            SelectLifetimeData();
            TheLifetimeVM.PopulatePropertiesFromPath();
        }
        private RelayCommand _commitNewLifetimeEntityToDeviceAndDB;
        public ICommand CommitNewLifetimeEntityToDeviceAndDB
        {
            get
            {
                if (_commitNewLifetimeEntityToDeviceAndDB == null)
                {
                    _commitNewLifetimeEntityToDeviceAndDB = new RelayCommand(param => this.CommitNewLifetimeEntityToDeviceAndDBExecute(param));
                }
                return _commitNewLifetimeEntityToDeviceAndDB;
            }
        }
        public virtual void CommitNewLifetimeEntityToDeviceAndDBExecute(object o)
        {
            var newDirectory = string.Concat(
                TheDevice.DeviceBatch.FilePath,
                @"\Lifetime\");
            var newPath = string.Concat(
                TheDevice.DeviceBatch.FilePath,
                @"\Lifetime\",
                TheDevice.Label,
                "_",
                SelectedPixel.Site,
                ".csv"
                );
            Directory.CreateDirectory(newDirectory);
            if (!File.Exists(newPath))
                File.Copy(TheLifetimeVM.TheLifetime.FilePath, newPath);
            TheLifetimeVM.TheLifetime.Pixel = ctx.Pixels.Where(x => x.PixelId == SelectedPixel.PixelId).First();
            //SelectedPixel.Lifetime = TheLifetimeVM.TheLifetime;
            ctx.SaveChanges();
            MessageBox.Show("Added Lifetime Data to device " + TheDevice.Label);
        }
        #endregion
    }
}
