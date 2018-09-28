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
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Quality_Inspection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };
        ObservableCollection<string> ShiftSource = new ObservableCollection<string> { "Morning", "First", "Lunch", "Second" };
        SolidColorBrush LSB = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue);
        SolidColorBrush SB = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
        SolidColorBrush LG = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush White = new SolidColorBrush(Windows.UI.Colors.White);
        SolidColorBrush SLB = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
        SolidColorBrush Red = new SolidColorBrush(Windows.UI.Colors.Red);
        SolidColorBrush Orng = new SolidColorBrush(Windows.UI.Colors.Orange);
        SolidColorBrush Yellow = new SolidColorBrush(Windows.UI.Colors.Yellow);
        SolidColorBrush Green = new SolidColorBrush(Windows.UI.Colors.Green);


        //private MainPage rootPage = MainPage.Current;

        List<CheckBox> DefectList = new List<CheckBox>();
        QualityCheck newCheck = new QualityCheck();
        StorageFolder saveHere;
        const int FPS = 60;

        DateTimeOffset beginTimeOfRecordedSession;
        DateTimeOffset endTimeOfRecordedSession;
        TimeSpan durationOfRecordedSession;
        DateTime beginTimeOfReplay;

        DispatcherTimer inkReplayTimer;

        InkStrokeBuilder strokeBuilder;
        IReadOnlyList<InkStroke> strokesToReplay;
        

        InkStrokeContainer[] twoSigs = new InkStrokeContainer[2];

        List<DateTimeOffset> masterList = new List<DateTimeOffset>();
        PartMaster partMasterList = new PartMaster();
        public class PartMaster
        {
            public string[] morningList { get; set; }
            public string[] firstList { get; set; }
            public string[] lunchList { get; set; }
            public string[] secondList { get; set; }
        }

        //List<bool[]> masterList = new List<bool[]>();
        bool[] thisList;
        bool isLoaded = false;
        public class QualityCheck
        {
            public bool sampleMatch { get; set; }
            public bool boxMatch { get; set; }
            public bool lidMatch { get; set; }
            public bool defects { get; set; }
            public DateTimeOffset date { get; set; }
            public int checkNumber { get; set; }
            public string partName { get; set; }
            public string lineName { get; set; }
            public bool[] defectList { get; set; }
            public string[] sigPath { get; set; }
            public string[] initals { get; set; }
        }


        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            LineBox.ItemsSource = LineSource;
            LineBox.SelectedIndex = 0;
            ShiftBox.ItemsSource = ShiftSource;
            ShiftBox.SelectedIndex = 0;
            newCheck.sigPath = new string[2];
            newCheck.initals = new string[2];

            

            TimeSpan period = TimeSpan.FromSeconds(5);

            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {

                Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        Time.Text = DateTime.Now.ToString("hh:mm");
                    });

            }, period);




            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Colors.Black;
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            SetupMasterList();
            DefectListSetup();
            isLoaded = true;
        }

        private async void SetupMasterList()
        {
            thisList = new bool[15];
            for (int i = 0; i < 15; i++)
            {
                thisList[i] = false;
            }
            try
            {
                String JsonFile = "MasterTracker.json";
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                StorageFile localFile = await localFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                masterList = JsonConvert.DeserializeObject(JsonString, typeof(List<DateTimeOffset>)) as List<DateTimeOffset>;
            }
            catch { masterList = new List<DateTimeOffset>(); }
            try
            {
                String JsonFile = "PartMaster_" + dateBox.Date.ToString("yyyy_MM_dd_") + ".json";
                StorageFolder localFolder = KnownFolders.MusicLibrary;
                var projectFolderName = dateBox.Date.ToString("yyyy_MM_dd");
                StorageFolder projectFolder = await localFolder.GetFolderAsync(projectFolderName);
                StorageFile localFile = await projectFolder.GetFileAsync(JsonFile);
                String JsonString = await FileIO.ReadTextAsync(localFile);
                partMasterList = JsonConvert.DeserializeObject(JsonString, typeof(PartMaster)) as PartMaster;
            }
            catch
            {
                partMasterList = new PartMaster();
            }
            if (partMasterList.firstList == null) {partMasterList.firstList = new string[15];}
            if (partMasterList.lunchList == null) { partMasterList.lunchList = new string[15]; }
            if (partMasterList.morningList == null) { partMasterList.morningList = new string[15]; }
            if (partMasterList.secondList == null) { partMasterList.secondList = new string[15]; }
        }

        private void DefectListSetup()
        {
            DefectList.Add(D0); DefectList.Add(D1); DefectList.Add(D2); DefectList.Add(D3); DefectList.Add(D4); DefectList.Add(D5); DefectList.Add(D6); DefectList.Add(D7);
            DefectList.Add(D8); DefectList.Add(D9); DefectList.Add(D10); DefectList.Add(D11);
        }

        private async void DoThis(object sender, RoutedEventArgs e)
        {
            SignPopup TechSign = new SignPopup();
            ContentDialogResult result = await TechSign.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                InkStrokeContainer thisStrokes = TechSign.returnSig();
                //inkyCanvas.InkPresenter.StrokeContainer.
                inkyCanvas.InkPresenter.StrokeContainer = thisStrokes;
                twoSigs[0] = thisStrokes;
            }
        }

        private async void DoThisDS(object sender, RoutedEventArgs e)
        {
            SignPopup TechSign = new SignPopup();
            ContentDialogResult result = await TechSign.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                InkStrokeContainer thisStrokes = TechSign.returnSig();
                //inkyCanvas.InkPresenter.StrokeContainer.
                inkyCanvasDS.InkPresenter.StrokeContainer = thisStrokes;
                twoSigs[1] = thisStrokes;
            }
        }

        private void SampleCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox)
            {
                SampleCheck_false.IsChecked = false;
                
            }
            else
            {
                SampleCheck_true.IsChecked = false;
            }
            newCheck.sampleMatch = whichBox;
        }

        private void PackageCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox)
            {
                PackageCheck_false.IsChecked = false;
            }
            else
            {
                PackageCheck_true.IsChecked = false;
            }
            newCheck.boxMatch = whichBox;
        }

        private void LidCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox)
            {
                LidCheck_false.IsChecked = false;
            }
            else
            {
                LidCheck_true.IsChecked = false;
            }
            newCheck.lidMatch = whichBox;
        }

        private void DefectCheck(object sender, RoutedEventArgs e)
        {
            CheckBox thisBox = sender as CheckBox;
            string boxName = thisBox.Name;
            string[] splitName = boxName.Split('_');
            bool whichBox = Convert.ToBoolean(splitName[1]);
            if (whichBox)
            {
                DefectCheck_false.IsChecked = false;
                DefectGrid.Background = White;
                foreach (CheckBox checkLister in DefectList)
                {
                    checkLister.IsEnabled = true;
                }
            }
            else
            {
                DefectCheck_true.IsChecked = false;
                DefectGrid.Background = LG;
                foreach(CheckBox checkLister in DefectList)
                {
                    checkLister.IsEnabled = false;
                }
            }
            newCheck.defects = whichBox;
        }
        private async Task newFolders()
        {
            StorageFolder rootFolder = KnownFolders.MusicLibrary;
            var projectFolderName = dateBox.Date.ToString("yyyy_MM_dd");
            StorageFolder projectFolder = await rootFolder.CreateFolderAsync(projectFolderName, CreationCollisionOption.OpenIfExists);
            projectFolderName = ShiftBox.SelectedItem as string;
            saveHere = await projectFolder.CreateFolderAsync(projectFolderName, CreationCollisionOption.OpenIfExists);
        }
        private async Task SaveSign1()
        {
            string fileName = "TechSig_" + dateBox.Date.ToString("yyyy_MM_dd_") + LineBox.SelectedItem +"_" + ShiftBox.SelectedItem +  ".gif"; //+ DateTimeOffset.Now.ToString("yyyy_MM_dd_HH:MM") 

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

        private async Task SaveSign2()
        {
            string fileName = "DSSig_" + dateBox.Date.ToString("yyyy_MM_dd_") + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".gif"; //+ DateTimeOffset.Now.ToString("yyyy_MM_dd_HH:MM") 

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
            //return newFile;
        }
        private bool[] DefectListMaker()
        {
            bool[] list = new bool[15];
            list[0] = Convert.ToBoolean(D0.IsChecked); list[1] = Convert.ToBoolean(D1.IsChecked); list[2] = Convert.ToBoolean(D2.IsChecked);
            list[3] = Convert.ToBoolean(D3.IsChecked); list[4] = Convert.ToBoolean(D4.IsChecked); list[5] = Convert.ToBoolean(D2.IsChecked);
            list[6] = Convert.ToBoolean(D6.IsChecked); list[7] = Convert.ToBoolean(D7.IsChecked); list[8] = Convert.ToBoolean(D2.IsChecked);
            list[9] = Convert.ToBoolean(D9.IsChecked); list[10] = Convert.ToBoolean(D10.IsChecked); list[11] = Convert.ToBoolean(D2.IsChecked);


            return list;
        }

        private async void BoopBoop(object sender, RoutedEventArgs e)
        {
            await newFolders();
            newCheck.lineName = LineBox.SelectedItem as string;
            thisList[LineBox.SelectedIndex] = true;
            newCheck.partName = PartBox.Text;
            newCheck.date = DateTimeOffset.Now.LocalDateTime;
            newCheck.checkNumber = ShiftBox.SelectedIndex;
            string[] initials = { QT_Initials.Text, DS_Initials.Text };
            newCheck.initals = initials;
            
            try { await SaveSign1(); } catch { }
            try { await SaveSign2(); } catch { }
            if(!masterList.Contains(dateBox.Date.Date))
            {
                masterList.Add(dateBox.Date.Date);
            }
            if (ShiftBox.SelectedIndex == 0)
            { partMasterList.morningList[LineBox.SelectedIndex] = PartBox.Text; }
            else if (ShiftBox.SelectedIndex == 1)
            { partMasterList.firstList[LineBox.SelectedIndex] = PartBox.Text; }
            else if (ShiftBox.SelectedIndex == 2)
            { partMasterList.lunchList[LineBox.SelectedIndex] = PartBox.Text; }
            else if (ShiftBox.SelectedIndex == 3)
            { partMasterList.secondList[LineBox.SelectedIndex] = PartBox.Text; }

            try { newCheck.defectList = DefectListMaker(); } catch { }


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
            //try
            //{
            /*
               // Open a file stream for writing.

           }
           catch { }
           */
            this.Background = White;
            LightLeds();
            //QualityCheck newCheck = new QualityCheck();
            //newCheck.sampleMatch = 
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
        }

        private void dateChange(object sender, DatePickerValueChangedEventArgs e)
        {
            SetupMasterList();
            DefectListSetup();
        }

        private void LoadShiftLED(object sender, SelectionChangedEventArgs e)
        {
            LightLeds();
        }

        private void LightLeds()
        { 
            S1.Fill = S2.Fill = S3.Fill = S4.Fill = White;
            int whichLine = LineBox.SelectedIndex;
            try
            {
                if (partMasterList.morningList[whichLine] != null)
                {
                    S1.Fill = Red;
                }
            }
            catch { }

            try
            {
                if (partMasterList.firstList[whichLine] != null)
                {
                    S2.Fill = Orng;
                }
            }
            catch { }

            try
            {
                if (partMasterList.lunchList[whichLine] != null)
                {
                    S3.Fill = Yellow;
                }
            }
            catch{ }

            try
            {
                if (partMasterList.secondList[whichLine] != null)
                {
                    S4.Fill = Green;
                }
            }
            catch { }
            loadChange();
        }

        private void ShiftChange(object sender, SelectionChangedEventArgs e)
        {
            loadChange();
        }
        private void loadChange()
        {
            if (isLoaded)
            {
                int shiftCheck = ShiftBox.SelectedIndex;
                int whichLine = LineBox.SelectedIndex;
                string date = dateBox.Date.Date.ToString("yyyy_MM_dd");
                if (shiftCheck == 0)
                {
                    if (partMasterList.morningList[whichLine] != null)
                    {
                        String name = "Sheet_" + date + "_" + LineBox.SelectedItem + "_" + ShiftBox.SelectedItem + ".json";
                    }
                }
                QualityCheck loadCheck = new QualityCheck();
            }


            /*
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
            try { QT_Initials.Text = loadCheck.initals[0]; } catch { }
            try { DS_Initials.Text = loadCheck.initals[1]; } catch { }

            try
            {
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
            }
            catch { }
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
            catch { }*/
        }
    }
}
