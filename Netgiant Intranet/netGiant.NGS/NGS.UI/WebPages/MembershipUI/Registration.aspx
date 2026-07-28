<%@ Page Title="Registration" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="Registration.aspx.cs" Inherits="NGS.UI.WebPages.MembershipUI.Registration" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="UserContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="SideBarMenuPlaceHolder" runat="server">
    <div class="sideBar_Menu gl_textType2b">
        <ul>
            <li>
                <span>
                    <asp:LinkButton ID="lnkAllUsers" runat="server" Text="Users" OnClick="lnkAllUsers_Click" />
                </span>
            </li>
            <li>
                <span>
                    <asp:LinkButton ID="lnkRegistration" runat="server" Text="CreateUser" OnClick="lnkRegistration_Click" />
                </span>
            </li>
            <li>
                <span>
                    <asp:LinkButton ID="lnkRoles" runat="server" Text="Roles" OnClick="lnkRoles_Click" />
                </span>
            </li>
        </ul>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <div class="qa_placeHolder">
        <div class="qa_placeHolderHeading gl_textType2b">Create User<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:UpdatePanel ID="upnlRegistration" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>

                    <asp:CreateUserWizard ID="CreateUserWizard1" runat="server" OnCreatedUser="CreateUserWizard1_CreatedUser" OnContinueButtonClick="CreateUserWizard1_ContinueButtonClick"
                        CreateUserButtonStyle-CssClass="qa_submitButton" TextBoxStyle-CssClass="login-input" CompleteSuccessTextStyle-VerticalAlign="NotSet">
                        <WizardSteps>
                            <asp:CreateUserWizardStep ID="CreateUserWizardStep1" runat="server" Title="">
                            </asp:CreateUserWizardStep>
                            <asp:CompleteWizardStep ID="CompleteWizardStep1" runat="server">
                            </asp:CompleteWizardStep>
                        </WizardSteps>
                    </asp:CreateUserWizard>

                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>
</asp:Content>
