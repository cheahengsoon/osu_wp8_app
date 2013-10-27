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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Treedent
{
    public partial class PhotoEntry : Grid, INotifyPropertyChanged
    {
        private TreedentClient client;
        private ITreedentPage hostWindow;
        public PhotoEntry(ITreedentPage mw, int number, string errorText = null)
        {
            client = TreedentState.Instance.Client;
            hostWindow = mw;
            InitializeComponent();
            if (errorText != null)
            {
                statusText = errorText;
                return;
            }
            BitmapImage bi = new BitmapImage();
            Stream s = new MemoryStream(TreedentState.Instance.CurrentPhoto);
            bi.SetSource(s);
            Source = bi;
            Number = number;
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

        public int Number
        {
            get;
            set;
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                OnPropertyChanged("UserName");
            }
        }

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

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            hostWindow.DoHandleTap(this);
        }
    }
}