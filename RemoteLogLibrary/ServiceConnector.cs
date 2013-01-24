using Newtonsoft.Json;
using RemoteLogLibrary.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace RemoteLogLibrary
{
    public class ServiceConnector
    {
        private const string REGS_URL = "/regs";
        private const string LOGS_URL = "/logs";
        private const string SETTINGS_TEMPLATE_URL = "/settings/{0}/{1}";

        private const string HTTP_GET = "GET";
        private const string HTTP_POST = "POST";
        private const string HTTP_PUT = "PUT";

        private readonly string COOKIE = "Cookie";
        private readonly string CONTENT_TYPE = "Content-Type";

        private string URL { get; set; }
        private string Authorization { get; set; }

        public ServiceConnector(string url, string username = null, string password = null)
        {
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }
            URL = url;
        }

        private WebClient CreateWebClientForPost()
        {
            WebClient wc = new WebClient();
            wc.Headers[CONTENT_TYPE] = "application/json";
            return wc;
        }

        public SettingsRespond LoadSettings(int deviceId, String appName)
        {
            string textrespond = null;
            Thread caller = System.Threading.Thread.CurrentThread;
            lock (this)
            {
                WebRequest wr = WebRequest.CreateHttp(new Uri(String.Format(URL + SETTINGS_TEMPLATE_URL, deviceId, appName)));                                
                wr.BeginGetResponse(new AsyncCallback((ia) =>
                    {
                        WebResponse response = wr.EndGetResponse(ia);
                        Stream s = response.GetResponseStream();
                        System.IO.StreamReader sr = new StreamReader(s);
                        textrespond = sr.ReadToEnd();
                        lock (this)
                        {
                            Monitor.PulseAll(this);
                        }
                    }), wr);
                Monitor.Wait(this);
            }
            SettingsRespond result = JsonConvert.DeserializeObject<SettingsRespond>(textrespond);
            return result;
        }
    }
}
