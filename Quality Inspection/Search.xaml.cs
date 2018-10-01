using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        ObservableCollection<string> LineSource = new ObservableCollection<string> { "1", "1A", "2", "2A", "3", "3A", "4", "5", "5B", "6", "6A", "7", "8", "9", "10" };
        List<string> partMasterList;
       List<MainPage.PartTracker> MasterDateList;
        public Search()
        {
            this.InitializeComponent();
            loadParts();
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MasterDateList = e.Parameter as List<MainPage.PartTracker>;
        }


        private async void loadParts()
        {
            StorageFile newFile = await KnownFolders.MusicLibrary.GetFileAsync("MasterParts.json");
            String json = await FileIO.ReadTextAsync(newFile);
            partMasterList = JsonConvert.DeserializeObject(json, typeof(List<String>)) as List<String>;

        }

        private void AutoTextChange(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suggestions = SearchControls(sender.Text);
                sender.ItemsSource = suggestions;
            }
        }



        private List<string> SearchControls(string query)
        {
            var suggestions = new List<string>();
            suggestions = partMasterList.Where(x => x.StartsWith(query)).ToList();
            return suggestions;
        }
        
        private void ClickHome(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            bord.Background = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void UnclickHome(object sender, PointerRoutedEventArgs e)
        {
            Image pic = sender as Image;
            Border bord = pic.Parent as Border;
            bord.BorderBrush = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
            bord.Background = new SolidColorBrush(Windows.UI.Colors.LightSteelBlue);
        }

        private void ChoseThis(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = args.SelectedItem.ToString();
            sender.Text = selectedItem;
        }

        private void SubmitQuery(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string PartName = sender.Text;
            List<string> dateStrings = new List<string>();
            List<string> lineStrings = new List<string>();
            foreach (MainPage.PartTracker thisPart in MasterDateList)
            {
                if(thisPart.partName == PartName)
                {
                    List<MainPage.MyDate> theseDates = thisPart.dates;
                    theseDates.OrderBy(x => x.date);
                    foreach (MainPage.MyDate thisdate in theseDates)
                    {
                        dateStrings.Add(thisdate.date.ToString("yyyy-MM-dd"));
                        lineStrings.Add(LineSource[thisdate.lineNumber]);
                       
                    }
                }
            }
            try
            {
                DateList.ItemsSource = dateStrings;
                LineList.ItemsSource = lineStrings;
            }
            catch { }

        }
    }
}
