using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NextionUploader.Nextion
{
    public class Uploader
    {
        public static readonly int[] baudrates = new int[] { 115200, 230400, 250000, 256000, 512000, 921600, 9600, 4800, 19200, 38400, 57600, 2400 };

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
        public bool CancelRequested { get; private set; }

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
            com1.BaseStream.Flush();
            com1.Write(messageEnd, 0, 3);
        }

        public unsafe void SendDisableLedsInstruction(SerialPort com1)
        {
            SendInstruction(com1, "page 0");

            string instruction = "dleds";
            byte[] messageEnd = new byte[] { 255, 255, 255 };

            var en = Encoding.GetEncoding("ascii");

            string text = instruction.Trim();

            com1.Write(messageEnd, 0, 3);
            if (text.Length > 0)
            {
                byte[] bytes = en.GetBytes(text);
                com1.Write(bytes, 0, bytes.Length);
            }
            com1.Write(messageEnd, 0, 3);

            Thread.Sleep(100);
            var t = com1.ReadExisting();
        }

        public void Upload(string ComPort, byte[] data, int uploadBaudrate = 115200, bool reset = false)
        {
            UploadProgress?.Invoke(this, 0);

            this.SerialPort = new SerialPort(ComPort);
            UploadStatus res = UploadStatus.Ok;
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
                    var firmwareVersion = int.Parse(model.FirmwareVersion);

                    Message?.Invoke(this, "Nextion found " + model.Model + " (Firmware version : " + model.FirmwareVersion + ")");

                    if (reset)
                    {
                        Message?.Invoke(this, "Resetting nextion");
                        SendInstruction(SerialPort, "rest");
                        Message?.Invoke(this, "Waiting 4 seconds ...");
                        for (int i = 0; i < 8; i++)
                        {
                            if (CancelRequested) return;

                            Thread.Sleep(500);
                        }
                        SerialPort.ReadExisting();
                    }

                    if (firmwareVersion < 130 && uploadBaudrate > 115200)
                    {
                        Message?.Invoke(this, "Nextion firmware does not supports high speed upload, forcing 115200bauds");
                        SwitchToUploadMode(data, 115200);
                    }
                    else if (model.Model[6] == 'T' && uploadBaudrate > 115200)
                    {
                        Message?.Invoke(this, "Basic model does not supports high speed upload, forcing 115200bauds");
                        SwitchToUploadMode(data, 115200);
                    }
                    else
                    {
                        SwitchToUploadMode(data, uploadBaudrate);
                    }

                    res = UploadData(data);
                }
                else
                {
                    if (!CancelRequested)
                    {
                        Message?.Invoke(this, "Nextion not found");
                    }
                }
            }
            finally
            {
                try
                {
                    if (CancelRequested)
                    {
                        Message?.Invoke(this, "Upload canceled");
                    }

                    SerialPort.Close();
                    Message?.Invoke(this, "Resetting arduino micro if required");
                    ResetMicro(ComPort);

                    if (res == UploadStatus.Ok)
                        Message?.Invoke(this, "Upload finished successfully.");
                }
                catch
                {
                }
            }
        }

        internal void Cancel()
        {
            this.CancelRequested = true;
        }

        private static void ResetMicro(string ComPort)
        {
            var sp = new SerialPort(ComPort);
            sp.BaudRate = 1200;
            sp.DtrEnable = true;
            sp.RtsEnable = true;
            Thread.Sleep(1000);
            sp.Close();
        }

        private NextionInfos DetectNextion()
        {
            foreach (var baudrate in baudrates)
            {
                if (CancelRequested) return null;

                try
                {
                    Message?.Invoke(this, "Trying " + SerialPort.PortName + ", baudrate " + baudrate.ToString());
                    this.SerialPort.Close();
                    this.SerialPort.BaudRate = baudrate;
                    this.SerialPort.WriteBufferSize = 10000;
                    this.SerialPort.ReadBufferSize = 10000;
                    this.SerialPort.Open();
                    this.SerialPort.DtrEnable = true;
                    this.SerialPort.RtsEnable = true;
                    this.SerialPort.WriteTimeout = 5000;
                    this.SerialPort.ReadTimeout = 500;

                    Thread.Sleep(400);
                    if (CancelRequested) return null;
                    ClearReadBuffer();
                    SendDisableLedsInstruction(SerialPort);

                    Thread.Sleep(400);
                    if (CancelRequested) return null;
                    ClearReadBuffer();
                    for (int i = 0; i < 50; i++)
                    {
                        if (CancelRequested) return null;
                        SendInstruction(SerialPort, "");
                    }

                    Thread.Sleep(400);
                    if (CancelRequested) return null;
                    ClearReadBuffer();

                    SendInstruction(SerialPort, "DRAKJHSUYDGBNCJHGJKSHBDN");
                    Thread.Sleep(100);
                    if (CancelRequested) return null;
                    SerialPort.ReadExisting();
                    SendInstruction(SerialPort, "connect");
                    SendInstruction(SerialPort, (char)0x255 + (char)0x255 + "connect");

                    var delay = 1800000 / baudrate + 50;
                    if (CancelRequested) return null;
                    Thread.Sleep(delay);
                    if (CancelRequested) return null;
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
                catch { }
            }
            return null;
        }

        private void SwitchToUploadMode(byte[] data, int uploadBaudrate)
        {
            Message?.Invoke(this, "Switching to upload mode");

            SendInstruction(SerialPort, "runmod=2");
            Thread.Sleep(100);
            var res = SerialPort.ReadExisting();

            SendInstruction(SerialPort, "print \"mystopyesABC\"");
            Thread.Sleep(100);
            res = SerialPort.ReadExisting();

            SendInstruction(SerialPort, "get dim");
            SendInstruction(SerialPort, "print \"ABC\"");

            Thread.Sleep(100);
            res = SerialPort.ReadExisting();

            SendInstruction(SerialPort, "00");

            SendInstruction(SerialPort, string.Format("whmi-wri {0},{1},0", data.Length, uploadBaudrate));
            SerialPort.ReadTimeout = 5000;
            SerialPort.BaseStream.Flush();
            Thread.Sleep(1000);

            var t = SerialPort.ReadExisting();

            SerialPort.BaudRate = uploadBaudrate;
        }

        private enum UploadStatus
        {
            Ok,
            Cancel,
            Error
        }

        private UploadStatus UploadData(byte[] data)
        {
            SerialPort.ReadTimeout = 2000;
            SerialPort.WriteTimeout = 5000;
            try
            {
                if (CancelRequested) return UploadStatus.Cancel;

                Message?.Invoke(this, "Starting upload");
                for (var i = 0; i < Math.Ceiling((double)data.Length / 4096d); i++)
                {
                    if (CancelRequested) return UploadStatus.Cancel;

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
                        return UploadStatus.Error;
                    }

                    UploadProgress?.Invoke(this, (double)i / Math.Ceiling((double)data.Length / 4096d));
                }
            }
            catch
            {
                Message?.Invoke(this, "Upload error");
                return UploadStatus.Error;
            }

            Message?.Invoke(this, "Upload finished");
            return UploadStatus.Ok;
        }
    }
}