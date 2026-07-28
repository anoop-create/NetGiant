using Microsoft.VisualBasic.FileIO;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class SharedFunctions
    {
        public static string GetConfigurationSetting(string settingName)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.settingName.ToLower() == settingName.ToLower()).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static string GetConfigurationSetting(string sectionName, string settingName)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower()).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static string GetConfigurationSetting(string sectionName, string settingName, int websiteFK)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower() &&
                        x.websiteFK == websiteFK).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static List<configurationSetting> GetConfigurationSettingList(string sectionName)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.configurationSetting.Where(x => x.sectionName == sectionName).ToList();
            }
        }

        public static DataTable ReadTextFile(string filePath)
        {
            DataTable dt = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(filePath, Encoding.GetEncoding("ISO-8859-1")))
                using (StreamReader reader = new StreamReader(filePath))
                {
                    csvReader.SetDelimiters(new string[] { FtpUtilities.DetectDelimiter(reader, File.ReadAllLines(filePath).Count()).ToString() });
                    csvReader.TrimWhiteSpace = true;

                    //column headers
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        dt.Columns.Add(datecolumn);
                    }

                    //column data
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();

                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }

                        dt.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error reading Text File: " + ex.Message, ex.InnerException);
            }

            return dt;
        }

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public static string CleanupProductURL(string url, int? websiteID = null)
        {
            string newUrl = Regex.Replace(url, @"[\,\(\)\[\]']", "");
            newUrl = Regex.Replace(newUrl, @"[\/\s\+\.]", "-");
            newUrl = Regex.Replace(newUrl, @"\-+", "-");

            if (websiteID != null && websiteID > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    var site = db.Website.Find(websiteID);
                    newUrl = "http://" + site.WebURL + "/product/" + newUrl + "/";
                }
            }
            else
            {
                newUrl = "/product/" + newUrl + "/";
            }

            return newUrl;
        }

        public static string DoReplacements(string originalString, Dictionary<string, string> replacements)
        {
            foreach (var replacement in replacements)
            {
                originalString = originalString.Replace(replacement.Key, replacement.Value);
            }

            return originalString;
        }
    }

    //Public Shared Classes
    public class SaveReturn
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string InnerException { get; set; }
        public string EntityValidationError { get; set; }
        public dynamic ReturnData { get; set; }
    }
}
