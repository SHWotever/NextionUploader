using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NextionUploader.Nextion
{
    public class Uploader
    {
        private int[] baudrates = new int[] { 115200, 9600, 4800, 19200, 38400, 57600, 2400 };

        public Uploader()
        {
            Message += (o, e) =>
            {
                Debug.WriteLine(e);
            };
        }

        public event EventHandler<string> Message;

        public event EventHandler<double> UploadProgress;

        public SerialPort SerialPort { get; private set; }

        public void ClearReadBuffer()
        {
            this.SerialPort.ReadExisting();
        }

        public unsafe void SendInstruction(SerialPort com1, string instruction)
        {
            byte[] messageEnd = new byte[] { 255, 255, 255 };

            var en = Encoding.GetEncoding("ascii");

            string text = instruction.Trim();

            if (text.Length > 0)
            {
                byte[] bytes = en.GetBytes(text);
                com1.Write(bytes, 0, bytes.Length);
            }
            com1.Write(messageEnd, 0, 3);
        }

        public void Upload(string ComPort, byte[] data, int uploadBaudrate = 115200)
        {
            UploadProgress?.Invoke(this, 0);

            this.SerialPort = new SerialPort(ComPort);
            try
            {
                try
                {
                    this.SerialPort.Open();
                }
                catch (Exception ex)
                {
                    Message?.Invoke(this, "Unable to open Serial Port " + ComPort);
                    return;
                }
                NextionInfos model = null;
                try
                {
                    model = DetectNextion();
                }
                catch
                {
                }

                if (model != null)
                {
                    Message?.Invoke(this, "Nextion found " + model.Model);
                    SwitchToUploadMode(data, uploadBaudrate);
                    UploadData(data);
                }
                else
                {
                    Message?.Invoke(this, "Nextion not found");
                }
            }
            finally
            {
                try
                {
                    SerialPort.Close();
                }
                catch
                {
                }
            }
        }

        private NextionInfos DetectNextion()
        {
            foreach (var baudrate in baudrates)
            {
                Message?.Invoke(this, "Trying " + SerialPort.PortName + ", baudrate " + baudrate.ToString());
                this.SerialPort.BaudRate = baudrate;
                Thread.Sleep(400);
                ClearReadBuffer();

                SendInstruction(SerialPort, "00");
                Thread.Sleep(100);
                SerialPort.ReadExisting();
                SendInstruction(this.SerialPort, "connect");

                var delay = 1800000 / baudrate + 50;
                Thread.Sleep(delay);
                try
                {
                    var incoming = SerialPort.ReadExisting().TrimEnd('?');
                    if (incoming.StartsWith("comok"))
                    {
                        return new NextionInfos(incoming);
                    }
                }
                catch { }

                ClearReadBuffer();
            }
            return null;
        }

        private void SwitchToUploadMode(byte[] data, int uploadBaudrate)
        {
            Message?.Invoke(this, "Switching to upload mode");
            SendInstruction(SerialPort, string.Format("whmi-wri {0},{1},res0", data.Length, uploadBaudrate));

            Thread.Sleep(500);

            var t = SerialPort.ReadExisting();

            SerialPort.BaudRate = uploadBaudrate;
        }

        private void UploadData(byte[] data)
        {
            SerialPort.ReadTimeout = 2000;
            try
            {
                Message?.Invoke(this, "Starting upload");
                for (var i = 0; i < Math.Ceiling((double)data.Length / 4096d); i++)
                {
                    SerialPort.Write(data, i * 4096, Math.Min(4096, data.Length - i * 4096));
                    SerialPort.BaseStream.Flush();

                    int d = 0;
                    try
                    {
                        d = SerialPort.ReadByte();
                    }
                    catch { }

                    if (d != 5)
                    {
                        Message?.Invoke(this, "Upload error");
                        return;
                    }

                    UploadProgress?.Invoke(this, (double)i / Math.Ceiling((double)data.Length / 4096d));
                }
            }
            catch
            {
                Message?.Invoke(this, "Upload error");
                return;
            }

            Message?.Invoke(this, "Upload successfull");
        }
    }
}