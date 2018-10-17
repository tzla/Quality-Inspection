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
        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };//line name list 
        SqlConnection conn;  //database connection item
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Shift Start", "Hour 1", "First Break", "Hour 3", "Hour 4", "After Lunch", "Hour 6", "Second Break", "Hour 8" };//check times
        ObservableCollection<string> RealShiftSource = new ObservableCollection<string> { "1st", "2nd", "3rd" };//list of shifts



        public SolidColorBrush LSB = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue);//pre-loaded colors for UI
        public SolidColorBrush SB = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
        SolidColorBrush LG = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.White);
        public SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);

        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red); //colors for check indicators
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
        //PartMaster partMasterList = new PartMaster();//indexes individual quality reports by line number,date, and shift


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
            DefectListSetup();//create list of the defect boxes
            isLoaded = true;//loaded flag is on. prevents unneccessary actions
        }

        /// <summary>
        /// Loads Data for lists OR creates new list if data is unavailable. 
        /// </summary>

        /// <summary>
        /// Creates a list of the defect checkboxes for easy access
        /// </summary>
        private void DefectListSetup()
        {
            if (DefectList.Count != 12)
            {
                DefectList.Add(D0); DefectList.Add(D1); DefectList.Add(D2); DefectList.Add(D3); DefectList.Add(D4); DefectList.Add(D5); DefectList.Add(D6); DefectList.Add(D7);
                DefectList.Add(D8); DefectList.Add(D9); DefectList.Add(D10); DefectList.Add(D11);
            }
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
                    catch (Exception ee) { LogError(ee.ToString()); }
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
        private async Task SaveSign(String ID, int index)
        {
            string fileName = ID + dateBox.Date.ToString("yyyy_MM_dd") + "_" + RealShiftBox.SelectedIndex + "_" + LineBox.SelectedIndex + "_" + ShiftBox.SelectedIndex + ".gif";

            StorageFile newFile = await saveHere.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            Windows.Storage.CachedFileManager.DeferUpdates(newFile);
            IRandomAccessStream stream =
                    await newFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            // Write the ink strokes to the output stream.
            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await twoSigs[index].SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            // Finalize write so other apps can update file.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(newFile);
            newCheck.sigPath[index] = fileName;
            //return newFile;
        }
        
        /// <summary>
        /// Processes the defect checkboxes for saving data
        /// </summary>
        private bool[] DefectListMaker()
        {
            bool[] list = new bool[12];
            int i = 0;
            foreach (CheckBox thisBox in DefectList)
            {
                list[i] = Convert.ToBoolean(thisBox.IsChecked);
                i++;
            }
            return list;
        }



        private String GetLastPart()
        {
            String output = "SELECT TOP 1 PartNumber From QualityCheck WHERE LineNumber = " + newCheck.lineName.ToString() + " ORDER BY CheckDate DESC, CheckNo DESC;";
            String recentName = "";
            using (conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = output;
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                recentName = reader.GetString(0);
                            }
                        }
                    }
                    catch (Exception e) { LogError(e.ToString()); }
                }

            }
            return recentName;
        }




        /// <summary>
        /// Processes and Saves the entered data
        /// </summary>
        private async void ConfirmData(object sender, RoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = SLB;
            bord.Background = SB;
            await newFolders();
            newCheck.lineName = LineBox.SelectedIndex;
            newCheck.partName = PartBox.Text;
            newCheck.date = DateTimeOffset.Now.LocalDateTime;
            newCheck.checkNumber = ShiftBox.SelectedIndex;

            String checkName = GetLastPart().TrimEnd();

            if (checkName != newCheck.partName)
            {
                ContentDialog noWifiDialog = new ContentDialog()
                {
                    Title = "Confirm Part Change",
                    Content = "Change Part From " + checkName + " to " + newCheck.partName,
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Cancel"
                };

                var result = await noWifiDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    
                }
                else
                {
                    return;
                }

            }
            

                string[] initials = { QT_Initials.Text, DS_Initials.Text, Sup_Initials.Text };
                newCheck.initals = initials;

                try { await SaveSign("TechSig_", 0); } catch (Exception ee) { LogError(ee.ToString()); }
                try { await SaveSign("DSSig_", 1); } catch (Exception ee) { LogError(ee.ToString()); }
                try { await SaveSign("SupSig_", 2); } catch (Exception ee) { LogError(ee.ToString()); }


                if (!masterList.Contains(dateBox.Date.Date))//adds date to master date list if not already on it
                {
                    masterList.Add(dateBox.Date.Date);
                }


                newCheck.defects = Convert.ToBoolean(DefectCheck_true.IsChecked);
                try { newCheck.defectList = DefectListMaker(); } catch (Exception ee) { LogError(ee.ToString()); }
                try { newCheck.notes = NoteBox.Text; } catch (Exception ee) { LogError(ee.ToString()); }

                String sqlString = sqlMaker(newCheck, dateBox.Date.Date.ToString("yyyy-MM-dd"));
                SQLSaver(sqlString);

                this.Background = White;
                LightLeds(); //adds visual cue for shift already saved
                NoteBox.Text = "";
                PartBox.Text = PartBox.Text.TrimEnd();
            
            
        }

        /// <summary>
        /// Makes SQL string for add check to database. 
        /// </summary>
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
                //output += thisTime + "',";

            }
            else
            {
                
            }
            output += DateTimeOffset.Now.ToString("HH:mm:ss") + "',";
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

                String specialID = "'" + dater + "-" + RealShiftBox.SelectedIndex.ToString() + "-" + checkNo + "-" + (thisCheck.lineName).ToString() + "'";
                SQLDefectMaker(specialID,thisCheck);

            //output = "INSERT INTO QualityCheck(CheckDate, CheckTime, CheckNo, SampleMatch, CheckShift, LineNumber, PartNumber) Values('2018-10-04', '12:24:10', 0, 2, 0, 3, 'WM88-173');";
            return output;
        }


        private void SQLDefectMaker(String specialID,QualityCheck thisCheck)
        {
            String output = "INSERT INTO QualityCheckDefects(SpecialCheck,Scratches,Dents,Sharps,LooseCups,Discoloration,Rust,CoatingPeel,Delamination,UnevenBottom,OpenCurl,Wrinkles,Cracks)";
            output += " VALUES (" + specialID ;
            foreach(bool check in thisCheck.defectList)
            {
                if(check)
                {
                    output += ",1";
                }
                else
                {
                    output += ",0";
                }
            }
            output += ");";
            using (conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = output;
                    try
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                            }
                        }
                    }
                    catch (Exception e) { LogError(e.ToString()); }
                }

            }

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


        private void ClickUnSave(object sender, PointerRoutedEventArgs e)
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
            NoteBox.Text = "";
            loadChange();
            DefectListSetup();
        }

        /// <summary>
        /// Visual Cue processor for changing line box
        /// </summary>
        private void LoadShiftLED(object sender, SelectionChangedEventArgs e)
        {
           
            LightLeds();
        }

        /// <summary>
        /// Indicates which hourly checks have been completed with "LEDS". 
        /// </summary>
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
                    catch (Exception e) { LogError(e.ToString()); }
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
           // PartBox.Text = "";
        }

        /// <summary>
        /// Handles changing the shift box
        /// </summary>
        private void ShiftChange(object sender, SelectionChangedEventArgs e)
        {
            NoteBox.Text = "";
            loadChange();
        }

        /// <summary>
        /// If line/shift/date is changed, create/clear individual quality check
        /// </summary>
        private void loadChange()
        {
            DefectCheck_true.IsChecked = false;
            DefectCheck_false.IsChecked = true;
            DefectGrid.Background = LG;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = "Check Defect Box To Enable";
            ToolTipService.SetToolTip(DefectGrid, toolTip);
            foreach (CheckBox thisDefect in DefectList)
            {
                thisDefect.IsChecked = false;
                thisDefect.IsEnabled = false;
            }
            //PartBox.Text = "";
            if (isLoaded)
            {
                String dater = dateBox.Date.Date.ToString("yyyy-MM-dd");
                String sqlString = dater + "-" + RealShiftBox.SelectedIndex.ToString() + "-" + ShiftBox.SelectedIndex.ToString() + "-" + (LineBox.SelectedIndex).ToString();
                String output = "Select SpecialCheck,* FROM QualityCheck WHERE(SpecialCheck = '" + sqlString + "');";
                bool badData = true;

                String result = null;
                using (conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = output;
                        try
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    result = reader.GetString(0).TrimEnd();
                                    if (result != null && result != "")
                                    {
                                        badData = false;
                                    }
                                }
                            }
                        }
                        catch (Exception e) { LogError(e.ToString());}
                    }
                    
                }
            }
        }

        /// <summary>
        /// Handles the return to this page by resetting icon buttons
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewBorder.BorderBrush = SB; SearchBorder.BorderBrush = SB; SaveBorder.BorderBrush = SB;
            ViewBorder.Background = LSB; SearchBorder.Background = LSB; SaveBorder.Background = LSB;
        }

        /// <summary>
        /// Handles the autosuggestion for the part name
        /// </summary>
        private void PartLookupTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<String> masterPartList = new List<string>();
            sender.Text = sender.Text.ToUpper();
            String thisString = "SELECT DISTINCT PartNumber FROM QualityCheck ORDER BY PartNumber;";
            //Creates Master List of Part from Database
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
                                String getter = reader.GetString(0).TrimEnd();
                                masterPartList.Add(getter);
                            }
                        }
                    }
                    catch(Exception e) { LogError(e.ToString()); }
                }
            }
            //Generate Auto-Complete Suggestions
            try
            {
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
            catch (Exception e) { LogError(e.ToString()); }
        }



        /// <summary>
        /// Clear the form data. Here in case of use. 
        /// </summary>
        private void ClearData()
        {
            PartBox.Text = "";
            NoteBox.Text = "";
            SampleCheck_false.IsChecked = SampleCheck_true.IsChecked = false;
            PackageCheck_false.IsChecked = PackageCheck_true.IsChecked = false;
            LidCheck_false.IsChecked = LidCheck_true.IsChecked = false;
            DefectCheck_false.IsChecked = DefectCheck_true.IsChecked = false;
            QT_Initials.Text = DS_Initials.Text = Sup_Initials.Text = "";
            inkyCanvas.InkPresenter.StrokeContainer = null;
            inkyCanvasDS.InkPresenter.StrokeContainer = null;
            inkyCanvasSup.InkPresenter.StrokeContainer = null;
            
        }



        /// <summary>
        /// Handles the autosuggestion selection
        /// </summary>
        private void ChoseThis(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            String selectedItem = args.SelectedItem.ToString();
            sender.Text = selectedItem.TrimEnd();
        }

        /// <summary>
        /// Creates daily error log ....eventually
        /// </summary>
        public async void LogError(String e)
        {
            Console.WriteLine(e + " @ " + DateTimeOffset.Now.ToString());
        }

        private void InitialLength(object sender, TextChangedEventArgs e)
        {
            TextBox initials = sender as TextBox;
            if(initials.Text.Length>3)
            {
                initials.Text = initials.Text.Remove(3);
            }
            
        }
    }
}
