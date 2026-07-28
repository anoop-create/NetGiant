using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class RandomText
    {
        static bool errorHasOccurred = false;
        static int websiteId = 0;
        static int languageId = 0;
        static string html = "";
        static int cnt = 0;
        private static List<AxisPriceView> axisPriceList;

        public static void GenerateProductText(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["websiteid"] + " Process Started");
            websiteId = Convert.ToInt32(parms["websiteid"]);
            SetLanguageId();
            GetAxisPrices();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    List<AxisFieldsAdditional> products = GetProducts(db);
                    Random rnd = new Random();

                    foreach (AxisFieldsAdditional afa in products)
                    {
                        AxisFields af = afa.AxisFields;
                        product p = afa.AxisFields.product;
                        productGroup pg = afa.AxisFields.product.productGroup;
                        productItemType pit = afa.AxisFields.product.productItemType;
                        manufacturer m = p.manufacturer;

                        bool isOwnBrand = false;
                        bool isAssembly = false;
                        bool isMaintenance = false;
                        List<int> randomEntries = new List<int>();

                        if (m.manufacturerName == "Own Brand" || m.manufacturerName == "Katun")
                        {
                            isOwnBrand = true;
                        }

                        if (p.productItemTypeFK == 2)
                        {
                            if (!CheckForSingles(p))
                            {
                                isAssembly = true;
                            }
                        }

                        if (p.productGroup.productTypeFK == 3 || p.productGroup.productTypeFK == 4)
                        {
                            for (int i = 1; i < 11; i++)
                            {
                                randomEntries.Add(rnd.Next(1,5));
                            }
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                        }
                        else
                        {
                            for (int i = 1; i < 11; i++)
                            {
                                randomEntries.Add(rnd.Next(1, 6));
                            }
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                            //randomEntries.Add(0);
                        }

                        if (af.attr9 == 5 || af.attr9 == 6)
                        {
                            isMaintenance = true;
                        }

                        html = StandardFunctions.GetRandomText(
                                                    websiteId,
                                                    pg.productTypeFK,
                                                    isOwnBrand,
                                                    isAssembly,
                                                    isMaintenance,
                                                    randomEntries,
                                                    rnd.Next(1, 6)
                        );

                        if (html != "")
                        {
                            html = ProductReplaces(html, afa);
                            afa.stockNoteDesc = "[ProductInfo,<div id=\"rt\">" + html + "</div>]";
                            cnt += 1;
                        }
                    }
                    db.SaveChanges();
                }
            }

            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Occured generating product random text" + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Message**: " + ex.Message + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Stack Trace**: " + ex.StackTrace + Environment.NewLine);
                errorHasOccurred = true;
            }

            stnFunc.AddToActivityLog("Finished Batch Program with switch: genproducttext. Products updated: " + cnt + Environment.NewLine);
            string acitivityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && Properties.Settings.Default.Environment == "Live")
                stnFunc.SendSimpleEmail("genproducttext", acitivityLogFileName);
        }

        private static bool CheckForSingles(product p)
        {
            bool single;
            var assemblySum = p.assemblyComponent.Sum(x => x.quantity);

            if (assemblySum == 1)
            {
                single = true;
            }
            else
            {
                single = false;
            }

            return single;
        }

        private static void SetLanguageId()
        {
            switch (websiteId)
            {
                case 3:
                    languageId = 5;
                    break;
                default:
                    languageId = websiteId;
                    break;
            }
        }

        private static List<AxisFieldsAdditional> GetProducts(ngmdEntities db)
        {
            List<AxisFieldsAdditional> products;
            products = (db.AxisFieldsAdditional
                        .Include("AxisFields.product.CrossSellingLink.product1.manufacturer")
                        .Include("AxisFields.product.productGroup.productType")
                        .Include("AxisFields.product.manufacturer")
                        .Include("AxisFields.product.productItemType")
                        .Where(x => (x.stockNoteDesc == "" || x.stockNoteDesc == null)
                            && x.AxisFields.product.productStatusFK == 1
                            && x.websiteFK == websiteId))
                        .ToList();
            return products;
        }

        private static string ProductReplaces(string html, AxisFieldsAdditional afa)
        {
            // Replace first occurence of Manufacturer Name with a link
            int pos = html.IndexOf("{ManufacturerName}");
            if (pos >= 0)
            {
                string productType = afa.AxisFields.product.productGroup.productType.productTypeName;
                if ("Franking Ink Solid Ink Toner".Contains(productType))
                {
                    string url = "/" + (productType + " cartridges").ToLower().Replace(" ", "-") + "/" + afa.AxisFields.product.manufacturer.manufacturerName.Replace(" ", "-") + "/";
                    html = html.Substring(0, pos) + "<a href=\"" + url + "\">" + afa.AxisFields.product.manufacturer.manufacturerName + "</a>" + html.Substring(pos + 18);
                }
            }
            html = html.Replace("{ManufacturerName}", afa.AxisFields.product.manufacturer.manufacturerName);
            html = html.Replace("{ProductName}", afa.AxisFields.product.productName + (afa.AxisFields.spec1 == null ? "" : " " + afa.AxisFields.spec1));
            html = html.Replace("{PartNo}", (afa.AxisFields.product.partNo == null ? "" : afa.AxisFields.product.partNo));
            html = html.Replace("{ProductType}", afa.AxisFields.product.productGroup.productType.productTypeName);
            html = html.Replace("{CartridgeDescription}", (afa.AxisFields.spec6 == null ? "" : afa.AxisFields.spec6));
            html = html.Replace("{ProductItemType}", afa.AxisFields.product.productItemType.productItemTypeName);

            html = ReplaceCrossSell(html, afa);

            return html;
        }

        private static string ReplaceCrossSell(string html, AxisFieldsAdditional afa)
        {
            if (afa.AxisFields.product.manufacturerFK == 278)
            {
                var crossSell = afa.AxisFields.product.crossSellingLink1.FirstOrDefault();
                var crossSaving = "";
                double crossSavingCalc = 0;

                if (crossSell != null)
                {
                    html = html.Replace("{CrossBrand}", crossSell.product.manufacturer.manufacturerName);
                    html = html.Replace("{CrossPartNo}", crossSell.product.partNo);

                    AxisPriceView prodPrice = axisPriceList.Where(x => x.partNo == afa.AxisFields.product.partNo && x.language == languageId)
                    .OrderBy(x => x.priceTypeID).FirstOrDefault();

                    AxisPriceView crossSellPrice = axisPriceList.Where(x => x.partNo == crossSell.product.partNo && x.language == languageId)
                    .OrderBy(x => x.priceTypeID).FirstOrDefault();

                    if (crossSellPrice != null && prodPrice != null)
                    {
                        if (websiteId != 2)
                        {
                            crossSavingCalc = Convert.ToDouble(crossSellPrice.tradePriceExVat - prodPrice.tradePriceExVat);
                            crossSaving = Math.Round(crossSavingCalc, 2).ToString("0.00");
                        }
                        else
                        {
                            crossSavingCalc = Convert.ToDouble(crossSellPrice.tradePriceIncVat - prodPrice.tradePriceIncVat);
                            crossSaving = Math.Round(crossSavingCalc, 2).ToString("0.00");
                        }
                    }
                    
                    html = html.Replace("{CrossSaving}", "£" + crossSaving);
                }
                else
                {
                    html = html.Replace("{CrossBrand}", "");
                    html = html.Replace("{CrossPartNo}", "");
                    html = html.Replace("{CrossSaving}", "");
                }

            }
            return html;
        }

        public static void GenerateEquipmentText(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["websiteid"] + " Process Started");
            websiteId = Convert.ToInt32(parms["websiteid"]);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    List<eqEquipment> equipment;
                    equipment = db.eqEquipment.ToList();
                    Random rnd = new Random();

                    foreach (eqEquipment equip in equipment)
                    {
                        if (NoteExists(equip, websiteId))
                        {
                            continue;
                        }

                        bool isOwnBrand = false;
                        bool isAssembly = false;
                        bool isMaintenance = false;
                        List<int> randomEntries = new List<int>();

                        for (int i = 1; i < 11; i++)
                        {
                            randomEntries.Add(rnd.Next(1, 6));
                        }

                        html = StandardFunctions.GetRandomText(
                                                    websiteId,
                                                    8,
                                                    isOwnBrand,
                                                    isAssembly,
                                                    isMaintenance,
                                                    randomEntries,
                                                    rnd.Next(1, 10)
                        );

                        html = EquipmentReplacements(html, equip);

                        if (html != "")
                        {
                            equipmentNotes en = new equipmentNotes()
                            {
                                websiteFK = websiteId,
                                note = "<div id=\"rt\">" + html + "</div>",
                                eqEquipmentFK = equip.eqEquipmentID
                            };

                            db.equipmentNotes.Add(en);
                            db.SaveChanges();
                            cnt += 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Occured generating equipment random text" + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Message**: " + ex.Message + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Stack Trace**: " + ex.StackTrace + Environment.NewLine);
                errorHasOccurred = true;
            }

            stnFunc.AddToActivityLog("Finished Batch Program with switch: genequipmenttext. Equipment updated: " + cnt + Environment.NewLine);
            string acitivityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && Properties.Settings.Default.Environment == "Live")
                stnFunc.SendSimpleEmail("genequipmenttext", acitivityLogFileName);
        }

        private static string EquipmentReplacements(string html, eqEquipment equip)
        {
            string manufacturerName = equip.manufacturer.manufacturerName;
            string printerName = equip.description;
            string cartridgeTypeName = equip.eqCartridgeType.eqCartridgeTypeName;
            string familyName = "";
            if(equip.eqFamilyMembership.Count > 0)
                familyName = equip.eqFamilyMembership.FirstOrDefault().eqFamily.description;

            int pos;
            // Replace first occurence of ManufacturerName with a link
            pos = html.IndexOf("{ManufacturerName}");
            if (pos >= 0)
            {
                string url = "/" + cartridgeTypeName.ToLower().Replace(" ", "-") + "/" + manufacturerName.Replace(" ", "-") + "/";
                html = html.Substring(0, pos) + "<a href=\"" + url + "\">" + manufacturerName + "</a>" + html.Substring(pos + 18);
            }
            // Replace first occurence of CartridgeTypeName Name with a link
            pos = html.IndexOf("{CartridgeTypeName}");
            if (pos >= 0)
            {
                string url = "/" + cartridgeTypeName.ToLower().Replace(" ", "-") + "/";
                html = html.Substring(0, pos) + "<a href=\"" + url + "\">" + cartridgeTypeName + "</a>" + html.Substring(pos + 19);
            }
            html = html.Replace("{ManufacturerName}", manufacturerName);
            html = html.Replace("{PrinterName}", printerName);
            html = html.Replace("{CartridgeTypeName}", cartridgeTypeName);
            html = html.Replace("{FamilyName}", familyName);
            return html;
        }

        private static bool NoteExists(eqEquipment equip, int websiteID)
        {
            var returnValue = false;
            var note = equip.equipmentNotes.Where(x => x.websiteFK == websiteID).FirstOrDefault();

            if (note != null)
                returnValue = true;

            return returnValue;
        }

        private static void GetAxisPrices()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                //Exclude account specific axis pricing
                axisPriceList = db.AxisPriceView.Where(x => x.account == null).ToList();
            }
        }
    }


}
