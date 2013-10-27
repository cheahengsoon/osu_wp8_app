using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Treedent
{
    public partial class LoginPopup : Grid, INotifyPropertyChanged
    {
        private TreedentClient client;
        public LoginPopup(ITreedentPage mw)
        {
            client = TreedentState.Instance.Client;
            mainWindow = mw;
            InitializeComponent();
        }

        private async void doLoginParallel(string user, string pass)
        {
            await client.Login(user, pass);
            TreedentMessage message = await client.GetResult();
            Dispatcher.BeginInvoke(async () =>
            {
                if (message.Code == 999)
                {
                    StatusText = message.DataMap["error"];
                }
                else if (message.Code == 2)
                {
                    LoginButton.IsEnabled = true;
                    TreedentState.Instance.SessionID = message.DataMap["Session"];
                    TreedentState.Instance.UserName = user;
                    TreedentState.Instance.LoggedIn = true;
                    mainWindow.DoHideLogin();
                    Task.Factory.StartNew(() =>
                    {
                        doAskSortParallel();
                    });
                }
                else
                {
                    StatusText = "Invalid message, code: " + message.Code;
                }
                LoginButton.IsEnabled = true;
            });
        }

        private async void doAskSortParallel()
        {
            await client.AskSort();
        }

        public void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            string user, pass;
            user = UsernameBox.Text;
            pass = PasswordBox.Password;
            Task.Factory.StartNew(() =>
            {
                doLoginParallel(user, pass);
            });
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DoHideLogin();
        }
        private ITreedentPage mainWindow;

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.DoNavigateRegister();
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
    }
}