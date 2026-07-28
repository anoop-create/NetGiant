using NGS.DataAccessLayer.SimpleEntities.SharedSE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace NGS.DataAccessLayer.Services.SharedServices
{
    [Serializable]
    public class EmailSentServices : GlobalServices
    {
        string m_selectStatement = "SELECT EmailSentID, EmailSentDate, EmailSentTo, UserFK, QuestionAnswerFK FROM ngmd.qa_EmailSent ";

        void readEmailSent(SqlDataReader reader, EmailSentSE emailSent)
        {
            emailSent.EmailSentID = reader.GetInt32(0);
            emailSent.EmailSentDate = reader.GetDateTime(1);
            emailSent.EmailSentTo = reader.GetString(2);
            emailSent.RelatedUserID = reader.GetString(3);
            emailSent.RelatedQuestionID = reader.GetInt32(4);
        }

        public List<EmailSentSE> GetByQuestionID(int Id)
        {
            List<EmailSentSE> emailSentList = new List<EmailSentSE>();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE QuestionAnswerFK = @QuestionAnswerID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, Id));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        while (reader.Read())
                        {
                            EmailSentSE emailSent = new EmailSentSE();
                            readEmailSent(reader, emailSent);

                            emailSentList.Add(emailSent);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return emailSentList;
        }

        public EmailSentSE Get(int questionId, string sendTo)
        {
            EmailSentSE emailSent = new EmailSentSE();

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE QuestionAnswerFK = @QuestionAnswerID AND EmailSentTo = @EmailSentTo;";

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, questionId));

                cmd.Parameters.Add(new SqlParameter(
                    "@EmailSentTo", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, sendTo));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            readEmailSent(reader, emailSent);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return emailSent;
        }

        public EmailSentSE Save(EmailSentSE emailSent)
        {
            EmailSentSE entity = null;
            
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO ngmd.qa_EmailSent ( EmailSentDate, EmailSentTo, UserFK, QuestionAnswerFK) " +
                    "SELECT GETDATE(), @EmailSentTo, @UserId, @QuestionAnswerID; " +
                    m_selectStatement + "WHERE EmailSentID = CONVERT(INT, SCOPE_IDENTITY());";

                cmd.Parameters.Add(new SqlParameter(
                    "@UserId", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, emailSent.RelatedUserID));

                cmd.Parameters.Add(new SqlParameter(
                    "@EmailSentTo", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, emailSent.EmailSentTo));

                cmd.Parameters.Add(new SqlParameter(
                    "@QuestionAnswerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, emailSent.RelatedQuestionID));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            entity = new EmailSentSE();
                            readEmailSent(reader, entity);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return entity;
        }
    }
}
