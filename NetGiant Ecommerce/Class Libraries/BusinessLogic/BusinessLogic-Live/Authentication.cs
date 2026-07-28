using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace BusinessLogic
{
    public class Authentication
    {
        /// <summary>
        /// Load the Authentication cookie into the session
        /// </summary>
        public static void LoadCookie()
        {
            string portalUserId = HttpContext.Current.Request.Cookies["__portalid"] == null ? "" : HttpContext.Current.Request.Cookies["__portalid"].Value;
            string userId = HttpContext.Current.Request.Cookies["__id"] == null ? "" : HttpContext.Current.Request.Cookies["__id"].Value;

            // If portalUserId is non blank this means the user is from Customer Services using the Portal
            if (portalUserId != "")
            {
                HttpContext.Current.Session["U_IsPortalUser"] = true;
                userId = portalUserId;
            }

            if (userId != "")
            {
                DataTable dt = new DataTable();
                if (userId.Contains("@"))
                {
                    dt = Touchpoints.GetUserData("", userId, "");
                }
                else
                {
                    dt = Touchpoints.GetUserData(userId, "", "");
                }
                PopulateSession(dt);
                if (dt.Rows.Count > 0)
                {
                    if (portalUserId == "")
                    {
                        WriteCookie("__id", userId, new TimeSpan(365, 0, 0, 0));
                        int accessCount;
                        if (int.TryParse(dt.Rows[0]["totallogins"].ToString(), out accessCount))
                        {
                            Touchpoints.UpdateLastLoggedIn(
                                HttpContext.Current.Session["U_AccountNo"].ToString(),
                                HttpContext.Current.Session["U_Email"].ToString(),
                                accessCount + 1);
                        }
                    }
                    else
                    {
                        WriteCookie("__portalid", userId, new TimeSpan(0, 2, 0, 0));
                    }
                }
            }
            else
            {
                HttpContext.Current.Session["U_Authenticated"] = false;
            }
        }

        /// <summary>
        /// Authenticate the user
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool Authenticate(string userName, string password)
        {
            bool isAuthenticated = false;

            if (userName != "")
            {
                DataTable dt = Touchpoints.GetUserData("", userName, password);
                PopulateSession(dt);
                if (dt.Rows.Count > 0)
                {
                    isAuthenticated = true;
                    string record = dt.Rows[0]["record"].ToString();
                    // Test if the record is a temporary one - if it is write email address to cookie rather than record id
                    if (record.Contains("/"))
                    {
                        WriteCookie("__id", record, new TimeSpan(365, 0, 0, 0));
                    }
                    else
                    {
                        WriteCookie("__id", userName, new TimeSpan(365, 0, 0, 0));
                    }
                }
            }

            return isAuthenticated;
        }

        /// <summary>
        /// Authenticate the user (do not expose to public)
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool PortalAuthenticate(string userId)
        {
            bool isAuthenticated = false;

            if (userId != "")
            {
                DataTable dt = Touchpoints.GetUserData(userId, "", "");
                PopulateSession(dt);
                if (dt.Rows.Count > 0)
                {
                    isAuthenticated = true;
                    WriteCookie("__portalid", dt.Rows[0]["record"].ToString(), new TimeSpan(0, 2, 0, 0));
                }
            }

            return isAuthenticated;
        }

        public static UserAccountInfo GetAccountInfo(string userName)
        {
            var userAccountInfo = new UserAccountInfo();
            
            if (userName != "")
            {
                DataTable dt = Touchpoints.GetUserData("", userName);
                if (dt.Rows.Count == 0)
                {
                    userAccountInfo.IsNewAccount = true;
                    LogOut();
                }
                else
                {
                    userAccountInfo.HasBlankPassword = dt.Rows[0]["password"].ToString() == "";
                }
            }

            return userAccountInfo;
        }

        /// <summary>
        /// Log the user out
        /// </summary>
        /// <returns></returns>
        public static bool LogOut(bool removeCookie = false)
        {
            List<string> toRemove = new List<string>();
            foreach (string key in HttpContext.Current.Session.Keys)
            {
                if (key.StartsWith("U_") || key.StartsWith("C_"))
                {
                    if (key != "U_IsPortalUser" && key != "U_AffiliateNo" && key != "U_CSUser")
                    {
                        toRemove.Add(key);
                    }
                }
            }
            foreach (string key in toRemove)
            {
                HttpContext.Current.Session.Remove(key);
            }
            HttpContext.Current.Session["U_Authenticated"] = false;
            HttpContext.Current.Session["U_FullyAuthenticated"] = false;

            if (removeCookie)
            {
                RemoveCookie("__id");
            }

            return true;
        }

        private static void PopulateSession(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];

                HttpContext.Current.Session["U_Email"]              = dr["email"].ToString();
                HttpContext.Current.Session["U_Password"]           = dr["password"].ToString();
                HttpContext.Current.Session["U_AccountNo"]          = dr["account"].ToString();
                HttpContext.Current.Session["U_Record"]             = dr["record"].ToString();
                HttpContext.Current.Session["U_Name"]               = dr["forename"].ToString().Trim() + " " + dr["surname"].ToString().Trim();
                HttpContext.Current.Session["U_IsOnMailingList"]    = dr["isOnMailingList"];
                HttpContext.Current.Session["U_IsContractPricing"]  = dr["isContractPricing"];
                HttpContext.Current.Session["U_IsAccountCustomer"]  = dr["isAccountCustomer"];
                HttpContext.Current.Session["U_Authenticated"]      = true;
                HttpContext.Current.Session["U_FullyAuthenticated"] = false;

                string id = HttpContext.Current.Session["U_Record"].ToString().Contains("/")
                    ? HttpContext.Current.Session["U_Record"].ToString()
                    : HttpContext.Current.Session["U_Email"].ToString();
                LoadFavouritePrinters(id);
                LoadRecentlyOrdered(HttpContext.Current.Session["U_Record"].ToString());
            }
        }

        /// <summary>
        /// Identify whether the user is authenticated
        /// </summary>
        /// <returns></returns>
        public static bool IsAuthenticated()
        {
            return (HttpContext.Current.Session["U_Authenticated"] != null ? (bool)HttpContext.Current.Session["U_Authenticated"] : false);
        }

        /// <summary>
        /// Identify whether the user is fully authenticated
        /// </summary>
        /// <returns></returns>
        public static bool IsFullyAuthenticated()
        {
            return HttpContext.Current.Session["U_FullyAuthenticated"] != null && (bool)HttpContext.Current.Session["U_FullyAuthenticated"];
        }

        /// <summary>
        /// Identify whether the user is not authenticated
        /// </summary>
        /// <returns></returns>
        public static bool IsNotAuthenticated()
        {
            return !(HttpContext.Current.Session["U_Authenticated"] != null ? (bool)HttpContext.Current.Session["U_Authenticated"] : false);
        }

        /// <summary>
        /// Identify whether the user is not authenticated
        /// </summary>
        /// <returns></returns>
        public static bool IsNotFullyAuthenticated()
        {
            return !(HttpContext.Current.Session["U_FullyAuthenticated"] != null ? (bool)HttpContext.Current.Session["U_FullyAuthenticated"] : false);
        }

        public static void WriteCookie(string name, string value, TimeSpan expiration)
        {
            DateTime expiry = System.DateTime.Now.Add(expiration);

            HttpCookie cookie = new HttpCookie(name);
            cookie.Value = value;
            cookie.Expires = expiry;
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void RemoveCookie(string cookieName)
        {
            DateTime expiry = System.DateTime.Now.Add(new TimeSpan(-1, 0, 0, 0));

            HttpCookie user = new HttpCookie(cookieName);
            user.Expires = expiry;
            HttpContext.Current.Response.Cookies.Add(user);
        }

        /// <summary>
        /// Fixes Session variables when temp record id switches to permanent record id
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="email"></param>
        /// <returns>True = Record is Permanent, False = Record is Temporary</returns>
        public static bool FixTempRecord(string recordId, string email)
        {
            bool isSuccess = true;

            // Attempt to fix the Account Number and Record Id when the RecordId is temporary
            if (!recordId.Contains("/"))
            {
                DataTable dt = Touchpoints.CheckTempRecord(recordId, "");

                isSuccess = false;
                if (dt.Rows.Count == 0)
                {
                    // Record Id doesn't exst so try the email address
                    dt = Touchpoints.CheckTempRecord("", email);
                    if (dt.Rows.Count > 0)
                    {
                        // Great! We've found the permanent record so reset the Session variables and Cookie
                        HttpContext.Current.Session["U_AccountNo"] = dt.Rows[0]["Account"].ToString();
                        HttpContext.Current.Session["U_Record"] = dt.Rows[0]["Record"].ToString();

                        WriteCookie("__id", HttpContext.Current.Session["U_Record"].ToString(), new TimeSpan(365, 0, 0, 0));
                        isSuccess = true;
                    }
                }
            }
            return isSuccess;
        }

        public static void LoadFavouritePrinters(string customerId)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@CustomerId", SqlDbType.VarChar);
            sqlParm.Value = customerId;
            sqlParms.Add(sqlParm);

            HttpContext.Current.Session["U_FavoutirePrinters"] = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetFavouritePrinters", sqlParms, "favprinters").Tables[0];
        }

        public static void LoadRecentlyOrdered(string customerId)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = HttpContext.Current.Session["U_AccountNo"].ToString();
            sqlParms.Add(sqlParm);
            DataTable dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetRecentlyOrdered", sqlParms, "products").Tables[0];

            string idArray = "";
            string comma = "";
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    idArray = idArray + comma + dr["Ref"].ToString().Trim();
                    if (comma == "")
                    {
                        comma = ",";
                    }
                }
            }

            sqlParms = new List<SqlParameter>();
            sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductRefArray", SqlDbType.VarChar);
            sqlParm.Value = idArray;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = HttpContext.Current.Session["U_AccountNo"].ToString();
            sqlParms.Add(sqlParm);
            HttpContext.Current.Session["U_RecentlyOrdered"] = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductFromAxisRef", sqlParms, "products").Tables[0];
        }
    }
}
