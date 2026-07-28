using NGS.DataAccessLayer.SimpleEntities.QuestionAnswersSE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;

namespace NGS.DataAccessLayer.Services.QuestionAnswersServices
{
    [Serializable]
    public class QuestionAnswerServices : GlobalServices
    {
        string m_selectStatement =
            "SELECT	qa.QuestionAnswerID, qa.Question, qa.Answer, qa.Email, qa.AskedDate, qa.RepliedDate, qa.ShowOnAllSites, " +
                    "g.GranularityID, qa.UserFK, qa.SourceWebsiteID, qa.ProductID, qa.AltRef " +
            "FROM	ngmd.qa_Main qa " +
                    "INNER JOIN ngmd.qa_Granularity g " +
                    "ON qa.GranularityFK = g.GranularityID ";

        void readQuestionAnswers(SqlDataReader reader, QuestionAnswersSE questionAnswers)
        {
            questionAnswers.QuestionAnswerID = reader.GetInt32(0);
            questionAnswers.Question = reader.GetString(1);
            questionAnswers.Answer = reader.GetString(2);
            questionAnswers.Email = reader.GetString(3);
            questionAnswers.AskedDate = reader.GetDateTime(4);
            if (!reader.IsDBNull(5)) questionAnswers.RepliedDate = reader.GetDateTime(5);
            questionAnswers.ShowOnAllSites = reader.GetByte(6);
            questionAnswers.RelatedGranularityID = reader.GetInt32(7);
            if (!reader.IsDBNull(8)) questionAnswers.RelatedUserID = reader.GetString(8);
            questionAnswers.SourceWebsiteID = reader.GetInt32(9);
            questionAnswers.ProductID = reader.GetInt32(10);
            questionAnswers.AltRef = reader.GetString(11);
        }

        #region Public Methods

        [AutoComplete]
        public QuestionAnswersSE GetQuestionAnswerByID(int id)
        {
            QuestionAnswersSE questionAnswer = null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE qa.QuestionAnswerID = @QuestionAnswerID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, id));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            questionAnswer = new QuestionAnswersSE();
                            readQuestionAnswers(reader, questionAnswer);
                        }

                        reader.Close();
                    }

                    catch(Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return questionAnswer;
        }

        [AutoComplete]
        public QuestionAnswersSE GetQuestionAnswerByAltRef(string altRef)
        {
            QuestionAnswersSE questionAnswer = null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE qa.AltRef = @AltRef;";

                cmd.Parameters.Add(new SqlParameter(
                    "@AltRef", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, altRef));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            questionAnswer = new QuestionAnswersSE();
                            readQuestionAnswers(reader, questionAnswer);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return questionAnswer;
        }

        [AutoComplete]
        public List<QuestionAnswersSE> GetAllQuestionAnswers()
        {
            List<QuestionAnswersSE> questionAnswers = new List<QuestionAnswersSE>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement;

                if (conn.State == ConnectionState.Closed) conn.Open();
                
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        QuestionAnswersSE qa = new QuestionAnswersSE();
                        readQuestionAnswers(reader, qa);

                        questionAnswers.Add(qa);
                    }

                    reader.Close();
                }
            }

            return questionAnswers;
        }

        [AutoComplete]
        public List<KeyValuePair<int, string>> GetAllGranularities()
        {
            List<KeyValuePair<int, string>> granualities = new List<KeyValuePair<int, string>>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT GranularityID, Granularity FROM ngmd.qa_Granularity;";

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        granualities.Add(new KeyValuePair<int, string>(reader.GetInt32(0), reader.GetString(1)));
                    }

                    reader.Close();
                }
            }

            return granualities;
        }

        [AutoComplete]
        public void Delete(QuestionAnswersSE questionAnswer)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM ngmd.qa_Main WHERE QuestionAnswerID = @QuestionAnswerID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.QuestionAnswerID));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                }

                finally
                {
                    conn.Close();
                }
            }
        }

        [AutoComplete]
        public QuestionAnswersSE Save(QuestionAnswersSE questionAnswer)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.SaveQuestionAnswer";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.QuestionAnswerID));

                cmd.Parameters.Add(new SqlParameter(
                    "@Question", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.Question));

                cmd.Parameters.Add(new SqlParameter(
                    "@Answer", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.Answer));

                cmd.Parameters.Add(new SqlParameter(
                    "@Email", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.Email));

                cmd.Parameters.Add(new SqlParameter(
                    "@AskedDate", SqlDbType.DateTime, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.AskedDate));

                cmd.Parameters.Add(new SqlParameter(
                    "@GranularityID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.RelatedGranularityID));

                cmd.Parameters.Add(new SqlParameter(
                    "@UserID", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.RelatedUserID ?? ""));

                cmd.Parameters.Add(new SqlParameter(
                    "@ShowOnAllSites", SqlDbType.TinyInt, 1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.ShowOnAllSites));

                //if (questionAnswer.ProductID > 0)
                //{
                    cmd.Parameters.Add(new SqlParameter(
                        "@WebsiteID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.SourceWebsiteID)); 
                //}
                
                cmd.Parameters.Add(new SqlParameter(
                    "@ProductID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.ProductID));

                cmd.Parameters.Add(new SqlParameter(
                    "@AltRef", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswer.AltRef));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (reader.Read())
                    {
                        readQuestionAnswers(reader, questionAnswer);
                    }
                }
            }

            return questionAnswer;
        }

        [AutoComplete]
        public List<QuestionAnswersSE> Search(string altRef, string question, byte unAnsweredQuestion)
        {
            List<QuestionAnswersSE> questionAnswers = new List<QuestionAnswersSE>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.SearchQuestion";

                cmd.Parameters.Add(new SqlParameter(
                    "@AltRef", SqlDbType.VarChar, 20, ParameterDirection.Input, true, 0, 0, "", DataRowVersion.Current, altRef));

                cmd.Parameters.Add(new SqlParameter(
                    "@SearchText", SqlDbType.VarChar, 1000, ParameterDirection.Input, true, 0, 0, "", DataRowVersion.Current, question));

                cmd.Parameters.Add(new SqlParameter(
                    "@UnAnsweredQuestion", SqlDbType.TinyInt, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, unAnsweredQuestion));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        QuestionAnswersSE qa = new QuestionAnswersSE();
                        readQuestionAnswers(reader, qa);

                        questionAnswers.Add(qa);
                    }

                    reader.Close();
                }
            }

            return questionAnswers;
        }

        [AutoComplete]
        public void AddSelectedWebsites(int questionAnswersID, int websiteID, byte showOnAll)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.QAWebsites";

                cmd.Parameters.Add(new SqlParameter(
                    "@WebsiteID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, websiteID));

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswersID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswersID));

                cmd.Parameters.Add(new SqlParameter(
                    "@ShowOnAll", SqlDbType.TinyInt, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, showOnAll));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                }

                finally
                {
                    conn.Close();
                }
            }
        }

        [AutoComplete]
        public List<KeyValuePair<int, int>> GetWebsiteMappings(int questionAnswersID)
        {
            List<KeyValuePair<int, int>> mappings = new List<KeyValuePair<int,int>>();
            
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT WebsiteFK, QuestionAnswerFK FROM ngmd.qa_WebsiteMapping WHERE QuestionAnswerFK = @QuestionAnswerID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionAnswersID));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        mappings.Add(new KeyValuePair<int, int>(reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }
            }

            return mappings;
        }

        [AutoComplete]
        public KeyValuePair<string, string> GetMembershipUser(string userId)
        {
            KeyValuePair<string, string> member;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT UserId, UserName FROM netgiantMembership.dbo.aspnet_Users WHERE UserId = '@UserID';";

                cmd.Parameters.Add(new SqlParameter(
                    "@UserID", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, userId));                

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    object uid = reader.GetGuid(0);
                    member = new KeyValuePair<string, string>(uid.ToString(), reader.GetString(1));
                    reader.Close();
                }
            }

            return member;
        }

        #endregion

        #region ObjectData Methods

        [AutoComplete]
        public int GetQACount()
        {
            int count = 0;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "Total"));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return count;
        }

        [AutoComplete]
        public int GetUnAnsweredQACount()
        {
            int count = 0;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetUnAnsweredQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "Total"));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return count;
        }

        [AutoComplete]
        public int GetFilteredQACount(string altRef, string filter)
        {
            int count = 0;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetFilteredQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "Total"));

                cmd.Parameters.Add(new SqlParameter(
                    "@AltRef", SqlDbType.VarChar, 55, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, altRef ?? ""));

                cmd.Parameters.Add(new SqlParameter(
                    "@SearchText", SqlDbType.VarChar, 55, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, filter ?? ""));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return count;
        }

        [AutoComplete]
        public DataTable GetQASummary(int startRow, int pageSize)
        {
            DataTable dtQASummary = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "All"));

                cmd.Parameters.Add(new SqlParameter(
                    "@StartRow", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, startRow));

                cmd.Parameters.Add(new SqlParameter(
                    "@PageSize", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, pageSize));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    dtQASummary.Load(reader);
                    reader.Close();
                }
            }

            return dtQASummary;
        }

        [AutoComplete]
        public DataTable GetUnAnsweredQASummary(int startRow, int pageSize)
        {
            DataTable dtQASummary = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetUnAnsweredQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "UnAnswered"));

                cmd.Parameters.Add(new SqlParameter(
                    "@StartRow", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, startRow));

                cmd.Parameters.Add(new SqlParameter(
                    "@PageSize", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, pageSize));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    dtQASummary.Load(reader);
                    reader.Close();
                }
            }

            return dtQASummary;
        }

        [AutoComplete]
        public DataTable GetFilteredQASummary(int startRow, int pageSize, string altRef, string filter)
        {
            DataTable dtQASummary = new DataTable();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.GetFilteredQASummary";

                cmd.Parameters.Add(new SqlParameter(
                    "@Mode", SqlDbType.VarChar, 12, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, "Filter"));

                cmd.Parameters.Add(new SqlParameter(
                    "@StartRow", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, startRow));

                cmd.Parameters.Add(new SqlParameter(
                    "@PageSize", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, pageSize));

                cmd.Parameters.Add(new SqlParameter(
                    "@AltRef", SqlDbType.VarChar, 55, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, altRef ?? ""));
                
                cmd.Parameters.Add(new SqlParameter(
                    "@SearchText", SqlDbType.VarChar, 55, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, filter ?? ""));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    dtQASummary.Load(reader);
                    reader.Close();
                }
            }

            return dtQASummary;
        }

        #endregion
    }
}
