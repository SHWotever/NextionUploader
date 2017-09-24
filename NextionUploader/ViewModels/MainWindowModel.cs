using NextionUploader.Framework;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Threading;

namespace NextionUploader
{
    public class MainWindowModel : PropertyChangedBase
    {
        private DispatcherTimer refreshTimer;

        public MainWindowModel()
        {
            ComPorts = new SortedObservableCollection<string, string>(i => i);
            Files = new SortedObservableCollection<string, string>(i => i);

            RefreshComPorts();
            RefreshFiles();

            this.refreshTimer = new DispatcherTimer();
            this.refreshTimer.Interval = TimeSpan.FromSeconds(0.5);
            this.refreshTimer.Tick += RefreshTimer_Tick;
            this.refreshTimer.IsEnabled = true;
        }

        public SortedObservableCollection<string, string> ComPorts
        {
            get { return GetPropertyValue<SortedObservableCollection<string, string>>(); }
            set { SetPropertyValue(value); }
        }

        public SortedObservableCollection<string, string> Files
        {
            get { return GetPropertyValue<SortedObservableCollection<string, string>>(); }
            set { SetPropertyValue(value); }
        }

        public string MessageLog
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string SelectedComPort
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string SelectedFile
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public double UploadProgress
        {
            get { return GetPropertyValue<double>(); }
            set { SetPropertyValue(value); }
        }

        private void RefreshComPorts()
        {
            var coms = SerialPort.GetPortNames();

            foreach (var p in coms)
            {
                if (!ComPorts.Contains(p))
                {
                    ComPorts.AddSorted(p);
                }
            }

            foreach (var p in ComPorts.ToList())
            {
                if (!coms.Contains(p))
                {
                    ComPorts.Remove(p);
                }
            }

            if (SelectedComPort == null && ComPorts.Count > 0)
            {
                SelectedComPort = ComPorts.First();
            }
        }

        private void RefreshFiles()
        {
            var files = Directory.GetFiles(".", "*.tft");

            foreach (var p in files)
            {
                Files.AddSorted(System.IO.Path.GetFileName(p));
            }

            if (SelectedFile == null && Files.Count > 0)
            {
                SelectedFile = Files.First();
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            this.RefreshComPorts();
        }
    }
}