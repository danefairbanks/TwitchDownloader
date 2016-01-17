using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace TwitchDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Links WebClient with Label and ProgressBar
        private Dictionary<WebClient, StackPanel> _links = new Dictionary<WebClient, StackPanel>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;
            var chunksUrl = App.GetTwitchVideoUrls(Url.Text);

            if (chunksUrl.Length > 1)
            {
                //Video is transcoded
                ChooseQuality chooseQuality = null;
                while (chooseQuality == null || chooseQuality.SelectedIndex == -1)
                {
                    chooseQuality = new ChooseQuality();
                    foreach (var chunk in chunksUrl)
                    {
                        var chunkUrl = new Uri(chunk);
                        chooseQuality.Choices.Children.Add(new RadioButton
                        {
                            Content = Regex.Replace(chunkUrl.Segments[chunkUrl.Segments.Length - 2].TrimEnd('/'), @"\b.", m => m.Value.ToUpper())
                        });
                    }
                    chooseQuality.ShowDialog();
                }

                StartDownload(chunksUrl[chooseQuality.SelectedIndex]);
            }
            else if (chunksUrl.Length == 1)
            {
                StartDownload(chunksUrl[0]);
            }
        }

        private void StartDownload(string url)
        {
            var uid = string.Empty;
            var chunks = App.GetTwitchChunkUrls(url, out uid);

            if (!string.IsNullOrWhiteSpace(uid) && chunks.Length > 0)
            {
                //Setup scratch folder to download too and combine
                var scratch = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), uid);
                if (!Directory.Exists(scratch)) Directory.CreateDirectory(scratch);
                if (File.Exists(Path.Combine(scratch, "list.txt"))) File.Delete(Path.Combine(scratch, "list.txt"));

                //Initialize Main Progressbar and Status
                Status.Tag = scratch;
                MainProgress.Value = 0;
                MainProgress.Minimum = 0;
                MainProgress.Maximum = chunks.Length;
                Status.Content = string.Format("Processed {0} out of {1} chunks...", MainProgress.Value, MainProgress.Maximum);

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += delegate (object s, DoWorkEventArgs args)
                {
                    using (var sw = new StreamWriter(Path.Combine(scratch, "list.txt")))
                    {
                        foreach (var chunk in chunks)
                        {
                            //Get Filename and save into list.txt for use later in ffmpeg
                            var chunkUri = new Uri(chunk);
                            var file = chunkUri.Segments[chunkUri.Segments.Length - 1];
                            sw.WriteLine("file '{0}'", Path.Combine(scratch, file));

                            using (var wc = new WebClient())
                            {
                                //Build Progress UI and Start Download
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    var sp = new StackPanel();
                                    var pb = new ProgressBar();
                                    var lbl = new Label();
                                    pb.Minimum = 0;
                                    pb.Maximum = 100;
                                    lbl.Content = string.Format("Connecting to {0} ...", file);
                                    lbl.Tag = file;
                                    sp.Children.Add(lbl);
                                    sp.Children.Add(pb);
                                    ProgressStack.Children.Add(sp);
                                    _links.Add(wc, sp);
                                }));
                                wc.DownloadFileCompleted += DownloadFileCompleted;
                                wc.DownloadProgressChanged += DownloadProgressChanged;
                                wc.DownloadFileAsync(new Uri(chunk), Path.Combine(scratch, file));

                                //Wait if there are too many downloads going on
                                int i = 6;
                                this.Dispatcher.Invoke((Action)(() => { i = ProgressStack.Children.OfType<StackPanel>().Count(); }));
                                while (i >= 5)
                                {
                                    Thread.Sleep(100);
                                    this.Dispatcher.Invoke((Action)(() => { i = ProgressStack.Children.OfType<StackPanel>().Count(); }));
                                }
                            }
                        }
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Update UI with progress of download
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var sp = _links[sender as WebClient];
                var lbl = sp.Children.OfType<Label>().First();
                var pb = sp.Children.OfType<ProgressBar>().First();
                lbl.Content = string.Format("{0} downloaded {1} of {2} bytes. {3} % complete...",
                    lbl.Tag,
                    e.BytesReceived,
                    e.TotalBytesToReceive,
                    e.ProgressPercentage);
                pb.Value = e.ProgressPercentage;
            }));
        }

        /// <summary>
        /// Download Complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                //Remove Progress bar and Update Global Status
                var sp = _links[sender as WebClient];
                ProgressStack.Children.Remove(sp);
                _links.Remove(sender as WebClient);
                Status.Content = string.Format("Processed {0} out of {1} chunks...", ++MainProgress.Value, MainProgress.Maximum);

                //If all downloads have been processed, use ffmpeg to combine chunks
                if(MainProgress.Value == MainProgress.Maximum)
                {
                    var url = new Uri(Url.Text);
                    var vodId = url.Segments[url.Segments.Length - 1];
                    var psi = new System.Diagnostics.ProcessStartInfo("ffmpeg.exe", string.Format("-y -f concat -i {0}\\list.txt -bsf:a aac_adtstoasc -c copy {1}\\v{2}.mp4", Status.Tag, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), vodId));
                    var proc = new System.Diagnostics.Process();
                    psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    proc.StartInfo = psi;
                    proc.Start();
                    proc.WaitForExit();

                    //Delete Scratch Data
                    Directory.Delete((string)Status.Tag, true);

                    //Reset Program
                    MainProgress.Value = 0;
                    MainProgress.Maximum = 0;
                    Status.Tag = null;
                    Status.Content = "Ready";
                    DownloadButton.IsEnabled = true;
                    Url.Text = string.Empty;
                }
            }));
        }
    }
}
