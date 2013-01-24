using Newtonsoft.Json;
using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace RemoteLogCore
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
        private readonly string CONTENT_TYPE_JSON = "application/json";

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

        /// <summary>
        /// Send device to server
        /// </summary>
        /// <param name="d"></param>
        /// <returns>Updated device, including DeviceID</returns>
        public Respond<Device> SaveDevice(Device d)
        {
            string toSend = JsonConvert.SerializeObject(d);
            string json = SendRequest(toSend, URL + REGS_URL, HTTP_POST);
            Respond<Device> res = JsonConvert.DeserializeObject<Respond<Device>>(json, RemoteLog.Settings);
            return res;
        }

        /// <summary>
        /// Update just pushtoken for device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="pushToken"></param>
        /// <returns></returns>
        public Respond<string> UpdatePushToken(int deviceId, string pushToken)
        {                        
            string json = SendRequest(pushToken, String.Format("{0}/{1}", URL + REGS_URL, deviceId), HTTP_PUT);
            Respond<string> response = JsonConvert.DeserializeObject<Respond<string>>(json, RemoteLog.Settings);
            return response;
        }

        public Respond<LogItem> SendItem(LogItem item)
        {
            string json = JsonConvert.SerializeObject(item, RemoteLog.Settings);
            string response = SendRequest(json, URL + LOGS_URL, HTTP_POST);
            Respond<LogItem> r = JsonConvert.DeserializeObject<Respond<LogItem>>(response, RemoteLog.Settings);
            return r;
        }

        public Respond<Object> SendItem(LogItemBlobRequest item)
        {            
            string json = JsonConvert.SerializeObject(item, RemoteLog.Settings);
            string url = String.Format("{0}?{1}",URL + LOGS_URL, HttpUtility.UrlEncode(json));
            string response = SendRequest(item.Data, url , HTTP_PUT);
            Respond<Object> r = JsonConvert.DeserializeObject<Respond<Object>>(response, RemoteLog.Settings);
            return r;
        }


        private string SendRequest(string toSend, string url, string method)
        {
            return SendRequest(UTF8Encoding.UTF8.GetBytes(toSend), url, method);
        }
        /// <summary>
        /// Send custom request to server
        /// </summary>
        /// <param name="toSend">json value of object</param>
        /// <param name="url">server url</param>
        /// <param name="method">http method</param>
        /// <returns>text entity of response</returns>
        private string SendRequest(byte[] toSend, string url, string method)
        {
            string textrespond = null;
            Thread caller = System.Threading.Thread.CurrentThread;
            lock (this)
            {
                WebRequest wr = WebRequest.CreateHttp(url);
                wr.Method = method;
                wr.ContentType = CONTENT_TYPE_JSON;
                //post
                wr.BeginGetRequestStream(new AsyncCallback((ia) =>
                {
                    try
                    {
                        Stream output = wr.EndGetRequestStream(ia);
                        output.Write(toSend, 0, toSend.Length);
                        output.Close();
                        //get response
                        wr.BeginGetResponse(new AsyncCallback((iar) =>
                        {
                            WebResponse resp = wr.EndGetResponse(iar);
                            Stream input = resp.GetResponseStream();
                            System.IO.StreamReader sr = new StreamReader(input);
                            textrespond = sr.ReadToEnd();
                            lock (this)
                            {
                                Monitor.PulseAll(this);
                            }
                        }), null);
                    }
                    catch (Exception)
                    {
                        lock (this)
                        {
                            Monitor.PulseAll(this);
                        }
                    }
                }), null);
                Monitor.Wait(this);
            }
            return textrespond;
        }

        public Respond<Settings[]> LoadSettings(int deviceId, String appName)
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
            Respond<Settings[]> result = JsonConvert.DeserializeObject<Respond<Settings[]>>(textrespond, RemoteLog.Settings);
            return result;
        }
    }
}
