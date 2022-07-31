using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
        private string folderDest,sel_file;

        private double curr,total;

        private WaveOutEvent outputDevice;

        private AudioFileReader audioFile;

        public MainWindow()
        {
            InitializeComponent();
            CheckPref();
        }
        
        //Class for creating a list based on this.
        public class CallInfo 
        {
            public string FileName { get; set; }

            public string Date { get; set; }

            public string Caller { get; set; }

            public string CallDate { get; set; }

            public string Calltime { get; set; }

            public string FullPath { get; set; }
        }
        // The said list for storing data 
        ObservableCollection<CallInfo> calls = new ObservableCollection<CallInfo>();

        //Gets the list of callers and stores them in the combo box for user to filter
        //Clear and reloaded for when the user selects a new folder
        private void CallerList()
        {
            dataGrid.SelectedIndex = -1;
            callerComboBx.SelectedIndex = -1;
            callerComboBx.Items.Clear();
            callerComboBx.Items.Add("");
            var callers = calls.Select(x => x.Caller).Distinct();
            foreach (var call in callers)
            {
                Trace.WriteLine(call);
                callerComboBx.Items.Add(call);
            }

        }

        //Loads the selected user folder.
        //Looks for the call files with certin names.
        //Breaks up the file name for later use.
        //Updates the DataGrid when complete.
        private void LoadFolder()
        {
            //string temp2 = " C:\\Users\\Saleem\\Desktop\\testcalls";
            //DirectoryInfo folder = new DirectoryInfo(temp2);

            DirectoryInfo folder = new DirectoryInfo(folderDest);   // for final 

            if (folder.Exists)
            {
                FileInfo[] files = folder.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temp = file.Name;
                    string fileN = file.Name;
                    if (fileN.Contains("Call recording"))
                    {
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

                }
                dataGrid.ItemsSource = calls;
                dataGrid.Items.Refresh();
                CallerList(); //Loads the Combo Box
            }
        }

        //Opens UI For user to select the desired folder they want to read from.
        //Save the user folder for next time they open the software.
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
            if (checkBox.IsChecked == true)
            {
                Trace.WriteLine("Checked");
                SavePref(folderDest, true);
            }
            //dataGrid.ItemsSource 
            calls.Clear();
            LoadFolder(); //Loads the selected folder

        }

        //Stop button function 
        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            curr = total + 2.0; //idk why i put this
            progressBr.Dispatcher.Invoke(() => progressBr.Value = 0.0); //supposed to clear progress bar not working ?
            outputDevice?.Stop();
        }

        //Play button function, Also it updates the progress bar
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
                //Progress bar async task 
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

        //Stops the current file, clear audio device and progress bar.
        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            outputDevice.Dispose();
            outputDevice = null;
            audioFile.Dispose();
            audioFile = null;
            progressBr.Dispatcher.Invoke(() => progressBr.Value = 0);
        }

        //Takes the selected item on the GridView and saves it for later use of loading files and player.
        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedIndex != -1)//Chec
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

        //Filters Callers with the Dropdown box.
        //Happens when the selection is changed.
        private void callerComboBx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Checkes the index so that it won't throw no blasted error.
            if (callerComboBx.SelectedIndex != -1)
            {
                string search = callerComboBx.SelectedValue.ToString(); //Gets the value of the selected dropdown
                ICollectionView collectionView = CollectionViewSource.GetDefaultView(calls); // Saves the call list into this collection view so i can filter it
                var filter1 = new Predicate<Object>(item => ((CallInfo)item).Caller.Contains(search)); //Uses some shit to filter the code.
                collectionView.Filter = filter1; // creates the filter 
                dataGrid.ItemsSource = collectionView; //apply the new filter list to the datagrid in the view

                Trace.WriteLine(callerComboBx.SelectedValue); //Output to Console

            }
        }

        //Save Function - Stores the folder path or clears it
        private void SavePref(string dest, bool s)
        {

            if (s == true)
            {
                Trace.WriteLine(Properties.Settings.Default["Folder"]);
                Properties.Settings.Default["Folder"] = dest;
                Properties.Settings.Default.Save();
                Trace.WriteLine(Properties.Settings.Default["Folder"]);
            }
            else
            {
                Trace.WriteLine(Properties.Settings.Default["Folder"]);
                Properties.Settings.Default["Folder"] = null;
                Properties.Settings.Default.Save();
                Trace.WriteLine(Properties.Settings.Default["Folder"]);
            }
        }

        //Check Function - Checks the Settings if the user saved the folder path then calls the LoadFolder
        private void CheckPref()
        {
            var folder = Properties.Settings.Default["Folder"];

            if (folder == null || folder == "")
            {
                Trace.WriteLine("True");
                call_dest.Content = "Select a folder";
                checkBox.IsChecked = false;
            }
            else
            {
                Trace.WriteLine("False");
                checkBox.IsChecked = true;
                call_dest.Content = folder;
                folderDest = folder.ToString();
                LoadFolder();
            }

        }
        //When the checkbox is clicked on it checks to see the state and perform a save or clear.
        private void checkBox_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Click?");
            if (checkBox.IsChecked == true)
            {
                Trace.WriteLine("Checked");
                SavePref(folderDest, true);
            }
            else
            {
                Trace.WriteLine("Un-Checked");
                SavePref("", false);
            }
        }
    }
}