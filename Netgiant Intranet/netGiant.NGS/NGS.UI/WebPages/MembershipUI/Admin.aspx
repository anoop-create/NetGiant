<%@ Page Title="Users" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="NGS.UI.WebPages.SecurityUI.Admin" %>

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
        <div class="qa_placeHolderHeading gl_textType2b">Users<hr /></div>
        <div class="qa_placeHolderBody">
            <asp:UpdatePanel ID="upnlUsers" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>
                    
                    Number of Users Online: <asp:Label id="UsersOnlineLabel" runat="Server" /><br />

                    <asp:DataGrid id="UserGrid" runat="server" CssClass="gl_TabularContainer gl_textType1a" HeaderStyle-CssClass="gl_border1g gl_textType1b"
                        CellPadding="2" CellSpacing="1" ItemStyle-CssClass="gl_border1g" AutoGenerateColumns="false">
                        <HeaderStyle BackColor="#1A1A1A" ForeColor="#FFFFFF" />
                        <Columns>
                            <asp:BoundColumn DataField="UserName" HeaderText="UserName"></asp:BoundColumn>
                            <asp:BoundColumn DataField="Email" HeaderText="Email"></asp:BoundColumn>
                            <asp:BoundColumn DataField="PasswordQuestion" HeaderText="PasswordQuestion"></asp:BoundColumn>
                            <asp:BoundColumn DataField="IsApproved" HeaderText="IsApproved"></asp:BoundColumn>
                            <asp:BoundColumn DataField="IsLockedOut" HeaderText="IsLockedOut"></asp:BoundColumn>
                            <asp:BoundColumn DataField="CreationDate" HeaderText="CreationDate"></asp:BoundColumn>
                            <asp:BoundColumn DataField="LastLoginDate" HeaderText="LastLoginDate"></asp:BoundColumn>
                            <asp:BoundColumn DataField="LastActivityDate" HeaderText="LastActivityDate"></asp:BoundColumn>
                            <asp:BoundColumn DataField="LastPasswordChangedDate" HeaderText="LastPasswordChangedDate"></asp:BoundColumn>
                            <asp:BoundColumn DataField="IsOnline" HeaderText="IsOnline"></asp:BoundColumn>
                        </Columns>
                    </asp:DataGrid>

                    <asp:Panel id="NavigationPanel" Visible="false" runat="server">
                        <table border="0">
                            <tr>
                            <td style="width:100px;">Page <asp:Label id="CurrentPageLabel" runat="server" />
                                of <asp:Label id="TotalPagesLabel" runat="server" /></td>
                            <td style="width:60px;"><asp:LinkButton id="PreviousButton" Text="< Prev"
                                                OnClick="PreviousButton_OnClick" runat="server" /></td>
                            <td style="width:60px;"><asp:LinkButton id="NextButton" Text="Next >"
                                                OnClick="NextButton_OnClick" runat="server" /></td>
                            </tr>
                        </table>
                    </asp:Panel>

                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>
        
</asp:Content>
