using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using NotificationsExtensions.Toasts;
using Windows.UI.Notifications;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WhatToWearApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string _mailAddress;
        private string _displayName = null;
        public static ApplicationDataContainer _settings = ApplicationData.Current.RoamingSettings;
        private int _eventsCount;
        private string _accessToken = null;
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Developer code - if you haven't registered the app yet, we warn you. 
            if (!App.Current.Resources.ContainsKey("ida:ClientId"))
            {
                InfoText.Text = ResourceLoader.GetForCurrentView().GetString("NoClientIdMessage");
                ConnectButton.IsEnabled = false;
            }
            else
            {
                InfoText.Text = ResourceLoader.GetForCurrentView().GetString("ConnectPrompt");
                ConnectButton.IsEnabled = true;
            }
        }

        public async Task<bool> SignInCurrentUserAsync()
        {           
            _accessToken = await AuthenticationHelper.GetAccessToken();
            if (_accessToken != null)
            {
                string userId = (string)_settings.Values["userID"];
                _mailAddress = (string)_settings.Values["userEmail"];
                _displayName = (string)_settings.Values["userName"];
                return true;
            }
            else
            {
                return false;
            }

        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;
            if (await SignInCurrentUserAsync())
            {
                InfoText.Text = "Welcome " + _displayName + "!" + Environment.NewLine;
               
                ConnectButton.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Visible;
                BtnGetEvents.Visibility = Visibility.Visible;
            }
            else
            {
              InfoText.Text = ResourceLoader.GetForCurrentView().GetString("AuthenticationErrorMessage");
            }

            ProgressBar.Visibility = Visibility.Collapsed;
            
        }
        
        private async void CopyRedirectUriToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var redirectURI = AuthenticationHelper.GetAppRedirectURI();

            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(redirectURI);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            Windows.ApplicationModel.DataTransfer.Clipboard.Flush();

            var dialog = new Windows.UI.Popups.MessageDialog(redirectURI, "App Redirect URI copied to clipboard");
            await dialog.ShowAsync();

        }

        private async void BtnGetEvents_Click(object sender, RoutedEventArgs e)
        {
            InfoText.Text = "It seems you have 4 meetings coming up!";
            using (var client = new HttpClient())
            {
                var uri = new Uri("https://graph.microsoft.com/v1.0/me/events");
                client.BaseAddress = uri;
              
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("Authorization", String.Format("Bearer {0}", _accessToken));

                //  HttpResponseMessage response = await client.GetAsync();
                var content = await client.GetStringAsync(uri);
                JObject o = JObject.Parse(content);

                //use linq to sort meetings by type

                //put notification logic here
                setReminder("Suit Up", "All Day Every Day", "Assets/Samples/Toasts/suitUp.png");

                //   }
            }
           // InfoText.Text = "It seems you have " + _displayName + "," + Environment.NewLine;
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;
            AuthenticationHelper.SignOut();
            ProgressBar.Visibility = Visibility.Collapsed;
            ConnectButton.Visibility = Visibility.Visible;
            InfoText.Text = ResourceLoader.GetForCurrentView().GetString("ConnectPrompt");
            this._displayName = null;
            this._mailAddress = null;
            BtnGetEvents.Visibility = Visibility.Collapsed;
        }


        #region "Toast Notification"

        private void Show(ToastContent content)
        {
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(content.GetXml()));
        }

        private void SuitUp_Click(object sender, RoutedEventArgs e)
        {
            setReminder("Suit Up", "10:00 AM - 10:30 AM", "Assets/Samples/Toasts/suitUp.png");
        }

        private void Meeting_Click(object sender, RoutedEventArgs e)
        {
            setReminder("Office Clothes", "10:00 AM - 10:30 AM", "Assets/Samples/Toasts/office.png");
        }

        private void Dev_Click(object sender, RoutedEventArgs e)
        {
            setReminder("Dev Camp Attire", "10:00 AM - 10:30 AM", "Assets/Samples/Toasts/casual.png");
        }

        private void setReminder(String message, String eventTime, String imgPath)
        {

            Show(new ToastContent()
            {
                Scenario = ToastScenario.Reminder,

                Visual = new ToastVisual()
                {
                    TitleText = new ToastText() { Text = message },
                    BodyTextLine1 = new ToastText() { Text = eventTime },
                    InlineImages = { new ToastImage() { Source = new ToastImageSource(imgPath) } }
                },

                Launch = "392914",

                Actions = new ToastActionsCustom()
                {
                    Inputs =
                    {
                        new ToastSelectionBox("snoozeAmount")
                        {
                            Title = "Remind me...",
                            Items =
                            {
                                new ToastSelectionBoxItem("1", "Super soon (1 min)"),
                                new ToastSelectionBoxItem("5", "In a few mins"),
                                new ToastSelectionBoxItem("15", "When it starts"),
                                new ToastSelectionBoxItem("60", "After it's done")
                            },
                            DefaultSelectionBoxItemId = "1"
                        }
                    },

                    Buttons =
                    {
                        new ToastButtonSnooze()
                        {
                            SelectionBoxId = "snoozeAmount"
                        },

                        new ToastButtonDismiss()
                    }
                }
            });
        }
        #endregion

    }
}
