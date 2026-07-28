using NGS.BusinessLayer.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace NGS.UI.WebPages.Models
{
    public class QASummaryModel
    {
        public QASummaryModel() { }

        public int GetRowCount()
        {
            return QuestionAnswers.GetQACount();
        }

        public int GetUnAnsweredQACount()
        {
            return QuestionAnswers.GetUnAnsweredQACount();
        }

        public int GetFilteredQACount(string altRef, string filter)
        {
            return QuestionAnswers.GetFilterQACount(altRef, filter);
        }

        public DataTable GetQASummary(int startRow, int pageSize)
        {
            DataTable dt = QuestionAnswers.GetQASummary(startRow, pageSize);
            return dt;
        }

        public DataTable GetUnAnsweredQASummary(int startRow, int pageSize)
        {
            DataTable dt = QuestionAnswers.GetUnAnsweredQASummary(startRow, pageSize);
            return dt;
        }

        public DataTable GetFilteredQASummary(int startRow, int pageSize, string altRef, string filter)
        {
            DataTable dt = QuestionAnswers.GetFilteredQASummary(startRow, pageSize, altRef, filter);
            return dt;
        }
    }
}