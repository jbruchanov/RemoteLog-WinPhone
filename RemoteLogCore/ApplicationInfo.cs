using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RemoteLogCore
{
    public static class ApplicationInfo
    {
        #region Constants

        /// <summary>
        /// Filename of the application manifest contained within the XAP file
        /// </summary>
        private const string AppManifestName = "WMAppManifest.xml";

        /// <summary>
        /// Name of the XML element containing the application information
        /// </summary>
        private const string AppNodeName = "App";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the application title
        /// </summary>
        public static string Title
        {
            get { return GetAppAttribute("Title"); }
        }

        /// <summary>
        /// Gets the application description
        /// </summary>
        public static string Description
        {
            get { return GetAppAttribute("Description"); }
        }

        /// <summary>
        /// Gets the application version
        /// </summary>
        public static string Version
        {
            get { return GetAppAttribute("Version"); }
        }

        /// <summary>
        /// Gets the application publisher
        /// </summary>
        public static string Publisher
        {
            get { return GetAppAttribute("Publisher"); }
        }

        /// <summary>
        /// Gets the application author
        /// </summary>
        public static string Author
        {
            get { return GetAppAttribute("Author"); }
        }

        public static string FileVersion
        {
            get { return GetAppAttribute("FileVersion"); }
        }


        #endregion

        #region Methods

        /// <summary> 
        /// Gets an attribute from the Windows Phone App Manifest App element 
        /// </summary> 
        /// <param name="attributeName">the attribute name</param> 
        /// <returns>the attribute value</returns> 
        private static string GetAppAttribute(string attributeName)
        {
            var settings = new XmlReaderSettings { XmlResolver = new XmlXapResolver() };

            using (var rdr = XmlReader.Create(AppManifestName, settings))
            {
                rdr.ReadToDescendant(AppNodeName);

                // Return the value of the requested XML attribute if found or NULL if the XML element with the application information was not found in the application manifest
                return !rdr.IsStartElement() ? null : rdr.GetAttribute(attributeName);
            }
        }

        #endregion
    }
}
