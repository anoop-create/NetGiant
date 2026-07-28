<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="NGS.UI.WebPages.MembershipUI.ChangePassword" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="UserContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="SideBarMenuPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <div class="qa_placeHolder">
        <div class="qa_placeHolderHeading gl_textType2b">Change Password<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:UpdatePanel ID="upnlUsers" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>
                    <asp:ChangePassword ID="ChangePassword1" runat="server" CancelDestinationPageUrl="~/WebPages/QuestionAnswersUI/Default.aspx" 
                        ContinueDestinationPageUrl="~/WebPages/QuestionAnswersUI/Default.aspx" ChangePasswordTitleText="" ChangePasswordButtonStyle-CssClass="qa_submitButton"
                        CancelButtonStyle-CssClass="qa_submitButton" ContinueButtonStyle-CssClass="qa_submitButton" TextBoxStyle-CssClass="login-input">
                    </asp:ChangePassword>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>
</asp:Content>
