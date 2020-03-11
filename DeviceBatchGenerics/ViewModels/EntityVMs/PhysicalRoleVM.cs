using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFDeviceBatchCodeFirst;
using DeviceBatchGenerics.Support.Bases;

namespace DeviceBatchGenerics.ViewModels.EntityVMs
{
    public class PhysicalRoleVM : CrudVMBase
    {
        public PhysicalRole ThePhysicalRole { get; set; }
        public PhysicalRoleVM()
        {
            ThePhysicalRole = new PhysicalRole();
        }
    }
}
