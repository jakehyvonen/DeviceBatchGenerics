using System.Windows;
using System.Windows.Controls;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support
{
    public class LayerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultDataTemplate { get; set; }
        public DataTemplate PatternedTCODataTemplate { get; set; }
        public DataTemplate SpinCoatedLayerDataTemplate { get; set; }
        public DataTemplate ThermallyEvaporatedLayerDataTemplate { get; set; }
        public DataTemplate EncapsulationDataTemplate { get; set; }
        public DataTemplate SputteringDataTemplate { get; set; }
        public DataTemplate IJPDataTemplate { get; set; }
        public DataTemplate UnknownDataTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container)
        {
            if (item != null)
            {
                //Debug.WriteLine("Selecting template for item: " + item.ToString());

                var layer = (Layer)item;
                if (layer.DepositionMethod == null)
                    return UnknownDataTemplate;
                if (layer.DepositionMethod.Name == "Thermal Evaporation")
                    return ThermallyEvaporatedLayerDataTemplate;
                if (layer.DepositionMethod.Name == "Spincoating")
                    return SpinCoatedLayerDataTemplate;
                if (layer.DepositionMethod.Name == "TCO Substrate")
                    return PatternedTCODataTemplate;
                if (layer.DepositionMethod.Name == "Manual Pipetting")
                    return EncapsulationDataTemplate;
                if (layer.DepositionMethod.Name == "Sputtering")
                    return SputteringDataTemplate;
                if (layer.DepositionMethod.Name == "Inkjet Printing")
                    return IJPDataTemplate;

                /*
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
                */
            }
            return DefaultDataTemplate;
        }
    }

}
