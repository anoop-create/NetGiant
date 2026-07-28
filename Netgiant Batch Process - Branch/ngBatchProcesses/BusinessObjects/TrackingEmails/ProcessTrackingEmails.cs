using ngBatchProcesses.BusinessObjects.Shared;
using ngBatchProcesses.BusinessObjects.TrackingEmails;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace ngBatchProcesses.BusinessObjects
{
    public class ProcessTrackingEmails
    {
        public static void Process()
        {
            try
            {
                FTP.GetFTPFiles();

                ProcessTrackingEmails p = new ProcessTrackingEmails();
                p.ProcessFiles();
            }
            catch (Exception ex)
            {
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string subject = "Delivery Tracking Information - **ERROR**";
                string message = "**Error** - " + ex.Message;
                string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                Email.SendEmail(toAddresses, from, subject, message, false);
            }
        }

        //Declare structs
        private struct EmailTemplatesStruct
        {
            public string TemplateName;
            public string TemplatePath;
            public string TemplateContent;
        }

        private struct FileAccessStruct
        {
            public bool Success;
            public string ErrorMessage;
            public StreamReader strRdr;
        }

        private struct CustomerDetailsStruct
        {
            public string Firstname;
            public string Surname;
            public string Email;
            public int? GroupCode;
            public string GroupDesc;
            public string CustRef;
            public string OrdNo;
            public string CustShortName;
        }

        private struct EmailTemplateMatchingStruct
        {
            public string TemplateName;
            public string FromEmail;
            public string TemplateSignature;
        }

        private struct FileColumnConfig
        {
            public string CustRefColumnName;
            public string TrackAddressColumnName;
            public string ItemDescColumnName;
            public string ItemQuantityColumnName;
            public int RequiredNumberOfFields;
        }

        private struct FileColumnPositions
        {
            public int CustRefColumnPosition;
            public int TrackAddressColumnPosition;
            public int ItemDescColumnPosition;
            public int ItemQuantityColumnPosition;
        }

        private readonly string adminEmailAddress = (string)Properties.Settings.Default["AdministratorEmail"];
        private FileColumnConfig fileColumnConfig = new FileColumnConfig();
        private FileColumnPositions fileColumnPositions = new FileColumnPositions();
        private List<EmailTemplatesStruct> EmailTemps = new List<EmailTemplatesStruct>();
        private List<string> ActivityLogArrayList = new List<string>();
        private readonly string activityLogFilePath = Properties.Settings.Default["ActivityLogPath"] +
                                             "\\ActivityLog_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss");

        private DataTable dtFileLines = new DataTable();
        private string supplierName = "";

        private void ProcessFiles()
        {

            LoadAvailableTemplates();

            //Get all files in file path directory and store in an array
            DirectoryInfo dirInfo = new DirectoryInfo((string)Properties.Settings.Default["FilePath"]);
            FileInfo[] filesArray = dirInfo.GetFiles().Where(file => (file.Attributes & FileAttributes.Hidden) == 0).ToArray(); ;

            //Get the supplier column spec and store in an array, once split on #
            string adventSettings = (string)Properties.Settings.Default["Suppliers"];
            string[] suppliersArray = adventSettings.Split('#');

            if (!filesArray.Any())
            {
                StandardFunctions.NoFilesInPickupDirectory(ref ActivityLogArrayList);
            }

            //Loop through each file in the file path directory
            foreach (FileInfo filePathInfo in filesArray)
            {
                bool isFileValid = StandardFunctions.CheckFileValid(filePathInfo.FullName);
                if (isFileValid == false)
                {
                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - Invalid file type in pickup directory, " + filePathInfo.FullName);
                    StandardFunctions.ArchiveFile(filePathInfo.FullName, ref ActivityLogArrayList, 2);
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(filePathInfo.FullName);
                int supplierConfigIndex = Array.FindIndex(suppliersArray, row => fileName.ToLower().Contains(row.Split('~')[0].ToLower()));

                if (supplierConfigIndex == -1)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    string subject = "Delivery Tracking Information - **ERROR**";
                    string message = "This file could not be matched to a known supplier config - " + filePathInfo.FullName;
                    string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                    Email.SendEmail(toAddresses, from, subject, message, false);
                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
                    StandardFunctions.ArchiveFile(filePathInfo.FullName, ref ActivityLogArrayList, 2);

                    continue;
                }

                string[] supplierConfig = suppliersArray[supplierConfigIndex].Split('~');
                supplierName = supplierConfig[0];

                fileColumnConfig.CustRefColumnName = supplierConfig[1];
                fileColumnConfig.TrackAddressColumnName = supplierConfig[2];
                fileColumnConfig.ItemDescColumnName = supplierConfig[3];
                fileColumnConfig.ItemQuantityColumnName = supplierConfig[4];
                fileColumnConfig.RequiredNumberOfFields = Convert.ToInt32(supplierConfig[5]);

                //Check that the file can be accessed
                FileAccessStruct fileAccessStruct = OpenSuppliedFile(filePathInfo.FullName);
                if (fileAccessStruct.Success)
                {
                    ProcessAccessedFile(fileAccessStruct, filePathInfo.FullName);
                    StandardFunctions.ArchiveFile(filePathInfo.FullName, ref ActivityLogArrayList, 1);
                }
                else
                {
                    ErrorAccessingFile(fileAccessStruct, filePathInfo.FullName);
                }

            }
            StandardFunctions.LogActivity(ref ActivityLogArrayList, activityLogFilePath);
            StandardFunctions stdFunc = new StandardFunctions();
            stdFunc.CleanupArchiveLocation((string)Properties.Settings.Default["ArchivedFilePath"]);
            stdFunc.CleanupActivityLogLocation((string)Properties.Settings.Default["ActivityLogPath"]);
        }

        private FileAccessStruct OpenSuppliedFile(string filePath)
        {
            FileAccessStruct fileAccessStruct = new FileAccessStruct();
            try
            {
                fileAccessStruct.strRdr = new StreamReader(filePath);
                fileAccessStruct.Success = true;
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "Successfully accessed file - " + filePath);
            }
            catch (IOException ex)
            {
                fileAccessStruct.Success = false;
                fileAccessStruct.ErrorMessage = ex.Message;
            }
            return fileAccessStruct;
        }

        private void ProcessAccessedFile(FileAccessStruct fileAccessStruct, string filePath)
        {
            string newline;
            string[] columnValuesCSV;
            char[] columnSeperator = new char[] { ',' };
            dtFileLines.Columns.Clear();
            dtFileLines.Rows.Clear();

            // Obtain the columns from the first line. 
            newline = fileAccessStruct.strRdr.ReadLine();
            columnValuesCSV = newline.Split(columnSeperator);

            //Initially set the column positions to -1, they will remain -1 if no match is found.
            fileColumnPositions.CustRefColumnPosition = -1;
            fileColumnPositions.ItemDescColumnPosition = -1;
            fileColumnPositions.ItemQuantityColumnPosition = -1;
            fileColumnPositions.TrackAddressColumnPosition = -1;

            int columnsMatched = 0;
            for (int x = 0; x <= columnValuesCSV.GetUpperBound(0); x++)
            {
                // Add the column to the datatable 
                dtFileLines.Columns.Add(StandardFunctions.FormatStringFromCSV(columnValuesCSV[x]));

                string currentValue = StandardFunctions.FormatStringFromCSV(columnValuesCSV[x].ToLower());

                if (currentValue == fileColumnConfig.CustRefColumnName.ToLower())
                {
                    fileColumnPositions.CustRefColumnPosition = x;
                    columnsMatched += 1;
                }
                else if (currentValue == fileColumnConfig.TrackAddressColumnName.ToLower())
                {
                    fileColumnPositions.TrackAddressColumnPosition = x;
                    columnsMatched += 1;
                }
                else if (currentValue == fileColumnConfig.ItemDescColumnName.ToLower())
                {
                    fileColumnPositions.ItemDescColumnPosition = x;
                    columnsMatched += 1;
                }
                else if (currentValue == fileColumnConfig.ItemQuantityColumnName.ToLower())
                {
                    fileColumnPositions.ItemQuantityColumnPosition = x;
                    columnsMatched += 1;
                }
            }

            while (!fileAccessStruct.strRdr.EndOfStream)
            {
                try
                {
                    // Split row of data into string array 
                    columnValuesCSV = fileAccessStruct.strRdr.ReadLine().Split(columnSeperator);

                    // add a new row with all of the values 
                    dtFileLines.Rows.Add(columnValuesCSV);
                }
                catch (Exception ex)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    //string subject = "Delivery Tracking Information - **ERROR**";
                    string body = "Error in this line of csv file - " + fileAccessStruct.strRdr.ReadLine() + " Error - " +
                                    ex.Message + " No email ent for this record. Moving to Next";
                    //string from = Properties.Settings.Default.DefaultEmailFromAddress;

                    //Email.SendEmail(toAddresses, from, subject, body, false);

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body);
                }
            }

            fileAccessStruct.strRdr.Close();

            if (fileColumnConfig.TrackAddressColumnName != "none")
            {
                try
                {
                    //Check whether the column names have been found correctly
                    StandardFunctions.CheckValidColumns(columnsMatched, fileColumnConfig.RequiredNumberOfFields);
                }
                catch (Exception ex)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    string subject = "Delivery Tracking Information - **ERROR**";
                    string body = "Could not find all columns in file " + filePath;
                    string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                    Email.SendEmail(toAddresses, from, subject, body + " - Detailed Error... " + ex.Message, true);

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body + " - Detailed Error... " + ex.Message);
                    return;
                }
                try
                {
                    ProcessLinesWithTrackingLink();
                }
                catch (Exception ex)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    string subject = "Delivery Tracking Information - **ERROR**";
                    string body = "Problem processing lines in file " + filePath;
                    string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                    Email.SendEmail(toAddresses, from, subject, body + " - Detailed Error... " + ex.Message, true);

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body + " - Detailed Error... " + ex.Message);
                    StandardFunctions.ArchiveFile(filePath, ref ActivityLogArrayList, 2);
                }
            }
            else
            {
                try
                {
                    //Check whether the column names have been found correctly
                    StandardFunctions.CheckValidColumns(columnsMatched, fileColumnConfig.RequiredNumberOfFields);
                }
                catch (Exception ex)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    string subject = "Delivery Tracking Information - **ERROR**";
                    string body = "Could not find all columns in file " + filePath;
                    string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                    Email.SendEmail(toAddresses, from, subject, body + " - Detailed Error... " + ex.Message, true);

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body + " - Detailed Error... " + ex.Message);
                    StandardFunctions.ArchiveFile(filePath, ref ActivityLogArrayList, 2);
                    return;
                }
                try
                {
                    ProcessLinesNoTrackingLink(filePath);
                }
                catch (Exception ex)
                {
                    List<string> toAddresses = new List<string>();
                    toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                    string subject = "Delivery Tracking Information - **ERROR**";
                    string body = "Problem processing lines in file " + filePath;
                    string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

                    Email.SendEmail(toAddresses, from, subject, body + " - Detailed Error... " + ex.Message, true);

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body + " - Detailed Error... " + ex.Message);
                    StandardFunctions.ArchiveFile(filePath, ref ActivityLogArrayList, 2);
                }
            }
        }

        private void ProcessLinesWithTrackingLink()
        {
            string lastCustRef = "";

            InitiateDataTableSorting();

            foreach (DataRow item in dtFileLines.Rows)
            {
                string currentCustRef = StandardFunctions.FormatStringFromCSV(item[fileColumnPositions.CustRefColumnPosition].ToString());
                string currentTrackLink = StandardFunctions.FormatStringFromCSV(item[fileColumnPositions.TrackAddressColumnPosition].ToString());

                if (supplierName.ToLower() == "advent")
                {
                    currentCustRef = SupplierSpecificFunctions.AdventCheckForOrderReplacements(currentCustRef, ref ActivityLogArrayList);
                    //Update the record in the datatable
                    item[fileColumnPositions.CustRefColumnPosition] = currentCustRef;
                }
                else if (supplierName.ToLower() == "ufp")
                {
                    currentCustRef = SupplierSpecificFunctions.UFPCheckForOrderReplacements(currentCustRef, ref ActivityLogArrayList);
                    //Update the record in the datatable
                    item[fileColumnPositions.CustRefColumnPosition] = currentCustRef;
                }

                if (lastCustRef != currentCustRef)
                {
                    lastCustRef = currentCustRef;

                    CustomerDetailsStruct custDetails = new CustomerDetailsStruct();
                    custDetails = GetCustomerInformation(currentCustRef);
                    string trackLink = item[fileColumnPositions.TrackAddressColumnPosition].ToString();
                    bool CustomerExcluded = CheckIfCustomerExcluded(custDetails);

                    if (custDetails.CustRef != null && trackLink != "" && CustomerExcluded == false)
                    {
                        SendTrackingEmail(custDetails.GroupCode, custDetails, currentTrackLink, currentCustRef);
                    }
                    else
                    {
                        StandardFunctions.EmailCriteriaProblem(custDetails.CustRef, custDetails.Email, trackLink,
                                                                CustomerExcluded, ref ActivityLogArrayList);
                    }
                }
            }
        }

        private void ProcessLinesNoTrackingLink(string filePath)
        {
            string lastCustRef = "";
            string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            string[] suppliersNoTrackingLinkArray = Convert.ToString(Properties.Settings.Default["SuppliersNoTrackingLink"]).Split('#');

            //Find the correct config for this filename
            //int supplierConfigIndex = Array.FindIndex(suppliersNoTrackingLinkArray, row => row.ToLower().Contains(fileName));
            int supplierConfigIndex = Array.FindIndex(suppliersNoTrackingLinkArray, row => fileName.ToLower().Contains(row.Split('~')[0].ToLower()));

            string[] supplierNoTrackingColumnsArray = suppliersNoTrackingLinkArray[supplierConfigIndex].Split('~');
            supplierName = supplierNoTrackingColumnsArray[0];
            string carrierName = supplierNoTrackingColumnsArray[1];
            string consignmentNo = supplierNoTrackingColumnsArray[2];

            InitiateDataTableSorting();

            foreach (DataRow item in dtFileLines.Rows)
            {
                string currentCustRef = StandardFunctions.FormatStringFromCSV(item[fileColumnPositions.CustRefColumnPosition].ToString());

                if (supplierName.ToLower() == "westcoast")
                {
                    currentCustRef = SupplierSpecificFunctions.WestcoastCheckForOrderReplacements(currentCustRef, ref ActivityLogArrayList);
                    //Update the record in the datatable
                    item[fileColumnPositions.CustRefColumnPosition] = currentCustRef;
                }
                else if (supplierName.ToLower() == "beta")
                {
                    currentCustRef = SupplierSpecificFunctions.BetaCheckForOrderReplacements(currentCustRef, ref ActivityLogArrayList);
                    //Update the record in the datatable
                    item[fileColumnPositions.CustRefColumnPosition] = currentCustRef;
                }
                else if (supplierName.ToLower() == "jettec")
                {
                    currentCustRef = SupplierSpecificFunctions.JettecCheckForOrderReplacements(currentCustRef, ref ActivityLogArrayList);
                    //Update the record in the datatable
                    item[fileColumnPositions.CustRefColumnPosition] = currentCustRef;
                }

                string currentConsignMentNo = StandardFunctions.FormatStringFromCSV(item[consignmentNo].ToString());
                if (lastCustRef != currentCustRef)
                {
                    lastCustRef = currentCustRef;

                    CustomerDetailsStruct custDetails = new CustomerDetailsStruct();
                    custDetails = GetCustomerInformation(currentCustRef);

                    string trackLink;

                    if (carrierName != "none")
                    {
                        trackLink = StandardFunctions.GenerateTrackingLink(item[carrierName].ToString(), currentConsignMentNo, ref ActivityLogArrayList);
                    }
                    else
                    {
                        trackLink = StandardFunctions.GenerateTrackingLinkNoCarrier(supplierName.ToLower(), currentConsignMentNo, ref ActivityLogArrayList);
                    }


                    bool CustomerExcluded = CheckIfCustomerExcluded(custDetails);

                    if (custDetails.CustRef != null && trackLink != "" && CustomerExcluded == false)
                    {
                        SendTrackingEmail(custDetails.GroupCode, custDetails, trackLink, currentCustRef);
                    }
                    else
                    {
                        StandardFunctions.EmailCriteriaProblem(custDetails.CustRef, custDetails.Email, trackLink,
                                                                CustomerExcluded, ref ActivityLogArrayList);
                    }
                }
            }
        }

        private void InitiateDataTableSorting()
        {
            //Pass the datatable to a new dataview to enable sorting based on column name
            DataView dv = new DataView(dtFileLines);
            dv.Sort = dv.Table.Columns[fileColumnPositions.CustRefColumnPosition].ColumnName + " ASC";
            dtFileLines = dv.ToTable();
            dv.Dispose();
        }

        private void LoadAvailableTemplates()
        {
            string[] tempArray = Directory.GetFiles((string)Properties.Settings.Default["EmailTemplatePath"]);
            for (int i = 0; i < tempArray.Count(); i++)
            {
                EmailTemplatesStruct emailTempItem = new EmailTemplatesStruct();
                emailTempItem.TemplateName = Path.GetFileNameWithoutExtension(tempArray[i]);
                emailTempItem.TemplatePath = tempArray[i];

                StreamReader strRdr = new StreamReader(tempArray[i]);
                emailTempItem.TemplateContent = strRdr.ReadToEnd();
                strRdr.Close();

                EmailTemps.Add(emailTempItem);
            }
        }

        private void ErrorAccessingFile(FileAccessStruct fileAccessStruct, string filePath)
        {
            List<string> toAddresses = new List<string>();
            toAddresses.Add(adminEmailAddress);

            string subject = "Delivery Tracking Information - **ERROR**";
            string body = "**File access ERROR**";
            string from = (string)Properties.Settings.Default["DefaultEmailFromAddress"];

            Email.SendEmail(toAddresses, from, subject, body + " - Detailed Error... " + fileAccessStruct.ErrorMessage, true);
            ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** - " + fileAccessStruct.ErrorMessage);
        }

        private CustomerDetailsStruct GetCustomerInformation(string custRef)
        {
            DataAccess GetCustInfo = new DataAccess();
            DataSet_ServerSBS.ng_GetCustomerInfoFromCustOrdRefDataTable dt1 = new DataSet_ServerSBS.ng_GetCustomerInfoFromCustOrdRefDataTable();
            CustomerDetailsStruct custDetails = new CustomerDetailsStruct();

            try
            {
                dt1 = GetCustInfo.GetCustomerInformation(custRef);
            }
            catch (Exception ex)
            {
                string body = "**ERROR**" + " DataAccess.GetCustomerInformation - " + ex.Message;

                var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "";

                body += $"{Environment.NewLine}{Environment.NewLine}" +
                              $"MESSAGE: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                              $"INNER EXCEPTION: {innerException}{Environment.NewLine}{Environment.NewLine}" +
                              $"STACK TRACE: {ex.StackTrace}{Environment.NewLine}";

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + body);
            }

            if (dt1.Rows.Count > 0)
            {
                //Setup the struct to return the customer details
                custDetails.Firstname = StandardFunctions.UppercaseFirst(dt1.Rows[0]["forename"].ToString().ToLower());
                custDetails.Surname = StandardFunctions.UppercaseFirst(dt1.Rows[0]["surname"].ToString().ToLower());
                custDetails.Email = StandardFunctions.UppercaseFirst(dt1.Rows[0]["email"].ToString().ToLower());
                custDetails.GroupCode = Convert.ToInt32(dt1.Rows[0]["custGroupCode"]);
                custDetails.GroupDesc = dt1.Rows[0]["custGroupDescription"].ToString();
                custDetails.OrdNo = dt1.Rows[0]["ordNo"].ToString();
                custDetails.CustShortName = dt1.Rows[0]["custShortName"].ToString();
                custDetails.CustRef = custRef;
            }
            else
            {
                custDetails.GroupCode = null;
                List<string> toAddresses = new List<string>();
                toAddresses.Add(adminEmailAddress);

                //string subject = "Delivery Tracking Information - **ERROR**";
                string body = "This customer order ref -[ " + custRef + "] couldn't be found in the sql database";
                //string from = Properties.Settings.Default.DefaultEmailFromAddress;

                //Email.SendEmail(toAddresses, from, subject, body, false);
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** - " + body);
            }

            GetCustInfo = null;
            dt1.Dispose();
            
            return custDetails;
        }

        private void SendTrackingEmail(int? customerGroup, CustomerDetailsStruct custDetails, string trackingAddress, string origCustRef)
        {
            try
            {
                EmailTemplateMatchingStruct emailTemplateMatchStruct = new EmailTemplateMatchingStruct();
                emailTemplateMatchStruct = GetTemplateDetails(customerGroup, custDetails, origCustRef);

                if (emailTemplateMatchStruct.TemplateName != null)
                {
                    List<string> toAddresses = new List<string>();

                    if (Convert.ToBoolean(Properties.Settings.Default["RunInDevMode"]) == false)
                    {
                        toAddresses.Add(custDetails.Email);
                    }
                    else
                    {
                        toAddresses.Add("richard.lee@netgiant.com");
                    }

                    string subject = "Delivery Tracking Information";
                    string body = ReplaceTemplateValues(emailTemplateMatchStruct, custDetails, trackingAddress);

                    Email.SendEmail(toAddresses, emailTemplateMatchStruct.FromEmail, subject, body, true, "", "richard.lee@netgiant.com");

                    ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "Email Sent to - " + custDetails.Email);
                }

            }
            catch (Exception ex)
            {
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** Email not sent to - " + custDetails.Email + "- " +
                                                                ex.Message + " Cust Ref=" + origCustRef);
            }
        }

        private EmailTemplateMatchingStruct GetTemplateDetails(int? customerGroup, CustomerDetailsStruct custDetails, string origCustRef)
        {
            EmailTemplateMatchingStruct emailTemplateMatchStruct = new EmailTemplateMatchingStruct();
            string templateMatchingString = (string)Properties.Settings.Default["EmailTemplateMatching"];
            String[][] templateMatchingArray = templateMatchingString.Split('#').Select(i => i.Split('~')).ToArray();
            int templateMatchingArrayIndex = -1;

            //Have to loop instead of array.findindex because it is a nested array.
            for (int i = 0; i <= templateMatchingArray.Length - 1; i++)
            {
                string[] templateGroupCodesArray = templateMatchingArray[i][1].Split(',');
                int[] templateGroupCodesArrayInt = Array.ConvertAll(templateGroupCodesArray, int.Parse);
                int templateGroupCodeIndex = Array.IndexOf(templateGroupCodesArrayInt, customerGroup);
                if (templateGroupCodeIndex != -1)
                {
                    templateMatchingArrayIndex = i;
                    break;
                }
            }

            if (templateMatchingArrayIndex != -1)
            {
                emailTemplateMatchStruct.TemplateName = templateMatchingArray[templateMatchingArrayIndex][0];
                emailTemplateMatchStruct.FromEmail = templateMatchingArray[templateMatchingArrayIndex][2];
                emailTemplateMatchStruct.TemplateSignature = templateMatchingArray[templateMatchingArrayIndex][3];
            }

            if (emailTemplateMatchStruct.TemplateName == null)
            {
                List<string> toAddresses = new List<string>();
                toAddresses.Add(adminEmailAddress);

                //string subject = "Delivery Tracking Information - **ERROR**";
                string body = "This customer order ref -[ " + origCustRef + "] could NOT be matched to a website template";
                //string from = Properties.Settings.Default.DefaultEmailFromAddress;

                //Email.SendEmail(toAddresses, from, subject, body, false);
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** - " + body);
            }
            return emailTemplateMatchStruct;
        }

        private string ReplaceTemplateValues(EmailTemplateMatchingStruct emailTemplateMatch, CustomerDetailsStruct custDetails, string trackingAddress)
        {
            string emailTemplateHTML = "";
            foreach (EmailTemplatesStruct item in EmailTemps)
            {
                if (item.TemplateName == emailTemplateMatch.TemplateName)
                {
                    emailTemplateHTML = item.TemplateContent;
                    emailTemplateHTML = emailTemplateHTML.Replace("[[ordno]]", custDetails.OrdNo);
                    emailTemplateHTML = emailTemplateHTML.Replace("[[firstname]]", custDetails.Firstname);
                    emailTemplateHTML = emailTemplateHTML.Replace("[[trackinglink]]", trackingAddress);
                    emailTemplateHTML = emailTemplateHTML.Replace("[[productsrows]]", GetProducts(custDetails, fileColumnConfig.CustRefColumnName));
                    emailTemplateHTML = emailTemplateHTML.Replace("[[signature]]", emailTemplateMatch.TemplateSignature);
                }
            }
            return emailTemplateHTML;
        }

        private string GetProducts(CustomerDetailsStruct custDetails, string custRefColumn)
        {
            StringBuilder sbProducts = new StringBuilder();

            if (fileColumnPositions.CustRefColumnPosition != -1 &&
                fileColumnPositions.ItemQuantityColumnPosition != -1 &&
                fileColumnPositions.ItemDescColumnPosition != -1)
            {

                sbProducts.AppendLine("<tr><td>The following items are included in this shipment;<br/><br/></td></tr><tr><td>");

                //Record the last iterated product description, to avoid duplicated product lines
                string lastProductDescription = "";

                foreach (DataRow row in dtFileLines.Rows)
                {
                    string rowCustRef = StandardFunctions.FormatStringFromCSV(row[fileColumnPositions.CustRefColumnPosition].ToString());
                    string rowProductQuantity = row[fileColumnPositions.ItemQuantityColumnPosition].ToString().Replace("\"", string.Empty);
                    string rowProductDescription = row[fileColumnPositions.ItemDescColumnPosition].ToString().Replace("\"", string.Empty);

                    if (rowCustRef == custDetails.CustRef && lastProductDescription.Trim().ToLower() != rowProductDescription.Trim().ToLower())
                    {
                        sbProducts.AppendLine("<p>");
                        sbProducts.AppendLine(rowProductQuantity);
                        sbProducts.AppendLine(" x ");
                        sbProducts.AppendLine(rowProductDescription);
                        sbProducts.AppendLine("</p>");
                        lastProductDescription = rowProductDescription;
                    }
                }
                sbProducts.AppendLine("<br/></td></tr>");
            }

            return sbProducts.ToString();
        }
        private bool CheckIfCustomerExcluded(CustomerDetailsStruct custDetails)
        {
            bool returnValue = false;

            try
            {
                string custShortName = custDetails.CustShortName.Trim().ToLower();
                string[] excludedCustomers = Convert.ToString(Properties.Settings.Default["ExcludedCustomers"]).ToLower().Split(',');

                if (Array.IndexOf(excludedCustomers, custShortName) != -1)
                {
                    returnValue = true;
                }
            }
            catch (Exception ex)
            {
                string body = "There was an problem checking if this customer name -[" + custDetails.CustShortName + "] is an excluded customer" +
                                ". Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** - " + body);
                return true;
            }

            return returnValue;
        }
    }
}
