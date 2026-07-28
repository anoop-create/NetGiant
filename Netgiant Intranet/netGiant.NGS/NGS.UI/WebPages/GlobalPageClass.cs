using System;
using System.Linq;
using System.Web.UI;

namespace NGS.UI.WebPages
{
    public class GlobalPageClass : System.Web.UI.Page
    {
        #region Global Methods

        public void DisplayAlert(string alertMessage, bool inUpdatePanel)
        {
            //Remove any invalid characters that would cause the alert to fail
            alertMessage = alertMessage.Replace("\"", string.Empty).Replace("\r\n", " ");

            string script = "<script language=\"javascript\">alert(\"" + alertMessage + "\")</script>";
            if (inUpdatePanel)
            {
                ScriptManager.RegisterStartupScript(Page, this.GetType(), Guid.NewGuid().ToString(), script, false);
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), Guid.NewGuid().ToString(), script, false);
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("Please provide a string!");

            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }

        #endregion
    }
}