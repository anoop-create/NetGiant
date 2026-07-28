<%@ Page Title="Log In" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="NGS.UI.WebPages.SecurityUI.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="UserContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="SideBarMenuPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">

    <div class="qa_placeHolder">
        <div class="qa_placeHolderHeading gl_textType2b">Log In<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:Login ID="Login1" runat="server" OnAuthenticate="Login1_Authenticate" OnLoginError="Login1_LoginError"
                InstructionText="" RememberMeText="Remember me">
                <LoginButtonStyle CssClass="qa_submitButton login-submit" />
                <TitleTextStyle CssClass="gl_hide" />
                <LabelStyle CssClass="login-label" />
                <TextBoxStyle CssClass="login-input" />
                <CheckBoxStyle CssClass="login-checkbox" />
                <FailureTextStyle CssClass="login-error" />
                <ValidatorTextStyle CssClass="login-error" />
            </asp:Login>
        </div>
    </div>

</asp:Content>
