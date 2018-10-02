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
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.WhiteSmoke);
        SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red);

        CalendarViewDayItem newDay = new CalendarViewDayItem();//allows for disabling of unworked days

        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Morning", "First", "Lunch", "Second" };//source lists

        List<DateTimeOffset> masterList; //master date list for enabling worked days
        List<List<Button>> buttonTracker = new List<List<Button>>(); //list of generated buttons
        MainPage.QualityCheck loadCheck = new MainPage.QualityCheck(); //the selected quality report
        MainPage.PartMaster partMasterList = new MainPage.PartMaster(); //list used to populate buttons

        List<string> MasterPartList = new List<string>(); //!!!! unsure why this is here

        StorageFolder shiftFolder;

        bool load = false; //two load flags
        bool load2 = false;

        /// <summary>
        /// Initialize component
        /// </summary>
        public ViewMaster()
        {
            this.InitializeComponent();     
        }

        /// <summary>
        /// Processes the navigation from MainPage
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            masterList = e.Parameter as List<DateTimeOffset>;
            for(int i =0;i<4;i++)
            {
                List<Button> newList = new List<Button>();
                buttonTracker.Add(newList);
            }
            Cale.SetDisplayDate(DateTimeOffset.Now.Date);
            loadMaster();
            makeButtons(1); makeButtons(2); makeButtons(3); makeButtons(4);
            DDD.Visibility = Visibility.Collapsed;
            
        }

        private void makeButtons(int col)
        {
            List<Button> buttonCol = new List<Button>();
            for(int i =0;i<15;i++)
            {
                Button newButton = new Button();
                newButton.HorizontalAlignment = HorizontalAlignment.Center;
                newButton.VerticalAlignment = VerticalAlignment.Center;
                newButton.Height = 26;
                newButton.Width = 100;
                newButton.FontSize = 12;
                newButton.Name = i.ToString() + "_" + (col-1).ToString();
                newButton.IsEnabled = false;
                Grud.Children.Add(newButton);
                Grid.SetRow(newButton, i+1);
                Grid.SetColumn(newButton, col);
                newButton.Content = i.ToString() + " Button";
                buttonCol.Add(newButton);
                newButton.Click += buttonClick;
            }
            buttonTracker[col - 1] = buttonCol;
        }

        private async void buttonClick(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            String[] locate = clickedButton.Name.Split('_');

            //gg.Text = clickedButton.Name;
            String date;
            try { date = Cale.SelectedDates[0].ToString("yyyy_MM_dd"); }
            catch { date = DateTimeOffset.Now.ToString("yyyy_MM_dd"); }
            String name = "Sheet_" + date + "_" + LineSource[Convert.ToInt16(locate[0])] + "_" + ShiftSource[Convert.ToInt16(locate[1])] + ".json";
            StorageFolder localFolder = KnownFolders.MusicLibrary;
            StorageFolder dayFolder = await localFolder.GetFolderAsync(date);
            shiftFolder = await dayFolder.GetFolderAsync(ShiftSource[Convert.ToInt16(locate[1])]);
            StorageFile localFile = await shiftFolder.GetFileAsync(name);
            String json = await FileIO.ReadTextAsync(localFile);
            loadCheck = JsonConvert.DeserializeObject(json, typeof(MainPage.QualityCheck)) as MainPage.QualityCheck;
            gg.Text = loadCheck.partName;
            GGG.Visibility = Visibility.Collapsed;
            DDD.Visibility = Visibility.Visible;
            loadDate.Date = loadCheck.date;
            Time.Text = loadCheck.date.ToString("hh:mm");
            PartBox.Text = loadCheck.partName;
            LineBox.Text = loadCheck.lineName;
            ShiftBox.Text = ShiftSource[loadCheck.checkNumber];
            NoteBox.Text = loadCheck.notes;
            if (loadCheck.sampleMatch)
            {
                SampleCheck_true.IsChecked = true;
            }
            else
            {
                SampleCheck_false.IsChecked = true;
            }
            if (loadCheck.lidMatch)
            {
                LidCheck_true.IsChecked = true;
            }
            else
            {
                LidCheck_false.IsChecked = true;
            }
            if (loadCheck.boxMatch)
            {
                PackageCheck_true.IsChecked = true;
            }
            else
            {
                PackageCheck_false.IsChecked = true;
            }
            if (loadCheck.defects)
            {
                DefectCheck_true.IsChecked = true;
            }
            else
            {
                DefectCheck_false.IsChecked = true;
            }
            try { QT_Initials.Text = loadCheck.initals[0]; } catch{ }
            try { DS_Initials.Text = loadCheck.initals[1]; } catch{ }
            if (loadCheck.defectList[0]) { D0.IsChecked = true; }
            if (loadCheck.defectList[1]) { D1.IsChecked = true; }
            if (loadCheck.defectList[2]) { D2.IsChecked = true; }
            if (loadCheck.defectList[3]) { D3.IsChecked = true; }
            if (loadCheck.defectList[4]) { D4.IsChecked = true; }
            if (loadCheck.defectList[5]) { D5.IsChecked = true; }
            if (loadCheck.defectList[6]) { D6.IsChecked = true; }
            if (loadCheck.defectList[7]) { D7.IsChecked = true; }
            if (loadCheck.defectList[8]) { D8.IsChecked = true; }
            if (loadCheck.defectList[9]) { D9.IsChecked = true; }
            if (loadCheck.defectList[10]) { D10.IsChecked = true; }
            if (loadCheck.defectList[11]) { D11.IsChecked = true; }



            try {
                localFile = await shiftFolder.GetFileAsync(loadCheck.sigPath[0]); 
                // User selects a file and picker returns a reference to the selected file.
                if (localFile != null)
                {
                    // Open a file stream for reading.
                    IRandomAccessStream stream = await localFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    // Read from file.
                    using (var inputStream = stream.GetInputStreamAt(0))
                    {
                        await inkyCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                    }
                    stream.Dispose();
                }
            } catch { }
            try
            {
                localFile = await shiftFolder.GetFileAsync(loadCheck.sigPath[1]);
                if (localFile != null)
                {
                    // Open a file stream for reading.
                    IRandomAccessStream stream = await localFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    // Read from file.
                    using (var inputStream = stream.GetInputStreamAt(0))
                    {
                        await inkyCanvasDS.InkPresenter.StrokeContainer.LoadAsync(stream);
                    }
                    stream.Dispose();
                }
            }
            catch { }


            //gg.Text = name;
        }
        private void ClickView(object sender, PointerRoutedEventArgs e)
        {
            ViewBorder.BorderBrush = SLB;
            ViewBorder.Background = SB;
            this.Frame.Navigate(typeof(ViewMaster), masterList);
        }

        private void ClickUnview(object sender, PointerRoutedEventArgs e)
        {
            ViewBorder.BorderBrush = SB;
            ViewBorder.Background = LSB;
            GGG.Visibility = Visibility.Visible;
            DDD.Visibility = Visibility.Collapsed;
        }

        private async void loadMaster()
        {
            try
            {
                StorageFile newFile = await KnownFolders.MusicLibrary.GetFileAsync("MasterParts.json");
                String json = await FileIO.ReadTextAsync(newFile);
                MasterPartList = JsonConvert.DeserializeObject(json, typeof(List<String>)) as List<String>;
            }
            catch { MasterPartList = new List<string>(); }
            if (MasterPartList == null)
            {
                MasterPartList = new List<string>();
            }
            String JsonFile;
            String projectFolderName;
            try
            {
                if(!load2)
                {
                    JsonFile = "PartMaster_" + DateTimeOffset.Now.ToString("yyyy_MM_dd_") + ".json";
                    projectFolderName = DateTimeOffset.Now.ToString("yyyy_MM_dd");
                    load2 = true;
                }
                else
                {
                    JsonFile = "PartMaster_" + Cale.SelectedDates[0].ToString("yyyy_MM_dd_") + ".json";
                    projectFolderName = Cale.SelectedDates[0].ToString("yyyy_MM_dd");
                }
                gg.Text = projectFolderName;
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                StorageFolder projectFolder = await localFolder.GetFolderAsync(projectFolderName);
                StorageFile localFile = await projectFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                partMasterList = JsonConvert.DeserializeObject(JsonString, typeof(MainPage.PartMaster)) as MainPage.PartMaster;
            }
            catch
            {
                partMasterList = new MainPage.PartMaster();
            }
            if (partMasterList.firstList == null) { partMasterList.firstList = new string[15]; }
            if (partMasterList.lunchList == null) { partMasterList.lunchList = new string[15]; }
            if (partMasterList.morningList == null) { partMasterList.morningList = new string[15]; }
            if (partMasterList.secondList == null) { partMasterList.secondList = new string[15]; }
            if (partMasterList.firstNotes == null) { partMasterList.firstNotes = new bool[15]; }
            if (partMasterList.lunchNotes == null) { partMasterList.lunchNotes = new bool[15]; }
            if (partMasterList.morningNotes == null) { partMasterList.morningNotes = new bool[15]; }
            if (partMasterList.secondNotes == null) { partMasterList.secondNotes = new bool[15]; }
            buttonBob();
        }

        private async void buttonBob()
        {
            int i = 0;
            List<Button> thisButtonList = buttonTracker[0];
            foreach (String thisName in partMasterList.morningList)
            {

                if (thisName != null)
                {
                    thisButtonList[i].Content = thisName;
                    thisButtonList[i].IsEnabled = true;
                }
                else
                {
                    thisButtonList[i].Content = "N/A";
                    thisButtonList[i].IsEnabled = false;
                }
                if (partMasterList.morningNotes[i]) { thisButtonList[i].Foreground = Red; }
                i++;
            }
            thisButtonList = buttonTracker[1];
            i = 0;
            foreach (String thisName in partMasterList.firstList)
            {

                if (thisName != null)
                {
                    thisButtonList[i].Content = thisName;
                    thisButtonList[i].IsEnabled = true;
                }
                else
                {
                    thisButtonList[i].Content = "N/A";
                    thisButtonList[i].IsEnabled = false;
                }
                if (partMasterList.firstNotes[i]) { thisButtonList[i].Foreground = Red; }
                i++;
            }
            thisButtonList = buttonTracker[2];
            i = 0;
            foreach (String thisName in partMasterList.lunchList)
            {

                if (thisName != null)
                {
                    thisButtonList[i].Content = thisName;
                    thisButtonList[i].IsEnabled = true;
                }
                else
                {
                    thisButtonList[i].Content = "N/A";
                    thisButtonList[i].IsEnabled = false;
                }
                if (partMasterList.lunchNotes[i]) { thisButtonList[i].Foreground = Red; }
                i++;
            }
            thisButtonList = buttonTracker[3];
            i = 0;
            foreach (String thisName in partMasterList.secondList)
            {

                if (thisName != null)
                {
                    thisButtonList[i].Content = thisName;
                    thisButtonList[i].IsEnabled = true;
                }
                else
                {
                    thisButtonList[i].Content = "N/A";
                    thisButtonList[i].IsEnabled = false;
                }
                if (partMasterList.secondNotes[i]) { thisButtonList[i].Foreground = Red; }
                i++;
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
                try
                {
                    String JsonFile = "PartMaster_" + args.Item.Date.Date.ToString("yyyy_MM_dd") + ".json";
                    StorageFolder localFolder = KnownFolders.MusicLibrary;
                    var projectFolderName = DateTimeOffset.Now.ToString("yyyy_MM_dd");
                    StorageFolder projectFolder = await localFolder.GetFolderAsync(projectFolderName);
                    StorageFile localFile = await projectFolder.GetFileAsync(JsonFile);
                    String JsonString = await FileIO.ReadTextAsync(localFile);
                    partMasterList = JsonConvert.DeserializeObject(JsonString, typeof(MainPage.PartMaster)) as MainPage.PartMaster;
                    makeButtons(1);
                    
                }
                catch { }
            }
        }
        private async Task saveMaster()
        {
            string json = JsonConvert.SerializeObject(MasterPartList);
            string fileName = "MasterParts.json";
            StorageFile newFile = await KnownFolders.MusicLibrary.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(newFile, json);
        }

        private async void hola(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            try { gg.Text = args.AddedDates[0].ToString(); } catch { }
            
                foreach (string part in partMasterList.firstList)
                {
                    if (!MasterPartList.Contains(part) && part != null)
                    {
                        MasterPartList.Add(part);
                    }
                }
            
            try
            {
                foreach (String part in partMasterList.morningList)
                {
                    if (!MasterPartList.Contains(part) && part != null)
                    {
                        MasterPartList.Add(part);
                    }
                }
            }catch{ }
            try
            {
                foreach (String part in partMasterList.lunchList)
                {
                    if (!MasterPartList.Contains(part) && part != null)
                    {
                        MasterPartList.Add(part);
                    }
                }
            }
            catch { }
            try
            {
                foreach (String part in partMasterList.secondList)
                {
                    if (!MasterPartList.Contains(part) && part != null)
                    {
                        MasterPartList.Add(part);
                    }
                }
            }
            catch { }
            await saveMaster();

            loadMaster();
            
        }

        private void BackToMain(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }

}
