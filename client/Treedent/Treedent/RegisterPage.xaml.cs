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
using System.Threading.Tasks;
using System.ComponentModel;

namespace Treedent
{
    public partial class RegisterPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        // Constructor
        public RegisterPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            client = TreedentState.Instance.Client;
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}

        private TreedentClient client;

        protected async Task validateForm()
        {
            string user = UsernameField.Text;
            string pw = PasswordField.Password;
            string confirm = ConfirmField.Password;
            if (user.Length > 0)
            {
                if (user.Length < 6)
                {
                    StatusText = "Username must be at least 6 characters long.";
                    return;
                }
            }
            if (pw.Length > 0)
            {
                if (pw.Length < 8)
                {
                    StatusText = "Password must be at least 8 characters long.";
                    return;
                }
                if (confirm.Length == 0)
                {
                    StatusText = "Please confirm your password.";
                    return;
                }
            }
            if (confirm.Length > 0)
            {
                if (confirm != pw)
                {
                    StatusText = "Passwords do not match.";
                    return;
                }
            }
            if (StatusText.Length > 0)
            {
                StatusText = "";
            }
        }

        public void Close_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
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

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void UsernameField_TextChanged(object sender, TextChangedEventArgs e)
        {
            await validateForm();
        }

        private async void PasswordField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            await validateForm();
        }

        private async void ConfirmField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            await validateForm();
        }

        private async void doRegisterParallel(string user, string pass)
        {
            TreedentMessage message;
            await client.Register(user, pass);
            message = await client.GetResult();
            Dispatcher.BeginInvoke(() =>
            {
                if (message.Code == 999)
                {
                    StatusText = message.DataMap["error"];
                }
                else if (message.Code == 1)
                {
                    TreedentState.Instance.SessionID = message.DataMap["Session"];
                    TreedentState.Instance.UserName = user;
                    TreedentState.Instance.LoggedIn = true;
                    NavigationService.GoBack();
                    Task.Factory.StartNew(() =>
                    {
                        doAskSortParallel();
                    });
                }
                else
                {
                    StatusText = "Invalid message, code: " + message.Code.ToString();
                }
                RegisterButton.IsEnabled = true;
            });
        }

        private async void doAskSortParallel()
        {
            await client.AskSort();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterButton.IsEnabled = false;
            string user, pass;
            user = UsernameField.Text;
            pass = PasswordField.Password;
            Task.Factory.StartNew(() =>
            {
                doRegisterParallel(user, pass);
            });
        }
    }
}