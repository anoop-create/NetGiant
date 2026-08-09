using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using EntityState = System.Data.Entity.EntityState;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class ImportCrossSellingLinkViewModel : JobStatusCommonViewModel
    {
        public string FilePath { get; set; }

        public void Import(string filePath)
        {
            DataTable dt = SharedFunctions.ReadTextFile(filePath);
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection cols = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in CrossSellingLinksAcceptedFields.Fields)
                {
                    if (cols.Contains(col))
                    {
                        mappedColumns.Add(colIndex, col);
                        colIndex++;
                    }
                }

                if (mappedColumns.Count == 0)
                {
                    throw new Exception("The columns titles are not correct for this import type.");
                }

                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return csvData;
        }

        private void ProcessRows(DataTable dt)
        {
            int currentRow = 1;
            var crossSellingLinkList = new List<CrossSellingLinkImportFields>();

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    if (DataTableColExists(dr, "Part No A") 
                        && DataTableColExists(dr, "Part No B")
                        && DataTableColExists(dr, "Type")
                        && DataTableColExists(dr, "Two Way Link"))
                    {
                        var fields = new CrossSellingLinkImportFields();
                        fields.PartNoA = Convert.ToString(dr["Part No A"]);
                        fields.PartNoB = Convert.ToString(dr["Part No B"]);
                        fields.Type = Convert.ToString(dr["Type"]);
                        fields.TwoWayLink = Convert.ToString(dr["Two Way Link"]).ToLower() == "y" ? true : false;
                        crossSellingLinkList.Add(fields);
                    }
                    currentRow++;
                }
                catch (Exception ex)
                {
                    var message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message);
                    WriteJobStatusRecord("Cross Selling Links - Working", message, SavingErrorType.Validation);
                }
            }

            dt = null;
            SaveRecords(crossSellingLinkList);
        }

        private void SaveRecords(List<CrossSellingLinkImportFields> crossSellingLinkList)
        {
            WriteJobStatusRecord("Cross Selling Links - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Save(crossSellingLinkList);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Cross Selling Links - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Cross Selling Links - Complete", "Successfully Saved Cross Selling Links", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void Save(List<CrossSellingLinkImportFields> crossSellingLinkList)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    for (int i = 0; i < crossSellingLinkList.Count; i++)
                    {
                        bool twoway = crossSellingLinkList[i].TwoWayLink;
                        var partnoA = crossSellingLinkList[i].PartNoA;
                        var partnoB = crossSellingLinkList[i].PartNoB;
                        var type = crossSellingLinkList[i].Type;

                        var cslink = new crossSellingLink
                        {
                            aProductFK = db.product.Where(w => w.partNo == partnoA)
                                .Select(x => x.productID)
                                .FirstOrDefault(),
                            bProductFK = db.product.Where(w => w.partNo == partnoB)
                                .Select(x => x.productID)
                                .FirstOrDefault(),
                            crossSellingLinkTypeFK = db.Lookup.Where(w => w.LookupType.LookupTypeName == "CrossSellingLinkType" && w.LookupName == type)
                                .Select(x => (byte)x.AltLookupId)
                                .FirstOrDefault()
                        };

                        SaveCrossSellLink(db, cslink);

                        if (twoway)
                        {
                            var twowaylink = new crossSellingLink
                            {
                                aProductFK = cslink.bProductFK,
                                bProductFK = cslink.aProductFK,
                                crossSellingLinkTypeFK = cslink.crossSellingLinkTypeFK
                            };

                            SaveCrossSellLink(db, twowaylink);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorString = "Could not save Cross Selling Links";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Cross Selling Links - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Cross Selling Links - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void SaveCrossSellLink(ngmdEntities db, crossSellingLink cslink)
        {
            var exists = db.crossSellingLink.Where(w => w.aProductFK == cslink.aProductFK && w.bProductFK == cslink.bProductFK).FirstOrDefault();

            if (exists == null)
            {
                db.Entry(cslink).State = EntityState.Added;
            }
            else
            {
                exists.aProductFK = cslink.aProductFK;
                exists.bProductFK = cslink.bProductFK;
                exists.crossSellingLinkTypeFK = cslink.crossSellingLinkTypeFK;
                db.Entry(exists).State = EntityState.Modified;
            }

            db.SaveChanges();
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }
    }
}
