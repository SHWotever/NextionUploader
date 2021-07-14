using NextionUploader.Nextion;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace NextionUploader
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowModel Model;

        private bool isUploading = false;

        public Action Cancel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(exeDir);

            Model = new MainWindowModel();

            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                if (File.Exists(args[1]) && Path.GetExtension(args[1]).Equals(".tft", StringComparison.InvariantCultureIgnoreCase))
                {
                    Model.PickedFile = args[1];
                    Model.ShowFileList = Visibility.Hidden;
                }
            }
            this.DataContext = Model;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (isUploading)
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            btnUpload.IsEnabled = false;
            btnCancel.IsEnabled = true;

            isUploading = true;

            Model.MessageLog = "";

            if (Model.SelectedComPort == null)
            {
                System.Windows.MessageBox.Show("Please select a serial port");
                return;
            }
            else if (Model.SelectedFile == null && Model.PickedFile == null)
            {
                System.Windows.MessageBox.Show("Please select a template");
                return;
            }

            var t = new Thread(() =>
            {
                var uploader = new Uploader();
                this.Cancel = () => uploader.Cancel();
                uploader.Message += Uploader_Message;
                uploader.UploadProgress += Uploader_UploadProgress;
                uploader.Upload(Model.SelectedComPort, File.ReadAllBytes(Model.PickedFile ?? Model.SelectedFile), Model.UploadBaudRate, Model.ResetNextionAtUpload);

                Dispatcher.Invoke(() =>
                {
                    Cancel = null;
                    btnUpload.IsEnabled = true;
                    btnCancel.IsEnabled = false;
                    isUploading = false;
                });
            });
            t.Start();
        }

        private void Uploader_Message(object sender, string e)
        {
            Dispatcher.Invoke(() =>
            {
                Model.MessageLog += e + "\r\n";
            });
        }

        private void Uploader_UploadProgress(object sender, double e)
        {
            Dispatcher.Invoke(() =>
            {
                Model.UploadProgress = e;
            });
        }

        private void btnFileChoose_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".tft";
            dlg.Filter = "TFT Files (*.tft)|*.tft";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                Model.PickedFile = dlg.FileName;
                Model.ShowFileList = Visibility.Collapsed;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var c = Cancel;
            if (c != null)
            {
                var dialogResult = System.Windows.MessageBox.Show("Are you sure to want to cancel upload ?", "Cancel", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                    Cancel();
            }
        }
    }
}