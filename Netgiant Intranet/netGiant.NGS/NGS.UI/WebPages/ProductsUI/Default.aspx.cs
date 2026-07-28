using System;
using System.Web.Security;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.ProductsUI
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (Roles.IsUserInRole("PMS") || Roles.IsUserInRole("Admin"))
            {
                LinkButton lnkProducts = this.Master.FindControl("lnkProducts") as LinkButton;
                lnkProducts.Attributes.Add("class", "activeMenu");
            }
            else
            {
                Response.Redirect("~/WebPages/MembershipUI/UnAuthorised.aspx");
            }
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}