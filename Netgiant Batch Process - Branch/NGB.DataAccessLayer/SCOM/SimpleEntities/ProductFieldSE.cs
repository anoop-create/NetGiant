using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGBP.DataAccessLayer.SCOM.SimpleEntities
{
    [Serializable]
    public class FieldSectionSE
    {
        byte m_fieldSectionID;
        string m_fieldSectionName;
        byte m_sequenceNo;
        DateTime m_dateLastUpdated;

        public byte FieldSectionID
        {
            get { return m_fieldSectionID; }
            set { m_fieldSectionID = value; }
        }

        public string FieldSectionName
        {
            get { return m_fieldSectionName; }
            set { m_fieldSectionName = value; }
        }

        public byte SequenceNo
        {
            get { return m_sequenceNo; }
            set { m_sequenceNo = value; }
        }

        public DateTime DateLastUpdated
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }
    }

    [Serializable]
    public class FieldSubSectionSE
    {
        byte m_fieldSubSectionID;
        string m_fieldSubSectionName;
        byte m_sequenceNo;
        DateTime m_dateLastUpdated;
        byte m_fieldSectionFK;

        public byte FieldSubSectionID
        {
            get { return m_fieldSubSectionID; }
            set { m_fieldSubSectionID = value; }
        }

        public string FieldSubSectionName
        {
            get { return m_fieldSubSectionName; }
            set { m_fieldSubSectionName = value; }
        }

        public byte SequenceNo
        {
            get { return m_sequenceNo; }
            set { m_sequenceNo = value; }
        }

        public DateTime DateLastUpdated
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }

        public byte FieldSectionFK
        {
            get { return m_fieldSectionFK; }
            set { m_fieldSectionFK = value; }
        }
    }

    [Serializable]
    public class FieldTypeSE
    {
        byte m_fieldTypeID;
        string m_fieldTypeName;
        DateTime m_dateLastUpdated;

        public byte FieldTypeID
        {
            get { return m_fieldTypeID; }
            set { m_fieldTypeID = value; }
        }

        public string FieldTypeName
        {
            get { return m_fieldTypeName; }
            set { m_fieldTypeName = value; }
        }

        public DateTime DateLastUpdated
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }
    }

    [Serializable]
    public class FieldNameSE
    {
        Int16 m_fieldNameID;
        string m_fieldName;
        string m_AxisTableName;
        string m_AxisFieldName;
        byte m_sequenceNo;
        bool m_websiteSpecific;
        DateTime m_dateLastUpdated;
        byte m_fieldTypeFK;
        byte m_fieldSubSectionFK;

        public Int16 FieldNameID
        {
            get { return m_fieldNameID; }
            set { m_fieldNameID = value; }
        }

        public string FieldName
        {
            get { return m_fieldName; }
            set { m_fieldName = value; }
        }

        public string AXISTableName
        {
            get { return m_AxisTableName; }
            set { m_AxisTableName = value; }
        }

        public string AXISFieldName
        {
            get { return m_AxisFieldName; }
            set { m_AxisFieldName = value; }
        }

        public byte SequenceNo
        {
            get { return m_sequenceNo; }
            set { m_sequenceNo = value; }
        }

        public bool WebsiteSpecific
        {
            get { return m_websiteSpecific; }
            set { m_websiteSpecific = value; }
        }

        public DateTime DateLastUpdated
        {
            get { return m_dateLastUpdated; }
            set { m_dateLastUpdated = value; }
        }

        public byte FieldTypeFK
        {
            get { return m_fieldTypeFK; }
            set { m_fieldTypeFK = value; }
        }

        public byte FieldSubSectionFK
        {
            get { return m_fieldSubSectionFK; }
            set { m_fieldSubSectionFK = value; }
        }
    }

    [Serializable]
    public class FieldValueSE
    {
        int m_fieldValueID;
        string m_fieldValueText;
        bool? m_fieldValueBool;
        double? m_fieldValueDouble;
        int? m_productFK;
        int? m_websiteFK;
        Int16 m_fieldNameFK;

        public int FieldValueID
        {
            get { return m_fieldValueID; }
            set { m_fieldValueID = value; }
        }

        public string FieldValueText
        {
            get { return m_fieldValueText; }
            set { m_fieldValueText = value; }
        }

        public bool? FieldValueBool
        {
            get { return m_fieldValueBool; }
            set { m_fieldValueBool = value; }
        }

        public double? FieldValueDouble
        {
            get { return m_fieldValueDouble; }
            set { m_fieldValueDouble = value; }
        }

        public int? ProductFK
        {
            get { return m_productFK; }
            set { m_productFK = value; }
        }

        public int? WebsiteFK
        {
            get { return m_websiteFK; }
            set { m_websiteFK = value; }
        }

        public Int16 FieldNameFK
        {
            get { return m_fieldNameFK; }
            set { m_fieldNameFK = value; }
        }
    }
}
