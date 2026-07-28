<%@ Page Title="Manage Roles" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="ManageRoles.aspx.cs" Inherits="NGS.UI.WebPages.MembershipUI.ManageRoles" %>
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
                    <asp:LinkButton ID="lnkRoles" runat="server" Text="Roles" />
                </span>
            </li>
        </ul>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <div class="qa_placeHolder">
        <div class="qa_placeHolderHeading gl_textType2b">Create Roles<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:UpdatePanel ID="upnlUsers" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>
                    
                    <div class="roles">
                        <table>
                            <tr>
                                <th style="text-align:right;">
                                    <asp:Label ID="lblNewRole" runat="server" Text="New Role:"></asp:Label>
                                </th>
                                <td>
                                    <asp:TextBox ID="txtNewRole" runat="server"></asp:TextBox>
                                    <asp:Button ID="btnNewRole" runat="server" Text="Create Role" CssClass="qa_submitButton" ValidationGroup="NewRole" OnClick="btnNewRole_Click" />
                                </td>
                            </tr>
                            <tr>
                                <th></th>
                                <td>
                                    <div style="height:10px;">
                                        <asp:RequiredFieldValidator ID="rfvNewRole" runat="server" SetFocusOnError="true" ErrorMessage="Please Provide New Role"
                                            Display="Dynamic" ControlToValidate="txtNewRole" ValidationGroup="NewRole" ForeColor="#FF00000"></asp:RequiredFieldValidator>
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <th></th>
                                <td>
                                    <asp:Label ID="lblNewRoleMessage" runat="server" ForeColor="#ff0000"></asp:Label>
                                </td>
                            </tr>
                        </table>
                    </div>

                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>

    <div class="qa_placeHolder">
        <div class="qa_placeHolderHeading gl_textType2b">Assign Roles<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:UpdatePanel ID="UpdatePanel1" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>

                    <div>
                        <table>
                            <tr>
                                <th>Available Users</th>
                                <th>Available Roles</th>
                                <th>Current User Role(s)</th>
                            </tr>
                            <tr>
                                <td>
                                    <asp:ListBox ID="listUsers" runat="server" CssClass="roles_List" OnSelectedIndexChanged="listUsers_SelectedIndexChanged" AutoPostBack="true"></asp:ListBox>
                                </td>
                                <td>
                                    <asp:ListBox ID="listRoles" runat="server" CssClass="roles_List"></asp:ListBox>
                                </td>
                                <td>
                                    <asp:ListBox ID="listCurrentUserRoles" runat="server" CssClass="roles_List"></asp:ListBox>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="3" class="gl_alignCenter">
                                    <asp:Button ID="btnAssignRoleToUser" runat="server" Text="Assign Role To User" CssClass="qa_submitButton" Width="160px" OnClick="btnAssignRoleToUser_Click" />
                                    <asp:Button ID="btnRemoveUserFromUser" runat="server" Text="Remove User From Role" CssClass="qa_submitButton" Width="160px" OnClick="btnRemoveUserFromUser_Click" />
                                    <asp:Button ID="btnRemoveRoles" runat="server" Text="Delete Roles" CssClass="qa_submitButton" Width="160px" OnClick="btnRemoveRoles_Click" />
                                </td>
                            </tr>
                        </table>
                    </div>

                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>
</asp:Content>
