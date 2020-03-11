using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFDeviceBatchCodeFirst;
using DeviceBatchGenerics.Support.Bases;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class LayerTemplateVM : CrudVMBase
    {
        public LayerTemplateVM()
        {
            TheLayerTemplate = new LayerTemplate();
            TheLayerTemplate.Layer = new Layer();
        }
        public LayerTemplate TheLayerTemplate { get; set; }
    }
}
