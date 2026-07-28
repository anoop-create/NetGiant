using System;

namespace NGBP.DataAccessLayer.SCOM.SimpleEntities
{
    [Serializable]
    public class FtpSE
    {
        int m_ftpDetailID;
        string m_ftpHost;
        string m_ftpUser;
        string m_ftpPwd;
        string m_ftpFolder;
        string m_ftpFilename;
        string m_ftpZipFileName;
        DateTime m_dateLastUpdated;
        bool m_fileColumnHeader;

        public int FtpDetailID
        {
            get { return m_ftpDetailID; }
            set { m_ftpDetailID = value; }
        }

        public string FtpHost
        {
            get { return m_ftpHost; }
            set { m_ftpHost = value; }
        }

        public string FtpUser
        {
            get { return m_ftpUser; }
            set { m_ftpUser = value; }
        }

        public string FtpPassword
        {
            get { return m_ftpPwd; }
            set { m_ftpPwd = value;}
        }

        public string FtpFolder
        {
            get { return m_ftpFolder; }
            set { m_ftpFolder = value; }
        }

        public string FtpFilename
        {
            get { return m_ftpFilename; }
            set { m_ftpFilename = value; }
        }

        public string FtpZipFilename
        {
            get { return m_ftpZipFileName; }
            set { m_ftpZipFileName = value; }
        }

        public DateTime DateLastUpdated
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }

        public bool FileColumnHeader
        {
            get { return m_fileColumnHeader; }
            set { m_fileColumnHeader = value; }
        }
    }
}
