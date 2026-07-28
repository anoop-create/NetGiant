using NGS.DataAccessLayer.Services.SharedServices;
using NGS.DataAccessLayer.SimpleEntities.SharedSE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.BusinessLayer.BusinessObjects.Shared
{
    [Serializable]
    public class User
    {
        public User()
        {
            Init();
        }
        
        void Init()
        {
            m_simpleEntity = new UserSE();
        }

        public static List<string> GetAllUserNames()
        {
            List<string> usernames = new List<string>();
            UserServices svc = new UserServices();
            usernames = svc.GetAllUserNames();
            return usernames;
        }

        public static List<string> GetAllRoleNames()
        {
            List<string> roles = new List<string>();
            UserServices svc = new UserServices();
            roles = svc.GetAllRoles();
            return roles;
        }
        
        internal UserSE m_simpleEntity;

        #region Properties

        public Guid ApplicationID
        {
            get { return m_simpleEntity.ApplicationID; }
            set { m_simpleEntity.ApplicationID = value; }
        }

        public Guid UserID
        {
            get { return m_simpleEntity.UserID; }
            set { m_simpleEntity.UserID = value; }
        }

        public string UserName
        {
            get { return m_simpleEntity.UserName; }
            set { m_simpleEntity.UserName = value; }
        }

        public string MobileAlias
        {
            get { return m_simpleEntity.MobileAlias; }
            set { m_simpleEntity.MobileAlias = value; }
        }

        public bool IsAnonymous
        {
            get { return m_simpleEntity.IsAnonymous; }
            set { m_simpleEntity.IsAnonymous = value; }
        }

        public DateTime LastActivityDate
        {
            get { return m_simpleEntity.LastActivityDate; }
            set { m_simpleEntity.LastActivityDate = value; }
        }

        #endregion
    }
}
