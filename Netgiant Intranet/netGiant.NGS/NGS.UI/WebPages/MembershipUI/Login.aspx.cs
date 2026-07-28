using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.SecurityUI
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
        }

        protected void Login1_Authenticate(object sender, AuthenticateEventArgs e)
        {
            if (Membership.ValidateUser(Login1.UserName, Login1.Password) == true)
            {
                Login1.Visible = true;
                Session["user"] = User.Identity.Name;
                Membership.GetUser(Login1.UserName, true);
                FormsAuthentication.RedirectFromLoginPage(Login1.UserName, true);
            }
        }

        protected void Login1_LoginError(object sender, EventArgs e)
        {
            MembershipUser userInfo = Membership.GetUser(Login1.UserName);

            if (userInfo != null)
            {
                if (userInfo.IsLockedOut)
                {
                    Login1.FailureText = "Your account has been locked out because of too many invalid login attempts. Please contact the administrator to have your account unlocked.";
                }
                else if (!userInfo.IsApproved)
                {
                    Login1.FailureText = "Your account has not yet been approved. You cannot login until an administrator has approved your account.";
                }
            }
        }
    }
}