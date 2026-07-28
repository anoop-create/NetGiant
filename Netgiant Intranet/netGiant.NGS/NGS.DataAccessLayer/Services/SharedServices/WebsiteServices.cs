using NGS.DataAccessLayer.SimpleEntities.SharedSE;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Text;

namespace NGS.DataAccessLayer.Services.SharedServices
{
    [Serializable]
    public class WebsiteServices : GlobalServices
    {
        string m_selectStatement = "SELECT WebsiteID, WebsiteName, WebsiteURL FROM ngmd.Website ";

        void readWebsite(SqlDataReader reader, WebsiteSE website)
        {
            website.WebsiteID = reader.GetInt32(0);
            website.WebsiteName = reader.GetString(1);
            website.WebsiteURL = reader.GetString(2);
        }

        [AutoComplete]
        public WebsiteSE GetWebsiteByID(int id)
        {
            WebsiteSE ws = null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE WebsiteID = @WebsiteID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@WebsiteID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, id));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            ws = new WebsiteSE();
                            readWebsite(reader, ws);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return ws;
        }

        [AutoComplete]
        public WebsiteSE GetWebsiteByName(string name)
        {
            WebsiteSE ws = null;

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE WebsiteName = @WebsiteName;";

                cmd.Parameters.Add(new SqlParameter(
                    "@WebsiteName", SqlDbType.VarChar, 100, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, name));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        if (reader.Read())
                        {
                            ws = new WebsiteSE();
                            readWebsite(reader, ws);
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.ToString());
                    }
                }
            }

            return ws;
        }

        [AutoComplete]
        public List<WebsiteSE> GetAllWebsites()
        {
            List<WebsiteSE> list = null;

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
                        WebsiteSE ws = new WebsiteSE();
                        readWebsite(reader, ws);

                        list.Add(ws);
                    }

                    reader.Close();
                }
            }

            return list;
        }
    }
}
