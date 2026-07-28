using NGS.BusinessLayer.BusinessObjects;
using NGS.BusinessLayer.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGS.UI.WebPages.QuestionAnswersUI
{
    public partial class Default : GlobalPageClass
    {
        public MembershipUser CurrentUser
        {
            get
            {
                if (!string.IsNullOrEmpty(User.Identity.Name))
                {
                    MembershipUser CurrentUser = Membership.GetUser(User.Identity.Name);
                    return CurrentUser;
                }

                return null;
            }
        }
        
        protected void Page_Init(object sender, EventArgs e)
        {
            if (Roles.IsUserInRole("QuestionAnswer") || Roles.IsUserInRole("Admin"))
            {
                LinkButton lnkQA = this.Master.FindControl("lnkQA") as LinkButton;
                lnkQA.Attributes.Add("class", "activeMenu");
            }
            else
            {
                Response.Redirect("~/WebPages/MembershipUI/UnAuthorised.aspx");
            }
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                getQuestionAnswers();
            }
        }

        #region Void Methods

        void getQuestionAnswers()
        {   
            gvQuestion.DataSourceID = "qaSummarySrc";
            gvQuestion.PageIndex = 0;
            gvQuestion.DataBind();

            if (gvQuestion.Rows.Count > 0)
            {
                pnlQuestions.Visible = true;
                pnlNoQuestions.Visible = false;
            }
            else
            {
                pnlQuestions.Visible = false;
                pnlNoQuestions.Visible = true;
                lblNoQuestions.InnerText = "No Questions Found";
            }

            AddStyleToLink("AllQuestions");
            divHeading.InnerText = "All Questions";
        }

        void AddStyleToLink(string linkName)
        {
            lnkSearch.Attributes.Remove("class");
            lnkUnAnsQuestions.Attributes.Remove("class");
            lnkAllQuestions.Attributes.Remove("class");
            lnkNewQA.Attributes.Remove("class");

            switch (linkName)
            {
                case "Search":
                    lnkSearch.Attributes.Add("class", "active");
                    break;
                case "UnAnsweredQuestions":
                    lnkUnAnsQuestions.Attributes.Add("class", "active");
                    break;
                case "AllQuestions":
                    lnkAllQuestions.Attributes.Add("class", "active");
                    break;
                case "AddQuestion":
                    lnkNewQA.Attributes.Add("class", "active");
                    break;
                default:
                    break;
            }
        }

        void sourceMapToCheckBoxes(int mapKey)
        {
            switch (mapKey.ToString())
            {
                //TonerGiant
                case "1":
                    cblSelectWebsites_Edit.Items.FindByText("TG").Selected = true;
                    break;
                //CartridgeMonkey
                case "2":
                    cblSelectWebsites_Edit.Items.FindByText("CM").Selected = true;
                    break;
                //NetGiant
                case "3":
                    cblSelectWebsites_Edit.Items.FindByText("NG").Selected = true;
                    break;
                default:
                    break;
            }
        }

        void getSourceWebsiteName(int sourceWebsiteID, out string sourceImageURL, out string sourceWebstieURL)
        {
            sourceImageURL = sourceWebstieURL = string.Empty;

            Website ws = Website.GetWebsiteByID(sourceWebsiteID);

            if (ws != null)
            {
                switch (ws.WebsiteName)
                {
                    case "cartridgemonkey":
                    case "betacartridgemonkey":
                        sourceImageURL = "~/Contents/images/CM-Favicon.jpg";
                        sourceWebstieURL = ws.WebsiteURL;
                        break;
                    case "tonergiant":
                    case "betatonergiant":
                        sourceImageURL = "~/Contents/images/TG-Favicon.jpg";
                        sourceWebstieURL = ws.WebsiteURL;
                        break;
                    case "netgiant":
                    case "betanetgiant":
                        sourceImageURL = "~/Contents/images/NG-Favicon.jpg";
                        sourceWebstieURL = ws.WebsiteURL;
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        protected void qaFilteredSummarySrc_Selecting(object sender, ObjectDataSourceSelectingEventArgs e)
        {
            e.InputParameters["altRef"] = ViewState["altRef"].ToString();
            e.InputParameters["filter"] = ViewState["filter"].ToString();
        }

        #region Gridview

        protected void gvQuestion_OnRowDataBound(object sender, GridViewRowEventArgs e)    
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView dr = (DataRowView)e.Row.DataItem;
                int sourceWebsiteID = Convert.ToInt32(dr["SourceWebsiteID"]);
                int productID = Convert.ToInt32(dr["ProductID"]);
                string altRef = dr["AltRef"].ToString();

                ((Literal)e.Row.FindControl("ltrGranuality")).Text = 
                    QuestionAnswers.GetAllGranuality().FirstOrDefault(x => x.Key == Convert.ToInt32(dr["GranularityID"])).Value;

                if (!string.IsNullOrEmpty(dr["UserFK"].ToString()))
                    ((Literal)e.Row.FindControl("ltrUser")).Text = Membership.GetUser(new Guid(dr["UserFK"].ToString())).UserName;

                Image imgSource = ((Image)e.Row.FindControl("imgSource"));
                
                string sourceImageURL, sourceWebstieURL;
                sourceImageURL = sourceWebstieURL = string.Empty;

                //Set source image url and tooltip
                if (Convert.ToInt32(dr["SourceWebsiteID"]) > 0)
                {
                    getSourceWebsiteName(sourceWebsiteID, out sourceImageURL, out sourceWebstieURL);
                    imgSource.ImageUrl = sourceImageURL;
                    imgSource.ToolTip = sourceWebstieURL;
                }
                else
                {
                    imgSource.Visible = false;
                }

                //Link for product url
                Website ws = new Website();
                string productURL = string.Empty;

                if (sourceWebsiteID > 0)
                {
                    ws = Website.GetWebsiteByID(sourceWebsiteID);

                    if (ws != null && productID > 0)
                    {
                        productURL = QuestionAnswers.GetProductURL(ws.WebsiteURL, productID);

                        if (!string.IsNullOrEmpty(productURL))
                        {
                            HyperLink altRefLink = ((HyperLink)e.Row.FindControl("hlAltRef"));
                            altRefLink.Text = altRef;
                            altRefLink.NavigateUrl = productURL;
                            altRefLink.Target = "_blank";
                            altRefLink.Visible = true;
                        }
                        else
                        {
                            Literal ltrAltRef = ((Literal)e.Row.FindControl("ltrAltRef"));
                            ltrAltRef.Text = altRef;
                            ltrAltRef.Visible = true;
                        }
                    }
                }
                else
                {
                    Literal ltrAltRef = ((Literal)e.Row.FindControl("ltrAltRef"));
                    ltrAltRef.Text = altRef;
                    ltrAltRef.Visible = true;
                }
            }
        }

        protected void gvQuestion_RowCommand(object sender, GridViewCommandEventArgs e)
        {   
            QuestionAnswers qa = new QuestionAnswers();

            if (e.CommandName == "DeleteQuestionAnswer")
            {
                qa = QuestionAnswers.GetQuestionAnswerByID(Convert.ToInt32(gvQuestion.DataKeys[int.Parse(e.CommandArgument.ToString())]["QuestionAnswerID"]));
                
                //Delete
                if (qa != null)
                    qa.Delete();

                //Reload data
                getQuestionAnswers();
            }
            else if (e.CommandName == "EditQuestionAnswer")
            {
                qa = QuestionAnswers.GetQuestionAnswerByID(Convert.ToInt32(gvQuestion.DataKeys[int.Parse(e.CommandArgument.ToString())]["QuestionAnswerID"]));

                if (qa != null)
                {
                    ViewState["qaID"] = qa.QuestionAnswerID.ToString();
                    
                    txtQuestion.Text = qa.Question;
                    txtAnswer.Text = qa.Answer;
                    txtEmail.Text = qa.Email;
                    txtAltRef.Text = qa.AltRef;

                    ddlGranularity.DataSource = QuestionAnswers.GetAllGranuality();
                    ddlGranularity.DataBind();
                    ddlGranularity.Items.Insert(0, new ListItem("Select...", ""));
                    ddlGranularity.Items.FindByValue(qa.RelatedGranularityID.ToString()).Selected = true;

                    if (ddlGranularity.SelectedItem.Text.Equals("All products", StringComparison.InvariantCultureIgnoreCase))
                    {
                        rfvAltRef_Edit.Enabled = false;
                        txtAltRef.Enabled = false;
                        txtAltRef.Text = "";
                    }
                    else
                    {
                        rfvAltRef_Edit.Enabled = true;
                        txtAltRef.Enabled = true;
                    }

                    List<KeyValuePair<int, int>> websiteMappings = QuestionAnswers.GetQAWebsitesMapping(qa.QuestionAnswerID);

                    if (qa.ShowOnAllWebsites == 0)
                    {
                        cbShowOnAll_Edit.Checked = false;
                        trSelectWebsites_Edit.Visible = true;

                        if (websiteMappings.Count > 0)
                        {
                            foreach (KeyValuePair<int, int> mapp in websiteMappings)
                            {
                                sourceMapToCheckBoxes(mapp.Key);
                            }
                        }
                    }
                    else
                    {
                        cbShowOnAll_Edit.Checked = true;
                        trSelectWebsites_Edit.Visible = false;
                        for (int i = 0; i < cblSelectWebsites_Edit.Items.Count; i++)
                        {
                            cblSelectWebsites_Edit.Items[i].Selected = false;
                        }
                    }

                    if (qa.SourceWebsiteID > 0)
                    {
                        //Get source website image and url
                        string sourceImageURL, sourceWebstieURL;
                        sourceImageURL = sourceWebstieURL = string.Empty;

                        getSourceWebsiteName(qa.SourceWebsiteID, out sourceImageURL, out sourceWebstieURL);
                        ltrSourceWebsiteURL.Text = sourceWebstieURL;
                    }
                    else
                    {
                        ltrSourceWebsiteURL.Text = string.Format("Added by {0}", Membership.GetUser(new Guid(qa.RelatedUserID)).UserName);
                    }
                    
                    panelOverlay.Visible = true;
                    panelPopUpPanel.Visible = true;
                }
            }
        }

        #endregion

        #region SideBar Links

        protected void lnkSearch_Click(object sender, EventArgs e)
        {
            lblNoQuestions.Visible = false;
            AddStyleToLink("Search");
            divHeading.InnerText = "Search";
            pnlOverlay.Visible = true;
            pnlSearch.Visible = true;
        }

        protected void lnkUnAnsQuestions_Click(object sender, EventArgs e)
        {
            AddStyleToLink("UnAnsweredQuestions");
            divHeading.InnerText = "UnAnswered Questions";

            gvQuestion.DataSourceID = "";
            gvQuestion.DataSourceID = "qaUnAnsSummarySrc";
            gvQuestion.PageIndex = 0;
            gvQuestion.DataBind();

            if (gvQuestion.Rows.Count == 0)
            {
                pnlQuestions.Visible = false;
                lblNoQuestions.InnerText = "No UnAnswered Questions Found!";
                pnlNoQuestions.Visible = true;
                return;
            }
        }

        protected void lnkAllQuestions_Click(object sender, EventArgs e)
        {
            getQuestionAnswers();
        }

        protected void lnkNewQA_Click(object sender, EventArgs e)
        {
            AddStyleToLink("AddQuestion");
            divHeading.InnerText = "Add Question";

            txtAskedDate_AddQuestion.Text = DateTime.Now.ToString();

            ddlGranuality_AddQuestion.DataSource = QuestionAnswers.GetAllGranuality();
            ddlGranuality_AddQuestion.DataBind();
            ddlGranuality_AddQuestion.Items.Insert(0, new ListItem("Select...", ""));
            ddlGranuality_AddQuestion.Items.FindByText("This product").Selected = true;

            pnlOverlay_AddQuestion.Visible = true;
            pnlPopup_AddQuestion.Visible = true;
            pnlNoQuestions.Visible = false;
        }

        #endregion

        #region Search

        protected void imgCloseSearch_Click(object sender, EventArgs e)
        {
            pnlOverlay.Visible = false;
            pnlSearch.Visible = false;

            Response.Redirect("~/WebPages/QuestionAnswersUI/Default.aspx");
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            if (txtSearchAltRef.Text.Trim().Length == 0 && txtSearchQuestion.Text.Trim().Length == 0)
            {
                DisplayAlert("Please provide either AltRef or question contents!", true);
                return;
            }
            else
            {
                ViewState["altRef"] = txtSearchAltRef.Text.Trim();
                ViewState["filter"] = txtSearchQuestion.Text.Trim();

                gvQuestion.DataSourceID = "";
                gvQuestion.DataSourceID = "qaFilteredSummarySrc";
                gvQuestion.PageIndex = 0;
                gvQuestion.DataBind();

                if (gvQuestion.Rows.Count == 0)
                {
                    pnlQuestions.Visible = false;
                    lblNoQuestions.InnerText = "No Questions Found Based On Search Term!";
                    pnlNoQuestions.Visible = true;
                    lblNoQuestions.Visible = true;
                    pnlOverlay.Visible = false;
                    pnlSearch.Visible = false;
                    return;
                }

                pnlOverlay.Visible = false;
                pnlSearch.Visible = false;
            }
        }

        #endregion

        #region Add Question

        void ClearAddQuestion()
        {   
            txtQuestion_AddQuestion.Text = txtAltRef.Text = txtEmail_AddQuestion.Text = txtAskedDate_AddQuestion.Text = string.Empty;
            ddlGranularity.ClearSelection();

            pnlOverlay_AddQuestion.Visible = false;
            pnlPopup_AddQuestion.Visible = false;

            Response.Redirect("~/WebPages/QuestionAnswersUI/Default.aspx");
        }
        
        protected void imgClose_AddQuestion_Click(object sender, ImageClickEventArgs e)
        {
            ClearAddQuestion();
        }

        protected void btnSave_AddQuestion_Click(object sender, EventArgs e)
        {
            QuestionAnswers qa = new QuestionAnswers();
            qa.Question = txtQuestion_AddQuestion.Text;
            qa.Answer = txtAnswer_AddQuestion.Text;
            qa.AltRef = txtAltRef_AddQuestion.Text;
            qa.Email = txtEmail_AddQuestion.Text;
            qa.AskedDate = Convert.ToDateTime(txtAskedDate_AddQuestion.Text);
            qa.RelatedGranularityID = Convert.ToInt32(ddlGranuality_AddQuestion.SelectedValue);
            qa.ShowOnAllWebsites = Convert.ToByte(cbShowOnAll_AddQuestion.Checked ? 1 : 0);
            qa.RelatedUserID = CurrentUser != null ? CurrentUser.ProviderUserKey.ToString() : "";
            qa.Save();

            //Add Selected Websites
            if (qa.QuestionAnswerID > 0 && !cbShowOnAll_AddQuestion.Checked)
            {
                for (int i = 0; i < cblSelectWebsites_AddQuestion.Items.Count; i++)
                {
                    if (cblSelectWebsites_AddQuestion.Items[i].Selected)
                    {
                        string domain = cblSelectWebsites_AddQuestion.Items[i].Text.ToLower().Equals("tg") ? "tonergiant" :
                            cblSelectWebsites_AddQuestion.Items[i].Text.ToLower().Equals("cm") ? "cartridgemonkey" :
                            cblSelectWebsites_AddQuestion.Items[i].Text.ToLower().Equals("ng") ? "netgiant" : "";

                        Website ws = new Website();

                        if (Request.Url.ToString().Contains("beta") || Request.Url.ToString().Contains("local"))
                        {
                            ws = Website.GetWebsiteByName(string.Format("{0}{1}", "beta", domain));
                        }
                        else
                        {
                            ws = Website.GetWebsiteByName(domain);
                        }

                        qa.AddWebsites(qa.QuestionAnswerID, ws.WebsiteID, 0);
                    }
                }
            }

            ClearAddQuestion();
        }

        protected void btnCancel_AddQuestion_Click(object sender, EventArgs e)
        {
            ClearAddQuestion();
        }

        protected void cbShowOnAll_AddQuestion_CheckedChanged(object sender, EventArgs e)
        {
            trSelectWebsites_AddQuestion.Visible = !cbShowOnAll_AddQuestion.Checked;
        }

        protected void ddlGranuality_AddQuestion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlGranuality_AddQuestion.SelectedItem.Text.Equals("All products", StringComparison.InvariantCultureIgnoreCase))
            {
                rfvAltRef_AddQuestion.Enabled = false;
                txtAltRef_AddQuestion.Enabled = false;
                txtAltRef_AddQuestion.Text = "";
            }
            else
            {
                rfvAltRef_AddQuestion.Enabled = true;
                txtAltRef_AddQuestion.Enabled = true;
            }
        }

        #endregion

        #region Edit Question

        void ClearPopupControls()
        {
            txtQuestion.Text = txtAnswer.Text = txtEmail.Text = txtAltRef.Text = string.Empty;
            ddlGranularity.ClearSelection();
        }

        protected void imgClosePopup_Click(object sender, EventArgs e)
        {
            ClearPopupControls();

            panelOverlay.Visible = false;
            panelPopUpPanel.Visible = false;
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            ClearPopupControls();

            panelOverlay.Visible = false;
            panelPopUpPanel.Visible = false;
        }
        
        protected void btnSave_Click(object sender, EventArgs e)
        {
            //Save
            QuestionAnswers qa = QuestionAnswers.GetQuestionAnswerByID(Convert.ToInt32(ViewState["qaID"]));
            qa.Question = txtQuestion.Text.Trim();
            qa.Answer = txtAnswer.Text.Trim();
            qa.Email = txtEmail.Text.Trim();
            qa.AltRef = txtAltRef.Text.Trim();
            qa.RelatedGranularityID = Convert.ToInt32(ddlGranularity.SelectedValue);
            qa.ShowOnAllWebsites = Convert.ToByte(cbShowOnAll_Edit.Checked ? 1 : 0);
            qa.RelatedUserID = CurrentUser != null ? CurrentUser.ProviderUserKey.ToString() : "";
            qa.Save();

            #region Mappings

            //Add Selected Websites mappings
            if (qa.QuestionAnswerID > 0 && !cbShowOnAll_Edit.Checked)
            {
                //Delete website mapping for the qa
                qa.AddWebsites(qa.QuestionAnswerID, 0, 1);

                //if all checkboxes are ticked
                bool allChecked = true;
                for (int i = 0; i < cblSelectWebsites_Edit.Items.Count; i++)
                {
                    if (!cblSelectWebsites_Edit.Items[i].Selected)
                    {
                        allChecked = false;
                        break;
                    }
                }

                //delete mapping if all websites are checked
                if (allChecked)
                {
                    qa.AddWebsites(qa.QuestionAnswerID, 0, 1);
                    qa.ShowOnAllWebsites = 1;
                    qa.Save();
                }
                else
                {
                    //Add new mappings based on the new selection
                    for (int i = 0; i < cblSelectWebsites_Edit.Items.Count; i++)
                    {
                        if (cblSelectWebsites_Edit.Items[i].Selected)
                        {
                            string domain = cblSelectWebsites_Edit.Items[i].Text.ToLower().Equals("tg") ? "tonergiant" :
                                cblSelectWebsites_Edit.Items[i].Text.ToLower().Equals("cm") ? "cartridgemonkey" :
                                cblSelectWebsites_Edit.Items[i].Text.ToLower().Equals("ng") ? "netgiant" : "";

                            Website ws = new Website();
                            ws = Website.GetWebsiteByName(domain);

                            qa.AddWebsites(qa.QuestionAnswerID, ws.WebsiteID, 0);
                        }
                    }
                }
            }
            else if (qa.QuestionAnswerID > 0 && cbShowOnAll_Edit.Checked)
            {
                qa.AddWebsites(qa.QuestionAnswerID, 0, 1);
            }

            #endregion

            #region Email

            //Send Email
            if (qa.SourceWebsiteID > 0 && qa.ProductID > 0 && !string.IsNullOrEmpty(qa.Answer) && !string.IsNullOrEmpty(qa.Email) && qa.RepliedDate.HasValue)
            {
                string body = string.Empty;
                bool isSent = EmailSent.GetByQuestionAndSendTo(qa.QuestionAnswerID, qa.Email);

                //Don't send email again if it has been send before
                if (!isSent)
                {
                    using (StreamReader reader = new StreamReader(string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates\\QAMail.html")))
                    {
                        body = reader.ReadToEnd();
                    }

                    Website ws = Website.GetWebsiteByID(qa.SourceWebsiteID);

                    if (null != ws)
                    {
                        QuestionAnswers.SendQAEmail(ws.WebsiteURL, qa.ProductID, ws.WebsiteURL, qa.Email, body);

                        //Record email sent
                        try
                        {
                            EmailSent emailSent = new EmailSent();
                            emailSent.EmailSentTo = qa.Email;
                            emailSent.RelatedUserID = CurrentUser.ProviderUserKey.ToString();
                            emailSent.RelatedQuestionID = qa.QuestionAnswerID;
                            emailSent.Save();
                        }

                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                    }
                }
            }

            #endregion

            //Hide Popup
            panelOverlay.Visible = false;
            panelPopUpPanel.Visible = false;

            //Fetch data
            getQuestionAnswers();
        }
        
        protected void cbShowOnAll_Edit_CheckedChanged(object sender, EventArgs e)
        {
            trSelectWebsites_Edit.Visible = !cbShowOnAll_Edit.Checked;
        }

        protected void ddlGranularity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlGranularity.SelectedItem.Text.Equals("All products", StringComparison.InvariantCultureIgnoreCase))
            {
                rfvAltRef_Edit.Enabled = false;
                txtAltRef.Enabled = false;
                txtAltRef.Text = "";
            }
            else
            {
                rfvAltRef_Edit.Enabled = true;
                txtAltRef.Enabled = true;
            }
        }

        #endregion
    }
}