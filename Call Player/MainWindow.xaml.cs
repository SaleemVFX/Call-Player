using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

namespace Call_Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IComponentConnector
    {
        private string folderDest;

        private string sel_file;

        private double curr;

        private double total;

        private WaveOutEvent outputDevice;

        private AudioFileReader audioFile;

        public MainWindow()
        {
            InitializeComponent();
            LoadFolder();
        }

        public class CallInfo
        {
            public string FileName { get; set; }

            public string Date { get; set; }

            public string Caller { get; set; }

            public string CallDate { get; set; }

            public string Calltime { get; set; }

            public string FullPath { get; set; }
        }
        List<CallInfo> calls = new List<CallInfo>();

        private void LoadFolder()
        {
            string temp2 = " C:\\Users\\Saleem\\Desktop\\testcalls";
            DirectoryInfo folder = new DirectoryInfo(temp2);
            if (folder.Exists)
            {
                FileInfo[] files = folder.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temp = file.Name;
                    string fileN = file.Name;
                    temp = Regex.Replace(temp, "\\bCall recording\\b", "", RegexOptions.IgnoreCase);
                    string[] split = temp.Split('_');
                    string time = Regex.Replace(split[2], "\\b.m4a\\b", "");
                    string dateN = file.LastWriteTime.ToString("dd-MM-yyyy");
                    string path = file.FullName.ToString();
                    calls.Add(new CallInfo
                    {
                        FileName = fileN,
                        Date = dateN,
                        Caller = split[0].Trim(),
                        CallDate = split[1],
                        Calltime = time,
                        FullPath = path
                    });
                }
                dataGrid.ItemsSource = calls;
                dataGrid.Items.Refresh();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory
            };
            DialogResult dlg = folderBrowser.ShowDialog();
            if (dlg == System.Windows.Forms.DialogResult.OK)
            {
                folderDest = folderBrowser.SelectedPath;
                Trace.WriteLine("path - " + folderDest);
                call_dest.Content = folderDest;
            }
            LoadFolder();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            curr = total + 2.0;
            progressBr.Dispatcher.Invoke(() => progressBr.Value = 0.0);
            outputDevice?.Stop();
        }

        private async void btn_play_Click(object sender, RoutedEventArgs e)
        {
            total = 0.0;
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (audioFile == null && sel_file != null)
            {
                Trace.WriteLine(sel_file);
                audioFile = new AudioFileReader(sel_file);
                Trace.WriteLine(audioFile.TotalTime);
                total = audioFile.TotalTime.TotalSeconds;
                outputDevice.Init(audioFile);
                outputDevice.Play();
                await Task.Run(delegate
                {
                    curr = 0.0;
                    progressBr.Dispatcher.Invoke(() => progressBr.Maximum = total);
                    while (curr < total)
                    {
                        curr = audioFile.CurrentTime.TotalSeconds;
                        progressBr.Dispatcher.Invoke(() => progressBr.Value = curr);
                        Trace.WriteLine(curr.ToString());
                    }
                });
            }
            Trace.WriteLine(total.ToString());
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            outputDevice.Dispose();
            outputDevice = null;
            audioFile.Dispose();
            audioFile = null;
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.DataGrid dataGrid = sender as System.Windows.Controls.DataGrid;
            DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(dataGrid.SelectedIndex);
            System.Windows.Controls.DataGridCell cell = dataGrid.Columns[2].GetCellContent(row).Parent as System.Windows.Controls.DataGridCell;
            System.Windows.Controls.DataGridCell cell2 = dataGrid.Columns[5].GetCellContent(row).Parent as System.Windows.Controls.DataGridCell;
            System.Windows.Controls.DataGridCell cell3 = dataGrid.Columns[0].GetCellContent(row).Parent as System.Windows.Controls.DataGridCell;
            string sel_name = ((TextBlock)cell.Content).Text;
            string filepath = ((TextBlock)cell2.Content).Text;
            string filenanme = ((TextBlock)cell3.Content).Text;
            sel_file = filepath;
            Trace.WriteLine(sel_file);
            Trace.WriteLine("Data: " + sel_name);
            caller_txtBx.Content = sel_name;
            file_txtBx.Content = filenanme;
        }
    }
}
