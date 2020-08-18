using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EmailForwarder
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        static string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSettingsSharing, GmailService.Scope.GmailLabels,
                                    GmailService.Scope.GmailModify, GmailService.Scope.GmailSettingsBasic, GmailService.Scope.GmailCompose, GmailService.Scope.GmailInsert,
                                    GmailService.Scope.GmailSend, GmailService.Scope.MailGoogleCom };
        static string ApplicationName = "Gmail Forwarder";

        GmailService service = GmailServices();
        ObservableCollection<Data> loadedData = new ObservableCollection<Data>();
        List<string> filterTypes = new List<string>();
        string Address;
        string ThreadIDAddressLocation = "ThreadAddress.txt";
        string SavedDataLocation = "SavedData.txt";
        int FilterType = 1;

        public MainWindow()
        {
            filterTypes.Add("Filtruj po adresie Email");
            filterTypes.Add("Filtruj po słowie kluczowym w temacie");
            var profile = service.Users.GetProfile("me").Execute();
            Address = profile.EmailAddress;
            Utilities.FileClean(ThreadIDAddressLocation);
            InitializeComponent();
        }

        private void LoadedWindow(object sender, RoutedEventArgs e)
        {
            loadedData = Data.LoadData(Address, ThreadIDAddressLocation, SavedDataLocation, service);
            
            ListofActiveFilters.ItemsSource = loadedData;
            FiltrSelection.ItemsSource = filterTypes;
            FiltrSelection.SelectedItem = filterTypes[0];
            
        }

        private void CreateOnClick(object sender, RoutedEventArgs e)
        {
            
            Utilities.CheckNRun(ref loadedData, Address, AddressToForward.Text, AddressFromForward.Text, LabelName.Text,SavedDataLocation, ThreadIDAddressLocation, FilterType, service);
            ListofActiveFilters.ItemsSource = loadedData;
        }

        private void ResetOnClick(object sender, RoutedEventArgs e)
        {
            AddressFromForward.Clear();
            AddressToForward.Clear();
            LabelName.Clear();
        }

        private static GmailService GmailServices()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        private void RemoveOnClick(object sender, RoutedEventArgs e)
        {
            Data toDelete = (Data)ListofActiveFilters.SelectedItem;
            Filtering.FilterDelete(service, toDelete.filter, toDelete.addressToForward, toDelete.labelName);
            loadedData.Remove(toDelete);
            Utilities.SaveToFile(ref loadedData, SavedDataLocation);

            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void FiltrChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FiltrSelection.SelectedItem.ToString() == filterTypes[1])
            {
                FilterType = 2;
            }
            else if(FiltrSelection.SelectedItem.ToString() == filterTypes[0])
            {
                FilterType = 1;
            }
        }
    }
}
