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
    public class UserServices : GlobalServices
    {
        //string m_selectStatment = "SELECT ApplicationId, UserId, UserName, MobileAlias, IsAnonymous, LastActivityDate FROM dbo.aspnet_Users";


        [AutoComplete]
        public List<string> GetAllUserNames()
        {
            List<string> users = new List<string>();

            using (SqlConnection conn = new SqlConnection(MembershipConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT UserName FROM dbo.aspnet_Users;";

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        users.Add(reader.GetString(0));
                    }
                }
            }

            return users;
        }

        [AutoComplete]
        public List<string> GetAllRoles()
        {
            List<string> roles = new List<string>();

            using (SqlConnection conn = new SqlConnection(MembershipConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT RoleName FROM dbo.aspnet_Roles;";

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (reader.Read())
                    {
                        roles.Add(reader.GetString(0));
                    }
                }
            }

            return roles;
        }
    }
}
