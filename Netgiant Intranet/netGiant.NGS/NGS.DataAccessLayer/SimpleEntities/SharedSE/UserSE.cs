using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.DataAccessLayer.SimpleEntities.SharedSE
{
    public class UserSE
    {
        Guid m_applicationId;
        Guid m_userId;
        string m_userName;
        string m_mobileAlias;
        bool m_isAnonymous;
        DateTime m_lastActivityDate;

        #region Properties

        public Guid ApplicationID
        {
            get { return m_applicationId; }
            set { m_applicationId = value; }
        }

        public Guid UserID
        {
            get { return m_userId; }
            set { m_userId = value; }
        }

        public string UserName
        {
            get { return m_userName; }
            set { m_userName = value; }
        }

        public string MobileAlias
        {
            get { return m_mobileAlias; }
            set { m_mobileAlias = value; }
        }

        public bool IsAnonymous
        {
            get { return m_isAnonymous; }
            set { m_isAnonymous = value; }
        }

        public DateTime LastActivityDate
        {
            get { return m_lastActivityDate; }
            set { m_lastActivityDate = value; }
        }

        #endregion
    }
}
