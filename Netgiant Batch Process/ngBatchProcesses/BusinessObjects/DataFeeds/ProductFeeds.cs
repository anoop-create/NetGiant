using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class ProductFeeds
    {
        // Possibly no longer used
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;

            //Setup the CSV file and the delim to use, 
            char inputDelim = new char();
            inputDelim = '\t';
            //Setup the CSV file and the delim to use, 
            char outputDelim = new char();
            outputDelim = ',';

            using (CsvFileWriter writer = new CsvFileWriter(parms["output"], outputDelim))
            {
                try
                {
                    using (CsvFileReader reader = new CsvFileReader(parms["input"], inputDelim))
                    {                        
                        CsvRow inputRow = new CsvRow();
                        bool isNotEOF = reader.ReadRow(inputRow);
                        while (isNotEOF)
                        {
                            int i = 0;
                            CsvRow outputRow = new CsvRow();
                            foreach (string elem in inputRow)
                            {
                                if (i != 16)
                                {
                                    outputRow.Add(elem);
                                }
                                i += 1;
                            }
                            writer.WriteRow(outputRow);
                            isNotEOF = reader.ReadRow(inputRow);
                        }                       
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Reading CSV File: " + parms["input"], ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //FTP output file
            if (parms.ContainsKey("ftpsite"))
            {
                string finalFileName = "";
                string[] fileParts = parms["output"].Split('\\');
                finalFileName = fileParts[fileParts.Length - 1]; //Kenshoo final Path Part
                //Marin final Path Part
                //finalFileName = fileParts[fileParts.Length - 1].Split('.')[0] + "_" + DateTime.Now.ToShortDateString().Replace("/", "") + "." + fileParts[fileParts.Length - 1].Split('.')[1];
                try
                {
                    Tuple<bool, string> rtn = FtpUtilities.UploadFTPFile(parms["output"],
                        parms["ftpsite"],
                        parms["ftpusername"],
                        parms["ftppassword"],
                        parms["ftppath"] + finalFileName,
                        parms.FirstOrDefault(x => x.Key == "subtype").Value == "usessl");
                    if (rtn.Item1)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                    }
                    else
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + parms["output"], ErrorCode = "ERROR" });
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + parms["output"], ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }

            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public static CsvRow KenshooFieldMappings(DataRow inputRow, Boolean shortCodeEntry)
        {
            const int partNumber = 0;
            const int manufacturerName = 1;
            const int description = 2;
            const int price = 4;
            const int categorisation = 5;
            const int stockOnHand = 8;
            const int productURL = 10;
            const int spec4 = 15;
            const int spec6 = 17;


            string wDescription;
            string wPartNo;
            string wManufacturer;
            string wCategory;
            string wSubcat2;
            string wSpec6;
            decimal wStock;
            decimal wPrice;
            decimal wPriceIncVAT;

            CsvRow outputRow = new CsvRow();

            wDescription = inputRow.Field<string>(description).Replace("\n", "").Replace("\r", "");
            wCategory = inputRow.Field<string>(categorisation);
            wStock = inputRow.Field<decimal>(stockOnHand);
            wSpec6 = inputRow.Field<string>(spec6);
            if (shortCodeEntry)
            {
                wPartNo = inputRow.Field<string>(spec4);
            } else {
                wPartNo = inputRow.Field<string>(partNumber);
            }
            wManufacturer = inputRow.Field<string>(manufacturerName);

            //Exclusions
            if (wStock == 0)
            {
                return outputRow;
            }
            if (wSpec6.ToLower() == "exclude from kenshoo")
            {
                return outputRow;
            }
            if (wDescription.Contains("scanner"))
            {
                return outputRow;
            }
            if (wCategory.Contains("Ink"))
            {
                if (!wDescription.Contains("Franking"))
                {
                    return outputRow;
                }
            }
            if (!shortCodeEntry && wPartNo.Length <= 4)
            {
                return outputRow;
            }
            if (wManufacturer == "Misc")
            {
                return outputRow;
            }
            if (wCategory == "Stationery")
            {
                return outputRow;
            }
            if (wCategory.Contains("Ink") || wCategory.Contains("Toner") || wCategory.Contains("Stationery"))
            {
                //OK
            }
            else
            {
                return outputRow;
            }

            //Generate Price
            wPriceIncVAT = inputRow.Field<decimal>(price);
            wPrice = Math.Round(wPriceIncVAT / 1.2M, 2, MidpointRounding.AwayFromZero);

            //SKU
            if (wPartNo.StartsWith("FR"))
            {
                wPartNo = wPartNo.Replace("FR", "");
            }
            if (PartNos.ContainsKey(wPartNo))
            {
                wPartNo = wPartNo.Replace(wPartNo, PartNos[wPartNo]);
            }
            //Special rule for Samsung suffixes
            if (wManufacturer == "Samsung" && (wPartNo.Substring(wPartNo.Length - 3) == "SEE" || wPartNo.Substring(wPartNo.Length - 3) == "ELS"))
            {
                wPartNo = wPartNo.Substring(0, wPartNo.Length - 3);
            }
            outputRow.Add(wPartNo);

            //Brand
            if (wManufacturer == "Hewlett Packard")
            {
                wManufacturer = "HP";
            }
            if (wManufacturer.Contains("Minolta"))
            {
                wManufacturer = "Konica";
            }
            if (wManufacturer.Contains("Own Brand"))
            {
                wManufacturer = "TonerGiant";
            }
            foreach (string descriptionKey in DescriptionsManu.Keys)
            {
                if (wDescription.Contains(descriptionKey))
                {
                    wManufacturer = DescriptionsManu[descriptionKey];
                    break;
                }
            }
            outputRow.Add(wManufacturer);

            //Model
            outputRow.Add(String.Format(wDescription));

            //Price
            outputRow.Add(wPrice.ToString("F"));

            //Category
            foreach (string descriptionKey in DescriptionsCat.Keys)
            {
                if (wDescription.Contains(descriptionKey))
                {
                    wCategory = DescriptionsCat[descriptionKey];
                    break;
                }
            }
            if (wCategory == "Stationery")
            {
                wCategory = " ";
            }
            if (wCategory == "Ink" && wDescription.Contains("Gel"))
            {
                wCategory = "Gel";
            }
            outputRow.Add(wCategory);

            //SubCat1
            outputRow.Add("");

            //SubCat2
            wSubcat2 = "";
            if (inputRow.Field<string>(categorisation).Contains("Printer") && wDescription.Contains("Colour"))
            {
                wSubcat2 = "Colour";
            }
            if (inputRow.Field<string>(categorisation).Contains("Toner") || inputRow.Field<string>(categorisation).Contains("Drum"))
            {
                if (wDescription.ToLower().Contains("cyan"))
                {
                    wSubcat2 = "Cyan";
                }
                if (wDescription.ToLower().Contains("black"))
                {
                    wSubcat2 = "Black";
                }
                if (wDescription.ToLower().Contains("magenta"))
                {
                    wSubcat2 = "Magenta";
                }
                if (wDescription.ToLower().Contains("yellow"))
                {
                    wSubcat2 = "Yellow";
                }
            }
            if (inputRow[categorisation].ToString() == "Ink" || inputRow[categorisation].ToString() == "Gel")
            {
                if (wDescription.ToLower().Contains("cyan"))
                {
                    wSubcat2 = "Cyan";
                }
                if (wDescription.ToLower().Contains("magenta"))
                {
                    wSubcat2 = "Magenta";
                }
                if (wDescription.ToLower().Contains("yellow"))
                {
                    wSubcat2 = "Yellow";
                }
                if (wDescription.ToLower().Contains("black"))
                {
                    wSubcat2 = "Black";
                }
            }
            if (inputRow[categorisation].ToString() == "Ink")
            {
                if (wDescription.ToLower().Contains("light cyan"))
                {
                    wSubcat2 = "Lt Cyan";
                }
                if (wDescription.ToLower().Contains("light magenta"))
                {
                    wSubcat2 = "Lt Magenta";
                }
                if (wDescription.ToLower().Contains("blue"))
                {
                    wSubcat2 = "Blue";
                }
                if (wDescription.ToLower().Contains("red"))
                {
                    wSubcat2 = "Red";
                }
            }
            foreach (string descriptionKey in DescriptionsCat2.Keys)
            {
                if (wDescription.Contains(descriptionKey))
                {
                    wSubcat2 = DescriptionsCat2[descriptionKey];
                    break;
                }
            }
            outputRow.Add(wSubcat2);

            //Stock On Hand
            if (wCategory == "Ink")
            {
                wStock = 0;
            }
            if (wDescription.Contains("HP 970") || wDescription.Contains("HP 971") || wDescription.Contains("HP 970XL") || wDescription.Contains("HP 971XL"))
            {
                wStock = 99;
            }
            outputRow.Add(wStock.ToString("F0"));

            //Promo
            outputRow.Add("");

            //Link
            outputRow.Add(String.Format(inputRow.Field<string>(productURL)));

            return outputRow;
        }

        public static CsvRow MarinFieldMappings(CsvRow inputRow)
        {
            const int manufacturerName = 0;
            const int productName = 1;
            const int partNumber = 2;
            const int categorisation = 3;
            const int description = 4;
            const int price = 5;
            const int productURL = 8;
            const int stockOnHand = 12;

            string wProductID;
            string wManufacturer;
            string wCategory;
            string wSubcat2;
            decimal wPrice;
            decimal wPriceIncVAT;

            CsvRow outputRow = new CsvRow();

            if (inputRow[manufacturerName] == "Manufacturer Name") {
                outputRow.Add("Product ID");
                outputRow.Add("Name");
                outputRow.Add("Model Number");
                outputRow.Add("Color");
                outputRow.Add("Brand");
                outputRow.Add("Product Price");
                outputRow.Add("Sale Price");
                outputRow.Add("Category");               
                //outputRow.Add("Live Ads Param 1");
                outputRow.Add("Product URL");
                outputRow.Add("Inventory Status");
                //outputRow.Add("Supplementary");
                //outputRow.Add("Inventory Type");
                //outputRow.Add("In Stock");
                //outputRow.Add("Sub_Cat1");
                //outputRow.Add("Sub_Cat2");
                //outputRow.Add("Stock");
                //outputRow.Add("Promo");
                //outputRow.Add("Link");

                return outputRow;
            }

            //Generate Price
            if (decimal.TryParse(inputRow[price], out wPriceIncVAT)) {
                // OK
            } else {
                wPriceIncVAT = 0;
            }
            // Take off 20%
            wPrice = Math.Round(wPriceIncVAT / 1.2M, 2, MidpointRounding.AwayFromZero);

            if (inputRow[productName].Contains("scanner")) {
                return outputRow;
            }
            if (inputRow[categorisation].Contains("Ink")) {
                if (!inputRow[productName].Contains("Franking")) {
                    return outputRow;
                }
            }
            if (inputRow[partNumber].Length <= 4) {
                return outputRow;
            }
            if (inputRow[manufacturerName] == "Misc") {
                return outputRow;
            }
            if (inputRow[categorisation] == "Stationery") {
                return outputRow;
            }
            if (inputRow[categorisation].Contains("Ink") || inputRow[categorisation].Contains("Toner") || inputRow[categorisation].Contains("Stationery")) {
                //OK
            } else {
                return outputRow;
            }
            
            //Product ID
            wProductID = inputRow[description].Split(' ')[0];
            outputRow.Add(wProductID);

            //Product Name
            outputRow.Add(String.Format(inputRow[productName]));

            //Model Number
            outputRow.Add(String.Format(inputRow[partNumber]));

            //Color (Uses SubCat2 processing)
            wSubcat2 = "";
            if (inputRow[categorisation].Contains("Printer") && inputRow[productName].Contains("Colour")) {
                wSubcat2 = "";
            }
            if (inputRow[categorisation].Contains("Toner") || inputRow[categorisation].Contains("Drum")) {
                if (inputRow[productName].ToLower().Contains("cyan")) {
                    wSubcat2 = "Cyan";
                }
                if (inputRow[productName].ToLower().Contains("black")) {
                    wSubcat2 = "Black";
                }
                if (inputRow[productName].ToLower().Contains("magenta")) {
                    wSubcat2 = "Magenta";
                }
                if (inputRow[productName].ToLower().Contains("yellow")) {
                    wSubcat2 = "Yellow";
                }
            }
            if (inputRow[categorisation] == "Ink" || inputRow[categorisation] == "Gel") {
                if (inputRow[productName].ToLower().Contains("cyan")) {
                    wSubcat2 = "Cyan";
                }
                if (inputRow[productName].ToLower().Contains("magenta")) {
                    wSubcat2 = "Magenta";
                }
                if (inputRow[productName].ToLower().Contains("yellow")) {
                    wSubcat2 = "Yellow";
                }
                if (inputRow[productName].ToLower().Contains("black")) {
                    wSubcat2 = "Black";
                }
            }
            if (inputRow[categorisation] == "Ink") {
                if (inputRow[productName].ToLower().Contains("light cyan")) {
                    wSubcat2 = "Lt Cyan";
                }
                if (inputRow[productName].ToLower().Contains("light magenta")) {
                    wSubcat2 = "Lt Magenta";
                }
                if (inputRow[productName].ToLower().Contains("blue")) {
                    wSubcat2 = "Blue";
                }
                if (inputRow[productName].ToLower().Contains("red")) {
                    wSubcat2 = "Red";
                }
            }
            foreach (string descriptionKey in DescriptionsCat2.Keys)
            {
                if (inputRow[productName].Contains(descriptionKey)) {
                    wSubcat2 = DescriptionsCat2[descriptionKey];
                    break;
                }
            }
            outputRow.Add(wSubcat2);

            //Brand
            wManufacturer = inputRow[manufacturerName];
            if (wManufacturer == "Hewlett Packard") {
                wManufacturer = "HP";
            }
            if (wManufacturer.Contains("Minolta")) {
                wManufacturer = "Konica";
            }
            if (wManufacturer.Contains("Own Brand")) {
                wManufacturer = "TonerGiant";
            }
            foreach (string descriptionKey in DescriptionsManu.Keys) {
                if (inputRow[productName].Contains(descriptionKey)) {
                    wManufacturer = DescriptionsManu[descriptionKey];
                    break;
                }
            }
            outputRow.Add(wManufacturer);

            //Product Price
            outputRow.Add(wPrice.ToString("F"));

            //Sale Price
            outputRow.Add("");

            //Category
            //wCategory = inputRow[categorisation];            
            //foreach (string descriptionKey in DescriptionsCat.Keys) 
            //{
            //    if (inputRow[productName].Contains(descriptionKey)) {
            //        wCategory = DescriptionsCat[descriptionKey];
            //        break;
            //    }
            //}
            //if (inputRow[categorisation] == "Ink" && inputRow[productName].Contains("Gel")) {
            //    wCategory = "Gel";
            //}
            wCategory = "PRODUCT";
            outputRow.Add(wCategory);

            //Product URL
            outputRow.Add(String.Format(inputRow[productURL]));

            //Inventory Status
            if (inputRow[stockOnHand] == "0") {
                outputRow.Add("FALSE");
            } else {
                outputRow.Add("TRUE");
            }

            //Supplementary
            //outputRow.Add("");

            //Inventory Type
            //outputRow.Add("PRODUCT");

            //In Stock
            //outputRow.Add("TRUE");

            //SubCat1
            //outputRow.Add("");

            //Stock On Hand
            //wStock = inputRow[stockOnHand];
            //if (inputRow[categorisation] == "Ink") {
            //    wStock = "0";
            //}
            //if (inputRow[description].Contains("HP 970") || inputRow[description].Contains("HP 971") || inputRow[description].Contains("HP 970XL") || inputRow[description].Contains("HP 971XL")) {
            //    wStock = "99";
            //}                
            //outputRow.Add(wStock);

            //outputRow.Add("");

            //outputRow.Add(String.Format(inputRow[productURL]));

            return outputRow;
        }

        public static Dictionary<string, string> PartNos = new Dictionary<string, string>();

        public static Dictionary<string, string> Manu = new Dictionary<string, string>();

        public static Dictionary<string, string> DescriptionsManu = new Dictionary<string, string>();

        public static Dictionary<string, string> DescriptionsCat = new Dictionary<string, string>();

        public static Dictionary<string, string> DescriptionsCat2 = new Dictionary<string, string>();

        public static void BuildDictionaries()
        {
            //PartNos - Straight replacement of part numbers <name> with <value>
            PartNos.Add("FR7935RN", "793-5RN");
            PartNos.Add("FR6201B", "620-1BI");
            PartNos.Add("FR6201R", "620-1RN");
            PartNos.Add("FR7659BN", "765-9BN");
            PartNos.Add("FR7659RN", "765-9RN");
            PartNos.Add("FR765E", "765-E");
            PartNos.Add("FR766B", "766-B");
            PartNos.Add("FR7678B", "767-8BN");
            PartNos.Add("FR7678R", "767-8RN");
            PartNos.Add("FR7935B", "793-5BI");
            PartNos.Add("FRDE6128R", "DE6128");
            PartNos.Add("FR780001", "K780002");
            PartNos.Add("FR780003", "K780003");
            PartNos.Add("FR769B", "769-B");
            PartNos.Add("FRB7950000203", "B7950000203");
            PartNos.Add("FRE74092001", "E74092-001");
            PartNos.Add("FRB795014", "B795014");

            //DescriptionsManu - If the description contains <name> make the Manufacturer <value>
            DescriptionsManu.Add("PB Compatible", "PB");
            DescriptionsManu.Add("Pitney Bowes", "PB");
            DescriptionsManu.Add("Optimail", "FP");
            DescriptionsManu.Add("mymail", "FP");
            DescriptionsManu.Add("Ultmail", "FP");
            DescriptionsManu.Add("Neopost", "Neopost");

            //DescriptionsCat - If the description contains <name> make the Category <value>
            DescriptionsCat.Add("Franking", "Cartridges");
            DescriptionsCat.Add("Drum", "Drum");
            DescriptionsCat.Add("Solid", "Solid Ink");
            DescriptionsCat.Add("Waste", "Waste Toner");
            DescriptionsCat.Add("Transfer Unit", "Transfer Unit");
            DescriptionsCat.Add("Belt", "Transfer Belt");
            DescriptionsCat.Add("Roller", "Transfer Roller");
            DescriptionsCat.Add("Fuser", "Fuser");
            DescriptionsCat.Add("Photoconductor", "Drum");
            DescriptionsCat.Add("Imaging", "Drum");
            DescriptionsCat.Add("Staple", "Staple Pack");
            DescriptionsCat.Add("Fax Ribbon", "Fax Ribbon");
            DescriptionsCat.Add("printhead", "Printhead");
            DescriptionsCat.Add("Print Unit", "Print Unit");
            DescriptionsCat.Add("Laser Printer", "Laser Printer");
            DescriptionsCat.Add("Inkjet Printer", "Inkjet Printer");
            DescriptionsCat.Add("Fabric Ribbon", "Ribbon");
            DescriptionsCat.Add("Photo Conductor", "Drum");
            DescriptionsCat.Add("Print Head", "Printhead");

            //DescriptionsCat2 - If the description contains <name> make the SubCat2 <value>
            DescriptionsCat2.Add("Mono", "Mono");
        }
    }
}