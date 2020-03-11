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
    public class PR650Controller : PRCameraController
    {
        /*extending the base class is cumbersome, but keeping this for reference

        public PR650Controller()
        {
            Initialize("\x0D","\x0D","B3");
        }
        public override void Initialize(string initResponse, string response, string initCommand, string comport = "COM2", int baud = 9600)
        {
            base.Initialize(initResponse, response, initCommand, comport, baud);
            EstablishConnection();
        }
        public override void EstablishConnection()
        {
            if(SendCommandAndWaitForResponse(InitialCommand,3333).Result == TimeOutResponse)
            {
                System.Windows.MessageBox.Show("Please turn on the PR650");
                EstablishConnection();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PR650 ACKed");
            }
            //base.EstablishConnection();
        }
        */
    }
}
