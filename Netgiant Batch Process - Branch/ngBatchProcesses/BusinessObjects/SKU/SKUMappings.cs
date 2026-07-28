using Microsoft.VisualBasic.FileIO;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.SKU
{
    /// <summary>
    /// Bulk insert into sku mappings
    /// </summary>
    public class SKUMappings
    {
        public SKUMappings()
        {
            hasErrorOccured = false;
        }

        public static bool hasErrorOccured;

        public static void CreateSKUMappings()
        {
            StandardFunctions stnFunc = new StandardFunctions();
            Properties.Settings settings = Properties.Settings.Default;
            string csvPath = settings.LocalDirectory + "SKUMappings\\skuMappings.csv";
            string csvArchivePath = settings.LocalDirectory + "SKUMappings\\Archive\\";
            
            stnFunc.AddToActivityLog("Started Batch Program with switch: createskumappings" + System.Environment.NewLine);

            try
            {
                // create bulk sku mappings
                SKUMappingServices.CreateSKUMappings(csvPath);

                // archive file
                stnFunc.CopyFileAndDelete(csvPath, csvArchivePath);

                stnFunc.AddToActivityLog("Executed stored procedure to create bulk sku mappings");
            }

            catch
            {
                stnFunc.AddToActivityLog("***Error*** Unable to create bulk sku mappings from the csv file :" + csvPath);
                hasErrorOccured = true;
            }

            stnFunc.AddToActivityLog("Finished Batch Program switch: createskumappings");
            string acitivityLogFileName = stnFunc.LogActivity();

            if (hasErrorOccured)
                stnFunc.SendSimpleEmail("Bulk Insert SKU Mappings", acitivityLogFileName);

            stnFunc = null;
        }
    }
}
