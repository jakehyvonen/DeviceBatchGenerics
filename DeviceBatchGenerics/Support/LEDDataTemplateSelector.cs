using System;
using System.Windows;
using System.Windows.Controls;
using EFDeviceBatchCodeFirst;
using DeviceBatchGenerics.ViewModels.EntityVMs;

namespace DeviceBatchGenerics.Support
{
    public class LEDDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultDataTemplate { get; set; }
        public DataTemplate LJVScanDataTemplate { get; set; }
        public DataTemplate ELSpectrasDataTemplate { get; set; }
        public DataTemplate LifetimesDataTemplate { get; set; }
        public DataTemplate LJVScanSummaryVMDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container)
        {
            Type itemType = System.Data.Entity.Core.Objects.ObjectContext.GetObjectType(item.GetType());


            if (itemType == typeof(LJVScan))
            {
                return LJVScanDataTemplate;
            }
            if (itemType == typeof(ELSpectrum))
            {
                return ELSpectrasDataTemplate;
            }
            if (itemType == typeof(LifetimeVM))
            {
                return LifetimesDataTemplate;
            }
            if (itemType == typeof(LJVScanSummaryVM))
                return LJVScanSummaryVMDataTemplate;//:P

            return DefaultDataTemplate;
        }
    }

}
