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
    public partial class TopPanel : Grid, INotifyPropertyChanged
    {
        private TreedentClient client;
        public TopPanel(ITreedentPage mw)
        {
            client = TreedentState.Instance.Client;
            hostWindow = mw;
            InitializeComponent();
            RefreshLoginButton();
        }

        public void RefreshLoginButton()
        {
            if (TreedentState.Instance.LoggedIn)
            {
                LoginText = "Logout";
                NameText = TreedentState.Instance.UserName;
            }
            else
            {
                LoginText = "Login";
                NameText = "";
            }
        }

        private ITreedentPage hostWindow;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string loginText;
        public string LoginText
        {
            get { return loginText; }
            set
            {
                loginText = value;
                OnPropertyChanged("LoginText");
            }
        }

        private string nameText;
        public string NameText
        {
            get { return nameText; }
            set
            {
                nameText = value;
                OnPropertyChanged("NameText");
            }
        }

        private async void doLogoutParallel()
        {
            TreedentMessage message;
            await client.Logout();
            message = await client.GetResult();
            Dispatcher.BeginInvoke(() =>
            {
                if (message.Code == 999)
                {
                    NameText = message.DataMap["error"];
                }
                else if (message.Code == 4)
                {
                    NameText = "";
                    TreedentState.Instance.UserName = "";
                    TreedentState.Instance.SessionID = "";
                    TreedentState.Instance.LoggedIn = false;
                }
                else
                {
                    NameText = "Invalid message, code: " + message.Code.ToString();
                }
            });
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (TreedentState.Instance.LoggedIn)
            {
                LoginText = "Login";
                Task.Factory.StartNew(() =>
                {
                    doLogoutParallel();
                });
            }
            else
            {
                hostWindow.DoShowLogin();
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            hostWindow.DoNavigateMain();
        }
    }
}