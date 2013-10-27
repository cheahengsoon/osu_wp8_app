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
    public partial class PhotoTakingPage : PhoneApplicationPage, ITreedentPage, INotifyPropertyChanged
    {
        private TreedentClient client;
        // Constructor
        public PhotoTakingPage()
        {
            buttonsEnabled = true;
            InitializeComponent();

            popup = new LoginPopup(this);
            MainWindowLoginPopup.Child = popup;
            top = new TopPanel(this);
            TitlePanel.Children.Add(top);
        }

        private async void setupCamera()
        {
            if (PhotoCaptureDevice.AvailableSensorLocations.Contains(CameraSensorLocation.Back))
            {
                Windows.Foundation.Size res = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back)[0];
                CaptureDevice = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Back, res);

                Seq = CaptureDevice.CreateCaptureSequence(1);
                CaptureStream = new MemoryStream();
                Seq.Frames[0].CaptureStream = CaptureStream.AsOutputStream();
                Dispatcher.BeginInvoke(() =>
                {
                    ViewFinderBrush.SetSource(CaptureDevice);
                });
                await CaptureDevice.PrepareCaptureSequenceAsync(Seq);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DoUpdateTop();
            ButtonsEnabled = true;
            Task.Factory.StartNew(() =>
            {
                setupCamera();
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CaptureDevice.Dispose();
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

        private async void TakePhotoAsync()
        {
            await Seq.StartCaptureAsync();
            CaptureStream.Seek(0, SeekOrigin.Begin);
            TreedentState.Instance.CurrentPhoto = new byte[CaptureStream.Length];
            CaptureStream.Read(TreedentState.Instance.CurrentPhoto, 0, (int)CaptureStream.Length);
            Dispatcher.BeginInvoke(() =>
            {
                Navigator.Navigate("/PhotoPreview.xaml");
            });
        }

        private void TakePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled = false;

            Task.Factory.StartNew(() =>
            {
                TakePhotoAsync();
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Navigator.GoBack();
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

        private PhotoCaptureDevice CaptureDevice;
        private MemoryStream CaptureStream;
        private CameraCaptureSequence Seq;
    }
}