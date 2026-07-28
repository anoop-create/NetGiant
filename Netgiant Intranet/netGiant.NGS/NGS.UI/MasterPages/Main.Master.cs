using NGS.UI.WebPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.MasterPages
{
    public partial class Main : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                SetUser();
            }
        }

        void SetUser()
        {
            string user = Page.User.Identity.Name;

            if (!string.IsNullOrEmpty(user))
            {
                if (Roles.IsUserInRole("admin"))
                {
                    lnkMembership.Visible = true;
                }
                
                hlMyProfile.Visible = true;

                string username = "Welcome";
                foreach (string word in user.Split('.'))
                {
                    username += " ";
                    username += GlobalPageClass.FirstCharToUpper(word);
                    break;
                }

                if (!string.IsNullOrEmpty(username.Trim()))
                {
                    lblUser.Text = username.Trim();
                    lblUser.Visible = true;
                }

                //Set User Image
                string imgURL = string.Format(@"~/Contents/images/users/{0}.jpg", user);
                bool imgExists = File.Exists(Server.MapPath(imgURL));
                imgUser.ImageUrl = imgExists ? imgURL : @"~/Contents/images/1pxTrans.png";
            }
        }

        protected void lnkQA_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/QuestionAnswersUI/Default.aspx");
        }

        protected void lnkProducts_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/ProductsUI/Default.aspx");
        }

        protected void Unnamed_LoggedOut(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
        }

        protected void lnkMembership_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/Admin.aspx");
        }
    }
}