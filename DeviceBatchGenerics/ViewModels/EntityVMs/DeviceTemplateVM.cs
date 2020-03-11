using EFDeviceBatchCodeFirst;
using DeviceBatchGenerics.Support.Bases;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class DeviceTemplateVM : CrudVMBase
    {
        public DeviceTemplateVM()
        {
            TheDeviceTemplate = new DeviceTemplate();
        }
        public DeviceTemplate TheDeviceTemplate { get; set; }
        private int _numberOfCopies = 2;
        public int NumberOfCopies
        {
            get
            {
                return _numberOfCopies;
            }
            set
            {
                _numberOfCopies = value;
            }
        }
    }
}
