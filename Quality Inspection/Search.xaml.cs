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
        SqlConnection conn;  //database connection item

        ObservableCollection<string> problemDateStrings = new ObservableCollection<string>();
        ObservableCollection<string> dateStrings = new ObservableCollection<string>();
        ObservableCollection<string> defectDateStrings = new ObservableCollection<string>();

        ObservableCollection<String> ChecksList = new ObservableCollection<string>();
        ObservableCollection<String> problemChecksList = new ObservableCollection<string>();


        public Search()
        {
            this.InitializeComponent();
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MasterDateList = e.Parameter as List<MainPage.PartTracker>;
        }

        /// <summary>
        /// Handles the autosuggestion selection
        /// </summary>
        private void ChoseThis(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            String selectedItem = args.SelectedItem.ToString();
            sender.Text = selectedItem.TrimEnd();
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

        private void PartLookupTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            /*dateStrings.Clear();
            defectDateStrings.Clear();
            problemDateStrings.Clear();
            ChecksList.Clear();
            problemChecksList.Clear();*/
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
                                var balls = reader.GetDateTime(0);
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

                    await Task.Delay(500);
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

        private async void SyncSelection(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListView thisList = sender as ListView;
                List<bool> problemList = new List<bool>();
                String output = "Select CheckInfo.CheckName, FirstShiftTime,CheckTime,QualityCheck.CheckNo, DefectCheck, Notes ";
                output += "FROM QualityCheck INNER JOIN CheckInfo ON CheckInfo.CheckNo = QualityCheck.CheckNo ";
                output += "WHERE PartNumber = '" + PartBox.Text + "' AND CheckDate = '" + DateList.SelectedItem.ToString().Split(':')[0] + "' ";
                output += "ORDER BY QualityCheck.CheckNo;";
                ChecksList.Clear();
                problemList.Clear();
                problemChecksList.Clear();

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
                                }
                            }
                        }
                        catch { }
                        output = "Select CheckInfo.CheckName, FirstShiftTime,CheckTime,QualityCheck.CheckNo, DefectCheck, Notes " +
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
                


                await Task.Delay(100);

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

        private async void ToggleList(object sender, RoutedEventArgs e)
        {
            ToggleSwitch thisSwitch = sender as ToggleSwitch;
            if (thisSwitch.IsOn)
            {
                try
                {
                    DateList.ItemsSource = problemDateStrings;
                    CheckList.ItemsSource = problemChecksList;
                }
                catch { }

                await Task.Delay(300);
            }
            else
            {
                try
                {
                    DateList.ItemsSource = dateStrings;
                    CheckList.ItemsSource = ChecksList;
                }
                catch { }

                await Task.Delay(300);
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
            
        }
    }
}
