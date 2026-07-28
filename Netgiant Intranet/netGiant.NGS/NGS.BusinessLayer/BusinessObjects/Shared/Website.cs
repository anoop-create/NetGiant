using NGS.DataAccessLayer.Services.SharedServices;
using NGS.DataAccessLayer.SimpleEntities.SharedSE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.BusinessLayer.BusinessObjects.Shared
{
    [Serializable]
    public class Website
    {
        public Website()
        {
            Init();
        }
        
        void Init()
        {
            m_simpleEntity = new WebsiteSE();
        }

        #region Public Methods

        public static Website GetWebsiteByID(int websiteID)
        {
            WebsiteSE entity = new WebsiteSE();

            WebsiteServices svc = new WebsiteServices();
            entity = svc.GetWebsiteByID(websiteID);

            Website ws = null;

            if (entity != null)
            {
                ws = new Website();
                ws.m_simpleEntity = entity;
            }

            return ws;
        }

        public static Website GetWebsiteByName(string websiteName)
        {
            WebsiteSE entity = new WebsiteSE();

            WebsiteServices svc = new WebsiteServices();
            entity = svc.GetWebsiteByName(websiteName);

            Website ws = null;

            if (entity != null)
            {
                ws = new Website();
                ws.m_simpleEntity = entity;
            }

            return ws;
        }

        public static List<Website> GetAllWebsites()
        {
            List<WebsiteSE> list = new List<WebsiteSE>();
            WebsiteServices svc = new WebsiteServices();
            list = svc.GetAllWebsites();

            List<Website> coll = new List<Website>();
            foreach (WebsiteSE entity in list)
            {
                Website ws = new Website();
                ws.m_simpleEntity = entity;
                coll.Add(ws);

            }

            return coll;
        }

        #endregion

        internal WebsiteSE m_simpleEntity;

        #region Properties

        public int WebsiteID
        {
            get { return m_simpleEntity.WebsiteID; }
            set { m_simpleEntity.WebsiteID = value; }
        }

        public string WebsiteName
        {
            get { return m_simpleEntity.WebsiteName; }
            set { m_simpleEntity.WebsiteName = value; }
        }

        public string WebsiteURL
        {
            get { return m_simpleEntity.WebsiteURL; }
            set { m_simpleEntity.WebsiteURL = value; }
        }

        #endregion
    }
}
