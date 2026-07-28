using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.MembershipUI
{
    public partial class Registration : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (Roles.IsUserInRole("Admin"))
            {
                LinkButton lnkMembership = this.Master.FindControl("lnkMembership") as LinkButton;
                lnkMembership.Attributes.Add("class", "activeMenu");
                AddStyleToLink("Registration");
            }
            else
            {
                Response.Redirect("~/WebPages/MembershipUI/UnAuthorised.aspx");
            }
        }

        protected void CreateUserWizard1_CreatedUser(object sender, EventArgs e)
        {
            MembershipCreateStatus p = MembershipCreateStatus.Success;
            Membership.CreateUser(CreateUserWizard1.UserName, CreateUserWizard1.Password, CreateUserWizard1.Email, CreateUserWizard1.Question, 
                CreateUserWizard1.Answer, true, out p);
        }

        protected void CreateUserWizard1_ContinueButtonClick(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            
            //TODO: find a better way
            if (Membership.ValidateUser("admin", "intranet.2014") == true)
            {
                Membership.GetUser("admin", true);
                FormsAuthentication.RedirectFromLoginPage("admin", true);
            }

            Response.Redirect("~/WebPages/MembershipUI/Admin.aspx");
        }

        protected void lnkRegistration_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/Registration.aspx");
        }

        protected void lnkAllUsers_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/Admin.aspx");
        }

        void AddStyleToLink(string linkName)
        {
            lnkAllUsers.Attributes.Remove("class");
            lnkRegistration.Attributes.Remove("class");

            switch (linkName)
            {
                case "AllUsers":
                    lnkAllUsers.Attributes.Add("class", "active");
                    break;
                case "Registration":
                    lnkRegistration.Attributes.Add("class", "active");
                    break;
                default:
                    break;
            }
        }

        protected void lnkRoles_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/ManageRoles.aspx");
        }
    }
}