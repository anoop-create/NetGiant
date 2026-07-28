using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.SecurityUI
{
    public partial class Admin : System.Web.UI.Page
    {
        int pageSize = 35;
        int totalUsers;
        int totalPages;
        int currentPage = 1;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (Roles.IsUserInRole("Admin"))
            {
                LinkButton lnkMembership = this.Master.FindControl("lnkMembership") as LinkButton;
                lnkMembership.Attributes.Add("class", "activeMenu");
                AddStyleToLink("AllUsers");
            }
            else
            {
                Response.Redirect("~/WebPages/MembershipUI/UnAuthorised.aspx");
            }
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                GetUsers();
            }
        }

        private void GetUsers()
        {
            UsersOnlineLabel.Text = Membership.GetNumberOfUsersOnline().ToString();

            UserGrid.DataSource = Membership.GetAllUsers(currentPage - 1, pageSize, out totalUsers);
            totalPages = ((totalUsers - 1) / pageSize) + 1;

            // Ensure that we do not navigate past the last page of users.
            if (currentPage > totalPages)
            {
                currentPage = totalPages;
                GetUsers();
                return;
            }

            UserGrid.DataBind();
            CurrentPageLabel.Text = currentPage.ToString();
            TotalPagesLabel.Text = totalPages.ToString();

            if (currentPage == totalPages)
                NextButton.Visible = false;
            else
                NextButton.Visible = true;

            if (currentPage == 1)
                PreviousButton.Visible = false;
            else
                PreviousButton.Visible = true;

            if (totalUsers <= 0)
                NavigationPanel.Visible = false;
            else
                NavigationPanel.Visible = true;
        }

        public void NextButton_OnClick(object sender, EventArgs args)
        {
            currentPage = Convert.ToInt32(CurrentPageLabel.Text);
            currentPage++;
            GetUsers();
        }

        public void PreviousButton_OnClick(object sender, EventArgs args)
        {
            currentPage = Convert.ToInt32(CurrentPageLabel.Text);
            currentPage--;
            GetUsers();
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