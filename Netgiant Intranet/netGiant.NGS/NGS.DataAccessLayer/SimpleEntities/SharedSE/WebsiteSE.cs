using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.DataAccessLayer.SimpleEntities.SharedSE
{
    [Serializable]
    public class WebsiteSE
    {
        //Fields
        int m_websiteId;
        string m_websiteName;
        string m_websiteURL;

        #region Properties

        public int WebsiteID
        {
            get { return m_websiteId; }
            set { m_websiteId = value; }
        }

        public string WebsiteName
        {
            get { return m_websiteName; }
            set { m_websiteName = value; }
        }

        public string WebsiteURL
        {
            get { return m_websiteURL; }
            set { m_websiteURL = value; }
        }

        #endregion
    }
}
