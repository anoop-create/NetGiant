using System;

namespace NGBP.DataAccessLayer.SCOM.SimpleEntities
{
    [Serializable]
    public class ProviderSE
    {
        int m_providerID;
        string m_providerName;
        string m_providerDesc;
        ProviderTypeSE m_relatedProviderType;
        FtpSE m_relatedFtpDetails;

        public int ProviderID
        {
            get { return m_providerID; }
            set { m_providerID = value; }
        }

        public string ProviderName
        {
            get { return m_providerName; }
            set { m_providerName = value; }
        }

        public string ProviderDescription
        {
            get { return m_providerDesc; }
            set { m_providerDesc = value; }
        }

        public ProviderTypeSE ProviderType
        {
            get { return m_relatedProviderType; }
            set { m_relatedProviderType = value; }
        }

        public FtpSE RelatedFtpDetails
        {
            get { return m_relatedFtpDetails; }
            set { m_relatedFtpDetails = value; }
        }
    }

    [Serializable]
    public class ProviderTypeSE
    {
        int m_providerTypeID;
        string m_providerTypeName;
        DateTime m_dateLastUpdated;

        public int ProviderTypeID
        {
            get { return m_providerTypeID; }
            set { m_providerTypeID = value; }
        }

        public string ProviderTypeName
        {
            get { return m_providerTypeName; }
            set { m_providerTypeName = value; }
        }

        public DateTime DateLastUpdate
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }
    }

    [Serializable]
    public class mfpnExtensions
    {
        int m_manuID;
        string m_extension;

        public int ManuID
        {
            get { return m_manuID; }
            set { m_manuID = value; }
        }

        public string Extension
        {
            get { return m_extension; }
            set { m_extension = value; }
        }
    }

    [Serializable]
    public class manufacturer
    {
        int m_manuID;
        string m_manufacturerName;

        public int ManuID
        {
            get { return m_manuID; }
            set { m_manuID = value; }
        }

        public string ManufacturerName
        {
            get { return m_manufacturerName; }
            set { m_manufacturerName = value; }
        }
    }

    [Serializable]
    public class SupplierManuMapping
    {
        string m_supplierManuRef;
        int m_manufacturerFK;
        int m_providerFK;

        public string SupplierManuRef
        {
            get { return m_supplierManuRef; }
            set { m_supplierManuRef = value; }
        }

        public int ManufacturerFK
        {
            get { return m_manufacturerFK; }
            set { m_manufacturerFK = value; }
        }

        public int ProviderFK
        {
            get { return m_providerFK; }
            set { m_providerFK = value; }
        }
    }
}
