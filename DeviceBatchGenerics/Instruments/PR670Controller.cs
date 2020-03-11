using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceBatchGenerics.Support.DataMapping;

namespace DeviceBatchGenerics.Instruments
{
    public class PR670Controller : PRCameraController
    {
        /*extending the base class is cumbersome, but keeping this for reference
        public PR670Controller()
        {
            Initialize("MODE", "\x0D\x0A", "PHOTO");
        }
        public override void Initialize(string initResponse, string response, string initCommand, string comport = "COM10", int baud = 115200)
        {
            base.Initialize(initResponse, response, initCommand, comport, baud);
            EstablishConnection();
        }
        public override void EstablishConnection()
        {
            if (SendCommandAndWaitForResponse(InitialCommand, 3333).Result == TimeOutResponse)
            {
                System.Windows.MessageBox.Show("Please turn on the PR670");
                EstablishConnection();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PR670 ACKed");
            }
        }
        */
    }
}
