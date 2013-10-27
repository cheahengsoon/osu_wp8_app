using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Json;
using System.IO.IsolatedStorage;
using Windows.Devices.Geolocation;

namespace Treedent
{
    public class TreedentClient
    {
        public TreedentClient()
        {

        }

        public async Task<TreedentMessage> GetResult()
        {
            while (true)
            {
                if (!gotResult)
                {
                    Thread.Sleep(1);
                    continue;
                }
                return result;
            }
        }

        public async Task Login(String Username, String Password)
        {
            gotResult = false;
            string parameters = string.Format("action=login&uname={0}&password={1}", HttpUtility.UrlEncode(Username), HttpUtility.UrlEncode(Password));
            await uploadString(parameters);
        }

        public async Task Logout()
        {
            gotResult = false;
            string parameters = string.Format("action=logout&session={0}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID));
            await uploadString(parameters);
        }

        public async Task Register(String Username, String Password)
        {
            gotResult = false;

            string parameters = string.Format("action=register&uname={0}&password={1}", HttpUtility.UrlEncode(Username), HttpUtility.UrlEncode(Password));
            await uploadString(parameters);
        }

        public async Task AskSort()
        {
            gotResult = false;

            string parameters = string.Format("action=sort&session={0}&lat={1}&long={2}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID),
                HttpUtility.UrlEncode(TreedentState.Instance.Latitude.ToString()), HttpUtility.UrlEncode(TreedentState.Instance.Longitude.ToString()));
            await uploadString(parameters);
        }

        public async Task UploadPhoto(UploadProgressChangedEventHandler e = null)
        {
            gotResult = false;
            string lat, lon;
            lat = TreedentState.Instance.Latitude.ToString();
            lon = TreedentState.Instance.Longitude.ToString();
            String dataString = BitConverter.ToString(TreedentState.Instance.CurrentPhoto).Replace("-", "");
            string parameters = string.Format("action=upload&session={0}&data={1}&lat={2}&long={3}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID), HttpUtility.UrlEncode(dataString), lat, lon);
            await uploadString(parameters, e);
        }

        public async Task GetNext(int last)
        {
            gotResult = false;
            string parameters = string.Format("action=next&session={0}&last={1}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID), HttpUtility.UrlEncode(last.ToString()));
            await uploadString(parameters);
        }

        public async Task AddGuess(string name, int index)
        {
            gotResult = false;
            string parameters = string.Format("action=guess&session={0}&index={1}&name={2}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID),
                HttpUtility.UrlEncode(index.ToString()), HttpUtility.UrlEncode(name));
            await uploadString(parameters);
        }

        public async Task GetInfo(int index)
        {
            gotResult = false;
            string parameters = string.Format("action=info&session={0}&index={1}", HttpUtility.UrlEncode(TreedentState.Instance.SessionID), HttpUtility.UrlEncode(index.ToString()));
            await uploadString(parameters);
        }

        private async Task uploadString(String data, UploadProgressChangedEventHandler e = null)
        {
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            wc.Headers[HttpRequestHeader.ContentLength] = data.Length.ToString();
            wc.UploadStringCompleted += new UploadStringCompletedEventHandler(responseHandler);
            if (e != null)
            {
                wc.UploadProgressChanged += e;
            }
            wc.UploadStringAsync(serverUri, "POST", data);
        }

        private void responseHandler(object sender, UploadStringCompletedEventArgs e)
        {
            string resultString = e.Result.Replace("\\", "");
            byte[] bytes = Encoding.Unicode.GetBytes(resultString);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TreedentMessage));
                result = (TreedentMessage)serializer.ReadObject(stream);
            }
            gotResult = true;
        }
        private TreedentMessage result;
        private bool gotResult;

        public static Uri serverUri = new Uri("https://osu-treedent.appspot.com/rpc");
        public static Uri debugUri = new Uri("http://localhost:8080/rpc");
        bool LoggedIn
        {
            get;
            set;
        }

        public string Username
        {
            get;
            set;
        }
    }
}
