using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using DeviceBatchGenerics.Support;
using EFDeviceBatchCodeFirst;
using Origin;

namespace DeviceBatchGenerics.Support.OriginSupport
{
    public class OriginController
    {
        /// <summary>
        /// Note: everything involving the Origin Application class must be done within the same thread
        /// </summary>
        /// <returns></returns>
        #region Initialization
        private async Task<OriginController> InitializeAsync()
        {
            return await Task.Run(() =>
            {
                return this;
            }).ConfigureAwait(false);
        }
        public static Task<OriginController> CreateAsync()
        {
            var ret = new OriginController();
            return ret.InitializeAsync();
        }
        #endregion
        #region Members
        Application originApp;
        Worksheet activeSheet;
        int colorCounter;
        int lineStyleCounter;
        List<string> plotColors = new List<string>()
        { "black", "red", "dark cyan", "blue", "magenta", "dark yellow", "orange", "gray", "wine",
            "royal", "violet", "pink", "yellow", "cyan", "olive", "black", "red", "dark cyan", "blue" };
        readonly List<string> lineStyles = new List<string>() { "solid", "dash", "dot", "dash dot dot" };
        string activeSheetName;
        string activePixelName;
        string activeTestCondition;
        ELSpecVM activeELSpecVM;
        GraphLayer activeGraphLayer1;
        GraphLayer activeGraphLayer2;
        DataPlot activeDataPlot;
        DeviceBatchVM dbvm;
        enum AxisType { Linear, Logarithmic }
        enum ProcDATField { Voltage, CurrentDensity, PhotoCurrent, Luminance, CurrentEff, PowerEff, EQE }
        Dictionary<ProcDATField, string> ProcDATLongNames = new Dictionary<ProcDATField, string>() {
             {ProcDATField.Voltage, "Voltage" }, {ProcDATField.CurrentDensity,"Current Density" },{ProcDATField.PhotoCurrent,"Photocurrent" },{ProcDATField.Luminance,"Luminance" },
        {ProcDATField.CurrentEff,"Current Eff." },{ProcDATField.PowerEff,"Power Eff." },{ProcDATField.EQE,"EQE" } };
        Dictionary<ProcDATField, string> ProcDATUnits = new Dictionary<ProcDATField, string>() {
            {ProcDATField.Voltage, "V" }, {ProcDATField.CurrentDensity,"mA/cm²" },{ProcDATField.Luminance,"cd/m²" },
        {ProcDATField.CurrentEff,"cd/A" },{ProcDATField.PowerEff,"lm/W" },{ProcDATField.EQE,"%" } };
        enum StatsDATField
        {
            Voltage, MeanCurrentDensity, CurrentDensityStdDev, MeanResistance, ResistanceStdDev, MeanPhotoCurrent, PhotoCurrentStdDev, MeanLuminance, LuminanceStdDev,
            MeanCurrentEff, CurrentEffStdDev, MeanPowerEff, PowerEffStdDev, MeanEQE, EQEStdDev, MeanCameraCIEx, CameraCIExStdDev, MeanCameraCIEy, CameraCIEyStdDev, PopulationSize
        }
        Dictionary<StatsDATField, string> StatsDATLongNames = new Dictionary<StatsDATField, string>() {
             {StatsDATField.Voltage, "Voltage" }, {StatsDATField.MeanCurrentDensity,"Current Density" },{StatsDATField.MeanResistance,"Resistance" },{StatsDATField.MeanLuminance,"Luminance" },
             {StatsDATField.MeanCurrentEff,"Current Eff." },{StatsDATField.MeanPowerEff,"Power Eff." },{StatsDATField.MeanEQE,"EQE" },{StatsDATField.MeanCameraCIEx,"CIE x" },{StatsDATField.MeanCameraCIEy,"CIE y" } };
        Dictionary<StatsDATField, string> StatsDATUnits = new Dictionary<StatsDATField, string>() {
            {StatsDATField.Voltage, "V" }, {StatsDATField.MeanCurrentDensity,"mA/cm²" },{StatsDATField.MeanLuminance,"cd/m²" },
        {StatsDATField.MeanCurrentEff,"cd/A" },{StatsDATField.MeanPowerEff,"lm/W" },{StatsDATField.MeanEQE,"%" },{StatsDATField.MeanCameraCIEx,"" },{StatsDATField.MeanCameraCIEy,"" } };
        enum FullDATField
        {
            Voltage, PhotoCurrentA, Current, CurrentDensity, Resistance, PhotoCurrentB, CameraLuminance, CameraCIEx, CameraCIEy, PhotoCurrentC,
            Luminance, CurrentEff, PowerEff, EQE, PCurrChangePercent, TimeStamp
        }
        Dictionary<FullDATField, string> FullDATLongNames = new Dictionary<FullDATField, string>() { { FullDATField.Voltage, "Voltage" }, {FullDATField.PhotoCurrentA, "PhotoCurrentA" },
        {FullDATField.Current,"Current" },{FullDATField.CurrentDensity,"Current Density" },{FullDATField.Resistance,"Resistance" },{FullDATField.PhotoCurrentB,"PhotoCurrentB" },
            {FullDATField.CameraLuminance,"CameraLuminance" },{FullDATField.CameraCIEx,"CameraCIEx" },{FullDATField.CameraCIEy,"CameraCIEy" },{FullDATField.PhotoCurrentC,"PhotoCurrentC" },
            {FullDATField.Luminance,"Luminance" },{FullDATField.CurrentEff,"Current Eff."},{FullDATField.PowerEff,"Power Eff."},{FullDATField.EQE,"EQE"},
            {FullDATField.PCurrChangePercent,"PDChangePercent" },{FullDATField.TimeStamp, "Time"}
        };
        Dictionary<FullDATField, string> FullDATUnits = new Dictionary<FullDATField, string>() { { FullDATField.Voltage, "V" },{ FullDATField.PhotoCurrentA, "A" },
            {FullDATField.Current,"A" },{FullDATField.CurrentDensity,"mA/cm²"},{FullDATField.Resistance,"Ω"},{FullDATField.PhotoCurrentB,"A"},{FullDATField.CameraLuminance,"cd/m²"},
            {FullDATField.CameraCIEx,"" },{FullDATField.CameraCIEy,""},{FullDATField.PhotoCurrentC,"A"},{FullDATField.Luminance,"cd/m²"},{FullDATField.CurrentEff,"cd/A"},
            {FullDATField.PowerEff,"lm/W" },{FullDATField.EQE,"%"},{FullDATField.PCurrChangePercent,"%"},{FullDATField.TimeStamp,"DateTime"}
        };
        #endregion
        #region Legacy OPJ Construction
        public Task GenerateProcDATOPJForSingleTestCondition(DeviceBatchVM DBVM, string testCondition)
        {
            return Task.Run(() =>
            {
                KillAllOriginApps();
                dbvm = DBVM;
                originApp = new Application
                {
                    Visible = MAINWND_VISIBLE.MAINWND_SHOW,
                };
                originApp.Reset();
                ConstructSingleTCDirectory(testCondition);
                foreach (Device d in dbvm.TheDeviceBatch.Devices)
                {
                    originApp.ActiveFolder = originApp.RootFolder.Folders.FolderFromPath(d.Label);
                    var ss = d.DeviceLJVScanSummaries.Where(x => x.TestCondition == testCondition).FirstOrDefault();
                    if (ss != null)
                    {
                        activeSheetName = string.Concat("sheet", ss.TestCondition, d.BatchIndex);
                        var charsToRemove = new string[] { "-", ".", "_" };
                        foreach (var c in charsToRemove)
                        {
                            activeSheetName = activeSheetName.Replace(c, string.Empty);
                        }
                        Debug.WriteLine("sn: " + activeSheetName);
                        ConstructSingleTCSheetsPlots(new LJVScanSummaryVM(ss), activeSheetName);
                    }
                }
                SaveOPJAndRelease(testCondition);
            });
        }
        private void ConstructSingleTCSheetsPlots(LJVScanSummaryVM ssvm, string sheetName = "Data")
        {
            activeSheet = originApp.FindWorksheet(sheetName);
            if (activeSheet == null)
                sheetName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, sheetName, "Origin", 2);
            activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
            int scanCounter = 0;
            colorCounter = 0;
            foreach (LJVScan scan in ssvm.TheLJVScanSummary.LJVScans)
            {
                activePixelName = scan.Pixel.Site;
                if (scan.Pixel.Site == "SiteA")
                {
                    activeSheet.Name = "SiteA";
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
                    PopulateProcDATSheet(new LJVScanVM(scan));
                    scanCounter++;
                }
                else
                {
                    originApp.WorksheetPages[sheetName].Layers.Add(scan.Pixel.Site);
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[scanCounter];
                    PopulateProcDATSheet(new LJVScanVM(scan));
                    scanCounter++;
                    colorCounter++;
                }

                PlotCEJ(scan.DeviceLJVScanSummary.Device.Label);
                PlotEQEL(scan.DeviceLJVScanSummary.Device.Label);
                PlotJV(scan.DeviceLJVScanSummary.Device.Label);
                PlotLJV(scan.DeviceLJVScanSummary.Device.Label);
                TileGraphs();
            }
        }
        private void PopulateProcDATSheet(LJVScanVM scan)
        {

            string legendName = string.Concat(scan.TheLJVScan.Pixel.Device.Label, scan.TheLJVScan.Pixel.Site.Replace("Site", string.Empty));
            activeSheet.Cols = 7;
            activeSheet.Columns[0].LongName = "Voltage";
            activeSheet.Columns[1].LongName = "Current Density";
            activeSheet.Columns[2].LongName = "Photocurrent";
            activeSheet.Columns[3].LongName = "Luminance";
            activeSheet.Columns[4].LongName = "Current Eff.";
            activeSheet.Columns[5].LongName = "Power Eff.";
            activeSheet.Columns[6].LongName = "EQE";
            activeSheet.Columns[0].Units = "V";
            activeSheet.Columns[1].Units = "mA/cm²";
            activeSheet.Columns[2].Units = "A";
            activeSheet.Columns[3].Units = "cd/m²";
            activeSheet.Columns[4].Units = "cd/A";
            activeSheet.Columns[5].Units = "lm/W";
            activeSheet.Columns[6].Units = "%";
            activeSheet.Columns[0].Comments = legendName;
            activeSheet.Columns[1].Comments = legendName;
            activeSheet.Columns[2].Comments = legendName;
            activeSheet.Columns[3].Comments = legendName;
            activeSheet.Columns[4].Comments = legendName;
            activeSheet.Columns[5].Comments = legendName;
            activeSheet.Columns[6].Comments = legendName;
            activeSheet.SetData(scan.ProcDatArray(), 0, 0);
        }
        private void PlotCEJ(string plotName)
        {
            String strGrName = string.Concat(plotName, "CEJ");
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            DataRange cejdr = activeSheet.NewDataRange();
            cejdr.Add("X", activeSheet, 0, 1, -1, 1);//2nd column (J)
            cejdr.Add("Y", activeSheet, 0, 4, -1, 4);//5th column (CE)
            String strPlotName = string.Concat(plotName, "CEJ_Scatter");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            activeDataPlot = activeGraphLayer1.DataPlots.Add(cejdr, PLOTTYPES.IDM_PLOT_LINESYMB);
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            FormatXYGraph();
        }
        private void PlotEQEL(string plotName)
        {
            String strGrName = string.Concat(plotName, "EQEL");
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];

            //prepare JVDataRange
            DataRange eqeldr = activeSheet.NewDataRange();
            eqeldr.Add("X", activeSheet, 0, 3, -1, 3);//3rd column (L)
            eqeldr.Add("Y", activeSheet, 0, 6, -1, 6);//6th column (EQE)

            String strPlotName = string.Concat(plotName, "EQEL_Scatter");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            if (activeDataPlot == null)
            {
                activeDataPlot = activeGraphLayer1.DataPlots.Add(eqeldr, PLOTTYPES.IDM_PLOT_LINESYMB);
                //activeDataPlot.Execute(string.Concat("set ", strPlotName, " -c 101"));
                activeDataPlot.Execute(string.Concat("set -csf 2"));
            }
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            FormatXYGraph();
        }
        private void PlotJV(string plotName, bool plottingCompare = false)
        {
            String strGrName = string.Concat(plotName, "JV");
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];

            //prepare JVDataRange
            DataRange jvdr = activeSheet.NewDataRange();
            jvdr.Add("X", activeSheet, 0, 0, -1, 0);//3rd column (L)
            jvdr.Add("Y", activeSheet, 0, 1, -1, 1);//6th column (EQE)

            String strPlotName = string.Concat(plotName, "JV_Scatter");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            activeDataPlot = activeGraphLayer1.DataPlots.Add(jvdr, PLOTTYPES.IDM_PLOT_LINESYMB);
            //activeDataPlot.Execute(string.Concat("set ", strPlotName, " -c 101"));
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            FormatXYGraph();
            //set log scale (type = 2)
            activeGraphLayer1.Execute("layer.x.type = 2");
            activeGraphLayer1.Execute("layer.y.type = 2");

        }
        private void PlotLJV(string plotName)
        {
            String strGrName = string.Concat(plotName, "LJV");
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "DoubleY", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            activeGraphLayer2 = (GraphLayer)originApp.GraphPages[strGrName].Layers[1];

            //prepare JVDataRange
            DataRange jvdr = activeSheet.NewDataRange(0, 0, -1, 1);
            DataRange lvdr = activeSheet.NewDataRange();
            lvdr.Add("X", activeSheet, 0, 0, -1, 0);//add 1st column (V) data
            lvdr.Add("Y", activeSheet, 0, 3, -1, 3);//3rd column (L)
            String strPlotName = string.Concat(plotName, "LJV Scatter");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            activeDataPlot = activeGraphLayer1.DataPlots.Add(jvdr, PLOTTYPES.IDM_PLOT_LINESYMB);
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            activeDataPlot = activeGraphLayer2.DataPlots.Add(lvdr, PLOTTYPES.IDM_PLOT_LINESYMB);
            activeGraphLayer2.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            activeGraphLayer2.Execute(string.Concat("set ", activeDataPlot.Name, " -k 4"));//change symbol for luminance to triangle
            activeGraphLayer1.Execute("layer.y.type = 2");
            activeGraphLayer1.Execute("Rescale");
            activeGraphLayer1.Execute("layer.y.rescale()");
            activeGraphLayer1.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
            activeGraphLayer2.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
            activeGraphLayer2.Execute("Rescale");
        }

        #endregion
        #region Compare Pix OPJ Construction
        public Task GenerateSingleTCComparePixOPJ(DeviceBatchVM DBVM, string testCondition)
        {
            return Task.Run(() =>
            {
                dbvm = DBVM;
                originApp = new Application
                {
                    Visible = MAINWND_VISIBLE.MAINWND_SHOW,
                };
                originApp.Reset();
                activeTestCondition = testCondition;
                PopulateCompareSheetThenPlotXY(ProcDATField.Voltage, ProcDATField.CurrentDensity, "JV", AxisType.Logarithmic, AxisType.Logarithmic);
                PopulateCompareSheetThenPlotXY(ProcDATField.CurrentDensity, ProcDATField.EQE, "EQEJ");
                //PopulateCompareSheetThenPlotXY(ProcDATField.Luminance, ProcDATField.EQE, "EQEL");
                PopulateCompareSheetThenPlotXY(ProcDATField.CurrentDensity, ProcDATField.CurrentEff, "CEJ");
                //PopulateCompareSheetThenPlotXY(ProcDATField.Luminance, ProcDATField.CurrentEff, "CEL");
                SaveOPJAndRelease(string.Concat("Compare_", testCondition));
            });
        }
        private void PopulateCompareSheetThenPlotXY(ProcDATField xval, ProcDATField yval, string sheetName, AxisType xAxis = AxisType.Linear, AxisType yAxis = AxisType.Linear)
        {
            //create worksheet
            activeSheet = originApp.FindWorksheet(sheetName);
            if (activeSheet == null)
                sheetName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, sheetName, "Origin", 2);
            activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
            //count columns to create from # of scans
            int colsCounter = 0;
            foreach (Device d in dbvm.TheDeviceBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    if (ss.TestCondition == activeTestCondition)
                    {
                        foreach (LJVScan scan in ss.LJVScans)
                        {
                            colsCounter++;
                        }
                    }
                }
            }
            activeSheet.Cols = colsCounter * 2;
            colsCounter = 0;
            //create graph layer
            String strGrName = string.Concat("graph_", sheetName);
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            String strPlotName = string.Concat(strGrName, "_line");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            colorCounter = 0;
            lineStyleCounter = 0;
            if (activeSheet.Cols > 0)
            {
                //populate columns with data then add DataPlots to GraphLayer
                foreach (Device d in dbvm.TheDeviceBatch.Devices)
                {
                    Debug.WriteLine("Plotting " + sheetName + " for " + d.Label);
                    foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                    {
                        if (ss.TestCondition == activeTestCondition)
                        {
                            lineStyleCounter = 0;
                            foreach (LJVScan scan in ss.LJVScans)
                            {
                                LJVScanVM vm = new LJVScanVM(scan);
                                var array = vm.ProcDatArray();
                                activeSheet.Columns[colsCounter].LongName = ProcDATLongNames[xval];
                                activeSheet.Columns[colsCounter].Type = COLTYPES.COLTYPE_X;
                                activeSheet.Columns[colsCounter].Units = ProcDATUnits[xval];
                                activeSheet.SetData(array.GetCol((int)xval), 0, colsCounter);
                                activeSheet.Columns[colsCounter + 1].LongName = ProcDATLongNames[yval];
                                activeSheet.Columns[colsCounter + 1].Type = COLTYPES.COLTYPE_Y;
                                activeSheet.Columns[colsCounter + 1].Units = ProcDATUnits[yval];
                                activeSheet.Columns[colsCounter + 1].Comments = string.Concat(d.Label, scan.Pixel.Site.Replace("Site", string.Empty));
                                activeSheet.SetData(array.GetCol((int)yval), 0, colsCounter + 1);
                                DataRange dr = activeSheet.NewDataRange();
                                dr.Add("X", activeSheet, 0, colsCounter, -1, colsCounter);//add data for X column
                                dr.Add("Y", activeSheet, 0, colsCounter + 1, -1, colsCounter + 1);
                                activeDataPlot = activeGraphLayer1.DataPlots.Add(dr, PLOTTYPES.IDM_PLOT_LINE);
                                activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
                                activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -d ", lineStyleCounter));
                                colsCounter += 2;
                                lineStyleCounter++;
                            }
                        }
                    }
                    colorCounter++;//use same line color for each device
                }
                if (xAxis == AxisType.Logarithmic)
                    activeGraphLayer1.Execute("layer.x.type = 2");
                if (yAxis == AxisType.Logarithmic)
                    activeGraphLayer1.Execute("layer.y.type = 2");
                FormatXYGraph();
            }
        }
        #endregion
        #region Compare Stats OPJ Construction
        public Task GenerateSingleTCCompareStatsOPJ(DeviceBatchVM DBVM, string testCondition)
        {
            return Task.Run(() =>
            {
                dbvm = DBVM;
                originApp = new Application
                {
                    Visible = MAINWND_VISIBLE.MAINWND_SHOW,
                };
                originApp.Reset();
                activeTestCondition = testCondition;
                PopulateCompareStatsSheetThenPlotXY(StatsDATField.Voltage, StatsDATField.MeanCurrentDensity, "JV", AxisType.Logarithmic, AxisType.Logarithmic);
                PopulateCompareStatsSheetThenPlotXY(StatsDATField.MeanCurrentDensity, StatsDATField.MeanEQE, "EQEJ", AxisType.Linear, AxisType.Linear);
                PopulateCompareStatsSheetThenPlotXY(StatsDATField.MeanCurrentDensity, StatsDATField.MeanCurrentEff, "CEJ", AxisType.Linear, AxisType.Linear);
                PopulateCompareStatsSheetThenPlotXY(StatsDATField.Voltage, StatsDATField.MeanCameraCIEx, "CIExV", AxisType.Linear, AxisType.Linear);
                PopulateCompareStatsSheetThenPlotXY(StatsDATField.Voltage, StatsDATField.MeanCameraCIEy, "CIEyV", AxisType.Linear, AxisType.Linear);

                //PopulateCompareStatsSheetThenPlotXY(ProcDATField.CurrentDensity, ProcDATField.EQE, "EQEJ");
                //PopulateCompareSheetThenPlotXY(ProcDATField.Luminance, ProcDATField.EQE, "EQEL");
                //PopulateCompareStatsSheetThenPlotXY(ProcDATField.CurrentDensity, ProcDATField.CurrentEff, "CEJ");
                //PopulateCompareSheetThenPlotXY(ProcDATField.Luminance, ProcDATField.CurrentEff, "CEL");
                SaveOPJAndRelease(string.Concat("CompareStats_", testCondition));
            });
        }
        private void PopulateCompareStatsSheetThenPlotXY(StatsDATField xval, StatsDATField yval, string sheetName, AxisType xAxis = AxisType.Linear, AxisType yAxis = AxisType.Linear)
        {
            //create worksheet
            activeSheet = originApp.FindWorksheet(sheetName);
            if (activeSheet == null)
                sheetName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, sheetName, "Origin", 2);
            activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
            //count columns to create from # of scans
            int colsCounter = 0;
            foreach (Device d in dbvm.TheDeviceBatch.Devices)
            {
                foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                {
                    if (ss.TestCondition == activeTestCondition)
                    {
                        foreach (LJVScan scan in ss.LJVScans)
                        {
                            colsCounter++;
                        }
                    }
                }
            }
            activeSheet.Cols = colsCounter * 3;//xval (mean),yval(mean),yerror (no xerror because messy)
            colsCounter = 0;
            //create graph layer
            String strGrName = string.Concat("graph_", sheetName);
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            String strPlotName = string.Concat(strGrName, "_line");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            colorCounter = 0;
            lineStyleCounter = 0;
            if (activeSheet.Cols > 0)
            {
                //populate columns with data then add DataPlots to GraphLayer
                foreach (Device d in dbvm.TheDeviceBatch.Devices)
                {
                    Debug.WriteLine("Plotting " + sheetName + " for " + d.Label);
                    foreach (DeviceLJVScanSummary ss in d.DeviceLJVScanSummaries)
                    {
                        if (ss.TestCondition == activeTestCondition)
                        {
                            lineStyleCounter = 0;
                            LJVScanSummaryVM vm = new LJVScanSummaryVM(ss);
                            var array = vm.StatsDatArray();
                            activeSheet.Columns[colsCounter].LongName = StatsDATLongNames[xval];
                            activeSheet.Columns[colsCounter].Type = COLTYPES.COLTYPE_X;
                            activeSheet.Columns[colsCounter].Units = StatsDATUnits[xval];
                            activeSheet.SetData(array.GetCol((int)xval), 0, colsCounter);
                            activeSheet.Columns[colsCounter + 1].LongName = StatsDATLongNames[yval];
                            activeSheet.Columns[colsCounter + 1].Type = COLTYPES.COLTYPE_Y;
                            activeSheet.Columns[colsCounter + 1].Units = StatsDATUnits[yval];
                            activeSheet.Columns[colsCounter + 1].Comments = d.Label;
                            activeSheet.SetData(array.GetCol((int)yval), 0, colsCounter + 1);
                            activeSheet.Columns[colsCounter + 2].Type = COLTYPES.COLTYPE_ERROR;//set y error stuff
                            activeSheet.SetData(array.GetCol((int)yval + 1), 0, colsCounter + 2);
                            DataRange dr = activeSheet.NewDataRange();
                            dr.Add("X", activeSheet, 0, colsCounter, -1, colsCounter);//add data for X column
                            dr.Add("Y", activeSheet, 0, colsCounter + 1, -1, colsCounter + 1);
                            dr.Add("ED", activeSheet, 0, colsCounter + 2, -1, colsCounter + 2);//ED==origin name for Yerror type XDDDD
                            activeDataPlot = activeGraphLayer1.DataPlots.Add(dr, PLOTTYPES.IDM_PLOT_LINE);
                            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
                            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -d ", lineStyleCounter));
                            colsCounter += 3;
                            lineStyleCounter++;
                        }
                    }
                    colorCounter++;//do this to use same line color for each device
                }
                if (xAxis == AxisType.Logarithmic)
                    activeGraphLayer1.Execute("layer.x.type = 2");
                if (yAxis == AxisType.Logarithmic)
                    activeGraphLayer1.Execute("layer.y.type = 2");
                FormatXYGraph();
            }
        }

        #endregion
        #region FullDAT All Pix Single TC Construction
        public Task GenerateFullDATOPJ(DeviceBatchVM DBVM, string testCondition)
        {
            return Task.Run(() =>
            {
                KillAllOriginApps();
                dbvm = DBVM;
                originApp = new Application
                {
                    Visible = MAINWND_VISIBLE.MAINWND_SHOW,
                };
                originApp.Reset();
                activeTestCondition = testCondition;
                ConstructSingleTCDirectory(testCondition);
                foreach (Device d in dbvm.TheDeviceBatch.Devices)
                {
                    originApp.ActiveFolder = originApp.RootFolder.Folders.FolderFromPath(d.Label);
                    var ss = d.DeviceLJVScanSummaries.Where(x => x.TestCondition == testCondition).FirstOrDefault();
                    if (ss != null)
                    {
                        activeSheetName = string.Concat("sheet", ss.TestCondition, d.BatchIndex);
                        var charsToRemove = new string[] { "-", ".", "_" };
                        foreach (var c in charsToRemove)
                        {
                            activeSheetName = activeSheetName.Replace(c, string.Empty);
                        }
                        Debug.WriteLine("activeSheetName: " + activeSheetName);
                        var ssvm = new LJVScanSummaryVM(ss);
                        ConstructFullDATSheetsPlots(ssvm, activeSheetName);
                        activeSheetName = "EL" + activeSheetName;
                        ConstructELSpecSheetsPlots(ssvm, activeSheetName);
                        TileGraphs();
                    }
                }
                SaveOPJAndRelease(String.Concat("FullDAT_", testCondition));
            });
        }
        void PopulateFullDATSheet(LJVScanVM scan)
        {
            string legendName = string.Concat(scan.TheLJVScan.Pixel.Device.Label, scan.TheLJVScan.Pixel.Site.Replace("Site", string.Empty));
            var data = scan.FullDatArray();
            activeSheet.Cols = Enum.GetNames(typeof(FullDATField)).Length;
            for (int i = 0; i < activeSheet.Cols; i++)
            {
                var prop = ((FullDATField)i);
                activeSheet.Columns[i].LongName = FullDATLongNames[prop];
                activeSheet.Columns[i].Units = FullDATUnits[prop];
                activeSheet.Columns[i].Comments = legendName;
            }
            activeSheet.SetData(data, 0, 0);
        }
        void ConstructFullDATSheetsPlots(LJVScanSummaryVM ssvm, string sheetName = "Data")
        {
            activeSheet = originApp.FindWorksheet(sheetName);
            if (activeSheet == null)
                sheetName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, sheetName, "Origin", 2);
            activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
            activeSheet.Execute("window -i");//minimize worksheet
            int scanCounter = 0;
            colorCounter = 0;
            foreach (LJVScan scan in ssvm.TheLJVScanSummary.LJVScans)
            {
                activePixelName = scan.Pixel.Site;
                if (scan.Pixel.Site == "SiteA")
                {
                    activeSheet.Name = "SiteA";
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
                    PopulateFullDATSheet(new LJVScanVM(scan));
                    scanCounter++;
                }
                else
                {
                    originApp.WorksheetPages[sheetName].Layers.Add(scan.Pixel.Site);
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[scanCounter];
                    PopulateFullDATSheet(new LJVScanVM(scan));
                    scanCounter++;
                    colorCounter++;
                }
                string label = scan.DeviceLJVScanSummary.Device.Label;
                PlotFullDATXY(FullDATField.Voltage, FullDATField.PCurrChangePercent, (label + "_PDCV"));
                PlotFullDATXY(FullDATField.CurrentDensity, FullDATField.CurrentEff, (label + "_CEJ"));
                PlotFullDATXY(FullDATField.Luminance, FullDATField.EQE, (label + "_EQEL"));
                PlotFullDATXY(FullDATField.Voltage, FullDATField.CurrentDensity, (label + "_JV"), AxisType.Logarithmic, AxisType.Logarithmic);
                PlotFullDATXY(FullDATField.Voltage, FullDATField.CurrentDensity, (label + "_LJV"), AxisType.Linear, AxisType.Logarithmic, "DoubleY");
            }
        }
        void PlotFullDATXY(FullDATField xval, FullDATField yval, string strGrName, AxisType xAxis = AxisType.Linear, AxisType yAxis = AxisType.Linear, string plotType = "Scatter", FullDATField yval2 = FullDATField.Luminance)
        {
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, plotType, 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            DataRange dr1 = activeSheet.NewDataRange();
            dr1.Add("X", activeSheet, 0, (int)xval, -1, (int)xval);//add data for X column
            dr1.Add("Y", activeSheet, 0, (int)yval, -1, (int)yval);
            String strPlotName = string.Concat(strGrName, "_plot");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            activeDataPlot = activeGraphLayer1.DataPlots.Add(dr1, PLOTTYPES.IDM_PLOT_LINESYMB);
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            if (xAxis == AxisType.Logarithmic)
                activeGraphLayer1.Execute("layer.x.type = 2");
            if (yAxis == AxisType.Logarithmic)
                activeGraphLayer1.Execute("layer.y.type = 2"); activeGraphLayer1.Execute("Rescale");
            activeGraphLayer1.Execute("layer.y.rescale()");
            activeGraphLayer1.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
            if (plotType == "DoubleY")
            {
                activeGraphLayer2 = (GraphLayer)originApp.GraphPages[strGrName].Layers[1];
                DataRange dr2 = activeSheet.NewDataRange();
                dr2.Add("X", activeSheet, 0, (int)xval, -1, (int)xval);//add data for X column
                dr2.Add("Y", activeSheet, 0, (int)yval2, -1, (int)yval2);
                activeDataPlot = activeGraphLayer2.DataPlots.Add(dr2, PLOTTYPES.IDM_PLOT_LINESYMB);
                activeGraphLayer2.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
                activeGraphLayer2.Execute(string.Concat("set ", activeDataPlot.Name, " -k 4"));//change symbol for luminance to triangle
                activeGraphLayer2.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
                activeGraphLayer2.Execute("Rescale");
            }
            FormatXYGraph();
        }
        void PopulateELSpecSheet(ELSpecVM spec)
        {
            activeELSpecVM = spec;
            //string legendName = string.Concat(spec.TheELSpectrum.Pixel.Device.Label, spec.TheELSpectrum.Pixel.Site.Replace("Site", string.Empty));
            var roundPeak = Math.Round(spec.TheELSpectrum.ELPeakLambda, 1);
            var roundFWHM = Math.Round(spec.TheELSpectrum.ELFWHM, 1);
            string legendName = string.Concat(spec.TheELSpectrum.Pixel.Site.Replace("Site", string.Empty), " peak: ", roundPeak, " FWHM: ", roundFWHM);
            var data = spec.ELSpecArray();
            activeSheet.Cols = 2;
            activeSheet.Columns[0].LongName = "Wavelength";
            activeSheet.Columns[0].Units = "nm";
            activeSheet.Columns[0].Comments = legendName;
            activeSheet.Columns[1].LongName = "Intensity";
            activeSheet.Columns[1].Units = "a.u.";
            activeSheet.Columns[1].Comments = legendName;
            activeSheet.SetData(data, 0, 0);
        }
        void ConstructELSpecSheetsPlots(LJVScanSummaryVM ssvm, string sheetName = "Data")
        {
            activeSheet = originApp.FindWorksheet(sheetName);
            if (activeSheet == null)
                sheetName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, sheetName, "Origin", 2);
            activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
            activeSheet.Execute("window -i");//minimize worksheet
            int scanCounter = 0;
            colorCounter = 0;
            foreach (ELSpectrum spec in ssvm.TheLJVScanSummary.ELSpectrums)
            {
                activePixelName = spec.Pixel.Site;
                if (activePixelName == "SiteA")
                {
                    activeSheet.Name = "SiteA";
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[0];
                    PopulateELSpecSheet(new ELSpecVM(spec));
                    scanCounter++;
                }
                else
                {
                    originApp.WorksheetPages[sheetName].Layers.Add(spec.Pixel.Site);
                    activeSheet = (Worksheet)originApp.WorksheetPages[sheetName].Layers[scanCounter];
                    PopulateELSpecSheet(new ELSpecVM(spec));
                    scanCounter++;
                    colorCounter++;
                }
                string label = spec.DeviceLJVScanSummary.Device.Label + "_EL";
                PlotELSpec(label);
            }
        }
        void PlotELSpec(string strGrName)
        {
            activeGraphLayer1 = originApp.FindGraphLayer(strGrName);
            if (activeGraphLayer1 == null)
                strGrName = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_GRAPH, strGrName, "Scatter", 2);
            activeGraphLayer1 = (GraphLayer)originApp.GraphPages[strGrName].Layers[0];
            DataRange eldr = activeSheet.NewDataRange();
            Debug.WriteLine("Convert.ToInt32(activeELSpecVM.MinLambdaCutoff): " + Convert.ToInt32(activeELSpecVM.MinLambdaCutoff));
            var minLam = activeELSpecVM.ELSpecList.IndexOf(activeELSpecVM.ELSpecList.Where(x => x.Wavelength == activeELSpecVM.MinLambdaCutoff).FirstOrDefault());
            var maxLam = activeELSpecVM.ELSpecList.IndexOf(activeELSpecVM.ELSpecList.Where(x => x.Wavelength == activeELSpecVM.MaxLambdaCutoff).FirstOrDefault());
            eldr.Add("X", activeSheet, minLam, 0, maxLam, 0);//add data for X column
            eldr.Add("Y", activeSheet, minLam, 1, maxLam, 1);
            String strPlotName = string.Concat(strGrName, "_plot");
            activeDataPlot = activeGraphLayer1.DataPlots[strPlotName];
            activeDataPlot = activeGraphLayer1.DataPlots.Add(eldr, PLOTTYPES.IDM_PLOT_LINESYMB);
            activeGraphLayer1.Execute(string.Concat("set ", activeDataPlot.Name, " -c color(", plotColors[colorCounter], ")"));
            activeGraphLayer1.Execute("Rescale");
            activeGraphLayer1.Execute("layer.y.rescale()");
            activeGraphLayer1.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
            FormatXYGraph();
        }
        #endregion
        private void ConstructSingleTCDirectory(string rootName)
        {
            //do this to trigger default folder creation (can't seem to instantiate origin app with RootFolder)
            String name = "Dumb";
            name = (String)originApp.CreatePage((int)Origin.PAGETYPES.OPT_WORKSHEET, name, "Origin", 2);
            originApp.DestroyPage(name);
            var defaultFolder = originApp.ActiveFolder;
            defaultFolder.Name = rootName;
            foreach (Device d in dbvm.TheDeviceBatch.Devices)
            {
                Folder existingFolder = originApp.RootFolder.Folders.FolderFromPath(d.Label);
                if (existingFolder == null)
                    originApp.RootFolder.Folders.Add(d.Label);
            }
        }
        private void FormatXYGraph()
        {
            activeGraphLayer1.Execute("layer.x.opposite = 1");
            activeGraphLayer1.Execute("layer.x.thickness = 3");
            activeGraphLayer1.Execute("layer.y.opposite = 1");
            activeGraphLayer1.Execute("layer.y.showAxes = 3");
            activeGraphLayer1.Execute("layer.x.label.pt = 25");
            activeGraphLayer1.Execute("layer.y.label.pt = 25");
            activeGraphLayer1.Execute("layer.x.ticks = 5");
            activeGraphLayer1.Execute("layer.y.ticks = 5");
            activeGraphLayer1.Execute("XB.fsize = 35");
            activeGraphLayer1.Execute("YL.fsize = 35");
            activeGraphLayer1.Execute("Rescale");
            activeGraphLayer1.Execute("layer.y.rescale()");
            activeGraphLayer1.Execute("legendupdate dest:=layer update:=reconstruct mode:=comment");
        }
        private const int SW_MAXIMIZE = 3;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private void TileGraphs()
        {
            try
            {
                originApp.Visible = Origin.MAINWND_VISIBLE.MAINWND_SHOWMAXIMIZED;
                activeSheet.Execute("window -h 1");
                originApp.Execute("window -s T");
            }
            catch (Exception e)
            {
                Debug.WriteLine("TileGraphs() Exception: " + e.ToString());
            }
        }
        private void KillAllOriginApps()
        {
            try
            {
                //nuke ya ler option is the only one that doesn't result in a persistent app instance
                var p = System.Diagnostics.Process.GetProcessesByName("Origin85").ToList();
                if (p != null)
                {
                    foreach (Process pr in p)
                    {
                        Debug.WriteLine("Killing process: " + pr.ProcessName);
                        //pr.Close();
                        //pr.Dispose();
                        pr.Kill();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("KillAllOriginApps Exception: " + e.ToString());
            }

        }
        private void SaveOPJAndRelease(string fileName)
        {
            string saveDir = string.Concat(dbvm.TheDeviceBatch.FilePath, @"\Origin\");
            if (!File.Exists(saveDir))
                Directory.CreateDirectory(saveDir);
            originApp.Save(string.Concat(saveDir, fileName));
            originApp.EndSession();
            originApp.Exit();
            KillAllOriginApps();
            originApp = null;
        }
    }
}
