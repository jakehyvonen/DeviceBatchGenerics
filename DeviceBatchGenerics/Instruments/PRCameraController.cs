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
    public class PRCameraController
    {
        bool isInitializing = true;
        bool isRecordingELSpec = false;
        ManualResetEvent[] dataReceivedEvent = new ManualResetEvent[1] { new ManualResetEvent(false) };
        SerialPort serialPort;
        public EventHandler DataToParse;
        public bool DataReceivedBool = false;
        public bool ExceededMeasurementRange = false;
        public string ReceivedData;
        public List<ELSpecDatum> PresentELSpec = new List<ELSpecDatum>();
        public string InitialSerialResponseTerminator;
        public string SerialResponseTerminator;
        public string InitialCommand;
        public string TimeOutResponse = "timed out";

        public void Initialize(string initResponse, string response, string initCommand, string comport = "COM2", int baud = 9600)
        {
            SetupSerialPort(comport, baud);
            InitialSerialResponseTerminator = initResponse;
            SerialResponseTerminator = response;
            InitialCommand = initCommand;
            EstablishConnection();
        }
        private void SetupSerialPort(string comport, int baud)
        {
            try
            {
                serialPort = new SerialPort(comport, baud, Parity.None, 8, StopBits.One);
                serialPort.Open();
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
                serialPort.DataReceived += TheSerialPort_DataReceived;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
        public async void EstablishConnection()
        {
            string response = await SendCommandAndWaitForResponse(InitialCommand, 1111);
            Debug.WriteLine("EstablishConnection response: " + response);
            response = await SendCommandAndWaitForResponse(InitialCommand, 1111);
            Debug.WriteLine("EstablishConnection response: " + response);
            if (response == TimeOutResponse)
            {
                System.Windows.MessageBox.Show("Please turn on the PhotoResearch Camera");
                EstablishConnection();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PRCamera ACKed");
            }
        }
        #region Measurement Tasks
        public async Task<PRCamRawLuminanceDatum> LuminanceMeasurement()
        {
            return ParseM1String(await SendCommandAndWaitForResponse("M1"));
        }
        public async Task<List<ELSpecDatum>> ELSpecMeasurement(bool usingM1Reading = false)
        {
            isRecordingELSpec = true;
            if (usingM1Reading)
              await SendCommandAndWaitForResponse("D5");//D5 doesn't take a new measurement, only fetches Radiance data
            else
              await SendCommandAndWaitForResponse("M5");//take measurement and return radiance curve
            return PresentELSpec;
        }
        public async Task<string> SendCommandAndWaitForResponse(string command, int timeoutms = 33333)
        {
            if (command.Substring(command.Length - 1) == "5")//if the last character is 5, we should expect a spectrum
            {
                
                Debug.WriteLine("Recording EL Spec");
                //serialPort.DiscardInBuffer();
                await Task.Delay(111);
            }
            return await Task.Run(() =>
            {
                Debug.WriteLine("Sending command to PR650: " + command);
                string response = TimeOutResponse;
                dataReceivedEvent = new ManualResetEvent[1] { new ManualResetEvent(false) };
                Debug.WriteLine("isRecordingELSpec1 = " + isRecordingELSpec);
                SendCommand(command);               
                var eventResponse = WaitHandle.WaitAny(dataReceivedEvent, timeoutms);
                if (eventResponse != WaitHandle.WaitTimeout)
                    response = ReceivedData;
                Debug.WriteLine("response: " + response);
                return response;
            }
            );
        }
        private Task SendCommand(string command)
        {
            return Task.Run(() =>
            {
                DataReceivedBool = false;
                byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                commandBytes = addByteToEndOfArray(commandBytes, 0x0D);//0x0D=carriage return in ASCII
                serialPort.Write(commandBytes, 0, commandBytes.Count());
            }
            );
        }
        #endregion
        private void TheSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Debug.WriteLine("isRecordingELSpec2 = " + isRecordingELSpec);
            if (isInitializing)
            {
                ReceivedData = serialPort.ReadTo(InitialSerialResponseTerminator); //read until MODE after initialization command
                Debug.WriteLine("Initial ReceivedData: " + ReceivedData);
                serialPort.ReadExisting();//clear the buffer
                serialPort.DiscardInBuffer();//maybe this is more appropriate
                if (ReceivedData.Count() > 2)
                {
                    isInitializing = false;
                    Debug.WriteLine("done initializing");
                }
            }
            else if (isRecordingELSpec)
            {
                Debug.WriteLine("Began recording EL Spectrum");                
                PresentELSpec = new List<ELSpecDatum>();
                ReceivedData = serialPort.ReadTo(SerialResponseTerminator); //read until CR LF (0x0D 0x0A)
                ReceivedData = serialPort.ReadTo(SerialResponseTerminator); //read until CR LF (0x0D 0x0A)
                bool reached780nm = false;
                while (!reached780nm)
                {
                    string specPoint = serialPort.ReadTo(SerialResponseTerminator);
                    Debug.WriteLine("specPoint: " + specPoint);
                    ELSpecDatum datum = ParsedSpecString(specPoint);
                    PresentELSpec.Add(datum);
                    if (datum.Wavelength == 780)
                    {
                        reached780nm = true;
                        isRecordingELSpec = false;
                        Debug.WriteLine("Successfully recorded EL Spectrum");
                    }
                }
            }
            else
            {
                ReceivedData = serialPort.ReadTo(SerialResponseTerminator); //read until CR LF (0x0D 0x0A)
                Debug.WriteLine("ReceivedData: " + ReceivedData);
            }
            DataReceivedBool = true;
            DataToParse?.Invoke(this, EventArgs.Empty);
            dataReceivedEvent[0].Set();
            serialPort.DiscardInBuffer();
        }
        private byte[] addByteToEndOfArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 0);
            newArray[bArray.Length] = newByte;
            return newArray;
        }
        #region Data Processing
        private PRCamRawLuminanceDatum ParseM1String(string s)
        {
            PRCamRawLuminanceDatum datum = new PRCamRawLuminanceDatum();
            try
            {
                string[] data = s.Split(',');
                if (data[0].Contains("19"))
                {
                    ExceededMeasurementRange = true;
                    Debug.WriteLine("Exceeded camera measurement range");
                }
                else
                {
                    datum.Luminance = Convert.ToDecimal(Convert.ToDouble(data[2]));//can't directly convert to decimal because reasons
                    datum.CIEx = Convert.ToDecimal(data[3]);
                    datum.CIEy = Convert.ToDecimal(data[4]);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return datum;
        }
        private ELSpecDatum ParsedSpecString(string specstring)
        {
            ELSpecDatum datum = new ELSpecDatum();
            try
            {
                string[] array = specstring.Split(',');
                datum.Wavelength = Convert.ToDouble(array[0]);
                datum.Intensity = Convert.ToDouble(array[1]);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("problem with data format: " + e.ToString());
            }
            return datum;
        }
        #endregion
    }
}
