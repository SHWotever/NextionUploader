using NextionUploader.Nextion;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;

namespace NextionUploader
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowModel Model;

        private bool isUploading = false;

        public MainWindow()
        {
            InitializeComponent();

            string exeDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(exeDir);

            Model = new MainWindowModel();
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
            isUploading = true;
            try
            {
                Model.MessageLog = "";

                if (Model.SelectedComPort == null)
                {
                    MessageBox.Show("Please select a serial port");
                    return;
                }
                else if (Model.SelectedFile == null)
                {
                    MessageBox.Show("Please select a template");
                    return;
                }

                await Task.Run(() =>
                {
                    var uploader = new Uploader();
                    uploader.Message += Uploader_Message;
                    uploader.UploadProgress += Uploader_UploadProgress;
                    uploader.Upload(Model.SelectedComPort, File.ReadAllBytes(Model.SelectedFile));
                });
            }
            finally
            {
                btnUpload.IsEnabled = true;
                isUploading = false;
            }
        }

     

        private void Uploader_Message(object sender, string e)
        {
            Model.MessageLog += e + "\r\n";
        }

        private void Uploader_UploadProgress(object sender, double e)
        {
            Model.UploadProgress = e;
        }
    }
}