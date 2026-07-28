using NGS.DataAccessLayer.Services.SharedServices;
using NGS.DataAccessLayer.SimpleEntities.SharedSE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.BusinessLayer.BusinessObjects.Shared
{
    public class EmailSent
    {
        public EmailSent()
        {
            Init();
        }

        void Init()
        {
            m_simpleEntity = new EmailSentSE();
        }

        public static bool GetByQuestionAndSendTo(int questionId, string sendTo)
        {
            EmailSentSE entity = new EmailSentSE();
            EmailSentServices svc = new EmailSentServices();
            entity = svc.Get(questionId, sendTo);

            if (entity != null && entity.EmailSentID > 0)
            {
                return true;
            }

            return false;
        }
        
        public static List<EmailSent> GetEmailSentByQuestionID(int Id)
        {
            
            List<EmailSentSE> entityList = new List<EmailSentSE>();
            
            EmailSentServices svc = new EmailSentServices();
            entityList = svc.GetByQuestionID(Id);

            List<EmailSent> coll = new List<EmailSent>();
            foreach (EmailSentSE entity in entityList)
            {
                EmailSent es = new EmailSent();
                es.m_simpleEntity = entity;
                coll.Add(es);
            }

            return coll;
        }
        
        public void Save()
        {
            EmailSentServices svc = new EmailSentServices();
            svc.Save(m_simpleEntity);
        }

        internal EmailSentSE m_simpleEntity;

        #region Properties

        public int EmailSentID
        {
            get { return m_simpleEntity.EmailSentID; }
            set { m_simpleEntity.EmailSentID = value; }
        }

        public DateTime EmailSentDate
        {
            get { return m_simpleEntity.EmailSentDate; }
            set { m_simpleEntity.EmailSentDate = value; }
        }

        public string EmailSentTo
        {
            get { return m_simpleEntity.EmailSentTo; }
            set { m_simpleEntity.EmailSentTo = value; }
        }

        public string RelatedUserID
        {
            get { return m_simpleEntity.RelatedUserID; }
            set { m_simpleEntity.RelatedUserID = value; }
        }

        public int RelatedQuestionID
        {
            get { return m_simpleEntity.RelatedQuestionID; }
            set { m_simpleEntity.RelatedQuestionID = value; }
        }

        #endregion
    }
}
