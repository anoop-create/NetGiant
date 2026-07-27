using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Collections;
using System.Web.Mvc;
using System.Text;
using System.Xml;
using System.Linq;
using System.Linq.Expressions;

namespace BusinessLogic
{
    public class DataCache
    {
        /// <summary>
        /// Retrieve an entry from the cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public static T GetCache<T>(string cacheKey) where T : class
        {
            T item = HttpContext.Current.Cache[cacheKey] as T;
            return item;
        }

        /// <summary>
        /// Write an entry to the cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool PutCache(string cacheKey, object item, int period = 0)
        {
            if (period == 0)
            {
                period = int.Parse(ConfigurationManager.AppSettings["CacheTime_Long"].ToString());
            }
            HttpContext.Current.Cache.Insert(cacheKey, item, null,
                DateTime.Now.AddHours(period),
                TimeSpan.Zero);
            return true;
        }

        /// <summary>
        /// Delete an entry from the cache
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public static bool DeleteCache(string cacheKey)
        {
            if (cacheKey == null)
            {
                foreach (DictionaryEntry c in HttpContext.Current.Cache)
                {
                    HttpContext.Current.Cache.Remove(c.Key.ToString());
                }
            }
            else
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
            return true;
        }

        public static string GetMenu(bool bypassCache = false)
        {
            string cacheKey = "Menu";
            string menu = GetCache<string>(cacheKey);
            if ((bypassCache) || (menu == null))
            {
                string prefix = Utilities.GetStaticFilePrefix();
                menu = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(prefix) + "\\main-menu.html");

                PutCache(cacheKey, menu);
            }

            return menu;
        }

        /// <summary>
        /// Retrieve all cms entries for a particular section and store in the cache
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSectionData(string sectionName, bool bypassCache = false)
        {
            string cacheKey = sectionName;
            Dictionary<string, string> sectionData = GetCache<Dictionary<string, string>>(cacheKey);
            if ((bypassCache) || (sectionData == null))
            {
                sectionData = new Dictionary<string, string>();

                List<cmsEntry> settings = EntityAccess.ReadCms(x => x.cmsSection.sectionName == sectionName);
                foreach (cmsEntry setting in settings)
                {
                    if (setting.redirectIsActive)
                    {
                        if (DateTime.Now > setting.redirectFrom && DateTime.Now < setting.redirectUntil)
                        {
                            sectionData.Add(setting.entryName, setting.cmsEntry2.cmsContent);
                        }
                        else
                        {
                            sectionData.Add(setting.entryName, setting.cmsContent);
                        }
                    }
                    else
                    {
                        sectionData.Add(setting.entryName, setting.cmsContent);
                    }
                }

                if (sectionData.Count > 0)
                {
                    PutCache(cacheKey, sectionData);
                }
            }

            return sectionData;
        }

        /// <summary>
        /// Retrieve a specific cms entry and store it in the cache
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="entryName"></param>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static string GetCMSEntry(string sectionName, string entryName, bool bypassCache = false)
        {
            string cacheKey = sectionName + "/" + entryName;
            object cacheItem = HttpContext.Current.Cache[cacheKey] as string;
            if ((bypassCache) || (cacheItem == null))
            {
                cmsEntry cms = EntityAccess.ReadCms(x => x.cmsSection.sectionName == sectionName
                                                         && x.entryName == entryName).FirstOrDefault();

                if (cms != null)
                {
                    cacheItem = cms.cmsContent;
                    if (cms.redirectIsActive)
                    {
                        if (DateTime.Now > cms.redirectFrom && DateTime.Now < cms.redirectUntil)
                        {
                            cacheItem = cms.cmsEntry2.cmsContent;
                        }
                    }
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (string) cacheItem ?? "";
        }

        public static List<Tuple<string, string, string>> GetSectionDataTriplet(string sectionName,
            bool bypassCache = false)
        {
            string cacheKey = sectionName + "T";
            List<Tuple<string, string, string>> sectionData = GetCache<List<Tuple<string, string, string>>>(cacheKey);
            if ((bypassCache) || (sectionData == null))
            {
                sectionData = new List<Tuple<string, string, string>>();

                List<cmsEntry> settings = EntityAccess.ReadCms(x => x.cmsSection.sectionName == sectionName);
                foreach (cmsEntry setting in settings)
                {
                    if (setting.redirectIsActive)
                    {
                        if (DateTime.Now > setting.redirectFrom && DateTime.Now < setting.redirectUntil)
                        {
                            sectionData.Add(new Tuple<string, string, string>(setting.entryName,
                                setting.cmsEntry2.cmsContent, setting.cmsEntry2.metaData));
                        }
                        else
                        {
                            sectionData.Add(new Tuple<string, string, string>(setting.entryName, setting.cmsContent,
                                setting.metaData));
                        }
                    }
                    else
                    {
                        sectionData.Add(new Tuple<string, string, string>(setting.entryName, setting.cmsContent,
                            setting.metaData));
                    }
                }

                if (sectionData.Count > 0)
                {
                    PutCache(cacheKey, sectionData);
                }
            }

            return sectionData;
        }

        public static Dictionary<string, List<SelectListItem>> GetCheckoutDropDowns(bool bypassCache = false)
        {
            var cacheKey = "CheckoutDropDowns";
            var cacheItem = (Dictionary<string, List<SelectListItem>>)HttpContext.Current.Cache[cacheKey];
            if ((bypassCache) || (cacheItem == null))
            {
                Dictionary<string, List<SelectListItem>> dict = new Dictionary<string, List<SelectListItem>>();

                var customerType = GetLookups(w => w.LookupType.LookupTypeName == "Customer Type");

                dict.Add("Customer Type", customerType.OrderBy(o => o.LookupName).Select(x => new SelectListItem
                {
                    Text = x.LookupName,
                    Value = x.LookupID.ToString()
                }).ToList());

                var organistationType = GetLookups(w => w.LookupType.LookupTypeName == "Organisation Type");

                dict.Add("Organisation Type", organistationType.OrderBy(o => o.LookupName).Select(x => new SelectListItem
                {
                    Text = x.LookupName,
                    Value = x.LookupID.ToString()
                }).ToList());

                var sector = GetLookups(w => w.LookupType.LookupTypeName == "Sector");

                dict.Add("Sector", sector.OrderBy(o => o.LookupName).Select(x => new SelectListItem
                {
                    Text = x.LookupName,
                    Value = x.LookupID.ToString()
                }).ToList());

                var numberOfStaff = GetLookups(w => w.LookupType.LookupTypeName == "Staff Count");

                dict.Add("Staff Count", numberOfStaff.Select(x => new SelectListItem
                {
                    Text = x.LookupName,
                    Value = x.LookupID.ToString()
                }).ToList());

                if (dict != null)
                {
                    cacheItem = dict;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return cacheItem;
        }

        public static List<Lookup> GetLookups(Predicate<Lookup> where = null, bool bypassCache = false)
        {
            string cacheKey = "Lookup";
            object cacheItem = HttpContext.Current.Cache[cacheKey] as IQueryable<Lookup>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<Lookup> li = new List<Lookup>();
                li = EntityAccess.ReadLookUp(x => true);
                if (li != null)
                {
                    cacheItem = li;
                    PutCache(cacheKey, cacheItem);
                }
            }

            if (where != null && cacheItem != null)
            {
                return ((List<Lookup>)cacheItem).FindAll(where).ToList();
            }
            return (List<Lookup>)cacheItem;
        }

        /// <summary>
        /// Retrive all manufacturers for a specific cartridge type and store the select list in the cache
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static List<SelectListItem> GetManufacturers(string typename, bool bypassCache = false)
        {
            string cacheKey = "Wiz/" + typename.Replace(' ', '-').ToLower();
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<SelectListItem>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<manufacturer> li = new List<manufacturer>();
                if (typename == "")
                {
                    li = EntityAccess.ReadManufacturer(x => x.eqEquipments.Any(y => y.statusFK == 1));
                }
                else
                {
                    string t = typename.Replace('-', ' ');
                    li = EntityAccess.ReadManufacturer(
                        x => x.eqEquipments.Any(y => y.eqCartridgeType.eqCartridgeTypeName == t && y.statusFK == 1));
                }

                List<SelectListItem> sl = li.Select(x => new SelectListItem
                    {
                        Text = x.manufacturerName,
                        Value = x.manufacturerID.ToString()
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                if (sl != null)
                {
                    cacheItem = sl;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (List<SelectListItem>) cacheItem;
        }

        /// <summary>
        /// Retrive all families for a specific cartridge type and manufacturer and store the select list in the cache
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="manufacturerId"></param>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static List<SelectListItem> GetFamilies(string typename, int manufacturerId, bool bypassCache = false)
        {
            string cacheKey = "Wiz/" + typename.Replace(' ', '-').ToLower() + "/" + manufacturerId.ToString();
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<SelectListItem>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<eqFamily> li = new List<eqFamily>();
                if (typename == "")
                {
                    li = EntityAccess.ReadFamily(x => x.manufacturerFK == manufacturerId &&
                                                      x.eqFamilyMemberships.Any(y => y.eqEquipment.statusFK == 1));
                }
                else
                {
                    string t = typename.Replace('-', ' ');
                    li = EntityAccess.ReadFamily(x => x.manufacturerFK == manufacturerId &&
                                                      x.eqFamilyMemberships.Any(
                                                          y => y.eqEquipment.eqCartridgeType.eqCartridgeTypeName == t &&
                                                               y.eqEquipment.statusFK == 1));
                }

                List<SelectListItem> sl = li.Select(x => new SelectListItem
                    {
                        Text = x.description,
                        Value = x.eqFamilyID.ToString()
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                if (sl != null)
                {
                    cacheItem = sl;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (List<SelectListItem>) cacheItem;
        }

        /// <summary>
        /// Retrive all equipment for a specific cartridge type, manufacturer and family and store the select list in the cache
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="manufacturerId"></param>
        /// <param name="familyId"></param>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static List<ExtdSelectListItem> GetEquipment(string typename, int manufacturerId, int familyId,
            bool bypassCache = false)
        {
            string cacheKey = "Wiz/" + typename.Replace(' ', '-').ToLower() + "/" + manufacturerId.ToString() + "/" +
                              familyId.ToString();
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<SelectListItem>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<eqEquipment> li = new List<eqEquipment>();
                if (typename == "")
                {
                    if (familyId == 0)
                    {
                        li = EntityAccess.ReadEquipment(x => x.manufacturerFK == manufacturerId && x.statusFK == 1);
                    }
                    else
                    {
                        li = EntityAccess.ReadEquipment(
                            x => x.manufacturerFK == manufacturerId && x.statusFK == 1 &&
                                 x.eqFamilyMemberships.Any(y => y.eqFamilyID == familyId));
                    }
                }
                else
                {
                    string t = typename.Replace('-', ' ');
                    if (familyId == 0)
                    {
                        li = EntityAccess.ReadEquipment(
                            x => x.eqCartridgeType.eqCartridgeTypeName == t && x.manufacturerFK == manufacturerId &&
                                 x.statusFK == 1);
                    }
                    else
                    {
                        li = EntityAccess.ReadEquipment(
                            x => x.eqCartridgeType.eqCartridgeTypeName == t && x.manufacturerFK == manufacturerId &&
                                 x.statusFK == 1 && x.eqFamilyMemberships.Any(y => y.eqFamilyID == familyId));
                    }
                }

                List<ExtdSelectListItem> sl = li.Select(x => new ExtdSelectListItem
                    {
                        Text = x.description ?? "",
                        Value = x.eqEquipmentID.ToString(),
                        Data = new {data_ctype = x.eqCartridgeType.eqCartridgeTypeName.ToLower().Replace(' ', '-')}
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                cacheItem = sl;
                PutCache(cacheKey, cacheItem);
            }
            return (List<ExtdSelectListItem>) cacheItem;
        }

        /// <summary>
        /// Retrieve list of Cartridge Types
        /// </summary>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static List<eqCartridgeType> GetCartridgeTypes(bool bypassCache = false)
        {
            string cacheKey = "CType";
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<eqCartridgeType>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<eqCartridgeType> li = new List<eqCartridgeType>();
                li = EntityAccess.ReadCartridgeType(x => true);

                if (li != null)
                {
                    cacheItem = li;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (List<eqCartridgeType>) cacheItem;
        }

        public static Tuple<List<ProductEntry>, List<ProductFilter>, string> GetCategoryProducts(int categoryId,
            string account, bool bypassCache = false)
        {
            string cacheKey1 = "CatProd/" + categoryId.ToString();
            string cacheKey2 = "CatFilt/" + categoryId.ToString();
            string cacheKey3 = "CatCrumb/" + categoryId.ToString();
            object cacheItem1 = HttpContext.Current.Cache[cacheKey1] as List<ProductEntry>;
            object cacheItem2 = HttpContext.Current.Cache[cacheKey2] as List<ProductFilter>;
            object cacheItem3 = HttpContext.Current.Cache[cacheKey3] as string;
            if (bypassCache || cacheItem1 == null || cacheItem2 == null || cacheItem3 == null)
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@CategoryID", SqlDbType.Int);
                sqlParm.Value = categoryId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
                sqlParm.Value = account;
                sqlParms.Add(sqlParm);
                DataSet ds = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetCategoryResults", sqlParms,
                    "catresults");

                // Breadcrumb Data
                DataTable dt = ds.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    List<string> bca = dt.Rows[0]["CategoryArray"].ToString().Split('|').ToList();
                    for (int i = 0; i < bca.Count; i++)
                    {
                        string[] s = bca[i].Split('#');
                        TagBuilder tag = new TagBuilder("a");
                        if (i == bca.Count - 1)
                        {
                            tag.Attributes.Add("href", "javascript:void(0)");
                        }
                        else
                        {
                            tag.Attributes.Add("href", "/catalogue/" + Utilities.CleanUrl(s[0]) + "-" + s[1] + "/");
                        }
                        tag.Attributes.Add("class", "second");
                        //tag.Attributes.Add("title", s[0]);
                        tag.InnerHtml = "&nbsp;" + s[0] + "&nbsp;";
                        sb = sb.Append(tag.ToString());

                        if (i != bca.Count - 1)
                        {
                            tag = new TagBuilder("i");
                            tag.Attributes.Add("class", "fa fa-chevron-right g-fs-xs-i");
                            sb = sb.Append(tag.ToString());
                        }
                    }
                    cacheItem3 = sb.ToString();
                    PutCache(cacheKey3, sb.ToString(),
                        int.Parse(ConfigurationManager.AppSettings["CacheTime_Short"].ToString()));
                }

                // Grid Data
                dt = ds.Tables[1];

                List<ProductEntry> lpe = new List<ProductEntry>();
                List<ProductFilter> lpf = new List<ProductFilter>();

                ProductEntry pe = new ProductEntry();
                List<ProductAttribute> lpa = new List<ProductAttribute>();
                ProductAttribute pa;
                int savedProductId = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    if (savedProductId != int.Parse(dr["ProductID"].ToString()))
                    {
                        if (savedProductId > 0)
                        {
                            pe.Attributes = lpa;
                            lpe.Add(pe);
                            lpa = new List<ProductAttribute>();
                        }

                        pe = ProductViewModel.CreateProductEntry(dr);
                        lpf = ProductViewModel.BuildProductFilter(lpf, 22, "Manufacturer", pe.BoBrandNo.ToString(), pe.Brand);

                        if (int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) == 3)
                        {
                            if (pe.AttribValue7 > 0)
                            {
                                lpf = ProductViewModel.BuildProductFilter(lpf, 31, "Product Type", pe.AttribValue7.ToString(), pe.AttDesc7);
                            }
                        }

                        if (pe.AttValue6 != 0 && pe.AttValue6 != 25)
                        {
                            lpf = ProductViewModel.BuildProductFilter(lpf, 6, "Promotion", pe.AttValue6.ToString(), pe.OfferFilterText);
                            pa = new ProductAttribute
                            {
                                Number = 6,
                                Name = "Promotion",
                                ValueId = pe.AttValue6.ToString(),
                                Value = pe.OfferFilterText
                            };
                            lpa.Add(pa);
                        }

                        savedProductId = int.Parse(dr["ProductID"].ToString());
                    }

                    if (dr["AttName"].ToString() != "" && int.Parse(dr["filterableAttributeID"].ToString()) != 0)
                    {
                        pa = new ProductAttribute
                        {
                            Number = int.Parse(dr["filterableAttributeID"].ToString()),
                            Name = dr["AttName"].ToString(),
                            ValueId = dr["AttValue"].ToString().Replace("+", "").Replace(",", "").Replace("-", "").Replace(" ", "").Replace("/", "_"),
                            Value = dr["AttValue"].ToString()
                        };
                        lpa.Add(pa);

                        lpf = ProductViewModel.BuildProductFilter(lpf,
                            int.Parse(dr["filterableAttributeID"].ToString()), dr["AttName"].ToString(),
                            dr["AttValue"].ToString(), dr["AttValue"].ToString());
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    pe.Attributes = lpa;
                    lpe.Add(pe);
                }

                lpe = lpe.OrderBy(x => x.PrimarySortSeq).ThenBy(x => x.AttDesc8).ToList();
                lpf = lpf.OrderBy(x => x.Name).ThenBy(x => x.ElementName).ToList();

                cacheItem1 = lpe;
                PutCache(cacheKey1, cacheItem1, int.Parse(ConfigurationManager.AppSettings["CacheTime_Short"].ToString()));
                cacheItem2 = lpf;
                PutCache(cacheKey2, cacheItem2, int.Parse(ConfigurationManager.AppSettings["CacheTime_Short"].ToString()));
            }
            return Tuple.Create((List<ProductEntry>) cacheItem1, (List<ProductFilter>) cacheItem2, (string) cacheItem3);
        }

        /// <summary>
        /// Retrive all product types and store the select list in the cache
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="bypassCache"></param>
        /// <returns></returns>
        public static List<SelectListItem> GetProductTypes(bool includeAllOption = true, bool bypassCache = false)
        {
            string cacheKey = "ProductTypes";
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<SelectListItem>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<productType> li = EntityAccess.ReadProductTypes(x => true);
                List<SelectListItem> sl = li.Select(x => new SelectListItem
                    {
                        Text = x.productTypeName,
                        Value = x.productTypeID.ToString()
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                if (includeAllOption)
                    sl.Insert(0, new SelectListItem {Text = "All", Value = "0"});

                if (sl != null)
                {
                    cacheItem = sl;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (List<SelectListItem>) cacheItem;
        }

        public static Dictionary<string, string> GetFeeFoScore(bool bypassCache = false)
        {
            string cacheKey = "FeeFoScore";
            Dictionary<string, string> cacheItem = HttpContext.Current.Cache[cacheKey] as Dictionary<string, string>;
            if ((bypassCache) || (cacheItem == null))
            {
                cacheItem = new Dictionary<string, string>();
                string vendorref = "";
                switch (int.Parse(ConfigurationManager.AppSettings["WebsiteId"]))
                {
                    case 1:
                    {
                        vendorref = "toner-giant";
                        break;
                    }
                    case 2:
                    {
                        vendorref = "cartridge-monkey";
                        break;
                    }
                    case 3:
                    {
                        vendorref = "netgiant-ltd";
                        break;
                    }
                }
                string url = "http://cdn2.feefo.com/api/xmlfeedback?merchantidentifier=" + vendorref + "&limit=0";

                try
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(url);
                    cacheItem.Add("Average", xmldoc.SelectSingleNode("FEEDBACKLIST/SUMMARY/AVERAGE").InnerText);
                    cacheItem.Add("FiveStar",
                        xmldoc.SelectSingleNode("FEEDBACKLIST/SUMMARY/FIVESTARAVERAGE").InnerText);
                    int c = 0;
                    int.TryParse(xmldoc.SelectSingleNode("FEEDBACKLIST/SUMMARY/COUNT").InnerText, out c);
                    cacheItem.Add("Count", string.Format("{0:N0}", c));
                    DataCache.PutCache(cacheKey, cacheItem);
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
            }
            return cacheItem;
        }

        public static List<ZoneLookup> BuildZoneLookup(bool bypassCache = false)
        {
            string cacheKey = "ZoneLookup";
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<ZoneLookup>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<deliveryZone> ldz = new List<deliveryZone>();
                ldz = EntityAccess.ReadDeliveryZones(x => true);
                List<ZoneLookup> lzl = new List<ZoneLookup>();

                foreach (deliveryZone dz in ldz)
                {
                    if (dz.Postcodes != "")
                    {
                        List<string> pcl = dz.Postcodes.Split(',').ToList();
                        foreach (string pc in pcl)
                        {
                            ZoneLookup zl = new ZoneLookup();
                            List<string> a = new List<string>();
                            if (pc.Contains("-"))
                            {
                                a = pc.Split('-').ToList();
                                zl = CreateZoneLookupEntry(a, dz);
                            }
                            else
                            {
                                a.Add(pc);
                                zl = CreateZoneLookupEntry(a, dz);
                            }
                            lzl.Add(zl);
                        }
                    }
                }

                if (lzl != null)
                {
                    cacheItem = lzl;
                    PutCache(cacheKey, cacheItem);
                }
            }
            return (List<ZoneLookup>) cacheItem;
        }

        private static ZoneLookup CreateZoneLookupEntry(List<string> lc, deliveryZone dz)
        {
            ZoneLookup zl = new ZoneLookup();
            int n;
            zl.ZoneId = dz.DeliveryZoneId;
            if (lc.Count > 1)
            {
                zl.Type = "Range";
                if (lc[1].Length > 1)
                {
                    zl.To = int.Parse(lc[1].Substring(0, 2));
                }
                else
                {
                    zl.To = int.Parse(lc[1].Substring(0, 1));
                }
                switch (lc[0].Length)
                {
                    case 4:
                        {
                            zl.Prefix = lc[0].Substring(0, 2);
                            zl.From = int.Parse(lc[0].Substring(2, 2));
                            break;
                        }
                    case 3:
                        {
                            if (int.TryParse(lc[0].Substring(1, 1), out n))
                            {
                                zl.Prefix = lc[0].Substring(0, 1);
                                zl.From = int.Parse(lc[0].Substring(1, 2));
                            }
                            else
                            {
                                zl.Prefix = lc[0].Substring(0, 2);
                                zl.From = int.Parse(lc[0].Substring(2, 1));
                            }
                            break;
                        }
                    case 2:
                        {
                            zl.Prefix = lc[0].Substring(0, 1);
                            zl.From = int.Parse(lc[0].Substring(1, 1));
                            break;
                        }
                }
            }
            else
            {
                zl.Type = "Prefix";
                zl.Prefix = lc[0].Substring(0, lc[0].Length);
            }
            
            return zl;
        }
    }
}