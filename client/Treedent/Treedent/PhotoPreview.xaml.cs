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
using System.Windows.Media.Imaging;

namespace Treedent
{
    public partial class PhotoPreviewPage : PhoneApplicationPage, ITreedentPage, INotifyPropertyChanged
    {
        private TreedentClient client;
        // Constructor
        public PhotoPreviewPage()
        {
            buttonsEnabled = true;
            InitializeComponent();

            popup = new LoginPopup(this);
            MainWindowLoginPopup.Child = popup;
            top = new TopPanel(this);
            TitlePanel.Children.Add(top);
            client = TreedentState.Instance.Client;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DoUpdateTop();
            BitmapImage bi = new BitmapImage();
            Stream s = new MemoryStream(TreedentState.Instance.CurrentPhoto);
            bi.SetSource(s);
            Source = bi;
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

        public void ProgressAction(object sender, UploadProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ProgressBarValue = e.ProgressPercentage;
            });
        }

        private async void uploadPhotoAsync()
        {
            await client.UploadPhoto(new UploadProgressChangedEventHandler(ProgressAction));
            TreedentMessage message = await client.GetResult();
            Dispatcher.BeginInvoke(() =>
            {
                if (message.Code == 999)
                {
                    StatusText = message.DataMap["error"];
                }
                else if (message.Code == 5)
                {
                    DoNavigateMain();
                }
                else
                {
                    StatusText = "Invalid message, code: " + message.Code;
                }
                ButtonsEnabled = true;
                progressBarEnabled = false;
            });
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (TreedentState.Instance.LoggedIn)
            {
                ButtonsEnabled = false;
                progressBarEnabled = true;
                Task.Factory.StartNew(() =>
                {
                    uploadPhotoAsync();
                });
            }
            else
            {
                DoShowLogin();
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            Navigator.GoBack();
        }

        private TopPanel top;
        private LoginPopup popup;

        private ImageSource source;
        public ImageSource Source
        {
            get { return source; }
            set
            {
                source = value;
                OnPropertyChanged("Source");
            }
        }

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

        private bool progressBarEnabled = false;
        public bool ProgressBarEnabled
        {
            get { return progressBarEnabled; }
            set
            {
                progressBarEnabled = value;
                OnPropertyChanged("ProgressBarEnabled");
            }
        }

        private int progressBarValue = 0;
        public int ProgressBarValue
        {
            get { return progressBarValue; }
            set
            {
                progressBarValue = value;
                OnPropertyChanged("ProgressBarValue");
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}