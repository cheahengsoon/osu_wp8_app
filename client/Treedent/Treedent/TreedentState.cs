using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Treedent
{
    class TreedentState
    {
        public static TreedentState Instance { get; private set; }

        private TreedentClient client;
        public TreedentClient Client
        {
            get { return client; }
        }

        public bool LoggedIn
        {
            get;
            set;
        }

        public string UserName
        {
            get;
            set;
        }

        public string SessionID
        {
            get;
            set;
        }

        public byte[] CurrentPhoto
        {
            get;
            set;
        }

        public double Longitude
        {
            get;
            set;
        }

        public double Latitude
        {
            get;
            set;
        }

        private async void getGeoSpace()
        {
            Latitude = 0;
            Longitude = 0;
            try
            {
                Geolocator g = new Geolocator();
                g.DesiredAccuracyInMeters = 250;


                Geoposition gop = await g.GetGeopositionAsync(maximumAge: TimeSpan.FromMinutes(5), timeout: TimeSpan.FromSeconds(10));
                Latitude = gop.Coordinate.Latitude;
                Longitude = gop.Coordinate.Longitude;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private TreedentState()
        {
            client = new TreedentClient();
            LoggedIn = false;
            UserName = "";
            Task.Factory.StartNew(() =>
            {
                getGeoSpace();
            });
        }

        static TreedentState() { Instance = new TreedentState(); }
    }
}
