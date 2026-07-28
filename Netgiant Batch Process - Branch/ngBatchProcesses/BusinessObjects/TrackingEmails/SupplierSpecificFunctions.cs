using System;
using System.Collections.Generic;

namespace ngBatchProcesses.BusinessObjects.TrackingEmails
{
    class SupplierSpecificFunctions
    {
        public static string WestcoastCheckForOrderReplacements(string s, ref List<string> ActivityLogArrayList)
        {
            //Westcoast - If the order is a replacement, symbol "/R", /RR or letter "A" is included at the end of the ref.
            //This should be removed before matching to the SQL db.
            string returnValue = "";
            try
            {
                string formatString = s.Trim().ToLower();

                if (formatString.Substring(formatString.Length - 2) == "/r")
                {
                    returnValue = formatString.Remove(formatString.Length - 2);
                }
                else if (formatString.Substring(formatString.Length - 1) == "a")
                {
                    returnValue = formatString.Remove(formatString.Length - 1);
                }
                else if (formatString.Substring(formatString.Length - 3) == "/rr")
                {
                    returnValue = formatString.Remove(formatString.Length - 3);
                }
                else
                {
                    returnValue = s;
                }
            }
            catch (Exception ex)
            {
                returnValue = s;
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string message = "There was an error in the function 'WestcoastCheckForOrderReplacements()', Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return returnValue;
        }
        public static string AdventCheckForOrderReplacements(string s, ref List<string> ActivityLogArrayList)
        {
            //Advent - If the order is a replacement, REPL is included at the end of the ref.
            //This should be removed before matching to the SQL db.
            string returnValue = "";
            try
            {
                string formatString = s.Trim().ToLower();

                if (formatString.Substring(formatString.Length - 4) == "repl")
                {
                    returnValue = formatString.Remove(formatString.Length - 4);
                }
                else if (formatString.Substring(formatString.Length - 1) == "r")
                {
                    returnValue = formatString.Remove(formatString.Length - 1);
                }
                else
                {
                    returnValue = s;
                }
            }
            catch (Exception ex)
            {
                returnValue = s;
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string message = "There was an error in the function 'AdventCheckForOrderReplacements()', Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return returnValue;
        }
        public static string BetaCheckForOrderReplacements(string s, ref List<string> ActivityLogArrayList)
        {
            //Beta - If the order is a replacement, REP- or REP is included at the start of the ref.
            //This should be removed before matching to the SQL db.
            string returnValue = "";
            try
            {
                string formatString = s.Trim().ToLower();

                if (formatString.Substring(0, 4) == "rep-")
                {
                    returnValue = formatString.Remove(0, 4);
                }
                else if (formatString.Substring(0, 3) == "rep")
                {
                    returnValue = formatString.Remove(0, 3);
                }
                else
                {
                    returnValue = s;
                }
            }
            catch (Exception ex)
            {
                returnValue = s;
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string message = "There was an error in the function 'BetaCheckForOrderReplacements()', Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return returnValue;
        }
        public static string JettecCheckForOrderReplacements(string s, ref List<string> ActivityLogArrayList)
        {
            //Jettec - If the order is a replacement, RMA included at the start of the ref.
            //This should be removed before matching to the SQL db.
            string returnValue = "";
            try
            {
                string formatString = s.Trim().ToLower();
                if (formatString.Substring(0, 3) == "rma")
                {
                    throw new Exception("Invalid Order Ref Found - " + formatString);
                }
                else
                {
                    returnValue = s;
                }
            }
            catch (Exception ex)
            {
                returnValue = s;
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string message = "There was an error in the function 'JettecCheckForOrderReplacements()', Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }
            return returnValue;
        }
        public static string UFPCheckForOrderReplacements(string s, ref List<string> ActivityLogArrayList)
        {
            //UFP - If the order is a replacement, REPL is included at the end of the ref.
            //This should be removed before matching to the SQL db.
            string returnValue = "";
            try
            {
                string formatString = s.Trim().ToLower();

                if (formatString.Substring(formatString.Length - 4) == "repl")
                {
                    returnValue = formatString.Remove(formatString.Length - 4);
                }
                else
                {
                    returnValue = s;
                }
            }
            catch (Exception ex)
            {
                returnValue = s;
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                string message = "There was an error in the function 'UFPCheckForOrderReplacements()', Detailed Error = " + ex.Message;

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return returnValue;
        }
    }
}
