using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support.Bases
{
    public class CrudVMBase : NotifyUIBase
    {
        protected CrudVMBase(string connString = "ServerConnectionString")
        {
            ctx = new DeviceBatchContext();
            ctx.Database.Connection.ConnectionString = ConfigurationManager.ConnectionStrings[connString].ConnectionString;
        }
        public CrudVMBase(DeviceBatchContext context)
        {
            System.Diagnostics.Debug.WriteLine("CrudVMBase instantiated");
            ctx = context;
        }
        public DeviceBatchContext ctx;
    }
}
