using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class IisLogViewModel : CommonViewModel
    {
        public DataSet LogAnalysis { get; set; }

        public IisLogViewModel GetLogAnalysis(int websiteId, DateTime date)
        {
            // Identify and copy files
            string dir = "";
            string ofile1 = "TG-temp";
            string ofile2 = "TG-temp";
            string ifile1 = "";
            string ifile2 = "";
            string pfx = "xxx";                                                     // Dev file pfx
            string ipath1 = @"\\SERVER-DEVAPP\C$\inetpub\logs\LogFiles\W3SVC14\";   // Dev path
            string ipath2 = @"\\SERVER-DEVAPP\C$\inetpub\logs\LogFiles\W3SVC14\";   // Dev path
            string opath  = @"\\SERVER-DEVSQL\E$\PMS\TempLogs\";                    // Dev path

            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                switch (websiteId)
                {
                    case 1:
                    {
                        dir = @"WWW.TONERGIANT.CO.UK (NEW)\";
                        ofile1 = "TG-temp1";
                        ofile2 = "TG-temp2";
                        pfx = "tonergiant_D";
                        break;
                    }
                    case 2:
                    {
                        dir = @"WWW.CARTRIDGEMONKEY.COM (NEW)\";
                        ofile1 = "CM-temp1";
                        ofile2 = "CM-temp2";
                        pfx = "cartridgemonkey_D";
                        break;
                    }
                    case 3:
                    {
                        dir = @"WWW.NETGIANT.COM (NEW)\";
                        ofile1 = "NG-temp1";
                        ofile2 = "NG-temp2";
                        pfx = "netgiant_D";
                        break;
                    }
                }
                pfx = pfx + date.ToString("yyyyMMdd") + "-";
                ipath1 = @"\\NETG-WEB-01\D$\inetpub\logs\AdvancedLogs\";
                ipath2 = @"\\NETG-WEB-02\D$\inetpub\logs\AdvancedLogs\";
                opath = @"\\NETG-SQL-01\D$\PMS\TempLogs\";
            }

            DirectoryInfo di = new DirectoryInfo(ipath1 + dir);
            foreach (FileInfo fi in di.GetFiles("*.log"))
            {
                if (fi.Name.StartsWith(pfx))
                {
                    ifile1 = fi.Name;
                    break;
                }
            }
            di = new DirectoryInfo(ipath2 + dir);
            foreach (FileInfo fi in di.GetFiles("*.log"))
            {
                if (fi.Name.StartsWith(pfx))
                {
                    ifile2 = fi.Name;
                    break;
                }
            }

            if (ifile1 != "")
            {
                File.Copy(ifile1, ofile1, true);
            }
            if (ifile2 != "")
            {
                File.Copy(ifile2, ofile2, true);
            }

            // Retrieve Log data
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@file1", SqlDbType.VarChar);
            sqlParm.Value = ofile1;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@file2", SqlDbType.VarChar);
            sqlParm.Value = ofile2;
            sqlParms.Add(sqlParm);
            LogAnalysis = SQLUtilities.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.AnalyseLogsForBots", sqlParms, "botdata");

            return this;
        }
    }
}
