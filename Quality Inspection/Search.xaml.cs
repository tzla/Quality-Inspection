using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Quality_Inspection
{
    /// <summary>
    /// Part Search Function Page
    /// </summary>
    public sealed partial class Search : Page
    {
        SolidColorBrush LSB = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue); //preset colors
        SolidColorBrush SB = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
        SolidColorBrush LG = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.White);
        SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red);

        ObservableCollection<string> newShiftSource = new ObservableCollection<string> { "Shift Start", "Hour 1", "First Break", "Hour 3", "Hour 4", "After Lunch", "Hour 6", "Second Break", "Hour 8" };
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Morning", "First", "Lunch", "Second" };//source lists
        ObservableCollection<string> RealShifts = new ObservableCollection<string> { "1st", "2nd", "3rd" };
        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };

        List<string> partMasterList;
        List<MainPage.PartTracker> MasterDateList;
        SqlConnection conn;  //database connection item
        DateTime balls = new DateTime();

        bool isToggle = false;

        ObservableCollection<string> problemDateStrings = new ObservableCollection<string>();
        ObservableCollection<string> dateStrings = new ObservableCollection<string>();
        ObservableCollection<string> defectDateStrings = new ObservableCollection<string>();

        ObservableCollection<String> ChecksList = new ObservableCollection<string>();
        ObservableCollection<String> problemChecksList = new ObservableCollection<string>();

        List<String> specialCheckList = new List<string>();
        List<String> specialProblemList = new List<string>();

        String mySpecialID = "";
        String balls2;

        /// <summary>
        /// Initialize the UI
        /// </summary>  
        public Search()
        {
            this.InitializeComponent();
            ShiftBox.ItemsSource = newShiftSource;
            LineBox.ItemsSource = LineSource;
            RealShiftBox.ItemsSource = RealShifts;
        }

        /// <summary>
        /// Loads the page when navigated to
        /// </summary>  
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
        /// Return to main page icon handler
        /// </summary>    
        private void ClickHome(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            bord.Background = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Return to main page icon handler
        /// </summary>  
        private void UnclickHome(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            bord.Background = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue);
        }

        /// <summary>
        /// Generates the autosuggestion list and suggestion
        /// </summary>  
        private void PartLookupTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ShowSwitch.IsOn = false;
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
                    catch (Exception e) { }
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
            catch (Exception e) {}
        }

        /// <summary>
        /// Loads the list of run dates for the selected part
        /// </summary>  
        private async void SubmitQuery(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string PartName = sender.Text;
            dateStrings.Clear();
            defectDateStrings.Clear();
            problemDateStrings.Clear();
            ChecksList.Clear();
            problemChecksList.Clear();
            sender.Text = sender.Text.ToUpper();
            String thisString = "Select DISTINCT CheckDate, LineInfo.LineName FROM QualityCheck INNER JOIN LineInfo ON LineInfo.LineNumber = QualityCheck.LineNumber WHERE PartNumber = '" + PartName  + "' ORDER BY CheckDate DESC;";
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
                                balls = reader.GetDateTime(0);
                                balls2 = reader.GetString(1).TrimEnd();
                                dateStrings.Add(balls.ToString("yyyy-MM-dd") + ":      " + reader.GetString(1).TrimEnd());

                            }
                        }
                    }
                    catch (Exception e) { }

                    String output = "Select DISTINCT CheckDate, LineInfo.LineName FROM QualityCheck" +
                             " INNER JOIN LineInfo ON LineInfo.LineNumber = QualityCheck.LineNumber" +
                             " WHERE PartNumber = '" + PartBox.Text + "' AND (NOT('' = CONVERT(varchar(max), Notes)) OR DefectCheck = 1) " +
                             "ORDER BY CheckDate DESC;";
                    cmd.CommandText = output;
                    try
                    {
                        using (SqlDataReader reader2 = cmd.ExecuteReader())
                        {
                            while (reader2.Read())
                            {
                                var balls = reader2.GetDateTime(0);
                                defectDateStrings.Add(balls.ToString("yyyy-MM-dd"));
                                problemDateStrings.Add(balls.ToString("yyyy-MM-dd") + ":      " + reader2.GetString(1).TrimEnd());
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        DateList.ItemsSource = dateStrings;
                    }
                    catch { }

                    await Task.Delay(1000);
                    for (int i = 0; i < DateList.Items.Count; i++)
                    {
                        try
                        {
                            var item = DateList.ContainerFromIndex(i) as ListViewItem;
                            String myString = DateList.Items[i].ToString().Split(':')[0];
                            if (defectDateStrings.Contains(myString))
                            {
                                item.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                                item.Background = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke);
                            }
                            else
                            {

                            }
                        }
                        catch { }
                    }
                    
                }
            }
        }

        /// <summary>
        /// Loads the list of checks for the selected part/date
        /// </summary>  
        private async void SyncSelection(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListView thisList = sender as ListView;
                List<bool> problemList = new List<bool>();
                String output = "Select CheckInfo.CheckName, FirstShiftTime,CheckTime,QualityCheck.CheckNo, DefectCheck, Notes, SpecialCheck ";
                output += "FROM QualityCheck INNER JOIN CheckInfo ON CheckInfo.CheckNo = QualityCheck.CheckNo ";
                output += "WHERE PartNumber = '" + PartBox.Text + "' AND CheckDate = '" + DateList.SelectedItem.ToString().Split(':')[0] + "' ";
                output += "ORDER BY QualityCheck.CheckNo;";
                ChecksList.Clear();
                problemList.Clear();
                problemChecksList.Clear();
                specialProblemList.Clear();
                specialCheckList.Clear();

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
                                    ChecksList.Add(reader.GetString(0));
                                    if (reader.GetBoolean(4) || reader.GetString(5) != "")
                                    {
                                        problemList.Add(true);
                                        
                                    }
                                    else
                                    {
                                        problemList.Add(false);
                                    }
                                    specialCheckList.Add(reader.GetString(6));
                                }
                            }
                        }
                        catch { }
                        output = "Select CheckInfo.CheckName, FirstShiftTime,CheckTime,QualityCheck.CheckNo, DefectCheck, Notes, SpecialCheck " +
                                 "FROM QualityCheck INNER JOIN CheckInfo ON CheckInfo.CheckNo = QualityCheck.CheckNo " +
                                 "WHERE PartNumber = '" + PartBox.Text + "' AND CheckDate = '" + DateList.SelectedItem.ToString().Split(':')[0] +
                                 "' AND (NOT('' = CONVERT(varchar(max), Notes)) OR DefectCheck = 1) " +
                                 "ORDER BY QualityCheck.CheckNo;";
                        cmd.CommandText = output;
                        try
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    problemChecksList.Add(reader.GetString(0));
                                    specialProblemList.Add(reader.GetString(6));
                                }
                            }
                        }
                        catch { }

                        
                    }
                }

                if(ShowSwitch.IsOn)
                {
                    CheckList.ItemsSource = problemChecksList;
                }
                else
                {
                    CheckList.ItemsSource = ChecksList;
                }
                


                await Task.Delay(1000);

                for (int i = 0; i < CheckList.Items.Count; i++)
                {
                    try
                    {
                        var item = CheckList.ContainerFromIndex(i) as ListViewItem;
                        String myString = CheckList.Items[i].ToString();
                        if (problemChecksList.Contains(myString))
                        {
                            item.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                            item.Background = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke);
                        }
                        else
                        {
                            item.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                            item.Background = new SolidColorBrush(Windows.UI.Colors.White);
                        }
                    }
                    catch { }
                }
            }
            catch { }




        }

        /// <summary>
        /// Handles the toggle switch for filter of interesting checks 
        /// </summary>  
        private async void ToggleList(object sender, RoutedEventArgs e)
        {
            isToggle = true;
            ToggleSwitch thisSwitch = sender as ToggleSwitch;
            if (thisSwitch.IsOn)
            {
                try
                {
                    DateList.ItemsSource = problemDateStrings;
                    CheckList.ItemsSource = problemChecksList;
                }
                catch { }
                try { CheckList.SelectedIndex = 0; } catch { }
                try { DateList.SelectedIndex = 0; } catch { }
                await Task.Delay(1000);
            }
            else
            {
                try
                {
                    DateList.ItemsSource = dateStrings;
                    CheckList.ItemsSource = ChecksList;
                }
                catch { }
                try { CheckList.SelectedIndex = 0; } catch { }
                try { DateList.SelectedIndex = 0; } catch { }
                await Task.Delay(1000);
                
            }
            for (int i = 0; i < DateList.Items.Count; i++)
            {
                try
                {
                    var item = DateList.ContainerFromIndex(i) as ListViewItem;
                    String myString = DateList.Items[i].ToString().Split(':')[0];
                    if (defectDateStrings.Contains(myString))
                    {
                        item.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                        item.Background = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke);
                    }
                    else
                    {

                    }
                }
                catch { }
            }
            isToggle = false;
        }

        /// <summary>
        /// Processes the edit switch on individual sheets
        /// </summary>
        private void EditSwitcher(object sender, RoutedEventArgs e)
        {
            ToggleSwitch editer = sender as ToggleSwitch;
            if (editer.IsOn)
            {
                OnOffState(true);
                SaveBorder.BorderBrush = TrashBorder.BorderBrush = SB;
                SaveBorder.Background = TrashBorder.Background = LSB;

                DefectGrid.Background = White;
            }
            else
            {
                OnOffState(false);
                SaveBorder.BorderBrush = TrashBorder.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                SaveBorder.Background = TrashBorder.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
                DefectGrid.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);

            }
        }

        /// <summary>
        /// Processes the edit toggle switch
        /// </summary>
        private void OnOffState(bool state)
        {
            D0.IsEnabled = D1.IsEnabled = D2.IsEnabled = D3.IsEnabled = D4.IsEnabled = D5.IsEnabled = D6.IsEnabled
                 = D7.IsEnabled = D8.IsEnabled = D9.IsEnabled = D10.IsEnabled = D11.IsEnabled = state;
            NoteBox.IsEnabled = DefectCheck_true.IsEnabled = DefectCheck_false.IsEnabled = LidCheck_true.IsEnabled = LidCheck_false.IsEnabled = state;
            SampleCheck_true.IsEnabled = SampleCheck_false.IsEnabled = PackageCheck_true.IsEnabled = PackageCheck_false.IsEnabled = state;
            PartBox1.IsEnabled = state;
            NoteBox.IsReadOnly = !state;
        }

        /// <summary>
        /// Handles sample match boxes
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
        }

        /// <summary>
        /// Handles package match boxes
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
        }

        /// <summary>
        /// After editing a sheet, handles the modifcation to the database
        /// </summary>
        private void SaveClick(object sender, PointerRoutedEventArgs e)
        {
            if (EditSwitch.IsOn)
            {
                SaveBorder.BorderBrush = SLB;
                SaveBorder.Background = SB;
                String output = "UPDATE QualityCheck SET ";
                output += "PartNumber = '" + PartBox1.Text + "', ";
                output += "SampleMatch = ";
                if ((bool)SampleCheck_true.IsChecked) { output += "1, "; } else { output += "0, "; }
                output += "PackageMatch = ";
                if ((bool)PackageCheck_true.IsChecked) { output += "1, "; } else { output += "0, "; }
                output += "LidMatch = ";
                if ((bool)LidCheck_true.IsChecked) { output += "1, "; } else { output += "0, "; }
                output += "DefectCheck = ";
                if ((bool)DefectCheck_true.IsChecked) { output += "1, "; } else { output += "0, "; }
                output += "Notes = '" + NoteBox.Text + "' ";
                output += " WHERE SpecialCheck = '" + mySpecialID.ToString() + "';";

                using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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
                        catch (Exception ee) { String error = ee.ToString(); }
                    }
                }
            }

        }

        /// <summary>
        /// Handles deleting sheet from database (does not delete signature file)
        /// </summary>
        private void TrashClick(object sender, PointerRoutedEventArgs e)
        {
            if (EditSwitch.IsOn)
            {
                TrashBorder.BorderBrush = SLB;
                TrashBorder.Background = SB;
                String output = "DELETE FROM QualityCheck ";
                output += " WHERE SpecialCheck = '" + mySpecialID.ToString() + "';";
                using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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
                        catch (Exception ee) { String error = ee.ToString(); }
                    }
                }

                output = "DELETE FROM QualityCheckDefects ";
                output += " WHERE SpecialCheck = '" + mySpecialID.ToString() + "';";
                using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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
                        catch (Exception ee) { String error = ee.ToString(); }
                    }
                }

                int col = Convert.ToInt16(mySpecialID.Split('-')[4]);
                int row = Convert.ToInt16(mySpecialID.Split('-')[5]);
            }
        }

        /// <summary>
        /// Handles the click release for icons
        /// </summary>
        private void Unclick(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = SB;
            bord.Background = LSB;
        }

        private void ClearSheet()
        {
            loadDate.Date = balls;

            ShiftBox.SelectedItem = CheckList.SelectedItem;

            RealShiftBox.SelectedIndex = 0;

            LineBox.SelectedItem = balls2;
            PartBox1.Text = PartBox.Text;
            QT_Initials.Text = "";
            DS_Initials.Text = "";
            Sup_Initials.Text = "";

            D0.IsEnabled = D1.IsEnabled = D2.IsEnabled = D3.IsEnabled = D4.IsEnabled = D5.IsEnabled = D6.IsEnabled
                 = D7.IsEnabled = D8.IsEnabled = D9.IsEnabled = D10.IsEnabled = D11.IsEnabled = false;
            D0.IsChecked = D1.IsChecked = D2.IsChecked = D3.IsChecked = D4.IsChecked = D5.IsChecked = D6.IsChecked
                 = D7.IsChecked = D8.IsChecked = D9.IsChecked = D10.IsChecked = D11.IsChecked = false;
        }


        private void LoadSheet(object sender, SelectionChangedEventArgs e)
        {
            ClearSheet();
            try
            {
                ListView thisList = sender as ListView;
                int index = thisList.SelectedIndex;
                String name = "";
                if (ShowSwitch.IsOn)
                {
                    name = specialProblemList[index].TrimEnd();
                }
                else
                {
                    name = specialCheckList[index].TrimEnd();
                }
                String output = "SELECT * FROM QualityCheck WHERE(SpecialCheck = '" + name + "');";
                String date = DateList.SelectedItem.ToString().Split(':')[0];
                using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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

                                    loadDate.Date = reader.GetDateTime(1);

                                    ShiftBox.SelectedIndex = reader.GetByte(3);

                                    RealShiftBox.SelectedIndex = reader.GetByte(4);

                                    LineBox.SelectedIndex = reader.GetInt32(10);

                                    QT_Initials.Text = reader.GetString(11);
                                    DS_Initials.Text = reader.GetString(12);
                                    Sup_Initials.Text = reader.GetString(17);



                                    String date2 = date.Replace('-', '_');
                                    try { loadSig(date2, reader.GetByte(3), reader.GetString(13).TrimEnd(), inkyCanvas); } catch { }
                                    try { loadSig(date2, reader.GetByte(3), reader.GetString(14).TrimEnd(), inkyCanvasDS); } catch { }
                                    try { loadSig(date2, reader.GetByte(3), reader.GetString(18).TrimEnd(), inkyCanvasSup); } catch { }



                                    PartBox1.Text = PartBox.Text;

                                    SampleCheck_true.IsChecked = reader.GetBoolean(6);
                                    SampleCheck_false.IsChecked = !reader.GetBoolean(6);

                                    PackageCheck_true.IsChecked = reader.GetBoolean(7);
                                    PackageCheck_false.IsChecked = !reader.GetBoolean(7);

                                    LidCheck_true.IsChecked = reader.GetBoolean(8);
                                    LidCheck_false.IsChecked = !reader.GetBoolean(8);


                                    DefectCheck_true.IsChecked = reader.GetBoolean(9);
                                    DefectCheck_false.IsChecked = !reader.GetBoolean(9);

                                    OnOffState(false);

                                    if (reader.GetBoolean(9))
                                    {
                                        GetMyDefects(name);
                                    }

                                    NoteBox.Text = reader.GetString(15).TrimEnd();


                                }
                            }
                        }
                        catch (Exception ee) { String error = ee.ToString(); }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Loads a signature for the specified check/person
        /// </summary>
        private async void loadSig(String date2, int shift, String pather, InkCanvas inky)
        {
            try
            {
                StorageFolder mainFolder = await KnownFolders.MusicLibrary.CreateFolderAsync(date2, CreationCollisionOption.OpenIfExists);
                IReadOnlyList<StorageFolder> Folders = await mainFolder.GetFoldersAsync();
                List<String> folderNames = new List<string>();

                foreach (StorageFolder thisFolder in Folders)
                {
                    folderNames.Add(thisFolder.Name);
                }
                ObservableCollection<String> checkNames = new ObservableCollection<string>();
                if (folderNames.Contains("Morning"))
                {
                    checkNames = ShiftSource;
                }
                else if (folderNames.Contains("Shift Start"))
                {
                    checkNames = newShiftSource;
                }

                String shiftName = checkNames[shift];
                StorageFolder sigFolder = await mainFolder.CreateFolderAsync(shiftName, CreationCollisionOption.OpenIfExists);
                StorageFile newFile = await sigFolder.CreateFileAsync(pather, CreationCollisionOption.OpenIfExists);


                // User selects a file and picker returns a reference to the selected file.
                if (newFile != null)
                {
                    // Open a file stream for reading.
                    IRandomAccessStream stream = await newFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    // Read from file.
                    using (var inputStream = stream.GetInputStreamAt(0))
                    {
                        await inky.InkPresenter.StrokeContainer.LoadAsync(inputStream);

                    }
                    stream.Dispose();
                }
            }
            catch { }
        }

        /// <summary>
        /// Loads the defects for the specified check
        /// </summary>
        private void GetMyDefects(String SpecialID)
        {
            String output = "SELECT * FROM QualityCheckDefects WHERE(SpecialCheck = '" + SpecialID + "');";
            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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
                                D0.IsChecked = reader.GetBoolean(1);
                                D1.IsChecked = reader.GetBoolean(2);
                                D2.IsChecked = reader.GetBoolean(3);
                                D3.IsChecked = reader.GetBoolean(4);
                                D4.IsChecked = reader.GetBoolean(5);
                                D5.IsChecked = reader.GetBoolean(6);
                                D6.IsChecked = reader.GetBoolean(7);
                                D7.IsChecked = reader.GetBoolean(8);
                                D8.IsChecked = reader.GetBoolean(9);
                                D9.IsChecked = reader.GetBoolean(10);
                                D10.IsChecked = reader.GetBoolean(11);
                                D11.IsChecked = reader.GetBoolean(12);
                            }
                        }
                    }
                    catch (Exception ee) { String error = ee.ToString(); }
                }
            }
        }
    }
}
