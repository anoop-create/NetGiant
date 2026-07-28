<%@ Page Title="Q&A" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="NGS.UI.WebPages.QuestionAnswersUI.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContentPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="UserContentPlaceHolder" runat="server">
    
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="SideBarMenuPlaceHolder" runat="server">
    <div class="sideBar_Menu gl_textType2b">
        <ul>
            <li>
                <span>
                    <asp:LinkButton ID="lnkSearch" runat="server" Text="Search" OnClick="lnkSearch_Click" />
                </span>
            </li>
            <li>
                <a href="#">
                    <span><asp:LinkButton ID="lnkUnAnsQuestions" runat="server" Text="Unanswered Questions" OnClick="lnkUnAnsQuestions_Click" /></span>
                </a>
            </li>
            <li>
                <a href="#">
                    <span><asp:LinkButton ID="lnkAllQuestions" runat="server" Text="All Questions" OnClick="lnkAllQuestions_Click" /></span>
                </a>
            </li>
            <li>
                <a href="#">
                    <span><asp:LinkButton ID="lnkNewQA" runat="server" Text="Add Question" OnClick="lnkNewQA_Click" /></span>
                </a>
            </li>
        </ul>
    </div>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="MainContentPlaceHolder" runat="server">
    <script type="text/javascript" >

        $(document).ready(function () {
            $(".showProgress").hide();
        });

        function ShowProgress() {
            setTimeout(function () {
                if (Page_IsValid) {

                    $('.Overlay').hide();
                    $('.PopUpPanel').hide();
                    var modal = $('<div class="progressModal" />');
                    modal.addClass("modal");
                    $('body').append(modal);
                    var loading = $(".showProgress");
                    loading.show();
                    ChangeProgress();
                    HideProgress();
                }
            }, 100);
        }

        function ChangeProgress() {
            setTimeout(function () {
                $('.showProgress span').html("Record Saved");
            }, 1000);
        }

        function HideProgress() {
            setTimeout(function () {
                $(".showProgress, .progressModal").hide();
            }, 2000);
        }

        $(document).on("click", "#ctl00_MainContentPlaceHolder_btnSave", function () {
            ShowProgress();
        });

        $(document).on("click", "#ctl00_MainContentPlaceHolder_btnSave_AddQuestion", function () {
            ShowProgress();
        });

    </script>
    <div class="qa_placeHolder">
        <div id="divHeading" runat="server" class="qa_placeHolderHeading gl_textType2b"></div>
        <div class="qa_placeHolderBody">
            <ajax:UpdatePanel ID="upnlQAs" runat="server" ChildrenAsTriggers="true" UpdateMode="Always">
                <ContentTemplate>
                            
                    <!-- QuesionAnswer Details -->
                    <asp:Panel ID="pnlQuestions" runat="server">
                        <asp:GridView ID="gvQuestion" runat="server" AutoGenerateColumns="False" AllowPaging="True" DataKeyNames="QuestionAnswerID"
                            CssClass="gl_TabularContainer gl_textType1a" OnRowDataBound="gvQuestion_OnRowDataBound" OnRowCommand="gvQuestion_RowCommand"
                            HeaderStyle-CssClass="gl_border1g gl_textType1b" PagerSettings-Mode="NumericFirstLast" PageSize="15">
                            <Columns>
                                <asp:BoundField DataField="Question" HeaderText="Questions" HeaderStyle-CssClass="gl_LargeCell" ItemStyle-CssClass="gl_LargeCell gl_border1g" />
                                <asp:BoundField DataField="Answer" HeaderText="Answers" HeaderStyle-CssClass="gl_LargeCell gl_hide" ItemStyle-CssClass="gl_LargeCell gl_border1g gl_hide" />
                                <asp:BoundField DataField="Email" HeaderText="Customer Email" ItemStyle-CssClass="gl_LargeCell gl_border1g gl_hide" HeaderStyle-CssClass="gl_LargeCell gl_hide" />
                                <asp:TemplateField HeaderText="AltRef" ItemStyle-CssClass="gl_MediumCell gl_border1g" HeaderStyle-CssClass="gl_MediumCell">
                                    <ItemTemplate>
                                        <asp:Literal ID="ltrAltRef" runat="server" Visible="false"></asp:Literal>
                                        <asp:HyperLink ID="hlAltRef" runat="server" Visible ="false"></asp:HyperLink>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <%--<asp:BoundField DataField="AltRef" HeaderText="AltRef" ItemStyle-CssClass="gl_MediumCell gl_border1g" HeaderStyle-CssClass="gl_MediumCell" />--%>
                                <asp:BoundField DataField="AskedDate" HeaderText="Asked Date" ItemStyle-CssClass="gl_SmallCell gl_border1g" HeaderStyle-CssClass="gl_MediumCell" ItemStyle-HorizontalAlign="Center" />
                                <asp:BoundField DataField="RepliedDate" HeaderText="Replied Date" ItemStyle-CssClass="gl_MediumCell gl_border1g" HeaderStyle-CssClass="gl_MediumCell" ItemStyle-HorizontalAlign="Center" />
                                <asp:TemplateField HeaderText="Granularity" ItemStyle-CssClass="gl_LargeCell gl_border1g" HeaderStyle-CssClass="gl_LargeCell">
                                    <ItemTemplate>
                                        <asp:Literal ID="ltrGranuality" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Replied By" ItemStyle-CssClass="gl_MediumCell gl_border1g" HeaderStyle-CssClass="gl_MediumCell">
                                    <ItemTemplate>
                                        <asp:Literal ID="ltrUser" runat="server" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Source" ItemStyle-CssClass="gl_SmallCell gl_border1g gl_alignCenter" HeaderStyle-CssClass="gl_SmallCell">
                                    <ItemTemplate>
                                        <asp:Image ID="imgSource" runat="server" ImageUrl="~/Contents/images/1pxTrans.png" Width="26" Height="26" />
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-CssClass="gl_SmallCell gl_border1g gl_alignCenter">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbEdit" runat="server" CommandName="EditQuestionAnswer" CommandArgument='<%# DataBinder.Eval(Container, "RowIndex") %>'>
                                            <asp:Image ID="imgEdit" runat="server" ImageUrl="~/Contents/images/1pxTrans.png" Width="16" ToolTip="Edit" CssClass="EditImage" />
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField ItemStyle-CssClass="gl_SmallCell gl_border1g gl_alignCenter">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbDelete" runat="server" CommandName="DeleteQuestionAnswer" CommandArgument='<%# DataBinder.Eval(Container, "RowIndex") %>'>
                                            <asp:Image ID="imgDelete" runat="server" ImageUrl="~/Contents/images/1pxTrans.png" Width="16" ToolTip="Delete" CssClass="DeleteImage" />
                                        </asp:LinkButton>
                                        <ajaxToolKit:ConfirmButtonExtender ID="cbeDelete" runat="server" ConfirmText="Are you sure you want to delete this question?"
                                            TargetControlID="lbDelete">
                                        </ajaxToolKit:ConfirmButtonExtender>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                            <PagerStyle CssClass="paging" HorizontalAlign="Center"/>
                        </asp:GridView>
                        <asp:ObjectDataSource ID="qaSummarySrc" runat="server" TypeName="NGS.UI.WebPages.Models.QASummaryModel"
                            StartRowIndexParameterName="startRow" MaximumRowsParameterName="pageSize" EnablePaging="True" SelectCountMethod="GetRowCount"
                            SelectMethod="GetQASummary">
                        </asp:ObjectDataSource>
                        <asp:ObjectDataSource ID="qaUnAnsSummarySrc" runat="server" TypeName="NGS.UI.WebPages.Models.QASummaryModel"
                            StartRowIndexParameterName="startRow" MaximumRowsParameterName="pageSize"  EnablePaging="true" SelectCountMethod="GetUnAnsweredQACount"
                            SelectMethod="GetUnAnsweredQASummary">
                        </asp:ObjectDataSource>
                        <asp:ObjectDataSource ID="qaFilteredSummarySrc" runat="server" TypeName="NGS.UI.WebPages.Models.QASummaryModel"
                            StartRowIndexParameterName="startRow" MaximumRowsParameterName="pageSize"  EnablePaging="true" SelectCountMethod="GetFilteredQACount"
                            SelectMethod="GetFilteredQASummary" OnSelecting="qaFilteredSummarySrc_Selecting">
                        </asp:ObjectDataSource>
                    </asp:Panel>
                    <asp:Panel ID="pnlNoQuestions" runat="server" CssClass="" Visible="false">
                        <label id="lblNoQuestions" runat="server" />
                    </asp:Panel>

                </ContentTemplate>
            </ajax:UpdatePanel>

            <!--Edit Popup -->
            <asp:updatepanel id="upPopUps" runat="server" updatemode="Always">
                <contenttemplate>
                    <asp:panel id="panelOverlay" runat="server" CssClass="Overlay" visible="false">
                        <asp:panel id="panelPopUpPanel" runat="server" CssClass="PopUpPanel" visible="false">
                            <div class="PopupTitleBar">
                                <div class="PopupTitle gl_textType2b">
                                    Edit Question & Answer
                                </div>
                                <div class="PopupTitleClose">
                                    <asp:ImageButton ID="imgClosePopup" runat="server" OnClick="imgClosePopup_Click" CssClass="PopupTitleCloseImage"
                                        ImageUrl="~/Contents/images/1pxTrans.png" ToolTip="Close" />
                                </div>
                            </div>
                            <div class="PopupContents">
                                <table class="PopupTable">
                                    <tr>
                                        <th>Question:</th>
                                        <td>
                                            <asp:TextBox ID="txtQuestion" runat="server" TextMode="MultiLine" Rows="5" Width="250" />
                                            <asp:RequiredFieldValidator ID="rfvQuestion_Edit" runat="server" ControlToValidate="txtQuestion" Display="None" 
                                                ErrorMessage="Question is required!" SetFocusOnError="true" Width="230px" ValidationGroup="EditQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceQuestion_Edit" runat="server" TargetControlID="rfvQuestion_Edit" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Answer:</th>
                                        <td>
                                            <asp:TextBox ID="txtAnswer" runat="server" TextMode="MultiLine" Rows="5" Width="250" />
                                            <asp:RequiredFieldValidator ID="rfvAnswer_Edit" runat="server" ControlToValidate="txtAnswer" Display="None" 
                                                ErrorMessage="Answer is required!" SetFocusOnError="true" Width="230px" ValidationGroup="EditQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceAnswer_Edit" runat="server" TargetControlID="rfvAnswer_Edit" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Email:</th>
                                        <td>
                                            <asp:TextBox ID="txtEmail" runat="server" Width="250" />
                                            <asp:RegularExpressionValidator ID="rfvEmail_Edit" runat="server" ErrorMessage="Invalid Email!" ControlToValidate="txtEmail"
                                                ValidationExpression="^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$" ValidationGroup="EditQuestion" Display="None" SetFocusOnError="true">
                                            </asp:RegularExpressionValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceEmail_Edit" runat="server" TargetControlID="rfvEmail_Edit" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Granularity:</th>
                                        <td>
                                            <asp:DropDownList ID="ddlGranularity" runat="server" DataValueField="Key" DataTextField="Value" Width="255" AutoPostBack="true"
                                                OnSelectedIndexChanged="ddlGranularity_SelectedIndexChanged" />
                                            <asp:RequiredFieldValidator ID="rfvGranularity_Edit" runat="server" ControlToValidate="ddlGranularity" Display="None" 
                                                ErrorMessage="Granularity is required!" SetFocusOnError="true" Width="230px" ValidationGroup="EditQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceGranularity_Edit" runat="server" TargetControlID="rfvGranularity_Edit" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Alt Reference:</th>
                                        <td>
                                            <asp:TextBox ID="txtAltRef" runat="server" Width="250" />
                                            <asp:RequiredFieldValidator ID="rfvAltRef_Edit" runat="server" ControlToValidate="txtAltRef" Display="None" 
                                                ErrorMessage="Alt Ref is required or select granularity as All Products." SetFocusOnError="true" Width="230px" ValidationGroup="EditQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceAltRef_Edit" runat="server" TargetControlID="rfvAltRef_Edit" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Show On All Websites?</th>
                                        <td>
                                            <asp:CheckBox ID="cbShowOnAll_Edit" runat="server" Checked="true" OnCheckedChanged="cbShowOnAll_Edit_CheckedChanged" AutoPostBack="true" />
                                            <span style="color:#FFFFFF;">
                                                Source Website:
                                                <asp:Literal ID="ltrSourceWebsiteURL" runat="server"></asp:Literal>
                                            </span>
                                        </td>
                                    </tr>
                                    <tr id="trSelectWebsites_Edit" runat="server" visible="false">
                                        <th>Select Websites:</th>
                                        <td>
                                            <asp:CheckBoxList ID="cblSelectWebsites_Edit" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" ForeColor="White">
                                                <asp:ListItem Text="CM" Value="1" />
                                                <asp:ListItem Text="NG" Value="2" />
                                                <asp:ListItem Text="TG" Value="3" />
                                            </asp:CheckBoxList>
                                        </td>
                                    </tr>
                                    <tr>
                                        <th></th>
                                        <td>
                                            <asp:Button ID="btnSave" runat="server" CssClass="qa_submitButton" Text="Save" ToolTip="Save Changes" OnClick="btnSave_Click" ValidationGroup="EditQuestion" />
                                            <asp:Button ID="btnCancel" runat="server" CssClass="qa_submitButton" Text="Cancel" ToolTip="Cancel Changes" OnClick="btnCancel_Click" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </asp:panel>
                    </asp:panel>
                    <ajaxToolKit:DragPanelExtender ID="dpeEditQuestion" runat="server" TargetControlID="panelOverlay" />
                </contenttemplate>
            </asp:updatepanel>

            <!-- Search Popup -->
            <asp:updatepanel id="upPopupSearch" runat="server" updatemode="Always">
                <contenttemplate>
                    <asp:panel id="pnlOverlay" runat="server" CssClass="Overlay" visible="false">
                        <asp:panel id="pnlSearch" runat="server" CssClass="PopUpPanel" visible="false">
                            <div class="PopupTitleBar">
                                <div class="PopupTitle gl_textType2b">
                                    Search Question & Answer
                                </div>
                                <div class="PopupTitleClose">
                                    <asp:ImageButton ID="imgCloseSearch" runat="server" OnClick="imgCloseSearch_Click" CssClass="PopupTitleCloseImage"
                                        ImageUrl="~/Contents/images/1pxTrans.png" ToolTip="Close" />
                                </div>
                            </div>
                            <div class="PopupContents">
                                <table class="PopupTable">
                                    <tr>
                                        <th>By AltRef:</th>
                                        <td>
                                            <asp:TextBox ID="txtSearchAltRef" runat="server" Width="250" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>By Question:</th>
                                        <td>
                                            <asp:TextBox ID="txtSearchQuestion" runat="server" Width="250" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th></th>
                                        <td>
                                            <asp:Button ID="btnSearch" runat="server" CssClass="qa_submitButton" Text="Search" ToolTip="Search" OnClick="btnSearch_Click" ValidationGroup="searchQA" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </asp:panel>
                    </asp:panel>
                    <ajaxToolKit:DragPanelExtender ID="dpeSearch" runat="server" TargetControlID="pnlOverlay" />
                </contenttemplate>
            </asp:updatepanel>

            <!-- Add Popup -->
            <asp:updatepanel id="upPopup_AddQuestion" runat="server" updatemode="Always">
                <contenttemplate>
                    <asp:panel id="pnlOverlay_AddQuestion" runat="server" CssClass="Overlay" visible="false">
                        <asp:panel id="pnlPopup_AddQuestion" runat="server" CssClass="PopUpPanel" visible="false">
                            <div class="PopupTitleBar">
                                <div class="PopupTitle gl_textType2b">
                                    Add Question
                                </div>
                                <div class="PopupTitleClose">
                                    <asp:ImageButton ID="imgClose_AddQuestion" runat="server" OnClick="imgClose_AddQuestion_Click" CssClass="PopupTitleCloseImage"
                                        ImageUrl="~/Contents/images/1pxTrans.png" ToolTip="Close" />
                                </div>
                            </div>
                            <div class="PopupContents">
                                <table class="PopupTable">
                                    <tr>
                                        <th>Question:</th>
                                        <td>
                                            <asp:TextBox ID="txtQuestion_AddQuestion" runat="server" TextMode="MultiLine" Rows="5" Width="350" />
                                            <asp:RequiredFieldValidator ID="rfvQuestion_AddQuestion" runat="server" ControlToValidate="txtQuestion_AddQuestion" Display="None" 
                                                ErrorMessage="Question is required!" SetFocusOnError="true" Width="230px" ValidationGroup="AddQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceQuestion_AddQuestion" runat="server" TargetControlID="rfvQuestion_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Answer:</th>
                                        <td>
                                            <asp:TextBox ID="txtAnswer_AddQuestion" runat="server" TextMode="MultiLine" Rows="5" Width="350" />
                                            <asp:RequiredFieldValidator ID="rfvAnswer_AddQuestion" runat="server" ControlToValidate="txtAnswer_AddQuestion" Display="None" 
                                                ErrorMessage="Answer is required!" SetFocusOnError="true" Width="230px" ValidationGroup="AddQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceAnswer_AddQuestion" runat="server" TargetControlID="rfvAnswer_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Granularity:</th>
                                        <td>
                                            <asp:DropDownList ID="ddlGranuality_AddQuestion" runat="server" DataValueField="Key" DataTextField="Value" Width="350" AutoPostBack="true"
                                                OnSelectedIndexChanged="ddlGranuality_AddQuestion_SelectedIndexChanged"></asp:DropDownList>
                                            <asp:RequiredFieldValidator ID="rfvGranulaity_AddQuestion" runat="server" ControlToValidate="ddlGranuality_AddQuestion" Display="None"
                                                ErrorMessage="Please select the granularity!" SetFocusOnError="true" Width="230px" ValidationGroup="AddQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceGranuality_AddQuestion" runat="server" TargetControlID="rfvGranulaity_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>AltRef:</th>
                                        <td>
                                            <asp:TextBox ID="txtAltRef_AddQuestion" runat="server" Width="150" />
                                            <asp:RequiredFieldValidator ID="rfvAltRef_AddQuestion" runat="server" ControlToValidate="txtAltRef_AddQuestion" Display="None" 
                                                ErrorMessage="AltRef is required or select granularity as All Products." SetFocusOnError="true" Width="230px" ValidationGroup="AddQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceAltRef" runat="server" TargetControlID="rfvAltRef_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Customer's Email:</th>
                                        <td>
                                            <asp:TextBox ID="txtEmail_AddQuestion" runat="server" Width="350" />
                                            <asp:RegularExpressionValidator ID="revEmail_AddQuestion" runat="server" ErrorMessage="Invalid Email!" ControlToValidate="txtEmail_AddQuestion"
                                                ValidationExpression="^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$" ValidationGroup="AddQuestion" Display="None" SetFocusOnError="true">
                                            </asp:RegularExpressionValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceValidEmail_AddQuestion" runat="server" TargetControlID="revEmail_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Asked Date:</th>
                                        <td>
                                            <asp:Panel ID="pnlAskedDate_AddQuestion" runat="server" Width="175" Height="23" BackColor="#FFFFFF">
                                                <asp:TextBox ID="txtAskedDate_AddQuestion" runat="server" Width="150" BorderStyle="None" />
                                                <asp:ImageButton ID="imgCalendar_AddQuestion" runat="server" ImageUrl="~/Contents/images/1pxTrans.png" CssClass="qa_CalandarImage" 
                                                    Width="16" Height="16" ImageAlign="AbsMiddle" />
                                            </asp:Panel>
                                            <ajaxToolKit:CalendarExtender ID="ceCalendar_AddQuestion" runat="server" PopupButtonID="imgCalendar_AddQuestion" TargetControlID="txtAskedDate_AddQuestion" CssClass="cal_Theme1"
                                                Format="dd/MM/yyyy HH:mm:ss" />
                                            <%--<ajaxToolKit:MaskedEditExtender ID="meeAskedDate_AddQuestion" runat="server" Mask="99/99/9999" MaskType="DateTime" UserDateFormat="DayMonthYear" 
                                                TargetControlID="txtAskedDate_AddQuestion" />--%>
                                            <asp:RequiredFieldValidator ID="rfvAskedDate_AddQuestion" runat="server" ControlToValidate="txtAskedDate_AddQuestion" Display="None"
                                                ErrorMessage="Asked Date is required!" SetFocusOnError="true" Width="230px" ValidationGroup="AddQuestion"></asp:RequiredFieldValidator>
                                            <ajaxToolKit:ValidatorCalloutExtender ID="vceAskedDate_AddQuestion" runat="server" TargetControlID="rfvAskedDate_AddQuestion" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Show On All Websites?</th>
                                        <td>
                                            <asp:CheckBox ID="cbShowOnAll_AddQuestion" runat="server" Checked="true" OnCheckedChanged="cbShowOnAll_AddQuestion_CheckedChanged" AutoPostBack="true" />
                                        </td>
                                    </tr>
                                    <tr id="trSelectWebsites_AddQuestion" runat="server" visible="false">
                                        <th>Select Websites:</th>
                                        <td>
                                            <asp:CheckBoxList ID="cblSelectWebsites_AddQuestion" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" ForeColor="White">
                                                <asp:ListItem Text="CM" Value="1" Selected="True" />
                                                <asp:ListItem Text="NG" Value="2" Selected="True" />
                                                <asp:ListItem Text="TG" Value="3" Selected="True" />
                                            </asp:CheckBoxList>
                                        </td>
                                    </tr>
                                    <tr>
                                        <th></th>
                                        <td>
                                            <asp:Button ID="btnSave_AddQuestion" runat="server" Text="Save" CssClass="qa_submitButton" ToolTip="Save" OnClick="btnSave_AddQuestion_Click" ValidationGroup="AddQuestion" />
                                            <asp:Button ID="btnCancel_AddQuestion" runat="server" Text="Cancel" CssClass="qa_submitButton" ToolTip="Cancel" OnClick="btnCancel_AddQuestion_Click" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </asp:panel>
                    </asp:panel>
                    <ajaxToolKit:DragPanelExtender ID="dpeQuestion_AddQuestion" runat="server" TargetControlID="pnlOverlay_AddQuestion" />
                </contenttemplate>
            </asp:updatepanel>

            <!--Add Progress -->
            <div class="showProgress gl_textType1b">
                <span>Saving, Please wait...</span>
            </div>

        </div>
    </div>
</asp:Content>
