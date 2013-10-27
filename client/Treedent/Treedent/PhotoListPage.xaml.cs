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
    public partial class PhotoListPage : PhoneApplicationPage, ITreedentPage, INotifyPropertyChanged
    {
        private TreedentClient client;
        // Constructor
        public PhotoListPage()
        {
            buttonsEnabled = true;
            InitializeComponent();

            popup = new LoginPopup(this);
            MainWindowLoginPopup.Child = popup;
            top = new TopPanel(this);
            TitlePanel.Children.Add(top);
        }

        private async void addContent()
        {
            for (int i = 0; i < 5; i++)
            {
                await TreedentState.Instance.Client.GetNext(i - 1);
                TreedentMessage message = await TreedentState.Instance.Client.GetResult();
                if (message.Code == 999) {
                    StatusText = message.DataMap["error"];
                    return;
                }
                if (message.Code == 7) {
                    return;
                }
                byte[] photo = StringToByteArrayFastest(message.DataMap["photo"]);
                TreedentState.Instance.CurrentPhoto = photo;
                int that = Convert.ToInt32(message.DataMap["index"]);
                Dispatcher.BeginInvoke(() =>
                {
                    PhotoEntry entry = new PhotoEntry(this, that);
                    contentStack.Children.Add(entry);
                });
            }
        }

        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!TreedentState.Instance.LoggedIn)
            {
                DoShowLogin();
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    addContent();
                });
            }
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
            PhotoEntry entry = e as PhotoEntry;
            Tuple<int, ImageSource> data = new Tuple<int, ImageSource>(entry.Number, entry.Source);
            Navigator.Navigate("/PhotoAnswerPage.xaml", data);
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

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {

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