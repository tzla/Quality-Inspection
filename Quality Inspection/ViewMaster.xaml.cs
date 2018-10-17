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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;
using System.Globalization;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Quality_Inspection
{
    /// <summary>
    /// Can review past reports via calender selection
    /// </summary>
    public sealed partial class ViewMaster : Page
    {
        SolidColorBrush LSB = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue); //preset colors
        SolidColorBrush SB = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
        SolidColorBrush LG = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.White);
        SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red);

        CalendarViewDayItem newDay = new CalendarViewDayItem();//allows for disabling of unworked days
        String chooseDate = "";
        String mySpecialID = "";

        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };
        ObservableCollection<string> newShiftSource = new ObservableCollection<string> { "Shift Start", "Hour 1", "First Break", "Hour 3", "Hour 4", "After Lunch", "Hour 6", "Second Break", "Hour 8" };
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Morning", "First", "Lunch", "Second" };//source lists
        ObservableCollection<string> RealShifts = new ObservableCollection<string> { "1st", "2nd", "3rd" };

        List<DateTimeOffset> masterList = new List<DateTimeOffset>(); //master date list for enabling worked days
        List<List<Button>> buttonTracker = new List<List<Button>>(); //list of generated buttons
        MainPage.QualityCheck loadCheck = new MainPage.QualityCheck(); //the selected quality report
        MainPage.PartMaster partMasterList = new MainPage.PartMaster(); //list used to populate buttons

        List<string> MasterPartList = new List<string>(); //!!!! unsure why this is here

        StorageFolder shiftFolder;

        bool load = false; //two load flags
        bool load2 = false;

        List<GetCheck> loadChecks = new List<GetCheck>();

        public class GetCheck
        {
            public String partNumber { get; set; }
            public int checkNumber { get; set; }
            public int checkShift { get; set; }
            public int lineNumber { get; set; }
            public String[] SpecialID { get; set; }
        }


        /// <summary>
        /// Initialize component
        /// </summary>
        public ViewMaster()
        {

            this.InitializeComponent();
            Shifter.ItemsSource = RealShifts;
            ShiftBox.ItemsSource = newShiftSource;
            LineBox.ItemsSource = LineSource;
            RealShiftBox.ItemsSource = RealShifts;
            Shifter.SelectedIndex = 0;
        }


        /// <summary>
        /// Processes the navigation from MainPage
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.InitializeComponent();

            //caches page for reloading when navigating to different pages
            SQL_MasterDate();
            for (int i = 0; i < 9; i++)
            {
                List<Button> newList = new List<Button>();
                buttonTracker.Add(newList);
            }
            Cale.SetDisplayDate(DateTimeOffset.Now.Date);
            SQL_Loader(DateTimeOffset.Now.Date.ToString("yyyy-MM-dd"));
            ButtonSetUp();
            loadMaster();
            chooseDate = DateTimeOffset.Now.Date.ToString("yyyy-MM-dd");
            DDD.Visibility = Visibility.Collapsed;

        }

        private void SQL_MasterDate()
        {
            String output = "SELECT DISTINCT CheckDate FROM QualityCheck ORDER BY CheckDate;";
            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = output;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //string[] parts = reader.GetString(0).Split('-');
                            //DateTime dt = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                            try
                            {
                                DateTimeOffset dayer = reader.GetDateTime(0);

                                masterList.Add(dayer);
                            }
                            catch (Exception ee)
                            {
                                Console.WriteLine(ee.ToString());
                            }


                        }
                    }

                }
            }

        }

        private void SQL_Loader(String date)
        {
            String output = "SELECT PartNumber,CheckNo,CheckShift,LineNumber,SpecialCheck FROM QualityCheck";
            output += " WHERE (CheckDate = '";
            output += date + "');";

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
                                GetCheck loadCheck = new GetCheck();
                                loadCheck.partNumber = reader.GetString(0);
                                loadCheck.checkNumber = (int)reader.GetByte(1); ;
                                loadCheck.checkShift = (int)reader.GetByte(2);
                                loadCheck.lineNumber = (int)reader.GetInt32(3);
                                loadCheck.SpecialID = reader.GetString(4).Split('-');
                                loadChecks.Add(loadCheck);
                            }
                        }
                    }
                    catch (Exception ee) { String error = ee.ToString(); }
                }

            }
        }

        private void ButtonSetUp()
        {
            if (load2)
            {
                //Grud.Children.Clear();
            }
            makeButtons(1); makeButtons(2); makeButtons(3); makeButtons(4);
            makeButtons(5); makeButtons(6); makeButtons(7); makeButtons(8); makeButtons(9);
            load2 = true;
        }

        private void makeButtons(int col)
        {
            if (!load2)
            {
                List<Button> buttonCol = new List<Button>();
                for (int i = 0; i < 15; i++)
                {
                    Button newButton = new Button();
                    newButton.HorizontalAlignment = HorizontalAlignment.Center;
                    newButton.VerticalAlignment = VerticalAlignment.Center;
                    newButton.Height = 26;
                    newButton.Width = 75;
                    newButton.FontSize = 8.5;
                    newButton.Name = i.ToString() + "_" + (col - 1).ToString();
                    newButton.IsEnabled = false;
                    Grud.Children.Add(newButton);
                    Grid.SetRow(newButton, i + 1);
                    Grid.SetColumn(newButton, col);
                    newButton.Content = "N/A";
                    buttonCol.Add(newButton);
                    newButton.Click += buttonClick;
                }
                buttonTracker[col - 1] = buttonCol;
            }
            else
            {
                ButtonTask();
            }
        }

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

        private void OnOffState(bool state)
        {
            D0.IsEnabled = D1.IsEnabled = D2.IsEnabled = D3.IsEnabled = D4.IsEnabled = D5.IsEnabled = D6.IsEnabled
                 = D7.IsEnabled = D8.IsEnabled = D9.IsEnabled = D10.IsEnabled = D11.IsEnabled = state;
            NoteBox.IsEnabled = DefectCheck_true.IsEnabled = DefectCheck_false.IsEnabled = LidCheck_true.IsEnabled = LidCheck_false.IsEnabled = state;
            SampleCheck_true.IsEnabled = SampleCheck_false.IsEnabled = PackageCheck_true.IsEnabled = PackageCheck_false.IsEnabled = state;
            PartBox.IsEnabled = state;
            NoteBox.IsReadOnly = !state;
        }

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

        private async void buttonClick(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            String[] locate = clickedButton.Name.Split('_');

            Cale.Visibility = Shifter.Visibility = gg.Visibility = BackButton.Visibility = Visibility.Collapsed;
            loadDate.IsEnabled = ShiftBox.IsEnabled = RealShiftBox.IsEnabled = LineBox.IsEnabled = false;



            //gg.Text = clickedButton.Name;
            String date = "";
            try { date = chooseDate; }
            catch { date = DateTimeOffset.Now.Date.ToString("yyyy-MM-dd"); }
            String name = date + "-" + Shifter.SelectedIndex + "-" + locate[1].ToString() + "-" + locate[0].ToString();
            String output = "SELECT * FROM QualityCheck WHERE(SpecialCheck = '" + name + "');";
            mySpecialID = name;
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
                                gg.Text = name;

                                loadDate.Date = reader.GetDateTime(1);

                                ShiftBox.SelectedIndex = reader.GetByte(3);

                                RealShiftBox.SelectedIndex = Shifter.SelectedIndex;

                                LineBox.SelectedIndex = reader.GetInt32(10);

                                QT_Initials.Text = reader.GetString(11);
                                DS_Initials.Text = reader.GetString(12);
                                Sup_Initials.Text = reader.GetString(17);

                               
                                
                                String date2 = date.Replace('-', '_');
                                try { loadSig(date2, reader.GetByte(3), reader.GetString(13).TrimEnd(), inkyCanvas); } catch { }
                                try { loadSig(date2, reader.GetByte(3), reader.GetString(14).TrimEnd(), inkyCanvasDS); } catch { }
                                try { loadSig(date2, reader.GetByte(3), reader.GetString(18).TrimEnd(), inkyCanvasSup); } catch { }



                                PartBox.Text = reader.GetString(5).TrimEnd();

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
            GGG.Visibility = Visibility.Collapsed;
            DDD.Visibility = Visibility.Visible;
        }


        private void ClickView(object sender, PointerRoutedEventArgs e)
        {
            ViewBorder.BorderBrush = SLB;
            ViewBorder.Background = SB;
            Cale.Visibility = Shifter.Visibility = gg.Visibility = BackButton.Visibility = Visibility.Visible;
            EditSwitch.IsOn = false;
        }

        private void ClickUnview(object sender, PointerRoutedEventArgs e)
        {
            ViewBorder.BorderBrush = SB;
            ViewBorder.Background = LSB;
            ButtonBack();
        }

        private async void ButtonBack()
        {
            GGG.Visibility = Visibility.Visible;
            DDD.Visibility = Visibility.Collapsed;
            Cale.Visibility = Shifter.Visibility = gg.Visibility = BackButton.Visibility = Visibility.Visible;
        }


        private async void loadMaster()
        {
            buttonBob();
        }

        private async void buttonBob()
        {
            foreach (GetCheck thisCheck in loadChecks)
            {
                int col = Convert.ToInt16(thisCheck.SpecialID[4]);
                int row = Convert.ToInt16(thisCheck.SpecialID[5]);
                String content = thisCheck.partNumber;
                List<Button> thisButtonList = buttonTracker[col];
                Button myButton = thisButtonList[row];
                myButton.Content = content;
                myButton.IsEnabled = true;
            }
        }

        private async void CalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (!load)
            {
                // Render basic day items.
                if (args.Phase == 0)
                {
                    // Register callback for next phase.
                    args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
                }
                // Set blackout dates.
                else if (args.Phase == 1)
                {
                    gg.Text = args.Item.Date.ToString();
                    // Blackout dates in the past, Sundays, and dates that are fully booked.
                    if (args.Item.Date.DayOfWeek == DayOfWeek.Sunday || !masterList.Contains(args.Item.Date.Date))
                    {
                        args.Item.IsBlackout = true;

                    }
                    // Register callback for next phase.
                    args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
                }

                // Set density bars.
            }
            else
            {
                args.Item.Background = new SolidColorBrush(Windows.UI.Colors.Aquamarine);

            }
        }
        private async Task saveMaster()
        {
        }

        private void ClearButtons()
        {
            foreach (List<Button> thisCol in buttonTracker)
            {
                foreach (Button thisButton in thisCol)
                {
                    Grud.Children.Remove(thisButton);
                }
            }
        }

        private async void ButtonTask()
        {
            
            loadChecks.Clear();
            SQL_Loader(chooseDate);
            ClearButtons();
            buttonTracker.Clear();
            
            for (int i = 0; i < 9; i++)
            {
                List<Button> newList = new List<Button>();
                buttonTracker.Add(newList);
            }
            ButtonSetUp();


            loadMaster();
        }
        
        private async void hola(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            //this.Background = Red;
            load2 = false;
            try { chooseDate = Cale.SelectedDates[0].Date.ToString("yyyy-MM-dd"); } catch { }
            ButtonTask(); 
        }

        private void BackToMain(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

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

        private void SaveClick(object sender, PointerRoutedEventArgs e)
        {
            if (EditSwitch.IsOn)
            {
                SaveBorder.BorderBrush = SLB;
                SaveBorder.Background = SB;
                String output = "UPDATE QualityCheck SET ";
                output += "PartNumber = '" + PartBox.Text + "', ";
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

                String output2 = "UPDATE QualityCheckDefects SET ";

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
                String content = PartBox.Text;
                List<Button> thisButtonList = buttonTracker[col];
                Button myButton = thisButtonList[row];
                myButton.Content = content;









            }

        }


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
                List<Button> thisButtonList = buttonTracker[col];
                Button myButton = thisButtonList[row];
                myButton.Content = "N/A";
                myButton.IsEnabled = false;









            }

        }

        private void SaveUnclick(object sender, PointerRoutedEventArgs e)
        {
            if (EditSwitch.IsOn)
            {
                SaveBorder.BorderBrush = SB;
                SaveBorder.Background = LSB;
                ButtonBack();
                EditSwitch.IsOn = false;
                
            }



        }

        private void TrashUnclick(object sender, PointerRoutedEventArgs e)
        {
            if (EditSwitch.IsOn)
            {
                TrashBorder.BorderBrush = SB;
                TrashBorder.Background = LSB;
                
                EditSwitch.IsOn = false;
                ButtonBack();
            }
            

        }

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

        private void ChoseThis(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            String selectedItem = args.SelectedItem.ToString();
            sender.Text = selectedItem.TrimEnd();
        }

        private void PartLookupTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<String> masterPartList = new List<string>();
            sender.Text = sender.Text.ToUpper();
            String thisString = "SELECT DISTINCT PartNumber FROM QualityCheck ORDER BY PartNumber;";
            //Creates Master List of Part from Database
            using (SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-10DBF13\SQLEXPRESS;Initial Catalog=QualityControl;Integrated Security=SSPI"))
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
                    catch (Exception e) {  }
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
            catch (Exception e) {  }
        }
    }

}
