using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using DeviceBatchGenerics.ViewModels.EntityVMs;
using DeviceBatchGenerics.ViewModels.PlottingVMs;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support
{
    public static class PlotBitmapGenerator
    {
        public static void UpdatePlotsForDeviceBatch(DeviceBatchVM DBVM)
        {

            DevicePlotVM plotVM;
            //first, make sure that we have folders for each test condition
            foreach (string tc in DBVM.TestConditions)
            {
                var testConditionPath = string.Concat(DBVM.TheDeviceBatch.FilePath, @"\", tc, @"\OxyPlots");
                Debug.WriteLine(testConditionPath);
                if (!File.Exists(testConditionPath))//create folders in which to store our bitmaps if it doesn't exist
                {
                    Directory.CreateDirectory(testConditionPath);
                    Directory.CreateDirectory(string.Concat(testConditionPath, @"\L-J-V\"));
                    Directory.CreateDirectory(string.Concat(testConditionPath, @"\J-V\"));
                    Directory.CreateDirectory(string.Concat(testConditionPath, @"\EQE-L\"));
                    Directory.CreateDirectory(string.Concat(testConditionPath, @"\EQE-J\"));
                }
                //next, cycle through each LJVScanSummary and generate bitmaps using OxyPlot
                foreach (Device d in DBVM.TheDeviceBatch.Devices)
                {
                    plotVM = new DevicePlotVM(d);
                    plotVM.SelectedTestCondition = tc;
                    plotVM.LJVPlotVM1.SaveLJVPlotBitmap(string.Concat(testConditionPath, @"\L-J-V\", d.Label, ".jpg"));
                    plotVM.LJVPlotVM1.SaveEQELPlotBitmap(string.Concat(testConditionPath, @"\EQE-L\", d.Label, ".jpg"));
                    plotVM.LJVPlotVM1.SaveEQEJPlotBitmap(string.Concat(testConditionPath, @"\EQE-J\", d.Label, ".jpg"));
                    plotVM.LJVPlotVM1.SaveJVPlotBitmap(string.Concat(testConditionPath, @"\J-V\", d.Label, ".jpg"));
                }
            }
            var agingPath = string.Concat(DBVM.TheDeviceBatch.FilePath, @"\Aging Plots\");
            Directory.CreateDirectory(string.Concat(agingPath, @"\L-J-V\"));
            Directory.CreateDirectory(string.Concat(agingPath, @"\J-V\"));
            Directory.CreateDirectory(string.Concat(agingPath, @"\EQE-L\"));
            Directory.CreateDirectory(string.Concat(agingPath, @"\EQE-J\"));
            Directory.CreateDirectory(string.Concat(agingPath, @"\EL Spectra\"));

            foreach (Device d in DBVM.TheDeviceBatch.Devices)
            {
                plotVM = new DevicePlotVM(d);
                plotVM.ActiveUserControl = plotVM.ControlsDict["Aging"];

                foreach (Pixel p in d.Pixels)
                {
                    plotVM.SelectedPixel = p;
                    plotVM.LJVPlotVM1.SaveLJVPlotBitmap(string.Concat(agingPath, @"\L-J-V\", d.Label, "_", p.Site, ".jpg"));
                    plotVM.LJVPlotVM1.SaveJVPlotBitmap(string.Concat(agingPath, @"\J-V\", d.Label, "_", p.Site, ".jpg"));
                    plotVM.LJVPlotVM1.SaveEQELPlotBitmap(string.Concat(agingPath, @"\EQE-L\", d.Label, "_", p.Site, ".jpg"));
                    plotVM.LJVPlotVM1.SaveEQEJPlotBitmap(string.Concat(agingPath, @"\EQE-J\", d.Label, "_", p.Site, ".jpg"));
                    plotVM.TheELSpecPlotVM.SaveELSpeclotBitmap(string.Concat(agingPath, @"\EL Spectra\", d.Label, "_", p.Site, ".jpg"));
                }
            }

        }
    }

}
