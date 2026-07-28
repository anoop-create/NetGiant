<%@ Page Title="UnAuthorised" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="UnAuthorised.aspx.cs" Inherits="NGS.UI.WebPages.MembershipUI.UnAuthorised" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="UserContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="SideBarMenuPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <div class="qa_placeHolder">
        <div id="div1" runat="server" class="qa_placeHolderHeading gl_textType2b"><h1>Unauthorised Access</h1></div>
        <div class="qa_placeHolderBody gl_textType2a">
            <p>You have attempted to access a page that you are not authorised to view.</p>
            <p>If you have questions, please contact the adminstrator.</p>
        </div>
    </div>
</asp:Content>
