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

namespace Treedent
{
    public partial class MainPage : PhoneApplicationPage, ITreedentPage
    {
        private TreedentClient client;
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            popup = new LoginPopup(this);
            MainWindowLoginPopup.Child = popup;
            top = new TopPanel(this);
            TitlePanel.Children.Add(top);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (Navigator.CanGoBack)
            {
                Navigator.RemoveBackEntry();
            }
            DoUpdateTop();
        }

        public void DoNavigateRegister() {
            Navigator.Navigate("/RegisterPage.xaml");
        }

        public void DoNavigateProfile()
        {

        }

        public void DoNavigateMain()
        {

        }

        public void DoHandleTap(Object e)
        {

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

        public NavigationService Navigator {
            get { return NavigationService; }
        }

        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Navigate("/PhotoListPage.xaml");
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            Navigator.Navigate("/TakePhoto.xaml");
        }

    }
}