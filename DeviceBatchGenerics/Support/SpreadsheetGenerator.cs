using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Reflection;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support
{
    public static class SpreadsheetGenerator
    {

        #region Legacy DFS Format
        public static void GenerateSummarySpreadsheet(DeviceBatch devBatch)
        {
            using (var summaryExcelPackage = new ExcelPackage())
            {
                if (devBatch.SpreadSheetPath != null)
                {
                    Debug.WriteLine("Updating data summary spreadsheet from DFS spreadsheet");
                    var EQESheet = summaryExcelPackage.Workbook.Worksheets.Add("EQE");
                    WriteEQEDataToWorksheet(EQESheet, devBatch, WriteDeviceStructuresToWorksheetFromDFSSpreadsheet(EQESheet, devBatch));
                    FormatWorksheet(EQESheet);
                    var ColorDataSheet = summaryExcelPackage.Workbook.Worksheets.Add("Color");
                    WriteColorDataToWorksheet(ColorDataSheet, devBatch, WriteDeviceStructuresToWorksheetFromDFSSpreadsheet(ColorDataSheet, devBatch));
                    FormatWorksheet(ColorDataSheet);
                    var CESheet = summaryExcelPackage.Workbook.Worksheets.Add("CE");
                    WriteCEDataToWorksheet(CESheet, devBatch, WriteDeviceStructuresToWorksheetFromDFSSpreadsheet(CESheet, devBatch));
                    FormatWorksheet(CESheet);
                    Process.Start("net.exe", @"use Z: \\169.254.190.155\NPI Shared Data\"); //probably unnecessary but was having an error and this doesn't break anything
                    //string saveString = devBatch.FilePath.Replace(@"Z:\", @"\\169.254.190.155\NPI Shared Data\");
                    string saveString = devBatch.FilePath;
                    saveString = string.Concat(saveString, @"\", devBatch.Name, ".xlsx");
                    summaryExcelPackage.SaveAs(new FileInfo(saveString));
                }
                if (devBatch.SpreadSheetPath == null)
                {
                    var EQESheet = summaryExcelPackage.Workbook.Worksheets.Add("EQE");
                    WriteEQEDataToWorksheet(EQESheet, devBatch, WriteDeviceStructuresToWorksheetFromDBEntities(EQESheet, devBatch));
                    FormatWorksheet(EQESheet);
                    var ColorDataSheet = summaryExcelPackage.Workbook.Worksheets.Add("Color");
                    WriteColorDataToWorksheet(ColorDataSheet, devBatch, WriteDeviceStructuresToWorksheetFromDBEntities(ColorDataSheet, devBatch));
                    FormatWorksheet(ColorDataSheet);
                    var CESheet = summaryExcelPackage.Workbook.Worksheets.Add("CE");
                    WriteCEDataToWorksheet(CESheet, devBatch, WriteDeviceStructuresToWorksheetFromDBEntities(CESheet, devBatch));
                    FormatWorksheet(CESheet);
                    Process.Start("net.exe", @"use Z: \\169.254.190.155\NPI Shared Data\"); //probably unnecessary but was having an error and this doesn't break anything
                    //string saveString = devBatch.FilePath.Replace(@"Z:\", @"\\169.254.190.155\NPI Shared Data\");
                    string saveString = devBatch.FilePath;
                    saveString = string.Concat(saveString, @"\", devBatch.Name, ".xlsx");
                    summaryExcelPackage.SaveAs(new FileInfo(saveString));
                }
            }
        }
        private static int WriteDeviceStructuresToWorksheetFromDFSSpreadsheet(ExcelWorksheet ws, DeviceBatch devBatch)
        {
            var DFSSpreadsheetFile = new FileInfo(devBatch.SpreadSheetPath);
            int columnCounter = 1;
            //find the highest index column and row that contain data (columnCounter and rowCounter)
            using (var spreadsheet = new ExcelPackage(DFSSpreadsheetFile))
            {
                if (spreadsheet.Workbook.Worksheets.Count > 0)
                {
                    var worksheet = spreadsheet.Workbook.Worksheets.First();//assume that everything is on the first worksheet;
                    int commentColumnIndex = 1;
                    int labelColumnIndex = 1;
                    string cellText = "";
                    while (!cellText.Contains("COMMENT"))
                    {
                        cellText = worksheet.Cells[1, columnCounter].Text.ToUpper();
                        Debug.WriteLine("Column header is: " + cellText + " at index: " + commentColumnIndex);
                        if (cellText.Contains("COMMENT"))
                            commentColumnIndex = columnCounter;
                        if (cellText.Contains("LABEL"))
                            labelColumnIndex = columnCounter;
                        columnCounter++;
                        if (columnCounter > 77)
                        {
                            Debug.WriteLine("Looks like there is no Comment column");
                            //System.Windows.Forms.MessageBox.Show("Looks like there is no Comment column");
                            break;
                        }
                    }
                    int presentBatchIndexForDevice = 0;
                    int rowCounter = 2; //to account for the case where devices aren't numbered sequentially or starting at 1, count with a different int
                    while (presentBatchIndexForDevice >= 0)
                    {
                        cellText = worksheet.Cells[rowCounter, 1].Text;
                        if (cellText != "")
                            presentBatchIndexForDevice = Convert.ToInt32(cellText);
                        else
                            break;
                        Debug.WriteLine("1st column cell contains: " + cellText + " at index " + rowCounter);
                        rowCounter++;
                    }
                    //copy values until the comment column if it's in the right place, otherwise copy until the label column
                    if (commentColumnIndex == labelColumnIndex + 1)
                        columnCounter = commentColumnIndex;
                    else
                        columnCounter = labelColumnIndex;
                    for (int i = 1; i <= columnCounter; i++)//iterate over every column
                    {
                        for (int j = 1; j <= rowCounter; j++)//iterate over every row
                        {
                            ws.Cells[j, i].Value = worksheet.Cells[j, i].Value;
                        }
                    }
                }
            }
            return columnCounter + 1; //this is startColumn for data writing methods so we don't want to overwrite comment or label columns
        }
        private static int WriteDeviceStructuresToWorksheetFromDBEntities(ExcelWorksheet ws, DeviceBatch devBatch)
        {
            int columnCounter = 1;

            return columnCounter;
        }
        private static void WriteEQEDataToWorksheet(ExcelWorksheet ws, DeviceBatch devBatch, int startColumn)
        {
            HashSet<string> testConditions = new HashSet<string>();
            foreach (Device d in devBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    testConditions.Add(ss.TestCondition);
                }
            }
            List<string> testConditionsList = testConditions.ToList();
            testConditionsList.Sort();
            //label test conditions
            for (int i = 1; i <= testConditionsList.Count(); i++)
            {
                ws.Cells[1, startColumn + i - 1].Value = testConditionsList[i - 1];
            }
            foreach (Device d in devBatch.Devices)
            {
                for (int i = 0; i < testConditions.Count(); i++)
                {
                    foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                    {
                        if (ss.TestCondition == testConditionsList[i])
                        {
                            List<LJVScan> scansWherePixelLitUp = ss.LJVScans.ToList();
                            for (int j = 0; j < scansWherePixelLitUp.Count; j++)
                            {
                                if (scansWherePixelLitUp[j].PixelLitUp.HasValue)//if PixelLitUp is not null
                                    if (!scansWherePixelLitUp[j].PixelLitUp.Value)//if it didn't light up
                                        scansWherePixelLitUp.Remove(scansWherePixelLitUp[j]);//remove it from the list
                            }
                            //Debug.WriteLine("Adding data to cell for device " + d.BatchIndex + " with testCondition " + ss.TestCondition);
                            if (scansWherePixelLitUp.Count > 0)
                            {
                                decimal minEQE = scansWherePixelLitUp.Where(x => x.MaxEQE == scansWherePixelLitUp.Min(y => y.MaxEQE)).First().MaxEQE;
                                decimal maxEQE = ss.MaxEQE;
                                decimal minLuminanceAtPeakEQE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Min(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                decimal maxLuminanceAtPeakEQE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Max(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                decimal minAt1kNitsEQE = scansWherePixelLitUp.Where(x => x.At1kNitsEQE == scansWherePixelLitUp.Min(y => y.At1kNitsEQE)).First().At1kNitsEQE;
                                decimal maxAt1kNitsEQE = ss.Max1kNitsEQE;
                                string cellString = string.Concat(
                                    minEQE,
                                    "-",
                                    maxEQE,
                                    "% ",
                                    minLuminanceAtPeakEQE,
                                    "-",
                                    maxLuminanceAtPeakEQE,
                                    "nits (@1K: ",
                                    minAt1kNitsEQE,
                                    "-",
                                    maxAt1kNitsEQE,
                                    "%)"
                                    );
                                ws.Cells[d.BatchIndex + 1, startColumn + i].Value = cellString;
                            }

                        }
                    }
                }
            }
        }
        private static void WriteCEDataToWorksheet(ExcelWorksheet ws, DeviceBatch devBatch, int startColumn)
        {
            HashSet<string> testConditions = new HashSet<string>();
            foreach (Device d in devBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    testConditions.Add(ss.TestCondition);
                }
            }
            List<string> testConditionsList = testConditions.ToList();
            testConditionsList.Sort();
            //label test conditions
            for (int i = 1; i <= testConditionsList.Count(); i++)
            {
                ws.Cells[1, startColumn + i - 1].Value = testConditionsList[i - 1];
            }
            foreach (Device d in devBatch.Devices)
            {
                for (int i = 0; i < testConditions.Count(); i++)
                {
                    foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                    {
                        if (ss.TestCondition == testConditionsList[i])
                        {
                            List<LJVScan> scansWherePixelLitUp = ss.LJVScans.ToList();
                            for (int j = 0; j < scansWherePixelLitUp.Count; j++)
                            {
                                if (scansWherePixelLitUp[j].PixelLitUp.HasValue)//if PixelLitUp is not null
                                    if (!scansWherePixelLitUp[j].PixelLitUp.Value)//if it didn't light up
                                        scansWherePixelLitUp.Remove(scansWherePixelLitUp[j]);//remove it from the list
                            }
                            if(scansWherePixelLitUp.Count > 0)
                            {
                                Debug.WriteLine("Adding data to cell for device " + d.BatchIndex + " with testCondition " + ss.TestCondition);
                                decimal minCE = scansWherePixelLitUp.Where(x => x.MaxCE == scansWherePixelLitUp.Min(y => y.MaxCE)).First().MaxCE;
                                decimal maxCE = ss.MaxCE;
                                decimal minLuminanceAtPeakCE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Min(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                decimal maxLuminanceAtPeakCE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Max(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                decimal minAt1kNitsCE = scansWherePixelLitUp.Where(x => x.At1kNitsCE == scansWherePixelLitUp.Min(y => y.At1kNitsCE)).First().At1kNitsCE;
                                decimal maxAt1kNitsCE = scansWherePixelLitUp.Where(x => x.At1kNitsCE == scansWherePixelLitUp.Max(y => y.At1kNitsCE)).First().At1kNitsCE;
                                string cellString = string.Concat(
                                    minCE,
                                    "-",
                                    maxCE,
                                    " cd/A ",
                                    "(@1K: ",
                                    minAt1kNitsCE,
                                    "-",
                                    maxAt1kNitsCE,
                                    " cd/A)"
                                    );
                                ws.Cells[d.BatchIndex + 1, startColumn + i].Value = cellString;
                            }                           
                        }
                    }
                }
            }
        }
        private static void WriteColorDataToWorksheet(ExcelWorksheet ws, DeviceBatch devBatch, int startColumn)
        {
            HashSet<string> testConditions = new HashSet<string>();
            foreach (Device d in devBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    testConditions.Add(ss.TestCondition);
                }
            }
            List<string> testConditionsList = testConditions.ToList();
            testConditionsList.Sort();
            //label test conditions
            for (int i = 1; i <= testConditionsList.Count(); i++)
            {
                ws.Cells[1, startColumn + i - 1].Value = testConditionsList[i - 1];
            }
            foreach (Device d in devBatch.Devices)
            {
                for (int i = 0; i < testConditions.Count(); i++)
                {
                    foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                    {
                        if (ss.TestCondition == testConditionsList[i])
                        {
                            LJVScanSummaryVM vm = new LJVScanSummaryVM(ss);

                            string cellString = string.Concat(
                                "Peak λ: ",
                                vm.PeakLambda.Mean,
                                " FWHM: ",
                                vm.FWHM.Mean,
                                " CIE x: ",
                                vm.CIEx.Mean,
                                " CIE y: ",
                                vm.CIEy.Mean
                                );
                            ws.Cells[d.BatchIndex + 1, startColumn + i].Value = cellString;
                        }
                    }
                }
            }
        }
        #endregion
        #region QDBatch Report
        public static void GenerateQDBatchReport(QDBatch qdBatch, string filePath = null)
        {
            Process.Start("net.exe", @"use Z: \\169.254.190.155\NPI Shared Data\"); //probably unnecessary but was having an error and this doesn't break anything
            if (filePath == null)
                filePath = @"Z:\Data (LJV, Lifetime)\QD Batches\";
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
            using (var qdbExcel = new ExcelPackage())
            {
                {
                    Debug.WriteLine("Updating report for QDBatch: " + qdBatch.Name);
                    var EQESheet = qdbExcel.Workbook.Worksheets.Add("EQE");
                    WriteQDBEQEToWS(EQESheet, qdBatch);
                    FormatQDBWorksheet(EQESheet);
                    var ColorDataSheet = qdbExcel.Workbook.Worksheets.Add("Color");
                    WriteQDBColorToWS(ColorDataSheet, qdBatch);
                    FormatQDBWorksheet(ColorDataSheet);
                    string saveString = string.Concat(filePath, qdBatch.Name, ".xlsx");
                    qdbExcel.SaveAs(new FileInfo(saveString));
                }
            }
        }
        private static void WriteQDBEQEToWS(ExcelWorksheet ws, QDBatch qdb)
        {
            int rowCounter = 1;
            List<Device> deviceList = new List<Device>(qdb.Devices);
            deviceList = deviceList.OrderBy(x => x.DeviceBatch.FabDate).ThenBy(y => y.BatchIndex).ToList();

            foreach (Device d in deviceList)
            {
                int columnCounter = 2;
                ws.Cells[rowCounter, 1].Value = d.Label;
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    List<LJVScan> scansWherePixelLitUp = ss.LJVScans.ToList();
                    for (int j = 0; j < scansWherePixelLitUp.Count; j++)
                    {
                        if (scansWherePixelLitUp[j].PixelLitUp.HasValue)//if PixelLitUp is not null
                            if (!scansWherePixelLitUp[j].PixelLitUp.Value)//if it didn't light up
                                scansWherePixelLitUp.Remove(scansWherePixelLitUp[j]);//remove it from the list
                    }
                    //Debug.WriteLine("Adding data to cell for device " + d.BatchIndex + " with testCondition " + ss.TestCondition);
                    if (scansWherePixelLitUp.Count > 0)
                    {
                        decimal minEQE = scansWherePixelLitUp.Where(x => x.MaxEQE == scansWherePixelLitUp.Min(y => y.MaxEQE)).First().MaxEQE;
                        decimal maxEQE = ss.MaxEQE;
                        decimal minLuminanceAtPeakEQE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Min(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                        decimal maxLuminanceAtPeakEQE = scansWherePixelLitUp.Where(x => x.LuminanceAtMaxEQE == scansWherePixelLitUp.Max(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                        decimal minAt1kNitsEQE = scansWherePixelLitUp.Where(x => x.At1kNitsEQE == scansWherePixelLitUp.Min(y => y.At1kNitsEQE)).First().At1kNitsEQE;
                        decimal maxAt1kNitsEQE = ss.Max1kNitsEQE;
                        string cellString = string.Concat(
                            ss.TestCondition,
                            "\n",
                            minEQE,
                            "-",
                            maxEQE,
                            "% ",
                            minLuminanceAtPeakEQE,
                            "-",
                            maxLuminanceAtPeakEQE,
                            "nits (@1K: ",
                            minAt1kNitsEQE,
                            "-",
                            maxAt1kNitsEQE,
                            "%)"
                            );
                        ws.Cells[rowCounter, columnCounter].Value = cellString;
                        columnCounter++;
                    }
                }
                rowCounter++;
            }
        }
        private static void WriteQDBColorToWS(ExcelWorksheet ws, QDBatch qdb)
        {
            int rowCounter = 1;
            List<Device> deviceList = new List<Device>(qdb.Devices);
            deviceList = deviceList.OrderBy(x => x.DeviceBatch.FabDate).ThenBy(y => y.BatchIndex).ToList();

            foreach (Device d in deviceList)
            {
                int columnCounter = 2;
                ws.Cells[rowCounter, 1].Value = d.Label;
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    List<LJVScan> scansWherePixelLitUp = ss.LJVScans.ToList();
                    for (int j = 0; j < scansWherePixelLitUp.Count; j++)
                    {
                        if (scansWherePixelLitUp[j].PixelLitUp.HasValue)//if PixelLitUp is not null
                            if (!scansWherePixelLitUp[j].PixelLitUp.Value)//if it didn't light up
                                scansWherePixelLitUp.Remove(scansWherePixelLitUp[j]);//remove it from the list
                    }
                    //Debug.WriteLine("Adding data to cell for device " + d.BatchIndex + " with testCondition " + ss.TestCondition);
                    if (scansWherePixelLitUp.Count > 0)
                    {
                        try
                        {
                            LJVScanSummaryVM vm = new LJVScanSummaryVM(ss);

                            string cellString = string.Concat(
                                 ss.TestCondition,
                                "\n",
                                "Peak λ: ",
                                vm.PeakLambda.Mean,
                                " FWHM: ",
                                vm.FWHM.Mean,
                                " CIE x: ",
                                vm.CIEx.Mean,
                                " CIE y: ",
                                vm.CIEy.Mean
                                );
                            ws.Cells[rowCounter, columnCounter].Value = cellString;
                            columnCounter++;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                        }

                    }
                }
                rowCounter++;
            }
        }
        #endregion
        private static void FormatWorksheet(ExcelWorksheet ws)
        {
            ws.DefaultRowHeight = 55;
            ws.DefaultColWidth = 17;
            ws.Row(1).Height = 15;
            ws.Column(1).Width = 7;
            ws.Column(2).Width = 7;
            ws.Cells["A1:Z333"].Style.WrapText = true;
            ws.Cells["A1:Z333"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            ws.View.FreezePanes(1, 33);
        }
        private static void FormatQDBWorksheet(ExcelWorksheet ws)
        {
            ws.DefaultRowHeight = 66;
            ws.DefaultColWidth = 19;
            //ws.Row(1).Height = 15;
            //ws.Column(1).Width = 7;
            //ws.Column(2).Width = 7;
            ws.Cells["A1:Z333"].Style.WrapText = true;
            ws.Cells["A1:Z333"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        }
        public static void GenerateDeviceFabricationSpecification(DeviceBatch devBatch)
        {
            try
            {
                using (var p = new ExcelPackage())
                {
                    string batchName = string.Concat(devBatch.FabDate.ToString("MMddyy"), " ", devBatch.Name);
                    //make a new workbook
                    var ws = p.Workbook.Worksheets.Add(batchName);
                    //find the maximum number of layers of a particular physical role in each device
                    List<PhysicalRole> rolesList = new List<PhysicalRole>();
                    HashSet<string> testConditions = new HashSet<string>();
                    foreach (Device d in devBatch.Devices)
                    {
                        foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                        {
                            testConditions.Add(ss.TestCondition);
                        }
                    }
                    List<string> testConditionsList = testConditions.ToList();
                    int testConditionCounter = 0;
                    foreach (string condition in testConditions)
                    {
                        Debug.WriteLine("Found testCondition: " + condition);
                        testConditionCounter++;
                    }
                    testConditionsList.Sort();
                    foreach (Device d in devBatch.Devices)
                    {
                        PhysicalRole previousRole = new PhysicalRole();
                        int duplicatesCounter = 0;
                        foreach (Layer l in d.Layers)
                        {
                            //if the physical role is not in the list, add it
                            if (!rolesList.Contains(l.PhysicalRole))
                            {
                                //if it's at the end of the present role list, add it
                                if (l.PositionIndex == rolesList.Count())
                                    rolesList.Add(l.PhysicalRole);
                                else //otherwise, find the index of previousRole in rolesList and insert l.PhysicalRole
                                {
                                    var previousRoleIndexInList = rolesList.IndexOf(previousRole);
                                    rolesList.Insert(previousRoleIndexInList + 1, l.PhysicalRole);
                                }
                                /*
                                else //else, insert it after the other layer with the same index
                                    rolesList.Insert(l.PositionIndex + duplicatesCounter, l.PhysicalRole);
                                    */
                            }
                            //if it's the same as the previous layer, add a duplicate (note that this causes problems if more than 2 layers share a physical role (unlikely scenario that I'm too lazy to figure out how to avoid))
                            if (l.PhysicalRole == previousRole)
                            {
                                int count = rolesList.Where(x => x.Equals(l.PhysicalRole)).Count();
                                if (count == 1)//don't keep adding the same layer for multiple bilayer devices
                                {
                                    int index = rolesList.IndexOf(previousRole);
                                    rolesList.Insert(index, l.PhysicalRole);
                                    duplicatesCounter++;
                                }
                            }
                            previousRole = l.PhysicalRole;
                        }
                    }
                    //add labels to the 2nd row of the spreadsheet (leaving space in first row for expansion)
                    ws.Cells[2, 1].Value = "Device #";
                    for (int i = 0; i < rolesList.Count; i++)
                    {
                        ws.Cells[2, i + 2].Value = rolesList[i].LongName;
                    }
                    //insert values from each layer into relevant cells                    
                    foreach (Device d in devBatch.Devices)
                    {
                        ws.Cells[d.BatchIndex + 2, 1].Value = d.BatchIndex;
                        int rolesListCounter = 0;
                        foreach (Layer l in d.Layers)
                        {
                            string cellString = "something weird happened";
                            //this is a lazy solution that needs to be fixed at some point since Patterned TCOs are not necessarilly produced via sputtering processes
                            if (l.DepositionMethod.Name == "TCO Substrate")
                            {
                                cellString =
                                    string.Concat(
                                        l.Material.Supplier,
                                        " ",
                                        l.Material.Name,
                                        " ",
                                        l.SheetResistance,
                                        "  Ω/□"
                                        );
                            }
                            else if (l.DepositionMethod.Name == "Spincoating")
                            {
                                cellString =
                                    string.Concat(
                                        l.Solution.Material.Name,
                                        " (",
                                        l.Solution.Solvent,
                                        ") ",
                                        l.Solution.Concentration,
                                        "mg/mL, ",
                                        l.RPM,
                                        "RPM, ",
                                        l.AnnealTemp,
                                        "C ",
                                        l.AnnealDuration,
                                        "m ",
                                        l.Comment
                                        );
                            }
                            else if (l.DepositionMethod.Name == "Thermal Evaporation")
                            {
                                cellString =
                                    string.Concat(
                                        l.Material.Name,
                                        " ",
                                        l.Thickness,
                                        "nm"
                                        );
                            }
                            else if (l.DepositionMethod.Name == "Manual Pipetting")
                            {
                                cellString =
                                    string.Concat(
                                        l.Material.Name,
                                        " ",
                                        l.CureCondition,
                                        " ",
                                        l.CureTime,
                                        "m"
                                        );
                            }
                            Debug.WriteLine("Physical role: " + l.PhysicalRole.LongName + " at index: " + rolesListCounter);
                            if (rolesListCounter < rolesList.Count() && l.PhysicalRole == rolesList[rolesListCounter])
                            {
                                ws.Cells[d.BatchIndex + 2, rolesListCounter + 2].Value = cellString;
                                rolesListCounter++;
                            }
                            else
                            {
                                while (rolesListCounter < rolesList.Count() && l.PhysicalRole != rolesList[rolesListCounter])
                                {
                                    rolesListCounter++;
                                    if (rolesListCounter < rolesList.Count && l.PhysicalRole == rolesList[rolesListCounter])
                                    {
                                        ws.Cells[d.BatchIndex + 2, rolesListCounter + 2].Value = cellString;
                                    }
                                }
                            }
                        }
                        ws.Cells[d.BatchIndex + 2, rolesList.Count + 2].Value = d.Label;
                        ws.Cells[d.BatchIndex + 2, rolesList.Count + 3].Value = d.Comment;
                        //data reporting section
                        for (int i = 0; i < testConditions.Count(); i++)
                        {
                            foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                            {
                                if (ss.TestCondition == testConditionsList[i])
                                {
                                    Debug.WriteLine("Adding data to cell for device " + d.BatchIndex + " with testCondition " + ss.TestCondition);
                                    decimal minEQE = ss.LJVScans.Where(x => x.MaxEQE == ss.LJVScans.Min(y => y.MaxEQE)).First().MaxEQE;
                                    decimal maxEQE = ss.MaxEQE;
                                    decimal minLuminanceAtPeakEQE = ss.LJVScans.Where(x => x.LuminanceAtMaxEQE == ss.LJVScans.Min(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                    decimal maxLuminanceAtPeakEQE = ss.LJVScans.Where(x => x.LuminanceAtMaxEQE == ss.LJVScans.Max(y => y.LuminanceAtMaxEQE)).First().LuminanceAtMaxEQE ?? default(int);
                                    decimal minAt1kNitsEQE = ss.LJVScans.Where(x => x.At1kNitsEQE == ss.LJVScans.Min(y => y.At1kNitsEQE)).First().At1kNitsEQE;
                                    decimal maxAt1kNitsEQE = ss.Max1kNitsEQE;
                                    string cellString = string.Concat(
                                        minEQE,
                                        "-",
                                        maxEQE,
                                        "% ",
                                        minLuminanceAtPeakEQE,
                                        "-",
                                        maxLuminanceAtPeakEQE,
                                        "nits (1K, ",
                                        minAt1kNitsEQE,
                                        "-",
                                        maxAt1kNitsEQE,
                                        "%)"
                                        );
                                    ws.Cells[d.BatchIndex + 2, rolesList.Count + i + 4].Value = cellString; //+4 because i starts at 0 and d.comment is offset by 3 columns from rolesList.Count
                                }
                            }
                        }
                    }
                    ws.Cells[2, rolesList.Count + 2].Value = "Device Label";
                    ws.Cells[2, rolesList.Count + 3].Value = "Comment";
                    //label test conditions
                    for (int i = 1; i <= testConditionsList.Count(); i++)
                    {
                        ws.Cells[2, rolesList.Count + i + 3].Value = testConditionsList[i - 1];
                    }
                    //spreadsheet formatting section
                    ws.DefaultRowHeight = 45;
                    ws.DefaultColWidth = 20;
                    ws.Row(1).Height = 15;
                    ws.Row(2).Height = 30;
                    ws.Column(1).Width = 7;
                    ws.Column(2).Width = 7;
                    ws.Column(rolesList.Count).Width = 9;
                    ws.Column(rolesList.Count).Width = 13;
                    ws.Cells["A1:Z33"].Style.WrapText = true;
                    ws.Cells["A1:Z33"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.View.FreezePanes(1, 33);
                    Process.Start("net.exe", @"use Z: \\169.254.190.155\NPI Shared Data\"); //probably unnecessary but was having an error and this doesn't break anything
                    //string saveString = devBatch.FilePath.Replace(@"Z:\", @"\\169.254.190.155\NPI Shared Data\");
                    string saveString = devBatch.FilePath;
                    saveString = string.Concat(saveString, @"\", devBatch.Name, ".xlsx");
                    p.SaveAs(new FileInfo(saveString));
                }
            }

            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                //MessageBox.Show(e.ToString());
            }

        }

        public static void GenerateLJVScanSummary(DeviceLJVScanSummary scanSummary)
        {

            Debug.WriteLine("Generating summary spreadsheet for " + scanSummary.Device.Label + " in directory: " + scanSummary.SpreadsheetReportPath);

            List<PropertyInfo> ignoredProps = new List<PropertyInfo>()
                {
                    typeof(LJVScan).GetProperty("LJVScanId"),
                    typeof(DeviceLJVScanSummary).GetProperty("DeviceLJVScanSummaryId"),
                    typeof(ELSpectrum).GetProperty("ELSpectrumId"),
                    typeof(LJVScan).GetProperty("FilePath"),
                    typeof(DeviceLJVScanSummary).GetProperty("FilePath"),
                    typeof(ELSpectrum).GetProperty("FilePath"),
                    typeof(LJVScan).GetProperty("Pixel"),
                    typeof(ELSpectrum).GetProperty("Pixel"),
                    typeof(LJVScan).GetProperty("DeviceLJVScanSummary"),
                    typeof(ELSpectrum).GetProperty("DeviceLJVScanSummary"),
                    typeof(LJVScan).GetProperty("Image"),
                    typeof(LJVScan).GetProperty("LJVScanSpec"),
                    typeof(DeviceLJVScanSummary).GetProperty("Device"),
                    typeof(DeviceLJVScanSummary).GetProperty("TestCondition"),
                    typeof(ELSpectrum).GetProperty("QDBatch"),
                    typeof(DeviceLJVScanSummary).GetProperty("ELSpectras"),
                    typeof(DeviceLJVScanSummary).GetProperty("LJVScans"),
                    typeof(DeviceLJVScanSummary).GetProperty("Images")
                 };
            var singleScanPropsList = new List<PropertyInfo>(typeof(LJVScan).GetProperties());
            var ELSpecPropsList = new List<PropertyInfo>(typeof(ELSpectrum).GetProperties());
            var scanSummaryPropsList = new List<PropertyInfo>(typeof(DeviceLJVScanSummary).GetProperties());
            //remove ignored properties            
            foreach (PropertyInfo p in ignoredProps)
            {
                if (p != null)
                {
                    for (int i = singleScanPropsList.Count - 1; i >= 0; i--)
                    {
                        if (singleScanPropsList[i].Name == p.Name)
                            singleScanPropsList.RemoveAt(i);
                    }
                    for (int i = scanSummaryPropsList.Count - 1; i >= 0; i--)
                    {
                        if (scanSummaryPropsList[i].Name == p.Name)
                            scanSummaryPropsList.RemoveAt(i);
                    }
                    for (int i = ELSpecPropsList.Count - 1; i >= 0; i--)
                    {
                        if (ELSpecPropsList[i].Name == p.Name)
                            ELSpecPropsList.RemoveAt(i);
                    }
                }
            }
            using (var p = new ExcelPackage())
            {
                string deviceName = scanSummary.Device.Label;
                //make a new workbook
                var ws = p.Workbook.Worksheets.Add(deviceName);
                //iterate over each LJVScan, then each property of LJVScan to populate ws cells
                int scanCounter = 1;
                //int propCounter = 2;
                ws.Cells[1, 1].Value = "Property Name";
                for (int i = 2; i <= singleScanPropsList.Count + 1; i++)
                {
                    ws.Cells[i, 1].Value = singleScanPropsList[i - 2].Name;
                }
                for (int i = 0; i <= ELSpecPropsList.Count - 1; i++)
                {
                    ws.Cells[i + singleScanPropsList.Count + 2, 1].Value = ELSpecPropsList[i].Name;
                }
                foreach (LJVScan l in scanSummary.LJVScans)
                {
                    try
                    {
                        ws.Cells[1, scanCounter + 1].Value = l.Pixel.Site;
                        for (int i = 2; i <= singleScanPropsList.Count + 1; i++)
                        {
                            ws.Cells[i, scanCounter + 1].Value = singleScanPropsList[i - 2].GetValue(l);
                        }
                        //match spectra to LJVscans by pixel name
                        foreach (ELSpectrum es in scanSummary.ELSpectrums)
                        {
                            if (es.Pixel.Site == l.Pixel.Site)
                            {
                                for (int i = 0; i <= ELSpecPropsList.Count - 1; i++)
                                {
                                    ws.Cells[i + singleScanPropsList.Count + 2, scanCounter + 1].Value = ELSpecPropsList[i].GetValue(es);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                    scanCounter++;
                }
                ws.Cells[singleScanPropsList.Count + ELSpecPropsList.Count + 3, 1].Value = "Device stats";
                for (int i = 0; i <= scanSummaryPropsList.Count - 1; i++)
                {
                    ws.Cells[i + singleScanPropsList.Count + ELSpecPropsList.Count + 4, 1].Value = scanSummaryPropsList[i].Name;
                    ws.Cells[i + singleScanPropsList.Count + ELSpecPropsList.Count + 4, 2].Value = scanSummaryPropsList[i].GetValue(scanSummary);
                }
                ws.Cells.AutoFitColumns();
                p.SaveAs(new FileInfo(scanSummary.SpreadsheetReportPath));
            }

        }
        public static void GenerateEMLCharacteristics(List<QDBatchVM> qdbatches)
        {
            string path = @"Z:\Data (LJV, Lifetime)\EML Characteristics.xlsx";
            //first generate a hashset of years that QDBatches have been synthesized
            HashSet<string> qdBatchYears = new HashSet<string>();
            foreach (QDBatchVM qdvm in qdbatches)
            {
                var thisdatetime = Convert.ToDateTime(qdvm.TheQDBatch.DateReceivedOrSynthesized);
                qdBatchYears.Add(thisdatetime.Year.ToString());
            }

            using (var p = new ExcelPackage())
            {
                foreach (string yearInWhichThingsWereSynthesized in qdBatchYears)
                {
                    var ws = p.Workbook.Worksheets.Add(yearInWhichThingsWereSynthesized);//make a new worksheet for each year
                    List<QDBatchVM> qdbatchesSynthesizedThisYear = new List<QDBatchVM>();//construct a list of qdbatches that were synthesized in the Current Year :^)
                    foreach (QDBatchVM qdvm in qdbatches)
                    {
                        var thisQDVMSynthYear = Convert.ToDateTime(qdvm.TheQDBatch.DateReceivedOrSynthesized).Year.ToString();//extract the year value then add to list if it matches
                        if (thisQDVMSynthYear == yearInWhichThingsWereSynthesized)
                            qdbatchesSynthesizedThisYear.Add(qdvm);
                    }
                    List<QDBatchVM> orderedQDBatchList = qdbatchesSynthesizedThisYear.OrderBy(o => o.TheQDBatch.DateReceivedOrSynthesized).ThenBy(i => i.TheQDBatch.Name).ToList();
                    //populate header cells with names
                    ws.View.FreezePanes(2, 1);
                    ws.Cells[1, 1].Value = "QDBatch Name";
                    ws.Cells[1, 2].Value = "Mean EL λ";
                    ws.Cells[1, 3].Value = "Mean FWHM";
                    ws.Cells[1, 4].Value = "Mean CIEx";
                    ws.Cells[1, 5].Value = "Mean CIEy";
                    ws.Cells[1, 6].Value = "Mean EQE";
                    ws.Cells[1, 7].Value = "Max EQE";
                    ws.Cells[1, 8].Value = "Std Dev EQE";
                    ws.Cells[1, 9].Value = "# Devices";
                    ws.Cells[1, 10].Value = "Min EL λ";
                    ws.Cells[1, 11].Value = "Max EL λ";
                    ws.Cells[1, 12].Value = "Std Dev";
                    ws.Cells[1, 13].Value = "Min FWHM";
                    ws.Cells[1, 14].Value = "Max FWHM";
                    ws.Cells[1, 15].Value = "Std Dev";
                    ws.Cells[1, 16].Value = "Min CIEx";
                    ws.Cells[1, 17].Value = "Max CIEx";
                    ws.Cells[1, 18].Value = "Std Dev";
                    ws.Cells[1, 19].Value = "Min CIEy";
                    ws.Cells[1, 20].Value = "Max CIEy";
                    ws.Cells[1, 21].Value = "Std Dev";
                    int rowCounter = 2;
                    foreach (QDBatchVM qdvm in orderedQDBatchList)//now populate the worksheet with values
                    {
                        System.Drawing.Color backgroundColor = System.Drawing.Color.Transparent;
                        switch (qdvm.TheQDBatch.Color)
                        {
                            case "Red":
                                backgroundColor = System.Drawing.Color.LightSalmon;
                                break;
                            case "Green":
                                backgroundColor = System.Drawing.Color.LightGreen;
                                break;
                            case "Blue":
                                backgroundColor = System.Drawing.Color.LightBlue;
                                break;
                        }
                        for (int i = 1; i < 26; i++)
                        {
                            ws.Cells[rowCounter, i].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[rowCounter, i].Style.Fill.BackgroundColor.SetColor(backgroundColor);
                        }
                        ws.Cells[rowCounter, 1].Value = qdvm.TheQDBatch.Name;
                        ws.Cells[rowCounter, 2].Value = qdvm.PeakLambda.Mean;
                        ws.Cells[rowCounter, 3].Value = qdvm.FWHM.Mean;
                        ws.Cells[rowCounter, 4].Value = qdvm.CIEx.Mean;
                        ws.Cells[rowCounter, 5].Value = qdvm.CIEy.Mean;
                        ws.Cells[rowCounter, 6].Value = qdvm.PeakEQE.Mean;
                        ws.Cells[rowCounter, 7].Value = qdvm.BestEQE;
                        ws.Cells[rowCounter, 8].Value = qdvm.PeakEQE.StdDev;
                        ws.Cells[rowCounter, 9].Value = qdvm.NumberOfDevices;
                        ws.Cells[rowCounter, 10].Value = qdvm.PeakLambda.Min;
                        ws.Cells[rowCounter, 11].Value = qdvm.PeakLambda.Max;
                        ws.Cells[rowCounter, 12].Value = qdvm.PeakLambda.StdDev;
                        ws.Cells[rowCounter, 13].Value = qdvm.FWHM.Min;
                        ws.Cells[rowCounter, 14].Value = qdvm.FWHM.Max;
                        ws.Cells[rowCounter, 15].Value = qdvm.FWHM.StdDev;
                        ws.Cells[rowCounter, 16].Value = qdvm.CIEx.Min;
                        ws.Cells[rowCounter, 17].Value = qdvm.CIEx.Max;
                        ws.Cells[rowCounter, 18].Value = qdvm.CIEx.StdDev;
                        ws.Cells[rowCounter, 19].Value = qdvm.CIEy.Min;
                        ws.Cells[rowCounter, 20].Value = qdvm.CIEy.Max;
                        ws.Cells[rowCounter, 21].Value = qdvm.CIEy.StdDev;

                        ws.Cells.AutoFitColumns();
                        rowCounter++;
                    }
                }
                p.SaveAs(new FileInfo(path));
            }
        }

    }

}
