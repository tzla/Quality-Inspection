using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Quality_Inspection
{
    /// <summary>
    /// Main page/entry point for program. 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //these two lists allow for using index location for saving data
        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };//line name list 
        SqlConnection conn;
       //ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Morning", "First", "Lunch", "Second" };//Shift list 
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Shift Start", "Hour 1", "First Break", "Hour 3", "Hour 4", "After Lunch", "Hour 6", "Second Break", "Hour 8" };
        ObservableCollection<string> RealShiftSource = new ObservableCollection<string> { "1st", "2nd", "3rd" };



        public SolidColorBrush LSB = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue);//pre-loaded colors for UI
        public SolidColorBrush SB = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
        SolidColorBrush LG = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.White);
        public SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);

        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red);
        SolidColorBrush Orng = new SolidColorBrush(Windows.UI.Colors.Orange);
        SolidColorBrush Yellow = new SolidColorBrush(Windows.UI.Colors.Yellow);
        SolidColorBrush Green = new SolidColorBrush(Windows.UI.Colors.Green);
        SolidColorBrush Blue = new SolidColorBrush(Windows.UI.Colors.Blue);
        SolidColorBrush Purp = new SolidColorBrush(Windows.UI.Colors.Purple);
        SolidColorBrush Indg = new SolidColorBrush(Windows.UI.Colors.Indigo);
        SolidColorBrush Pink = new SolidColorBrush(Windows.UI.Colors.Pink);
        SolidColorBrush Brown = new SolidColorBrush(Windows.UI.Colors.Brown);

        List<CheckBox> DefectList = new List<CheckBox>();//list of checked defects
        QualityCheck newCheck = new QualityCheck();//individual quality report for specific line and shift
        StorageFolder saveHere; //tracks the location for saving data

        List<PartTracker> PartSearcher = new List<PartTracker>(); //used to create index file for search purposes

        InkStrokeContainer[] twoSigs = new InkStrokeContainer[3];//stores the two signatures 

        List<DateTimeOffset> masterList = new List<DateTimeOffset>();//creates a master list of active days for calender search
        PartMaster partMasterList = new PartMaster();//indexes individual quality reports by line number,date, and shift


        /// <summary>
        /// Tracks the line a specific part ran on for run day
        /// </summary>
        public class MyDate
        {
            public DateTimeOffset date { get; set; }
            public int lineNumber { get; set; }
        }

        /// <summary>
        /// Indexes each individual part for search. 
        /// </summary>
        public class PartTracker 
        {
            public string partName { get; set; }
            public List<MyDate> dates { get; set; }
        }

        /// <summary>
        /// Indexes part name and line with corresponding notes for each day. 
        /// </summary>
        public class PartMaster
        {
            public string[] morningList { get; set; } //lists of part names corresponding to line number
            public string[] firstList { get; set; }
            public string[] lunchList { get; set; }
            public string[] secondList { get; set; }

            public bool[] morningNotes { get; set; } //corresponding quality notes 
            public bool[] firstNotes { get; set; }
            public bool[] lunchNotes { get; set; }
            public bool[] secondNotes { get; set; }
        }
        
        bool isLoaded = false; //tracks if program is loaded 


        /// <summary>
        /// This is the individual quality report for a single line for a single block of time
        /// </summary>
        public class QualityCheck 
        {
            public bool sampleMatch { get; set; }
            public bool boxMatch { get; set; }
            public bool lidMatch { get; set; }
            public bool defects { get; set; }
            public DateTimeOffset date { get; set; }
            public int checkNumber { get; set; }
            public string partName { get; set; }
            public int lineName { get; set; }
            public bool[] defectList { get; set; }
            public string[] sigPath { get; set; }
            public string[] initals { get; set; }
            public string notes { get; set; }
        }

        /// <summary>
        /// Home/Data Entry Page
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();//loads UI
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled; //caches page for reloading when navigating to different pages
            LineBox.ItemsSource = LineSource; //links the Line Number box with the list of line names
            LineBox.SelectedIndex = 0; //sets default line to 1
            ShiftBox.ItemsSource = ShiftSource; //links hour box to the list of hour names

            RealShiftBox.ItemsSource = RealShiftSource;//links to SHIFT chooser
            RealShiftBox.SelectedIndex = 0;//automatically choose day/1st

            ShiftBox.SelectedIndex = 0; //selects morning as default
            newCheck.sigPath = new string[3]; //initializes list of file location paths
            newCheck.initals = new string[3]; //initializes list of initials for sign off
            TimeSpan period = TimeSpan.FromSeconds(5);//creates a timer to update clock every 5 seconds

            Time.Text = DateTime.Now.ToString("hh:mm");//loads clock on startup
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) => //run the timer and update
            {

                Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        Time.Text = DateTime.Now.ToString("hh:mm");
                    });

            }, period);

            SetupMasterList();//load data and setup lists
            DefectListSetup();//create list of the defect boxes
            isLoaded = true;//loaded flag is on. prevents unneccessary actions
        }

        /// <summary>
        /// Loads Data for lists OR creates new list if data is unavailable. 
        /// </summary>
        private async void SetupMasterList()
        {
            try //loads master date list
            {
                String JsonFile = "MasterTracker.json";
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                StorageFile localFile = await localFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                masterList = JsonConvert.DeserializeObject(JsonString, typeof(List<DateTimeOffset>)) as List<DateTimeOffset>;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                masterList = new List<DateTimeOffset>();
            }

            try //loads the part indexer for the chosen day
            {
                String JsonFile = "PartMaster_" + dateBox.Date.ToString("yyyy_MM_dd_") + ".json";
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                var projectFolderName = dateBox.Date.ToString("yyyy_MM_dd");
                StorageFolder projectFolder = await localFolder.GetFolderAsync(projectFolderName);
                StorageFile localFile = await projectFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                partMasterList = JsonConvert.DeserializeObject(JsonString, typeof(PartMaster)) as PartMaster;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                partMasterList = new PartMaster();
            }

            try //loads the part tracking index
            {
                String JsonFile = "PartSearch.json";
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                StorageFile localFile = await localFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                PartSearcher = JsonConvert.DeserializeObject(JsonString, typeof(List<PartTracker>)) as List<PartTracker>;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PartSearcher = new List<PartTracker>();
            }

            //if lists dont load, instantiates the daily lists 

            if (partMasterList.firstList == null) { partMasterList.firstList = new string[15]; }
            if (partMasterList.lunchList == null) { partMasterList.lunchList = new string[15]; }
            if (partMasterList.morningList == null) { partMasterList.morningList = new string[15]; }
            if (partMasterList.secondList == null) { partMasterList.secondList = new string[15]; }
            if (partMasterList.firstNotes == null) { partMasterList.firstNotes = new bool[15]; }
            if (partMasterList.lunchNotes == null) { partMasterList.lunchNotes = new bool[15]; }
            if (partMasterList.morningNotes == null) { partMasterList.morningNotes = new bool[15]; }
            if (partMasterList.secondNotes == null) { partMasterList.secondNotes = new bool[15]; }
        }

        /// <summary>
        /// Creates a list of the defect checkboxes for easy access
        /// </summary>
        private void DefectListSetup()
        {
            DefectList.Add(D0); DefectList.Add(D1); DefectList.Add(D2); DefectList.Add(D3); DefectList.Add(D4); DefectList.Add(D5); DefectList.Add(D6); DefectList.Add(D7);
            DefectList.Add(D8); DefectList.Add(D9); DefectList.Add(D10); DefectList.Add(D11);
        }

        /// <summary>
        /// Saves the QA tech signature to sig list
        /// </summary>
        private async void SaveSig(object sender, RoutedEventArgs e)
        {
            SignPopup TechSign = new SignPopup();
            ContentDialogResult result = await TechSign.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                InkStrokeContainer thisStrokes = TechSign.returnSig();
                inkyCanvas.InkPresenter.StrokeContainer = thisStrokes;
                twoSigs[0] = thisStrokes;
            }
        }
        /// <summary>
        /// Saves Diesetter signature to sig list
        /// </summary>
        private async void SaveSigDS(object sender, RoutedEventArgs e)
        {
            SignPopup TechSign = new SignPopup();
            ContentDialogResult result = await TechSign.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                InkStrokeContainer thisStrokes = TechSign.returnSig();
                inkyCanvasDS.InkPresenter.StrokeContainer = thisStrokes;
                twoSigs[1] = thisStrokes;
            }
        }

        /// <summary>
        /// Saves Diesetter signature to sig list
        /// </summary>
        private async void SaveSigSup(object sender, RoutedEventArgs e)
        {
            SignPopup TechSign = new SignPopup();
            ContentDialogResult result = await TechSign.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                InkStrokeContainer thisStrokes = TechSign.returnSig();
                inkyCanvasSup.InkPresenter.StrokeContainer = thisStrokes;
                twoSigs[2] = thisStrokes;
            }
        }
        

        /// <summary>
        /// Processes the sample match checkboxes
        /// </summary>
        private void SampleCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox) //makes one box checked at a time
            {
                SampleCheck_false.IsChecked = false;
            }
            else
            {
                SampleCheck_true.IsChecked = false;
            }
            newCheck.sampleMatch = whichBox;
        }

        /// <summary>
        /// Processes the match package checkboxes
        /// </summary>
        private void PackageCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox) //makes one box checked at a time
            {
                PackageCheck_false.IsChecked = false;
            }
            else
            {
                PackageCheck_true.IsChecked = false;
            }
            newCheck.boxMatch = whichBox;
        }

        /// <summary>
        /// Processes the lid fit checkboxes
        /// </summary>
        private void LidCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox) //makes one box checked at a time
            {
                LidCheck_false.IsChecked = false;
            }
            else
            {
                LidCheck_true.IsChecked = false;
            }
            newCheck.lidMatch = whichBox;
        }

        private void SQLSaver(String thisString)
        {
            using (conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = thisString;
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                            }
                        }
                    }
                    catch { }
                }

            }
        }


        /// <summary>
        /// Processes the defect checkboxes
        /// </summary>
        private void DefectCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox) //makes one box checked at a time
            {
                DefectCheck_false.IsChecked = false;
                DefectGrid.Background = White;
                ToolTip toolTip = new ToolTip();
                ToolTipService.SetToolTip(DefectGrid, null);
                foreach (CheckBox checkLister in DefectList)
                {
                    checkLister.IsEnabled = true;
                }
            }
            else
            {
                DefectCheck_true.IsChecked = false;
                DefectGrid.Background = LG;
                ToolTip toolTip = new ToolTip();
                toolTip.Content = "Check Defect Box To Enable";
                ToolTipService.SetToolTip(DefectGrid, toolTip);
                foreach (CheckBox checkLister in DefectList)
                {
                    checkLister.IsEnabled = false;
                    checkLister.IsChecked = false; //clears defects if part is defect free
                }
            }
            newCheck.defects = whichBox;
        }

        /// <summary>
        /// Create/Open the appropriate folder for saving/loading data
        /// </summary>
        private async Task newFolders()
        {
            StorageFolder rootFolder = KnownFolders.MusicLibrary;
            var projectFolderName = dateBox.Date.ToString("yyyy_MM_dd");
            StorageFolder projectFolder = await rootFolder.CreateFolderAsync(projectFolderName, CreationCollisionOption.OpenIfExists);
            projectFolderName = ShiftBox.SelectedItem as string;
            saveHere = await projectFolder.CreateFolderAsync(projectFolderName, CreationCollisionOption.OpenIfExists);
        }

        /// <summary>
        /// Processes the signature for QA Tech
        /// </summary>
        private async Task SaveSign1()
        {
            string fileName = "TechSig_" + dateBox.Date.ToString("yyyy_MM_dd_") + "_" + RealShiftBox.SelectedIndex + "_" + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".gif";

            StorageFile newFile = await saveHere.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            Windows.Storage.CachedFileManager.DeferUpdates(newFile);
            IRandomAccessStream stream =
                    await newFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            // Write the ink strokes to the output stream.
            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await twoSigs[0].SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            // Finalize write so other apps can update file.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(newFile);
            newCheck.sigPath[0] = fileName;
            //return newFile;
        }

        /// <summary>
        /// Processes the signature for the diesetter
        /// </summary>
        private async Task SaveSign2()
        {
            string fileName = "DSSig_" + dateBox.Date.ToString("yyyy_MM_dd_") + "_" + RealShiftBox.SelectedIndex + "_" + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".gif";

            StorageFile newFile = await saveHere.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            Windows.Storage.CachedFileManager.DeferUpdates(newFile);
            IRandomAccessStream stream =
                    await newFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            // Write the ink strokes to the output stream.
            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await twoSigs[1].SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            // Finalize write so other apps can update file.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(newFile);
            newCheck.sigPath[1] = fileName;
        }

        private async Task SaveSign3()
        {
            string fileName = "SupSig_" + dateBox.Date.ToString("yyyy_MM_dd_") + "_" + RealShiftBox.SelectedIndex + "_" + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".gif";

            StorageFile newFile = await saveHere.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            Windows.Storage.CachedFileManager.DeferUpdates(newFile);
            IRandomAccessStream stream =
                    await newFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            // Write the ink strokes to the output stream.
            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await twoSigs[2].SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            // Finalize write so other apps can update file.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(newFile);
            newCheck.sigPath[2] = fileName;
        }
        /// <summary>
        /// Processes the defect checkboxes for saving data
        /// </summary>
        private bool[] DefectListMaker()
        {
            bool[] list = new bool[15];
            int i = 0;
            foreach (CheckBox thisBox in DefectList)
            {
                list[i] = Convert.ToBoolean(thisBox.IsChecked);
                i++;
            }
            return list;
        }

        /// <summary>
        /// Processes and Saves the entered data
        /// </summary>
        private async void ConfirmData(object sender, RoutedEventArgs e)
        {
            await newFolders();
            newCheck.lineName = LineBox.SelectedIndex;
            newCheck.partName = PartBox.Text;
            newCheck.date = DateTimeOffset.Now.LocalDateTime;
            newCheck.checkNumber = ShiftBox.SelectedIndex;
            string[] initials = { QT_Initials.Text, DS_Initials.Text, Sup_Initials.Text };
            newCheck.initals = initials;
            
            try { await SaveSign1(); } catch (Exception ee) { Console.WriteLine(ee); }
            try { await SaveSign2(); } catch (Exception ee) { Console.WriteLine(ee); }
            try { await SaveSign3(); } catch (Exception ee) { Console.WriteLine(ee); }


            if (!masterList.Contains(dateBox.Date.Date))//adds date to master date list if not already on it
            {
                masterList.Add(dateBox.Date.Date);
            }
            /*
            if (ShiftBox.SelectedIndex == 0) 
            {
                partMasterList.morningList[LineBox.SelectedIndex] = PartBox.Text;
                if (NoteBox.Text.Length > 0)
                {
                    partMasterList.morningNotes[LineBox.SelectedIndex] = true;
                }
                else
                {
                    partMasterList.morningNotes[LineBox.SelectedIndex] = false;
                }
            }//saves the appropriate data for the daily part tracker
            else if (ShiftBox.SelectedIndex == 1)
            {
                partMasterList.firstList[LineBox.SelectedIndex] = PartBox.Text;
                if (NoteBox.Text.Length > 0)
                {
                    partMasterList.firstNotes[LineBox.SelectedIndex] = true;
                }
                else
                {
                    partMasterList.firstNotes[LineBox.SelectedIndex] = false;
                }
            }
            else if (ShiftBox.SelectedIndex == 2)
            {
                partMasterList.lunchList[LineBox.SelectedIndex] = PartBox.Text;
                if (NoteBox.Text.Length > 0)
                {
                    partMasterList.lunchNotes[LineBox.SelectedIndex] = true;
                }
                else
                {
                    partMasterList.lunchNotes[LineBox.SelectedIndex] = false;
                }
            }
            else if (ShiftBox.SelectedIndex == 3)
            {
                partMasterList.secondList[LineBox.SelectedIndex] = PartBox.Text;
                if (NoteBox.Text.Length > 0)
                {
                    partMasterList.secondNotes[LineBox.SelectedIndex] = true;
                }
                else
                {
                    partMasterList.secondNotes[LineBox.SelectedIndex] = false;
                }
            }
            */

            newCheck.defects = Convert.ToBoolean(DefectCheck_true.IsChecked);
            try { newCheck.defectList = DefectListMaker(); } catch { } 
            try { newCheck.notes = NoteBox.Text; } catch { }

            MasterPartCheck(PartBox.Text);


            String sqlString = sqlMaker(newCheck, dateBox.Date.Date.ToString("yyyy-MM-dd"));
            SQLSaver(sqlString);
            
            string json = JsonConvert.SerializeObject(newCheck);
            string fileName = "Sheet_" + dateBox.Date.ToString("yyyy_MM_dd_") + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".json";
            StorageFile newFile = await saveHere.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(newFile, json);

            StorageFolder localFolder = KnownFolders.MusicLibrary;
            json = JsonConvert.SerializeObject(masterList);
            fileName = "MasterTracker.json";
            newFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(newFile, json);

            StorageFolder rootFolder = KnownFolders.MusicLibrary;
            var projectFolderName = dateBox.Date.ToString("yyyy_MM_dd");
            StorageFolder projectFolder = await rootFolder.CreateFolderAsync(projectFolderName, CreationCollisionOption.OpenIfExists);
            json = JsonConvert.SerializeObject(partMasterList);
            fileName = "PartMaster_" + dateBox.Date.ToString("yyyy_MM_dd_") + ".json";
            newFile = await projectFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(newFile, json);
            PartSearchCreator(); 


            this.Background = White;
            LightLeds(); //adds visual cue for shift already saved
            NoteBox.Text = "";
        }
        
        private String sqlMaker(QualityCheck thisCheck, String dater)
        {
            String output = "";
            output += "INSERT INTO QualityCheck(CheckDate, CheckTime, CheckNo, CheckShift, PartNumber, SampleMatch, " +
                "PackageMatch, LidMatch, DefectCheck, LineNumber, QAInitials, DSInitials,SupInitials, QAPath, DSPath,SupPath, Notes, SpecialCheck) " +
                "VALUES('" + dater + "','";
            int checkNo = thisCheck.checkNumber;
            if (thisCheck.date.Date.ToString("HH:mm:ss") == "00:00:00" && (RealShiftBox.SelectedIndex==0))
            {
                String thisTime = "07:15:00";
                if (checkNo == 1) { thisTime = "08:00:00"; }
                else if (checkNo == 2) { thisTime = "9:20:00"; }
                else if (checkNo == 3) { thisTime = "10:00:00"; }
                else if (checkNo == 4) { thisTime = "11:00:00"; }
                else if (checkNo == 5) { thisTime = "12:10:00"; }
                else if (checkNo == 6) { thisTime = "13:00:00"; }
                else if (checkNo == 7) { thisTime = "14:10:00"; }
                else if (checkNo == 8) { thisTime = "15:00:00"; }
                output += thisTime + "',";

            }
            else
            {
                output += thisCheck.date.Date.ToString("HH:mm:ss") + "',";
            }
            
            output += checkNo.ToString() + ",";
            output += RealShiftBox.SelectedIndex.ToString() + ",'" + thisCheck.partName + "',";
            if (thisCheck.sampleMatch) { output += "1,"; } else { output += "0,"; }
            if (thisCheck.boxMatch) { output += "1,"; } else { output += "0,"; }
            if (thisCheck.lidMatch) { output += "1,"; } else { output += "0,"; }
            if (thisCheck.defects) { output += "1,"; } else { output += "0,"; } ///ADD DEFECT DATABASE HERE 
            int lineInd = (thisCheck.lineName);
            output += lineInd.ToString() + ",'";
            output += thisCheck.initals[0] + "','" + thisCheck.initals[1] + "','" + thisCheck.initals[2] + "','";
            output += thisCheck.sigPath[0] + "','" + thisCheck.sigPath[1] + "','" + thisCheck.sigPath[2] + "','";
            try { output += thisCheck.notes + "','"; } catch { }
            output += dater + "-" + RealShiftBox.SelectedIndex.ToString() + "-" + checkNo + "-" + (thisCheck.lineName).ToString();
            output += "'); ";
            //output = "INSERT INTO QualityCheck(CheckDate, CheckTime, CheckNo, SampleMatch, CheckShift, LineNumber, PartNumber) Values('2018-10-04', '12:24:10', 0, 2, 0, 3, 'WM88-173');";
            return output;
        }

        private async void MasterPartCheck(string partName)
        {
            try
            {
                StorageFile newFile = await KnownFolders.MusicLibrary.GetFileAsync("MasterParts.json");
                String json = await FileIO.ReadTextAsync(newFile);
                List<string> masterPartList = JsonConvert.DeserializeObject(json, typeof(List<String>)) as List<String>;
                if (!masterPartList.Contains(partName))
                {
                    masterPartList.Add(partName);
                    StorageFolder localFolder = KnownFolders.MusicLibrary;
                    json = JsonConvert.SerializeObject(masterPartList);
                    string fileName = "MasterParts.json";
                    newFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await Windows.Storage.FileIO.WriteTextAsync(newFile, json);
                }
            }
            catch { }
        }

        /// <summary>
        /// Indexes parts for search feature
        /// </summary>
        private async void PartSearchCreator()
        {
            string PartName = PartBox.Text;
            bool isIn = false;
            int i = 0;
            MyDate newDate = new MyDate();
            foreach(PartTracker thisTracker in PartSearcher)
            {
                if(thisTracker.partName == PartName)
                {
                    isIn = true;
                    bool isInDate = false;
                    foreach(MyDate dateSearch in thisTracker.dates)
                    {
                        if(dateSearch.date == dateBox.Date.Date)
                        {
                            isInDate = true;
                        }
                    }
                    if(!isInDate)
                    {
                        newDate.date = dateBox.Date.Date;
                        newDate.lineNumber = LineBox.SelectedIndex;
                    }
                }
                i++;
            }
            if (!isIn)
            {
                PartTracker myTracker = new PartTracker();
                List<MyDate> newDates = new List<MyDate>();
                myTracker.partName = PartBox.Text;
                newDate.lineNumber = LineBox.SelectedIndex;
                newDate.date = dateBox.Date.Date;
                newDates.Add(newDate);
                myTracker.dates = newDates;
                PartSearcher.Add(myTracker);
            }
            StorageFolder localFolder = KnownFolders.MusicLibrary;
            String json = JsonConvert.SerializeObject(PartSearcher);
            String fileName = "PartSearch.json";
            StorageFile newFile = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(newFile, json);
        }

        /// <summary>
        /// Processes Icon Button Presses for navigating pages
        /// </summary>
        private void ClickView(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = SLB;
            bord.Background = SB;
            if (bord.Name == "ViewBorder")
            {
                this.Frame.Navigate(typeof(ViewMaster), masterList);
            }
            else if (bord.Name == "SearchBorder")
            {
                this.Frame.Navigate(typeof(Search), PartSearcher);
            }
        }

        /// <summary>
        /// Resets Icon Buttons to default state
        /// </summary>
        private void ClickUnview(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = SB;
            bord.Background = LSB;
        }

        /// <summary>
        /// Reloads data if a different date is selected. 
        /// </summary>
        private void DateChange(object sender, DatePickerValueChangedEventArgs e)
        {
            SetupMasterList();
            DefectListSetup();
        }

        /// <summary>
        /// Visual Cue processor for changing line box
        /// </summary>
        private void LoadShiftLED(object sender, SelectionChangedEventArgs e)
        {
            LightLeds();
        }



        private bool[] GetLEDS(int lineNumber)
        {
            bool[] output = new bool[9];
            for(int i =0;i<9;i++)
            {
                output[i] = false;
            }
            String thisString = "";
            thisString += "SELECT CheckNo FROM QualityCheck WHERE(CheckDate = '";
            thisString += dateBox.Date.Date.ToString("yyyy-MM-dd");
            thisString += "' AND LineNumber = ";
            thisString += lineNumber.ToString() + ");";

            using (conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = thisString;
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int CheckNo = (int)reader.GetByte(0);
                                output[CheckNo] = true;
                            }
                        }
                    }
                    catch { }
                }

            }


            return output;
        }
        /// <summary>
        /// Creates the visual cues for which shifts are already entered,
        /// </summary>
        private void LightLeds()
        {
            S1.Fill = S2.Fill = S3.Fill = S4.Fill = S5.Fill = S6.Fill = S7.Fill = S8.Fill = S9.Fill = White;
            int whichLine = LineBox.SelectedIndex;
            bool[] LedList = GetLEDS(whichLine);
            if(LedList[0]){ S1.Fill = Red; }
            if (LedList[1]) { S2.Fill = Orng; }
            if (LedList[2]) { S3.Fill = Yellow; }
            if (LedList[3]) { S4.Fill = Green; }
            if (LedList[4]) { S5.Fill = Blue; }
            if (LedList[5]) { S6.Fill = Purp; }
            if (LedList[6]) { S7.Fill = Indg; }
            if (LedList[7]) { S8.Fill = Pink; }
            if (LedList[8]) { S9.Fill = Brown; }



            loadChange();
        }

        /// <summary>
        /// Handles changing the shift box
        /// </summary>
        private void ShiftChange(object sender, SelectionChangedEventArgs e)
        {
            loadChange();
        }

        /// <summary>
        /// If line/shift/date is changed, create/clear individual quality check
        /// </summary>
        private void loadChange()
        {
            if (isLoaded)
            {
                int shiftCheck = ShiftBox.SelectedIndex;
                int whichLine = LineBox.SelectedIndex;
                string date = dateBox.Date.Date.ToString("yyyy_MM_dd");

                /*  FUTURE FEATURE: LOAD PREVIOUS DATA INTO MAIN PAGE 
                if (shiftCheck == 0)
                {
                    if (partMasterList.morningList[whichLine] != null)
                    {
                        String name = "Sheet_" + date + "_" + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".json";
                    }
                }*/
                QualityCheck loadCheck = new QualityCheck();
            }
        }

        /// <summary>
        /// Handles the return to this page by resetting icon buttons
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewBorder.BorderBrush = SB; SearchBorder.BorderBrush = SB;
            ViewBorder.Background = LSB; SearchBorder.Background = LSB;
        }

        /// <summary>
        /// Handles the autosuggestion for the part name
        /// </summary>
        private async void PartLookupTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            sender.Text = sender.Text.ToUpper();
            try
            {
                StorageFile newFile = await KnownFolders.MusicLibrary.GetFileAsync("MasterParts.json");
                String json = await FileIO.ReadTextAsync(newFile);
                List<string> masterPartList = JsonConvert.DeserializeObject(json, typeof(List<String>)) as List<String>;
                masterPartList.Sort();
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    List<string> suggestions = SearchControls(sender.Text);
                    sender.ItemsSource = suggestions;
                }
                List<string> SearchControls(string query)
                {
                    var suggestions = new List<string>();
                    suggestions = masterPartList.Where(x => x.StartsWith(query)).ToList();
                    return suggestions;
                }
            }
            catch { }
        }

        /// <summary>
        /// Handles the autosuggestion selection
        /// </summary>
        private void ChoseThis(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = args.SelectedItem.ToString();
            sender.Text = selectedItem;
        }
    }
}
