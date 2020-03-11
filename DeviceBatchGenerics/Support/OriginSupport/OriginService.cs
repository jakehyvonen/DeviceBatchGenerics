using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceBatchGenerics.Support.OriginSupport
{
    public static class OriginService
    {
        static public OriginController OriginController { get; set; }
        static public Task CreateControllerAsync()
        {
            return Task.Run(async () =>
            {
                OriginController = await OriginController.CreateAsync();
            });
        }
    }
}
