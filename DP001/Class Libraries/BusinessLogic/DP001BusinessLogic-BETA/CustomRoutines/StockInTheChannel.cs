using DP001BusinessLogic.Pricing;
using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DP001BusinessLogic.CustomRoutines
{
    public class StockInTheChannel
    {
        public static MemoryStream CreateInMemoryCsv(List<PriceRuleDetail> priceRules, Channel channel)
        {
            var memoryStream = new MemoryStream();

            try
            {
                CommonDataFunctions.CreateLogEntry(channel, "START Create in memory CSV", "Information");
                var ftp = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Additional Inventory").FirstOrDefault();
                if (ftp != null)
                {
                    var hostDetails = new Ftp.FtpHostDetails()
                    {
                        FtpHost = ftp.FTPServer,
                        FtpUser = ftp.FTPUser,
                        FtpPassword = ftp.FTPPassword,
                        FileName = ftp.FTPFileName,
                        SavePath = channel.TenantFK.ToString() + "\\" + ftp.FTPFileName,
                        Protocol = CommonFunctions.LookupFtpProtocol(ftp.FTPProtocolFK),
                        BlobContainer = "tenantfolders"
                    };

                    var result = Ftp.DownloadFTPFile(hostDetails);
                    var fileContent = CommonFunctions.ReadFileToString(result.Path, result.BlobContainer);

                    using (var writer = new Csv.CsvFileWriter(memoryStream, ','))
                    using (var reader = new TextFieldParser(new StringReader(fileContent)))
                    {
                        reader.SetDelimiters(new string[] { "," });
                        reader.TrimWhiteSpace = true;

                        var headings = reader.ReadFields();

                        var firstRow = new Csv.CsvRow();
                        foreach (var field in headings)
                        {
                            firstRow.Add(field);
                        }

                        writer.WriteRow(firstRow);

                        while (!reader.EndOfData)
                        {
                            var rowData = reader.ReadFields();
                            var productId = CommonFunctions.GetRowFieldData(rowData, 0);
                            var stock = CommonFunctions.GetRowFieldData(rowData, 1);
                            var price = priceRules.Find(x => x.Product.ClientProductID == productId).Product.Price;
                            var cost = CommonFunctions.GetRowFieldData(rowData, 3);
                            var distributorId = CommonFunctions.GetRowFieldData(rowData, 4);

                            var newRow = new Csv.CsvRow();
                            newRow.Add(productId);
                            newRow.Add(stock);
                            newRow.Add(price.ToString());
                            newRow.Add(cost);
                            newRow.Add(distributorId);
                            writer.WriteRow(newRow);
                        }

                        writer.Flush();
                    }
                }

                CommonDataFunctions.CreateLogEntry(channel, "END Create in memory CSV", "Information");
            }
            catch (Exception ex)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Could not Create in memory CSV. Reason:" +
                    ex.Message, "Notification");
            }

            return memoryStream;
        }

        public static void OutputPricesToSitc(Channel channel, MemoryStream stream)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START Output Prices to SITC", "Information");

            try
            {
                var relationalFtp = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Additional Inventory").FirstOrDefault();
                var outputFtp = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Output Inventory").FirstOrDefault();

                //Copy output files from output ftp in the old folder
                var copyOldDetails = new FtpCopyDetails()
                {
                    SourceHost = outputFtp.FTPServer,
                    SourceUsername = outputFtp.FTPUser,
                    SourcePassword = outputFtp.FTPPassword,
                    SourceProtocol = CommonFunctions.LookupFtpProtocol(outputFtp.FTPProtocolFK),
                    DestinationHost = outputFtp.FTPServer,
                    DestinationUsername = outputFtp.FTPUser,
                    DestinationPassword = outputFtp.FTPPassword,
                    DestinationFolderPath = "Old",
                    DestinationProtocol = CommonFunctions.LookupFtpProtocol(outputFtp.FTPProtocolFK)
                };

                var exclude = new List<string>()
                {
                    "Old"
                };

                var copyResult = Ftp.CopyAllFilesFromFtpToFtp(copyOldDetails, exclude);

                if (!copyResult.IsSuccess)
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Error: Could not back up files to old folder", "Error");
                    throw new ApplicationException("ERROR - FTP connection failure. Mesaage: " + copyResult.ErrorMessage);
                }
                else
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Successfully backed up files to old folder.", "Information");
                }

                // Copy Files across from relational ftp folder to the output folder
                var copyDetails = new FtpCopyDetails()
                {
                    SourceHost = relationalFtp.FTPServer,
                    SourceUsername = relationalFtp.FTPUser,
                    SourcePassword = relationalFtp.FTPPassword,
                    SourceProtocol = CommonFunctions.LookupFtpProtocol(relationalFtp.FTPProtocolFK),
                    SourceFolderPath = relationalFtp.FTPPath,
                    DestinationHost = outputFtp.FTPServer,
                    DestinationUsername = outputFtp.FTPUser,
                    DestinationPassword = outputFtp.FTPPassword,
                    DestinationProtocol = CommonFunctions.LookupFtpProtocol(outputFtp.FTPProtocolFK),
                    DestinationFolderPath = outputFtp.FTPPath
                };

                var copyResult2 = Ftp.CopyAllFilesFromFtpToFtp(copyDetails);

                if (!copyResult2.IsSuccess)
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Error: Could not copy files from additional to output ftp.", "Error");
                    throw new ApplicationException("ERROR - Could not copy files from relational FTP to output FTP. Error: " + copyResult2.ErrorMessage);
                }
                else
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Successfully copied files from additional to output ftp.", "Information");
                }

                // Upload the new StockAndPrices.csv file to the ouput folder
                var hostDetails = new Ftp.FtpHostDetails()
                {
                    BlobContainer = "tenantfolders",
                    FileName = outputFtp.FTPFileName,
                    FtpHost = outputFtp.FTPServer,
                    FtpUser = outputFtp.FTPUser,
                    FtpPassword = outputFtp.FTPPassword,
                    Protocol = CommonFunctions.LookupFtpProtocol(outputFtp.FTPProtocolFK)
                };

                try
                {
                    Ftp.UploadFTPFile(hostDetails, stream);
                    CommonDataFunctions.CreateLogEntry(channel, "Successfully copied price file to output ftp.", "Information");
                }
                catch (Exception ex)
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Error: Could not copy price file to output ftp.", "Information");
                    throw new ApplicationException("ERROR - Could not copy price file to output ftp. Error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                CommonDataFunctions.CreateLogEntry(channel, ex.Message, "Notification");
            }

            CommonDataFunctions.CreateLogEntry(channel, "END Output Prices to SITC", "Information");
        }
    }
}
