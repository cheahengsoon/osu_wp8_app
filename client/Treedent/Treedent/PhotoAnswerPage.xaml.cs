using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Treedent.Resources;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Phone.Media.Capture;
using Microsoft.Xna.Framework.Media;
using Microsoft.Devices;
using System.Windows.Media;
using System.IO;

namespace Treedent
{
    public partial class PhotoAnswerPage : PhoneApplicationPage, ITreedentPage, INotifyPropertyChanged
    {
        private int index;
        // Constructor
        public PhotoAnswerPage()
        {
            buttonsEnabled = true;
            Tuple<int, ImageSource> passed = Navigator.GetLastNavigationData() as Tuple<int, ImageSource>;
            index = passed.Item1;
            InitializeComponent();

            popup = new LoginPopup(this);
            MainWindowLoginPopup.Child = popup;
            top = new TopPanel(this);
            TitlePanel.Children.Add(top);
            SourceImage = passed.Item2;
            Task.Factory.StartNew(() =>
            {
                doSetupAsync();
            });

        }

        private async void doSetupAsync()
        {
            TreedentClient client = TreedentState.Instance.Client;
            await client.GetInfo(index);
            TreedentMessage message = await client.GetResult();
            Dispatcher.BeginInvoke(() =>
            {
                if (message.Code == 999)
                {
                    StatusText = message.DataMap["error"];
                }
                else if (message.Code == 8)
                {
                    PlantName = message.DataMap["name"];
                }
                else if (message.Code == 9)
                {
                    PlantName = "Unknown";
                }
                else
                {
                    StatusText = "Incorrect action, Code: " + message.Code;
                }
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DoUpdateTop();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

        }

        public void DoNavigateRegister()
        {
            Navigator.Navigate("/RegisterPage.xaml");
        }

        public void DoNavigateProfile()
        {

        }

        public void DoHandleTap(Object e)
        {

        }

        public void DoNavigateMain()
        {
            Navigator.Navigate("/MainPage.xaml");
        }

        public void DoShowLogin()
        {
            MainWindowLoginPopup.IsOpen = true;
        }

        public void DoHideLogin()
        {
            MainWindowLoginPopup.IsOpen = false;
            DoUpdateTop();
        }

        public void DoUpdateTop()
        {
            top.RefreshLoginButton();
        }

        private TopPanel top;
        private LoginPopup popup;

        public NavigationService Navigator
        {
            get { return NavigationService; }
        }

        private bool buttonsEnabled;
        public bool ButtonsEnabled
        {
            get { return buttonsEnabled; }
            set
            {
                buttonsEnabled = value;
                OnPropertyChanged("ButtonsEnabled");
            }
        }

        private string statusText;
        public string StatusText
        {
            get { return statusText; }
            set
            {
                statusText = value;
                OnPropertyChanged("StatusText");
            }
        }
        private string plantName;
        public string PlantName
        {
            get { return plantName; }
            set
            {
                plantName = value;
                OnPropertyChanged("PlantName");
            }
        }
        private string userGivenName;
        public string UserGivenName
        {
            get { return userGivenName; }
            set
            {
                userGivenName = value;
                OnPropertyChanged("UserGivenName");
            }
        }

        private ImageSource sourceImage;
        public ImageSource SourceImage
        {
            get { return sourceImage; }
            set
            {
                sourceImage = value;
                OnPropertyChanged("SourceImage");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private async void doUploadAsync()
        {
            TreedentClient client = TreedentState.Instance.Client;
            await client.AddGuess(UserGivenName, index);
            TreedentMessage message = await client.GetResult();
            Dispatcher.BeginInvoke(() =>
            {
                if (message.Code == 999)
                {
                    StatusText = message.DataMap["error"];
                }
                else if (message.Code == 10)
                {
                    Navigator.GoBack();
                }
                else
                {
                    StatusText = "Invalid action, Code: " + message.Code;
                }
                ButtonsEnabled = true;
            });
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled = false;
            Task.Factory.StartNew(() =>
            {
                doUploadAsync();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Navigator.GoBack();
        }
    }
}