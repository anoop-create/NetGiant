using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using NGBP.DataAccessLayer.SCOM.Services;
using System.Xml;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.AXIS
{
    public class AxisFeeds
    {
        static AxisFeeds()
        {
            errorOccured = false;
        }

        static StandardFunctions std = new StandardFunctions();
        static bool errorOccured;
        static List<KeyValuePair<int, int>> axisQueueIdentifiers = new List<KeyValuePair<int, int>>();
        delegate bool IsFullUpdate(DataRow row);

        /// <summary>
        /// Processes the Axis Queue in the PMS database.
        /// Generates a feed that is then passed to Axis Diplomat.
        /// </summary>
        /// <param name="parms">Parameters supplied via switches</param>
        public static void ProcessAxisQueue(Dictionary<string, string> parms)
        {
            std.AddToActivityLog("Started with switch - " + parms["type"] + ", output - " + parms["output"]);

            try
            {
                AXISQueueFeedServices axis = new AXISQueueFeedServices();
                DataTable data = axis.GetAXISQueueFeedData();
                WriteAxisXML(data, parms["output"], m => (m["CRUD"].ToString() == "C") || (m["CRUD"].ToString() == "U"
                                                            && m["fieldName"].ToString() != "price"));
                axis = null;

                std.AddToActivityLog("Sucessfully processed Axis Queue");

            }
            catch (Exception e)
            {
                std.AddToActivityLog("**Error Occured** - " + e.Message);
                errorOccured = true;
            }

            std.AddToActivityLog("Finished");
            string acitivityLogFileName = std.LogActivity();
            if (errorOccured == true && Properties.Settings.Default.Environment == "Live") 
            {
                std.SendSimpleEmail(parms["type"], acitivityLogFileName); 
            }
        }

        private static void WriteAxisXML(DataTable dt, string outputPath, IsFullUpdate isFull)
        {
            if (dt.Rows.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
                sb.Append(@"<products xmlns=""http://resources.intoscape.com/xml/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://resources.intoscape.com/xml/isxml.xsd"">");

                foreach (DataRow row in dt.Rows)
                {
                    //if (row["price"].ToString() != "")
                    //{

                    if (Convert.ToInt32(row["productStatusFK"]) == 1 && row["price"].ToString() == "")
                    {
                        continue;
                    }

                    if (!axisQueueIdentifiers.Exists(m => m.Key == Convert.ToInt32(row["AXISQueueID"])))
                    {

                        sb.Append(@"<product ");
                        sb.Append(PopulateMainXML(row, isFull));

                        if (isFull(row))
                        {
                            sb.Append(PopulateDataAttributesXML(row).ToString());
                        }

                        sb.Append(PopulateRelatedXML(row, isFull).ToString());
                        sb.Append(ExtractProviderData(row));
                        sb.Append(@"</product>");
                        axisQueueIdentifiers.Add(new KeyValuePair<int, int>(Convert.ToInt32(row["AXISQueueID"]), Convert.ToInt32(row["AXISQueueDetailsID"])));
                    }
                    //}
                }

                sb.Append("</products>");

                WriteFileToDisk(outputPath, sb);
                UpdateCompletedDate();
                ClearAxisQueueRecords();
                UploadToFTP(outputPath);
            }

        }

        private static void UploadToFTP(string outputPath)
        {
            Properties.Settings settings = Properties.Settings.Default;
            string ftpFileName = "XMLGateway_Products_" + DateTime.Now.ToString("ddMMyyyyhhmmss") + ".xml";

            if (settings.Environment == "Live")
                FtpUtilities.UploadFTPFile(outputPath, settings.ServerRDSFTPHostExternal + ftpFileName, settings.ServerRDSFTPUsername,
                                        settings.ServerRDSFTPPassword);
        }

        private static StringBuilder PopulateMainXML(DataRow row, IsFullUpdate isFull)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format(@"productId=""{0}"" ", row["productFK"]));
            sb.Append(string.Format(@"importType=""{0}"" ", isFull(row) == true ? "Full" : "Partial"));
            sb.Append(string.Format(@"mfpn1=""{0}"" ", row["partNo"]));
            sb.Append(string.Format(@"stock=""{0}"" ", row["quantity"].ToString() == "" ? "0" : row["quantity"]));
            sb.Append(string.Format(@"status=""{0}"" ", SetProductStatus(Convert.ToInt32(row["productStatusFK"]))));
            sb.Append(string.Format(@"price=""{0}"" ", row["price"].ToString() == "" ? "0.00" : row["price"]));
            sb.Append(string.Format(@"bsp=""{0}"" ", row["bestSuppPrice"].ToString() == "" ? "0.00" : row["bestSuppPrice"]));
            sb.Append(string.Format(@"ownProductId=""{0}"" ", row["partNo"]));
            sb.Append(string.Format(@"salesGroup=""{0}"" ", row["salesAreaGroupNo"]));
            sb.Append(string.Format(@"productGroup=""{0}"" ", row["productGroupNo"]));
            sb.Append(string.Format(@"infiniteStock=""{0}"" ", "N"));
            sb.Append(string.Format(@"mfpn2=""{0}"" ", ""));
            sb.Append(@">");

            if (isFull(row))
            {
                sb.Append(@"<productDetails " + string.Format(@"unspsc=""{0}"" ", row["UNSPSCCode"]));
                sb.Append(string.Format(@"upcean=""{0}"" ", ""));
                sb.Append(string.Format(@"manufacturer=""{0}"" ", row["manufacturerName"]));
                sb.Append(string.Format(@"manufacturerId=""{0}"" ", row["manufacturerFK"]));
                sb.Append(@"/>");
            }

            return sb;
        }

        private static string SetProductStatus(int prodStatusFK)
        {
            return prodStatusFK == 1 ? "Live on Web" : "Deactivated";
        }

        private static StringBuilder PopulateRelatedXML(DataRow row, IsFullUpdate isFull)
        {
            StringBuilder sb = new StringBuilder();

            if (isFull(row))
            {
                sb.Append(@"<description>" + RemoveInvalidXmlChars(row["productName"].ToString()) + "</description>");
                sb.Append(@"<shortDescription></shortDescription>");
                sb.Append(@"<marketingDescription />");
                sb.Append(@"<image />");
                sb.Append(@"<relatedItems />");
                sb.Append(@"</metaData>");
            }

            sb.Append(@"<Sites><Site siteId=""2"" /></Sites>");

            return sb;
        }

        private static StringBuilder PopulateDataAttributesXML(DataRow row)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"<metaData>");
            sb.Append(@"<metaDataId />");
            sb.Append(@"<mainSpecification>");

            if (row["CRUD"].ToString() == "C")
            {
                foreach (DataRow att in GetDataSupplierData(row).Rows)
                {
                    sb.Append(string.Format(@"<Item Section="""" Header=""{0}"" Body=""{1}"" DisplayOrder=""{2}"" />",
                                                att["attributeName"], att["attributeValue"], att["Row"]));
                }
            }

            sb.Append(@"</mainSpecification>");
            sb.Append(@"<extendedSpecification><Items /></extendedSpecification>");

            return sb;
        }

        private static StringBuilder ExtractProviderData(DataRow row)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<Suppliers>");

            string[] suppliersArray = row["suppliers"].ToString().Split(',');
            foreach (string sup in suppliersArray.Where(x => x != string.Empty))
            {
                string[] supDetails = sup.Split('#');
                string axisSuppplierRef = supDetails[0].ToString();
                string providerPartNo = supDetails[1].ToString();

                sb.Append(string.Format(@"<Supplier supplierId=""{0}"" partNo=""{1}"" />", axisSuppplierRef, providerPartNo));

            }

            sb.Append("</Suppliers>");

            return sb;
        }

        private static DataTable GetDataSupplierData(DataRow dr)
        {
            return AXISQueueFeedServices.GetDataSupplierAttributes(dr["partNo"].ToString(), dr["manufacturerName"].ToString(),
                                                                 dr["dataSupplierFK"].ToString());
        }

        private static void WriteFileToDisk(string outputPath, StringBuilder sb)
        {
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                sw.Write(sb);
                sw.Close();
            }
        }

        private static void UpdateCompletedDate()
        {
            try
            {
                foreach (var aq in axisQueueIdentifiers)
                {
                    AXISQueueFeedServices.UpdateAXISQueueCompletedDate(aq.Key);
                }

                std.AddToActivityLog("Successfully updated completed dates");
            }
            catch (Exception e)
            {
                std.AddToActivityLog("Error Updating Completed Date:");
                std.AddToActivityLog("Error Message: " + e.Message);
                std.AddToActivityLog("Stack Trace: " + e.StackTrace);
            }
        }

        private static void ClearAxisQueueRecords()
        {
            try
            {
                int daysOlderThan = 3;
                AXISQueueFeedServices.ClearAxisQueueRecords(daysOlderThan);
                std.AddToActivityLog("Successfully cleared Axis Queue Records");
            }
            catch (Exception e)
            {
                std.AddToActivityLog("Error Updating Completed Date:");
                std.AddToActivityLog("Error Message: " + e.Message);
                std.AddToActivityLog("Stack Trace: " + e.StackTrace);
            }
        }

        static string RemoveInvalidXmlChars(string text)
        {
            return text.Replace("&", "and");
        }
    }
}
