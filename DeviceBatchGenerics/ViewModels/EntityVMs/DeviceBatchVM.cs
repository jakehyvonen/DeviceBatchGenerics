using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Data.Entity.Validation;
using OfficeOpenXml;
using DeviceBatchGenerics.Support.Bases;
using DeviceBatchGenerics.Support;
using EFDeviceBatchCodeFirst;
using System.Threading.Tasks;
using DeviceBatchGenerics.Support.ExtendedTreeView;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class DeviceBatchVM : CrudVMBase
    {
        #region Construction
        public DeviceBatchVM(DeviceBatch devbatch, bool shouldUpdateData = true)
        {
            //Debug.WriteLine("constructing devbatchVM");
            TheDeviceBatch = ctx.DeviceBatches
                .Where(db => db.DeviceBatchId == devbatch.DeviceBatchId)
                .First();
            TheDeviceBatch.Devices.OrderBy(x => x.BatchIndex);
            IsReadOnly = true;
            IsDetailedView = false;
            GetTestConditionsFromChildren();
            if (shouldUpdateData)
            {
                PopulateDeviceVMCollection();
                TryToUpdateDataAndSpreadsheetsFromDevBatchPath();
            }
        }
        #endregion
        #region Members
        DeviceBatch _theDeviceBatch;
        DeviceVM _selectedDeviceVM;
        private List<string> _testConditions = new List<string>();
        bool _isReadOnly;
        bool _isDetailedView;
        private List<string> _selectedRawDATFiles;
        private List<string> _selectedProcDATFiles;
        private List<string> _selectedELSpecFiles;
        private List<string> _selectedImageFiles;
        private ObservableCollection<DeviceVM> _deviceVMCollection;
        System.Windows.Controls.UserControl _activeUserControl;
        #endregion
        #region Properties
        public DeviceBatch TheDeviceBatch
        {
            get { return _theDeviceBatch; }
            set
            {
                {
                    _theDeviceBatch = value;
                    OnPropertyChanged();
                }
            }
        }
        public DeviceVM SelectedDeviceVM
        {
            get { return _selectedDeviceVM; }
            set
            {
                _selectedDeviceVM = value;
                OnPropertyChanged();
                Debug.WriteLine("SelectedDeviceVM changed");
            }
        }
        public List<string> TestConditions
        {
            get
            {
                return _testConditions;
            }
            set
            {
                _testConditions = value;
                OnPropertyChanged();
            }
        }
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                {
                    _isReadOnly = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsDetailedView
        {
            get { return _isDetailedView; }
            set
            {
                {
                    _isDetailedView = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<DeviceVM> DeviceVMCollection
        {
            get { return _deviceVMCollection; }
            set
            {
                _deviceVMCollection = value;
                OnPropertyChanged();
            }
        }
        public System.Windows.Controls.UserControl ActiveUserControl
        {
            get { return _activeUserControl; }
            set
            {
                _activeUserControl = value;
                OnPropertyChanged();
            }
        }
        #endregion
        #region Methods       
        public void OrganizeDataByDevicePixel(bool overWriteData = true)
        {
            string devicesPath = string.Concat(TheDeviceBatch.FilePath, @"\SortedByDevice\");
            Directory.CreateDirectory(devicesPath);
            var ip = new ItemProvider();
            List<string> allPaths = Directory.GetFiles(TheDeviceBatch.FilePath, ".", SearchOption.AllDirectories).ToList();
            foreach (Device d in TheDeviceBatch.Devices)
            {
                string devicePath = string.Concat(devicesPath, d.Label, @"\");
                if (!Directory.Exists(devicePath))
                    Directory.CreateDirectory(devicePath);
                string statsDATPath = string.Concat(devicePath, @"\Statistical Data\");
                string summariesPath = string.Concat(devicePath, @"\Scan Summaries\");
                string oxyPlotsPath = string.Concat(devicePath, @"\OxyPlots\");
                Directory.CreateDirectory(statsDATPath);
                Directory.CreateDirectory(summariesPath);
                Directory.CreateDirectory(oxyPlotsPath);
                //generate statsDAT files if necessary
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    Debug.WriteLine("ss.StatsDataPath: " + ss.StatsDataPath);
                    if (!File.Exists(ss.StatsDataPath) || ss.StatsDataPath == null || ss.StatsDataPath == "null")
                    {
                        GenerateStatsData();
                    }
                }
                //copy files
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    string newPath = string.Concat(statsDATPath, ss.StatsDataPath.Substring(ss.StatsDataPath.LastIndexOf(@"\")));
                    File.Copy(ss.StatsDataPath, newPath, overWriteData);
                    newPath = string.Concat(summariesPath, ss.SpreadsheetReportPath.Substring(ss.SpreadsheetReportPath.LastIndexOf(@"\")));
                    File.Copy(ss.SpreadsheetReportPath, newPath, overWriteData);
                }
                foreach (Pixel p in d.Pixels)
                {
                    string pixelPath = string.Concat(devicePath, p.Site);
                    string pixelSpectraPath = string.Concat(pixelPath, @"\EL Spectra\");
                    string pixelFullDATPath = string.Concat(pixelPath, @"\Full Data\");
                    string pixelImagesPath = string.Concat(pixelPath, @"\Images\");
                    string pixelProcDATPath = string.Concat(pixelPath, @"\Processed Data\");
                    string pixelRawDATPath = string.Concat(pixelPath, @"\Raw Data\");
                    Directory.CreateDirectory(pixelPath);
                    Directory.CreateDirectory(pixelSpectraPath);
                    Directory.CreateDirectory(pixelFullDATPath);
                    Directory.CreateDirectory(pixelImagesPath);
                    Directory.CreateDirectory(pixelProcDATPath);
                    Directory.CreateDirectory(pixelRawDATPath);
                    foreach (LJVScan scan in p.LJVScans)
                    {
                        string newPath = string.Concat(pixelFullDATPath, scan.FullDATFilePath.Substring(scan.FullDATFilePath.LastIndexOf(@"\")));
                        File.Copy(scan.FullDATFilePath, newPath, overWriteData);
                        newPath = string.Concat(pixelProcDATPath, scan.ProcDATFilePath.Substring(scan.ProcDATFilePath.LastIndexOf(@"\")));
                        File.Copy(scan.ProcDATFilePath, newPath, overWriteData);
                        newPath = string.Concat(pixelRawDATPath, scan.RawDATFilePath.Substring(scan.RawDATFilePath.LastIndexOf(@"\")));
                        File.Copy(scan.RawDATFilePath, newPath, overWriteData);
                    }
                    foreach (ELSpectrum els in p.ELSpectrums)
                    {
                        string newPath = string.Concat(pixelSpectraPath, els.FilePath.Substring(els.FilePath.LastIndexOf(@"\")));
                        File.Copy(els.FilePath, newPath, overWriteData);
                    }
                    foreach (Image i in p.Images)
                    {
                        string newPath = string.Concat(pixelImagesPath, i.FilePath.Substring(i.FilePath.LastIndexOf(@"\")));
                        File.Copy(i.FilePath, newPath, overWriteData);
                    }
                    Debug.WriteLine("done copying " + d.Label + p.Site);
                }
                //copy OxyPlots by recursive search
                var eqejPaths = allPaths.Where(x => x.Contains(string.Concat(@"\OxyPlots\EQE-J\", d.Label))).ToList();
                var eqelPaths = allPaths.Where(x => x.Contains(string.Concat(@"\OxyPlots\EQE-L\", d.Label))).ToList();
                var jvPaths = allPaths.Where(x => x.Contains(string.Concat(@"\OxyPlots\J-V\", d.Label))).ToList();
                var ljvPaths = allPaths.Where(x => x.Contains(string.Concat(@"\OxyPlots\L-J-V\", d.Label))).ToList();
                foreach (string s in eqejPaths)
                {
                    foreach (string tc in TestConditions)
                        if (s.Contains(tc))
                        {
                            string newDir = string.Concat(oxyPlotsPath, @"\EQE-J\");
                            Directory.CreateDirectory(newDir);
                            File.Copy(s, string.Concat(newDir, "EQEJ_", tc, ".jpg"), overWriteData);
                        }
                }
                foreach (string s in eqelPaths)
                {
                    foreach (string tc in TestConditions)
                        if (s.Contains(tc))
                        {
                            string newDir = string.Concat(oxyPlotsPath, @"\EQE-L\");
                            Directory.CreateDirectory(newDir);
                            File.Copy(s, string.Concat(newDir, "EQEL_", tc, ".jpg"), overWriteData);
                        }
                }
                foreach (string s in jvPaths)
                {
                    foreach (string tc in TestConditions)
                        if (s.Contains(tc))
                        {
                            string newDir = string.Concat(oxyPlotsPath, @"\J-V\");
                            Directory.CreateDirectory(newDir);
                            File.Copy(s, string.Concat(newDir, "JV_", tc, ".jpg"), overWriteData);
                        }
                }
                foreach (string s in ljvPaths)
                {
                    foreach (string tc in TestConditions)
                        if (s.Contains(tc))
                        {
                            string newDir = string.Concat(oxyPlotsPath, @"\L-J-V\");
                            Directory.CreateDirectory(newDir);
                            File.Copy(s, string.Concat(newDir, "LJV_", tc, ".jpg"), overWriteData);
                        }
                }
                //throw new Exception();
            }

        }
        private void GetTestConditionsFromChildren()
        {
            HashSet<string> testConditionsHash = new HashSet<string>();
            foreach (Device d in TheDeviceBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    testConditionsHash.Add(ss.TestCondition);
                }
            }
            TestConditions = testConditionsHash.ToList();
            TestConditions.Sort();
        }
        private void PopulateDeviceVMCollection()
        {
            DeviceVMCollection = new ObservableCollection<DeviceVM>();
            foreach (Device d in TheDeviceBatch.Devices)
            {
                DeviceVMCollection.Add(new DeviceVM(d));
            }
        }
        /// <summary>
        /// Returns a list of Tuples where Item1 = BatchIndex, Item2 = TestCondition, Item3 = FilePath
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<Tuple<int, string, string>> IndexAListOfPaths(List<string> list)
        {
            var indexedList = new List<Tuple<int, string, string>>();

            foreach (string s in list)
            {
                var fileNameDeviceIndex = s.Substring(s.LastIndexOf("-") + 1, (s.IndexOf("_Site") - s.LastIndexOf("-")) - 1);
                int batchIndex = 0;
                Int32.TryParse(fileNameDeviceIndex, out batchIndex);
                Debug.WriteLine("Found a file that corresponds to device #: " + fileNameDeviceIndex);
                var fileNameTestCondition = s.Substring(s.IndexOf("_") + 1, (s.LastIndexOf("-") - s.IndexOf("_") - 1));
                Debug.WriteLine("with TestCondition: " + fileNameTestCondition);
                if (!s.Contains("Backup"))//ignore files in backup directories
                    indexedList.Add(new Tuple<int, string, string>(batchIndex, fileNameTestCondition, s));
            }
            return indexedList;

        }
        /// <summary>
        /// returns a list of tuples where tuples corresponding to data already attached to the DBContext are removed
        /// </summary>
        /// <param name="existingDataList"></param>
        /// <param name="totalDataList"></param>
        /// <returns></returns>
        private List<Tuple<int, string, string>> ParseForNewData(List<Tuple<int, string>> existingDataList, List<Tuple<int, string, string>> totalDataList)
        {
            var newDataList = new List<Tuple<int, string, string>>();
            try
            {
                foreach (Tuple<int, string, string> td in totalDataList)
                {
                    bool DataIsNew = true;

                    foreach (Tuple<int, string> ed in existingDataList)
                    {
                        if (td.Item1 == ed.Item1 && td.Item2 == ed.Item2)
                        {
                            Debug.WriteLine("Data for Device # " + td.Item1 + " with TestCondition " + td.Item2 + " is already included in this DeviceBatch model");
                            DataIsNew = false;
                        }
                    }
                    if (DataIsNew)
                        newDataList.Add(td);
                }
                return newDataList;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return newDataList;
            }
        }
        public bool GenerateNewDataEntitiesFromPath(string fp)
        {
            bool generatedNewEntities = false;
            _selectedRawDATFiles = Directory.GetFiles(fp, "*.rawDAT", SearchOption.AllDirectories).ToList<string>();
            _selectedProcDATFiles = Directory.GetFiles(fp, "*.procDAT", SearchOption.AllDirectories).ToList<string>();
            _selectedELSpecFiles = Directory.GetFiles(fp, "*.ELSpectrum", SearchOption.AllDirectories).ToList<string>();
            _selectedImageFiles = Directory.GetFiles(fp, "*.jpg", SearchOption.AllDirectories).ToList<string>();
            _selectedImageFiles = _selectedImageFiles.Where(x => !x.Contains("Cropped")).ToList();
            _selectedImageFiles = _selectedImageFiles.Where(x => !x.Contains("Plots")).ToList();
            //generate a hashset for all Devices + TestConditions already added to the DeviceBatch
            var existingScanList = new List<Tuple<int, string>>();
            foreach (Device d in TheDeviceBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    existingScanList.Add(new Tuple<int, string>(d.BatchIndex, ss.TestCondition));
                }
            }
            //get device BatchIndex and TestCondition from filepath
            var fileNameRawDATList = IndexAListOfPaths(_selectedRawDATFiles);
            var fileNameProcDATList = IndexAListOfPaths(_selectedProcDATFiles);
            var fileNameELSpecList = IndexAListOfPaths(_selectedELSpecFiles);
            var fileNameImageList = IndexAListOfPaths(_selectedImageFiles);
            //select only filepaths that have not already been added to the DeviceBatch
            var newRawDatList = ParseForNewData(existingScanList, fileNameRawDATList);
            var newProcDatList = ParseForNewData(existingScanList, fileNameProcDATList);
            var newELSpecList = ParseForNewData(existingScanList, fileNameELSpecList);
            var newImageList = ParseForNewData(existingScanList, fileNameImageList);
            //filter out duplicates using a hashset and remap the filepath (tuple item 3) for spreadsheet generation           
            var testConditionsToAdd = new HashSet<Tuple<int, string, string>>();
            foreach (Tuple<int, string, string> t in newProcDatList)
            {
                //Debug.WriteLine("newProcDatList entry for device: " + t.Item1 + " testCondition: " + t.Item2);
                var index = t.Item3.IndexOf("Processed Data");
                var testConditionPath = string.Concat(t.Item3.Remove(index), @"Scan Summaries\");
                Directory.CreateDirectory(testConditionPath);
                //Debug.WriteLine("testConditionPath is: " + testConditionPath);
                testConditionsToAdd.Add(new Tuple<int, string, string>(t.Item1, t.Item2, testConditionPath));
            }
            foreach (Tuple<int, string, string> t in testConditionsToAdd)
            {
                Debug.WriteLine("testConditionsToAdd entry for device: " + t.Item1 + " testCondition: " + t.Item2);
            }
            var testConditionsToAddList = testConditionsToAdd.ToList();
            //create new data entities for devices with data in 'new' lists
            //(this assumes that we will never encounter the case where we are adding ELSpec or Image but not a procDAT)
            foreach (Tuple<int, string, string> testcondition in testConditionsToAddList)
            {
                Debug.WriteLine("Device batchIndex: " + testcondition.Item1 + "testcondition: " + testcondition.Item2);
                foreach (Device d in TheDeviceBatch.Devices)
                {
                    //first check to make sure that there is actually data available to add to the new ScanSummary
                    bool dataIsAvailable = false;
                    foreach (Tuple<int, string, string> t in newProcDatList)
                    {
                        if (t.Item1 == testcondition.Item1 && t.Item1 == d.BatchIndex && t.Item2 == testcondition.Item2)
                        {
                            dataIsAvailable = true;
                            Debug.WriteLine("Data available for device with batchIndex: " + t.Item1 + " testcondition: " + t.Item2);
                        }
                    }
                    if (dataIsAvailable)
                    {
                        var ScanSummary = new LJVScanSummaryVM();
                        ctx.DeviceLJVScanSummaries.Add(ScanSummary.TheLJVScanSummary);
                        //ctx.Entry(ScanSummary.TheLJVScanSummary);
                        d.DeviceLJVScanSummaries.Add(ScanSummary.TheLJVScanSummary);
                        ScanSummary.TheLJVScanSummary.Device = d;
                        ScanSummary.TheLJVScanSummary.SpreadsheetReportPath = string.Concat(testcondition.Item3, d.Label, "_", testcondition.Item2, ".xlsx");
                        ScanSummary.TheLJVScanSummary.TestCondition = testcondition.Item2;
                        foreach (Pixel p in d.Pixels)
                        {
                            LJVScan newScan = null;
                            ELSpectrum newELSpec = null;
                            EFDeviceBatchCodeFirst.Image newImage = null;
                            foreach (Tuple<int, string, string> t in newProcDatList)
                            {
                                if (t.Item1 == d.BatchIndex && t.Item2 == testcondition.Item2 && t.Item3.Substring(t.Item3.LastIndexOf("_") + 1, 5) == p.Site)
                                {
                                    Debug.WriteLine("Adding new LJVScan to device #" + d.BatchIndex);
                                    var scanVM = new LJVScanVM();
                                    scanVM.PopulatePropertiesFromPath(t.Item3);
                                    scanVM.TheLJVScan.Pixel = p;
                                    newScan = scanVM.TheLJVScan;
                                    newScan.ProcDATFilePath = t.Item3;
                                    newScan.DeviceLJVScanSummary = ScanSummary.TheLJVScanSummary;
                                    string rawDatPath = t.Item3.Replace("Processed Data", "Raw Data");
                                    rawDatPath = rawDatPath.Replace(".procDAT", ".rawDAT");
                                    if (File.Exists(rawDatPath))
                                    {
                                        newScan.RawDATFilePath = rawDatPath;
                                        scanVM = new LJVScanVM(newScan);
                                    }
                                    ctx.LJVScans.Add(newScan);
                                }
                            }
                            foreach (Tuple<int, string, string> t in newELSpecList)
                            {
                                if (t.Item1 == d.BatchIndex && t.Item2 == testcondition.Item2 && t.Item3.Substring(t.Item3.LastIndexOf("_") + 1, 5) == p.Site)
                                {
                                    var ELSpecVM = new ELSpecVM();
                                    ELSpecVM.PopulatePropertiesFromPath(t.Item3);
                                    ELSpecVM.TheELSpectrum.QDBatch = d.QDBatch;
                                    ELSpecVM.TheELSpectrum.Pixel = p;
                                    newELSpec = ELSpecVM.TheELSpectrum;
                                    ctx.ELSpectra.Add(newELSpec);
                                }
                            }
                            foreach (Tuple<int, string, string> t in newImageList)
                            {
                                if (t.Item1 == d.BatchIndex && t.Item2 == testcondition.Item2 && t.Item3.Substring(t.Item3.LastIndexOf("_") + 1, 5) == p.Site && !t.Item3.Contains("Cropped"))
                                {
                                    var ImageVM = new ImageVM();
                                    ImageVM.TheImage.FilePath = t.Item3;
                                    ImageVM.TheImage.Pixel = p;
                                    newImage = ImageVM.TheImage;
                                    ctx.Images.Add(newImage);
                                }
                            }
                            if (newScan != null && newELSpec != null && newImage != null)//only generate entities if data exists
                            {
                                newELSpec.DeviceLJVScanSummary = ScanSummary.TheLJVScanSummary;
                                newScan.Image = newImage;
                                p.LJVScans.Add(newScan);
                                p.ELSpectrums.Add(newELSpec);
                                ScanSummary.TheLJVScanSummary.LJVScans.Add(newScan);
                                ScanSummary.TheLJVScanSummary.ELSpectrums.Add(newELSpec);
                                ScanSummary.TheLJVScanSummary.Images.Add(newImage);
                            }
                        }
                        ScanSummary.PopulateEntityPropertiesFromChildren();
                        SpreadsheetGenerator.GenerateLJVScanSummary(ScanSummary.TheLJVScanSummary);
                        generatedNewEntities = true;
                    }
                }
            }
            try
            {
                ctx.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                Debug.WriteLine(e.ToString());
                foreach (var eve in e.EntityValidationErrors)
                {
                    Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors: ", eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage);
                    }
                }
            }
            return generatedNewEntities;
        }
        private void UpdateDeviceLayersFromSpreadsheet()
        {
            if (TheDeviceBatch.SpreadSheetPath != null)
            {
                using (var spreadsheet = new ExcelPackage(new FileInfo(TheDeviceBatch.SpreadSheetPath)))
                {
                    var worksheet = spreadsheet.Workbook.Worksheets.First();//assume that everything is on the first worksheet;
                    int commentColumnIndex = 1;
                    int EMLColumnIndex = -1;
                    string cellText = "";
                    List<Tuple<int, string>> textRolesList = new List<Tuple<int, string>>();//usually each layer has a PhysicalRole but for now we will only track whatever text the user includes in the column header
                    while (!cellText.Contains("COMMENT"))
                    {
                        cellText = worksheet.Cells[1, commentColumnIndex].Text.ToUpper();
                        Debug.WriteLine("Column header is: " + cellText + " at index: " + commentColumnIndex);
                        if (!cellText.Contains("#") && !cellText.Contains("LABEL") && !cellText.Contains("EQE"))//ignore these columns
                            textRolesList.Add(new Tuple<int, string>(commentColumnIndex, cellText));
                        if (cellText == "EML" | cellText == "EMISSIVE LAYER" | cellText.Contains("QD"))
                        {
                            EMLColumnIndex = commentColumnIndex;
                            Debug.WriteLine("It looks like the EML column index is: " + EMLColumnIndex);
                        }
                        commentColumnIndex++;
                        if (commentColumnIndex > 77)
                        {
                            Debug.WriteLine("Looks like there is no Comment column");
                            //System.Windows.Forms.MessageBox.Show("Looks like there is no Comment column");
                            break;
                        }
                    }
                    int presentBatchIndexForDevice = 0;
                    int rowCounter = 2; //to account for the case where devices aren't numbered sequentially or starting at 1, count with a different int
                    List<Tuple<int, Device>> indexedDeviceList = new List<Tuple<int, Device>>();
                    while (presentBatchIndexForDevice >= 0)
                    {
                        cellText = worksheet.Cells[rowCounter, 1].Text;
                        try
                        {
                            presentBatchIndexForDevice = Convert.ToInt32(cellText);
                            try
                            {
                                var theDevice = TheDeviceBatch.Devices.Where(x => x.BatchIndex == presentBatchIndexForDevice).First();
                                indexedDeviceList.Add(new Tuple<int, Device>(rowCounter, theDevice));
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show("Mismatch between spreadsheet device # and database. Please re-create the DeviceBatch");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                            presentBatchIndexForDevice = -1;
                        }
                        rowCounter++;
                    }
                    foreach (Tuple<int, Device> indexedDev in indexedDeviceList)
                    {
                        //indexedDev.Item2.Label = "";
                        indexedDev.Item2.Layers.Clear();
                        indexedDev.Item2.NumberOfLayers = 0;
                        string possibleQDBatch = null;
                        foreach (Tuple<int, string> columnRole in textRolesList)
                        {
                            cellText = worksheet.Cells[indexedDev.Item1, columnRole.Item1].Text;
                            int layerPositionIndex = 0;
                            if (cellText != "")
                            {
                                layerPositionIndex++;
                                var newLayer = new Layer();
                                newLayer.PositionIndex = layerPositionIndex;
                                newLayer.SpreadSheetCellText = cellText;
                                TryToAssignPhysicalRoleFromString(newLayer, columnRole.Item2);
                                if (columnRole.Item1 == EMLColumnIndex)
                                {
                                    int firstSpaceIndex = cellText.IndexOf(" ");
                                    Debug.WriteLine("firstSpaceIndex is: " + firstSpaceIndex);
                                    int firstCommaIndex = cellText.IndexOf(",");
                                    Debug.WriteLine("firstCommaIndex is: " + firstCommaIndex);
                                    if (firstSpaceIndex < firstCommaIndex && firstSpaceIndex > 0)
                                        possibleQDBatch = cellText.Substring(0, firstSpaceIndex);
                                    if (firstCommaIndex < firstSpaceIndex && firstCommaIndex > 0)
                                        possibleQDBatch = cellText.Substring(0, firstCommaIndex);
                                    if (firstCommaIndex == -1 && firstSpaceIndex > 0)
                                        possibleQDBatch = cellText.Substring(0, firstSpaceIndex);
                                    if (firstSpaceIndex == -1 && firstCommaIndex > 0)
                                        possibleQDBatch = cellText.Substring(0, firstCommaIndex);
                                    Debug.WriteLine("Looks like there's a QDBatch for device in row: " + indexedDev.Item1 + " named: " + possibleQDBatch);
                                    newLayer.PhysicalRole = ctx.PhysicalRoles.Where(x => x.ShortName == "EML").First();
                                    if (possibleQDBatch != null)
                                        TryToAssignQDBatchFromString(indexedDev.Item2, possibleQDBatch);
                                }
                                indexedDev.Item2.Layers.Add(newLayer);
                                indexedDev.Item2.NumberOfLayers++;
                            }
                        }
                    }
                }
                ctx.SaveChanges();
            }
        }
        private void TryToAssignPhysicalRoleFromString(Layer l, string s)
        {
            PhysicalRole roleToAssign = ctx.PhysicalRoles.Where(x => x.ShortName == "UN").First();//unassigned role
            PhysicalRole roleFoundInDB = null;
            try
            {
                roleFoundInDB = ctx.PhysicalRoles.Where(x => x.ShortName == s | x.LongName == s).First();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            if (roleFoundInDB != null)
            {
                roleToAssign = roleFoundInDB;
                Debug.WriteLine("Assigned role: " + roleToAssign.ShortName);
            }
            l.PhysicalRole = roleToAssign;
        }
        private void TryToAssignQDBatchFromString(Device dev, string possibleQDBatch)
        {
            var qdBatchFromDB = ctx.Materials.Where(x => x.Name == possibleQDBatch).FirstOrDefault();
            if (qdBatchFromDB != null)
            {
                dev.QDBatch = (QDBatch)qdBatchFromDB;
            }
            else//add a new material 
            {
                QDBatch newQDBatch = new QDBatch();
                newQDBatch.Name = possibleQDBatch;
                newQDBatch.PhysicalRole = ctx.PhysicalRoles.Where(x => x.ShortName == "EML").FirstOrDefault();
                newQDBatch.DepositionMethod = ctx.DepositionMethods.Where(x => x.Name == "Spincoating").FirstOrDefault();
                ctx.Materials.Add(newQDBatch);
                try
                {
                    ctx.SaveChanges();
                }
                catch (DbEntityValidationException e)
                {
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following error: ", eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Debug.WriteLine("- property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage);
                        }
                    }
                    Debug.WriteLine(e.ToString());
                }
                dev.QDBatch = newQDBatch;
            }

        }
        private Task GenerateStatsData()
        {
            return Task.Run(() =>
            {
                foreach (Device d in TheDeviceBatch.Devices)
                {
                    foreach (DeviceLJVScanSummary summary in d.DeviceLJVScanSummaries)
                    {
                        LJVScanSummaryVM summaryVM = new LJVScanSummaryVM(summary);
                        if (summaryVM.TheLJVScanSummary.StatsDataPath == null)
                            summaryVM.GenerateStatsData();
                    }
                }
            });
        }
        public Task TryToUpdateDataAndSpreadsheetsFromDevBatchPath()
        {
            return Task.Run(() =>
            {
                var result = GenerateNewDataEntitiesFromPath(TheDeviceBatch.FilePath);
                UpdateSpreadsheetsExecute(new object());
                GenerateStatsData();
            });
        }
        #endregion
        #region Commands


        private RelayCommand _deleteAllBatchData;
        public ICommand DeleteAllBatchData
        {
            get
            {
                if (_deleteAllBatchData == null)
                {
                    _deleteAllBatchData = new RelayCommand(param => this.DeleteAllBatchDataExecute(param));
                }
                return _deleteAllBatchData;
            }
        }
        public void DeleteAllBatchDataExecute(object o)
        {
            try
            {
                foreach (Device d in TheDeviceBatch.Devices)
                {
                    var scansToRemove = d.DeviceLJVScanSummaries.ToList();
                    for (int i = scansToRemove.Count - 1; i >= 0; i--)
                    {
                        d.DeviceLJVScanSummaries.Remove(scansToRemove[i]);
                    }
                    ctx.DeviceLJVScanSummaries.RemoveRange(scansToRemove);
                }
                ctx.SaveChanges();
                MessageBox.Show("Data successfully removed");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                MessageBox.Show(e.ToString());
            }
        }
        private RelayCommand _addDataToDeviceBatch;
        public ICommand AddDataToDeviceBatch
        {
            get
            {
                if (_addDataToDeviceBatch == null)
                {
                    _addDataToDeviceBatch = new RelayCommand(param => this.AddDataToDeviceBatchExecute(param));
                }
                return _addDataToDeviceBatch;
            }
        }
        public void AddDataToDeviceBatchExecute(object o)
        {
            FolderBrowserDialog folderBrowswer = new FolderBrowserDialog();
            if (folderBrowswer.ShowDialog() == DialogResult.OK)
            {
                GenerateNewDataEntitiesFromPath(folderBrowswer.SelectedPath);
                UpdateSpreadsheetsExecute(new object());
            }
            MessageBox.Show("Successfully added data to " + TheDeviceBatch.Name);
            /*
            try
            {
                FolderBrowserDialog folderBrowswer = new FolderBrowserDialog();
                if (folderBrowswer.ShowDialog() == DialogResult.OK)
                {
                    GenerateNewDataEntitiesFromPath(folderBrowswer.SelectedPath);
                    UpdateSpreadsheetsExecute(new object());
                }
                MessageBox.Show("Successfully added data to " + TheDeviceBatch.Name);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            */
        }
        private RelayCommand _updateSpreadsheets;
        public ICommand UpdateSpreadsheets
        {
            get
            {
                if (_updateSpreadsheets == null)
                {
                    _updateSpreadsheets = new RelayCommand(param => this.UpdateSpreadsheetsExecute(param));
                }
                return _updateSpreadsheets;
            }
        }
        public void UpdateSpreadsheetsExecute(object o)
        {
            SpreadsheetGenerator.GenerateSummarySpreadsheet(TheDeviceBatch);
            foreach (Device d in TheDeviceBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    SpreadsheetGenerator.GenerateLJVScanSummary(ss);
                }
            }
            //MessageBox.Show("Updated spreadsheets for batch " + TheDeviceBatch.Name);
        }
        private RelayCommand _updatePlotBitmaps;
        public ICommand UpdatePlotBitmaps
        {
            get
            {
                if (_updatePlotBitmaps == null)
                {
                    _updatePlotBitmaps = new RelayCommand(param => this.UpdatePlotBitmapsExecute(param));
                }
                return _updatePlotBitmaps;
            }
        }
        public void UpdatePlotBitmapsExecute(object o)
        {
            PlotBitmapGenerator.UpdatePlotsForDeviceBatch(this);
            Debug.WriteLine("Updated plot images for batch " + TheDeviceBatch.Name);
        }
        private RelayCommand _SaveDeviceBatchChanges;
        public ICommand SaveDeviceBatchChanges
        {
            get
            {
                if (_SaveDeviceBatchChanges == null)
                {
                    _SaveDeviceBatchChanges = new RelayCommand(param => this.SaveDeviceBatchChangesExecute(param));
                }
                return _SaveDeviceBatchChanges;
            }
        }
        public void SaveDeviceBatchChangesExecute(object o)
        {
            try
            {
                ctx.SaveChanges();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        #endregion
    }
}
