using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.DataLayer.NetgiantMembership;
using System.Data.Entity;
using System.Net.Mail;
using PagedList;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Configuration;

namespace netGiant.Intranet.BusinessLayer.ViewModels.QA
{
    public class QAViewModel : CommonViewModel
    {
        public QAViewModel()
        {
            ListOfQAs = null;
            _ctx = new ngmdEntities();
        }
        private ngmdEntities _ctx;

        public PagedList.IPagedList<qa_Main> ListOfQAs { get; set; }
        public qa_Main QA { get; set; }
        public IQueryable<SelectListItem> AllGranularities { get; set; }
        public IQueryable<Website> AllWebsites { get; set; }
        public List<Website> SelectedWebsites { get; set; }
        public int[] SelectedWebsiteIDs { get; set; }
        public string UserName { get; set; }
        public int SelectedGranualityID { get; set; }
        public IQueryable<TelerikQA> QAList { get; set; }
        public IQueryable<SelectListItem> AllEquipment { get; set; }

        public void GetQAList()
        {
            string env = "www.";
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                env = "beta.";
            }
            QAList = _ctx.GetQAList().ToList()
                .Select(x => new TelerikQA
                {
                    Id = x.Id,
                    Question = x.Question,
                    ProductID = x.ProductId == null ? "" : x.ProductId.ToString(),
                    ProductUrl = string.IsNullOrEmpty(x.ProductURL) 
                    ? "" 
                    : x.WebsiteId == 1
                        ? "https://www.tonergiant.co.uk/".Replace("www.", env) + x.ProductURL
                        : "https://www.cartridgemonkey.com/".Replace("www.", env) + x.ProductURL,
                    EquipmentName = x.Equipment == null ? "" : x.Equipment.ToString(),
                    EquipmentURL = CreateEquipmentURL(x.Equipment, x.WebsiteId, env, x.EquipmentType),
                    AskedDate = x.AskedDate,
                    RepliedDate = x.RepliedDate,
                    RepliedBy = !string.IsNullOrEmpty(x.RepliedBy) ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x.RepliedBy.Split('@')[0].Replace(".", " ")) : "",
                    Granularity = x.Granularity,
                    WebsiteName = x.WebsiteId == 1
                    ? "TonerGiant"
                    : x.WebsiteId == 2
                        ? "CartridgeMonkey"
                        : "NetGiant",
                    WebsiteLogoUrl = x.WebsiteId == 1
                    ? "../Content/images/TG-Favicon.jpg"
                    : x.WebsiteId == 2
                        ? "../Content/images/CM-Favicon.jpg"
                        : "../Content/images/NG-Favicon.jpg"
                })
                .AsQueryable();
        }


        public static string CreateEquipmentURL(string EquipmentName, int Website, string Env, string EquipmentType)
        {
            if (string.IsNullOrEmpty(EquipmentName) || string.IsNullOrEmpty(EquipmentType))
            {
                return "";
            }
            string qwe = "https://" + Env;
            qwe += Website == 1 ? "tonergiant.co.uk/model/" : "cartridgemonkey.com/model/" ;
            qwe += EquipmentName.Replace(" ", "-") + "-" + EquipmentType.Replace(" ", "-").ToLower() + "/";

            return qwe;
        }

        public QAViewModel Get(int? page, string search, string searchBy, string orderBy, int? granularityID)
        {
            int pagesize = 25;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    var list = db.qa_Main
                        .Include(p => p.qa_WebsiteMapping)
                        .Include(p => p.qa_Granularity)
                        .Include(p => p.eqEquipment)
                        .Include(p => p.product.AxisFields);

                    if (!string.IsNullOrEmpty(search) || (!string.IsNullOrEmpty(searchBy) && searchBy.Equals("unAns")))
                    {
                        switch (searchBy)
                        {
                            case "altRef":
                                list = list.Where(x => x.product.partNo.ToLower().Contains(search.ToLower().Trim()));
                                break;

                            case "question":
                                list = list.Where(x => x.Question.ToLower().Contains(search.ToLower().Trim()));
                                break;

                            case "answer":
                                list = list.Where(x => x.Answer.ToLower().Contains(search.ToLower().Trim()));
                                break;

                            case "source":
                                list = list.Where(x => x.SourceWebsiteID == db.Website.FirstOrDefault(y => y.WebsiteName.ToLower().Contains(search.ToLower().Trim())).WebsiteID);
                                break;

                            case "unAns":
                                list = list.Where(x => x.Answer.Trim() == "");
                                break;

                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "questionAsc":
                            list = list.OrderBy(x => x.Question);
                            break;
                        case "questionDesc":
                            list = list.OrderByDescending(x => x.Question);
                            break;
                        case "partNoAsc":
                            list = list.OrderBy(x => x.product.partNo);
                            break;
                        case "partNoDesc":
                            list = list.OrderByDescending(x => x.product.partNo);
                            break;
                        case "equipmentAsc":
                            list = list.OrderBy(x => x.eqEquipment.description);
                            break;
                        case "equipmentDesc":
                            list = list.OrderByDescending(x => x.eqEquipment.description);
                            break;
                        case "askedDateAsc":
                            list = list.OrderBy(x => x.AskedDate);
                            break;
                        case "askedDateDesc":
                            list = list.OrderByDescending(x => x.AskedDate);
                            break;
                        case "repliedDateAsc":
                            list = list.OrderBy(x => x.RepliedDate);
                            break;
                        case "repliedDateDesc":
                            list = list.OrderByDescending(x => x.RepliedDate);
                            break;
                        default:
                            list = list.OrderByDescending(x => x.AskedDate);
                            break;
                    }

                    if (granularityID != null && granularityID > 0)
                    {
                        list = list.Where(x => x.GranularityFK == granularityID);
                    }

                    ListOfQAs = list.ToPagedList(pageNumber, pagesize);

                    //Get source website details
                    foreach (qa_Main qa in ListOfQAs)
                    {
                        if (qa.SourceWebsiteID > 0)
                        {
                            qa.SourceWebsite = db.Website.Find(qa.SourceWebsiteID);
                        }
                    }

                    GetAllGranualities();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return this;
        }

        public static QAViewModel Create(int id)
        {
            QAViewModel vModel = new QAViewModel();
            vModel.SelectedWebsites = new List<Website>();
            vModel.SelectedWebsiteIDs = new int[0];

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    //Get the entity based on id
                    if (id == 0)
                    {
                        vModel.QA = new qa_Main();
                    }
                    else
                    {
                        vModel.QA = db.qa_Main.Find(id);
                        //vModel.SelectedGranularityID = vModel.QA.GranularityFK;

                        //Mappings
                        if (vModel.QA.ShowOnAllSites == 1)
                        {
                            vModel.SelectedWebsites = db.Website.ToList();

                            //Delete mapping if exists when show on all sites is true
                            if (db.qa_WebsiteMapping.Any(x => x.QuestionAnswerFK == id))
                            {
                                QAViewModel.DeleteAllMappingsByQuestionID(id);
                            }
                        }
                        else
                        {
                            //Get existing mapping websites
                            foreach (qa_WebsiteMapping mapp in db.qa_WebsiteMapping.Where(x => x.QuestionAnswerFK == id))
                            {
                                vModel.SelectedWebsites.Add(db.Website.FirstOrDefault(x => x.WebsiteID == mapp.WebsiteFK));
                            }
                        }
                    }

                    //Get all sites
                    vModel.AllWebsites = db.Website.ToList().AsQueryable();
                    vModel.AllGranularities = SelectListViewModel.GetAllGranularities();
                    vModel.AllEquipment = SelectListViewModel.GetAllEquipment();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return vModel;
        }

        public void Save(QAViewModel vModel)
        {
            qa_Main qa = new qa_Main();
            qa = vModel.QA;

            if (!qa.RepliedDate.HasValue)
                qa.RepliedDate = DateTime.Now;

            if (string.IsNullOrEmpty(qa.UserFK))
            {
                using (membershipEntities dbMembership = new membershipEntities())
                {
                    qa.UserFK = dbMembership.AspNetUsers.FirstOrDefault(x => x.UserName.Equals(UserName)).Id;
                }
            }

            using (ngmdEntities db = new ngmdEntities())
            {
                qa.Email = (qa.Email ?? "");
                qa.dateLastUpdate = DateTime.Now;

                //Update
                if (vModel.QA.QuestionAnswerID > 0)
                {
                    db.Entry(qa).State = EntityState.Modified;
                }
                //Add
                else
                {
                    qa.AskedDate = DateTime.Now;

                    if (vModel.SelectedWebsiteIDs != null && vModel.SelectedWebsiteIDs.Count() > 0) { qa.ShowOnAllSites = 0; } else { qa.ShowOnAllSites = 1; }

                    db.qa_Main.Add(qa);
                }

                db.SaveChanges();

                //Create mapping if required
                if (vModel.SelectedWebsiteIDs != null && vModel.SelectedWebsiteIDs.Count() > 0 &&
                    vModel.SelectedWebsiteIDs.Count() != db.Website.Count())
                {
                    QAViewModel.CreateMapping(vModel.SelectedWebsiteIDs, qa.QuestionAnswerID);

                    qa.ShowOnAllSites = 0;
                    db.Entry(qa).State = EntityState.Modified;
                    db.SaveChanges();

                }
                else
                {
                    QAViewModel.DeleteAllMappingsByQuestionID(qa.QuestionAnswerID);

                    qa.ShowOnAllSites = 1;
                    db.Entry(qa).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        public SaveReturn Delete(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        qa_Main e = db.qa_Main.Where(x => x.QuestionAnswerID == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public static void DeleteAllMappingsByQuestionID(int qaID)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    List<qa_WebsiteMapping> mappings = db.qa_WebsiteMapping.Where(x => x.QuestionAnswerFK == qaID).ToList();

                    foreach (qa_WebsiteMapping mapp in mappings)
                    {
                        db.qa_WebsiteMapping.Remove(mapp);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static void CreateMapping(int[] WebsiteIDs, int qaID)
        {
            qa_WebsiteMapping mapping = new qa_WebsiteMapping();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    int[] notSeletectedIDs = db.Website.Select(x => x.WebsiteID).Except(WebsiteIDs).ToArray();

                    foreach (int id in WebsiteIDs)
                    {
                        //Don't create mapping if already exists
                        if (!db.qa_WebsiteMapping.Any(x => x.QuestionAnswerFK == qaID && x.WebsiteFK == id))
                        {
                            mapping.QuestionAnswerFK = qaID;
                            mapping.WebsiteFK = id;

                            db.qa_WebsiteMapping.Add(mapping);
                            db.SaveChanges();
                        }
                    }

                    //Delete mapping if exists but not seleceted this time
                    foreach (int id in notSeletectedIDs)
                    {
                        if (db.qa_WebsiteMapping.Any(x => x.QuestionAnswerFK == qaID && x.WebsiteFK == id))
                        {
                            qa_WebsiteMapping mapp = db.qa_WebsiteMapping.FirstOrDefault(x => x.QuestionAnswerFK == qaID && x.WebsiteFK == id);
                            db.qa_WebsiteMapping.Remove(mapp);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void SendCustomerAnsweredEmail(int id)
        {
            qa_Main qaRecord;
            product productRecord;
            eqEquipment equip;
            Lookup cartridgeType;
            Website website;
            string url= "";

            using (ngmdEntities db = new ngmdEntities())
            {
                qaRecord = db.qa_Main.Find(id);
                productRecord = db.product.Include(x => x.AxisFields).FirstOrDefault(x => x.productID == qaRecord.ProductID);
                equip = db.eqEquipment.Find(qaRecord.eqEquipmentFK);
                cartridgeType = equip == null ? null : db.Lookup.FirstOrDefault(x => x.AltLookupId == equip.eqCartridgeTypeFK && x.LookupType.LookupTypeName == "CartridgeType");
                website = db.Website.Find(qaRecord.SourceWebsiteID);
            }

            if (qaRecord != null)
            {
                var body = SharedFunctions.GetConfigurationSetting("PMS", "QaCustomerAnsweredEmail", qaRecord.SourceWebsiteID);
                var supportEmail = SharedFunctions.GetConfigurationSetting("Website Application Variables", "helpEmailAddress", qaRecord.SourceWebsiteID);

                if (productRecord != null)
                {
                    url = SharedFunctions.CleanupProductURL(productRecord.productName + "-" +
                                                            productRecord.partNo + "-" + productRecord.AxisFields.stockReference, qaRecord.SourceWebsiteID);
                }
                if (equip != null)
                {
                    url = "https://" + website.WebURL + "/model/" + equip.description.Replace(" ", "-") + "-" + cartridgeType.LookupName.Replace(" ", "-").ToLower();
                }

                var replacements = new Dictionary<string, string>
                {
                    {"[productdescription]", productRecord != null ? productRecord.productName : equip.description},
                    {"[question]", qaRecord.Question},
                    {"[link]", "<a href=" + url + " style='font-size:12.0pt;color:#558ED5;mso-style-textfill-fill-color:#558ED5;'>Click here to view our response.</a>"},
                    {"[answer]", qaRecord.Answer}
                };

                body = SharedFunctions.DoReplacements(body, replacements);

                EmailUtilities.SendEmail("Your Question has been Answered", body, true, 
                    MailPriority.Normal, new List<string>{qaRecord.Email}, supportEmail);
            }
        }
        
        private void GetAllGranualities()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AllGranularities = db.qa_Granularity
                    .OrderBy(x => x.SearchTitle)
                    .Select(x => new SelectListItem
                {
                    Value = x.GranularityID.ToString(),
                    Text = x.SearchTitle
                }).ToList().AsQueryable();
            }
        }

        public class TelerikQA
        {
            public int Id { get; set; }
            public string Question { get; set; }
            public string ProductID { get; set; }
            public string ProductUrl { get; set; }
            public string EquipmentName { get; set; }
            public string EquipmentURL { get; set; }
            public DateTime? AskedDate { get; set; }
            public DateTime? RepliedDate { get; set; }
            public string RepliedBy { get; set; }
            public string WebsiteName { get; set; }
            public string WebsiteLogoUrl { get; set; }
            public string Granularity { get; set; }
            public string EquipmentType { get; set; }
        }
    }
}