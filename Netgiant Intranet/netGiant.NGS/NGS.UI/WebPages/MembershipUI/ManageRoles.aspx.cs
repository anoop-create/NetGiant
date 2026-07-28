using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.MembershipUI
{
    public partial class ManageRoles : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (Roles.IsUserInRole("Admin"))
            {
                LinkButton lnkMembership = this.Master.FindControl("lnkMembership") as LinkButton;
                lnkMembership.Attributes.Add("class", "activeMenu");
                AddStyleToLink("ManageRoles");
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
                BindUsers();
                BindRoles();
            }
        }

        protected void btnAssignRoleToUser_Click(object sender, EventArgs e)
        {
            lblNewRoleMessage.Text = "";
            ClearCurrentUserRolesList();

            try
            {
                if (!Roles.IsUserInRole(listRoles.SelectedItem.Text))
                {
                    Roles.AddUserToRole(listUsers.SelectedItem.Text, listRoles.SelectedItem.Text);
                    BindUsers();
                    BindRoles();
                    lblNewRoleMessage.Text = "Role Assigned To User Successfully";
                }
                else
                {
                    lblNewRoleMessage.Text = "Role Already Assigned To User";
                }
            }
            catch (Exception ex)
            {
                lblNewRoleMessage.Text = ex.Message;
            }
        }

        protected void btnRemoveUserFromUser_Click(object sender, EventArgs e)
        {
            lblNewRoleMessage.Text = "";
            ClearCurrentUserRolesList();
            
            try
            {
                Roles.RemoveUserFromRole(listUsers.SelectedItem.Text, listRoles.SelectedItem.Text);
                BindUsers();
                BindRoles();
                lblNewRoleMessage.Text = "User Is Removed From The Role Successfully";
            }
            catch (Exception ex)
            {
                lblNewRoleMessage.Text = ex.Message;
            }
        }

        protected void btnRemoveRoles_Click(object sender, EventArgs e)
        {
            lblNewRoleMessage.Text = string.Empty;
            ClearCurrentUserRolesList();

            try
            {
                Roles.DeleteRole(listRoles.SelectedItem.Text);
                BindUsers();
                BindRoles();
                lblNewRoleMessage.Text = "Role Removed Successfully";
            }
            catch (Exception ex)
            {
                lblNewRoleMessage.Text = ex.Message;
            }
        }

        public void BindRoles()
        {
            listRoles.DataSource = NGS.BusinessLayer.BusinessObjects.Shared.User.GetAllRoleNames();
            listRoles.DataBind();
        }

        public void BindUsers()
        {
            listUsers.DataSource = NGS.BusinessLayer.BusinessObjects.Shared.User.GetAllUserNames();
            listUsers.DataBind();
        }

        void ClearCurrentUserRolesList()
        {
            listCurrentUserRoles.Dispose();
            listCurrentUserRoles.Items.Clear();
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
                case "ManageRoles":
                    lnkRoles.Attributes.Add("class", "active");
                    break;
                default:
                    break;
            }
        }

        protected void btnNewRole_Click(object sender, EventArgs e)
        {
            lblNewRoleMessage.Text = string.Empty;
            
            try
            {
                if (!Roles.RoleExists(txtNewRole.Text.Trim()))
                {
                    Roles.CreateRole(txtNewRole.Text.Trim());
                    lblNewRoleMessage.Text = string.Empty;
                    BindUsers();
                    BindRoles();
                    lblNewRoleMessage.Text = "Role Created Successfully";
                }
                else
                {
                    lblNewRoleMessage.Text = string.Format("{0} Role Already Exists.", txtNewRole.Text.Trim());
                }
            }

            catch (Exception ex)
            {
                lblNewRoleMessage.Text = ex.Message;
            }
        }

        protected void listUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearCurrentUserRolesList();
            listCurrentUserRoles.DataSource = Roles.GetRolesForUser(listUsers.SelectedItem.Text);
            listCurrentUserRoles.DataBind();
        }

        protected void lnkRegistration_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/Registration.aspx");
        }

        protected void lnkAllUsers_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WebPages/MembershipUI/Admin.aspx");
        }
    }
}