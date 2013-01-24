using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace RemoteLogCore.Model
{
    public class LogItemBlobRequest
    {
        public const string MIME_TEXT_PLAIN = "text/plain";
        public const string MIME_TEXT_JSON = "text/json";
        public const string MIME_IMAGE_JPEG = "image/jpeg";
        public const string MIME_IMAGE_PNG = "image/png";

        #region members
        private byte[] mData;

        #endregion

        #region Properties

        public int LogItemID { get; set; }

        public string MimeType { get; set; }

        public int DataLength { get; set; }

        public string FileName { get; set; }

        [JsonIgnore]
        public byte[] Data
        {
            get
            {
                return mData;
            }
            set
            {
                mData = value;
                if (mData != null)
                {
                    DataLength = mData.Length;
                }
                else
                {
                    DataLength = 0;
                }
            }
        }

        #endregion

        public LogItemBlobRequest() { }

        public LogItemBlobRequest(string mime, string filename, string data) :
            this(mime, filename, UTF8Encoding.UTF8.GetBytes(data)) { }        

        public LogItemBlobRequest(string mime, string filename, byte[] data)
        {
            MimeType = mime;
            FileName = filename;
            Data = data;
        }

        public bool IsUnhandledException { get; set; }
    }
}
