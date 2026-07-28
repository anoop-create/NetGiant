using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher
{
    public class PromotionalVouchersViewModel : HelperViewModel
    {
        public PromotionalVouchersViewModel()
        {
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;
        public VoucherPromo pvouch { get; set; }
        public bool IsCustomerVoucher { get; set; } = false;
        public bool IsGlobal { get; set; } = false;
        public bool IsUsed { get; set; } = false;
        public bool IsSingleUse { get; set; } = false;
        public bool ForGeneralUse { get; set; } = false;
        public bool IsForTrade { get; set; } = false;
        public bool IsForCustomer { get; set; } = false;
        public bool SendEmail { get; set; } = false;
        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string CustomerEmail { get; set; }

        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllVoucherTypes { get; set; }
        public IQueryable<SelectListItem> AllVoucherPromoGroups { get; set; }

        public IQueryable<TelerikPromotionalVouchers> PromotionalVouchersList { get; set; }
        public IQueryable<TelerikPromotionalVouchers> CustomerVoucherList { get; set; }

        public void GetPromotionalVouchers()
        {
            PromotionalVouchersList = GetPromotionalVouchers(w => w.AccountNumber == null && !w.IsSingleUse);
        }

        public void GetCustomerVouchers()
        {
            CustomerVoucherList = GetPromotionalVouchers(w => w.AccountNumber != null || w.IsSingleUse);
        }

        public IQueryable<TelerikPromotionalVouchers> GetPromotionalVouchers(Func<VoucherPromo, bool> where)
        {
            List<Lookup> lvt = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "VoucherType");
            return _ctx.VoucherPromo
                .Where(where)
                .Select(x => new TelerikPromotionalVouchers
                {
                    PromotionalVoucherId = x.VoucherPromoId,
                    PromotionalVoucherCode = x.VoucherCode,
                    PromotionalVoucherAccountNo = x.AccountNumber,
                    PromotionalVoucherIsGlobal = x.IsGlobal,
                    PromotionalVoucherIsUsed = x.IsUsed,
                    PromotionalVoucherIsSingleUse = x.IsSingleUse,
                    PromotionalVoucherForGeneralUse = x.ForGeneralUse,
                    PromotionalVoucherDescription = x.Description,
                    PromotionalVoucherValidFrom = x.ValidFrom,
                    PromotionalVoucherValidTo = x.ValidTo,
                    PromotionalVoucherStockRef = x.StockRef,
                    PromotionalVoucherMinBasketValue = x.MinBasketValue,
                    PromotionalVoucherMinQualValue = x.MinQualValue,
                    PromotionalVoucherAmount = x.Amount ?? 0,
                    PromotionalVoucherPercentage = x.Percentage ?? 0,
                    PromotionalVoucherGiftStockRef = x.GiftStockRef,
                    PromotionalVoucherMultiBuyQualNo = x.MultiBuyQualNo,
                    PromotionalVoucherMultiBuyNoDiscounted = x.MultiBuyNoDiscounted,

                    // from DB foreign keys
                    PromotionalVoucherPromoGroup = x.VoucherPromoGroup.GroupName,
                    PromotionalVoucherType = lvt.Find(y => y.AltLookupId == x.VoucherTypeFk).LookupName,
                    PromotionalVoucherWebsite = x.Website.FriendlyName
                })
                .AsQueryable();
        }

        public class TelerikPromotionalVouchers
        {
            public int PromotionalVoucherId { get; set; }
            public string PromotionalVoucherCode { get; set; }
            public string PromotionalVoucherAccountNo { get; set; }
            public bool? PromotionalVoucherIsGlobal { get; set; }
            public bool? PromotionalVoucherIsUsed { get; set; }
            public bool PromotionalVoucherIsSingleUse { get; set; }
            public bool? PromotionalVoucherForGeneralUse { get; set; }
            public string PromotionalVoucherDescription { get; set; }
            public DateTime PromotionalVoucherValidFrom { get; set; }
            public DateTime PromotionalVoucherValidTo { get; set; }
            public string PromotionalVoucherStockRef { get; set; }
            public decimal PromotionalVoucherMinBasketValue { get; set; }
            public decimal PromotionalVoucherMinQualValue { get; set; }
            public decimal PromotionalVoucherAmount { get; set; }
            public decimal PromotionalVoucherPercentage { get; set; }
            public string PromotionalVoucherGiftStockRef { get; set; }
            public int? PromotionalVoucherMultiBuyQualNo { get; set; }
            public int? PromotionalVoucherMultiBuyNoDiscounted { get; set; }
            public string PromotionalVoucherPromoGroup { get; set; }
            public string PromotionalVoucherType { get; set; }
            public string PromotionalVoucherWebsite { get; set; }
        }

        public static PromotionalVouchersViewModel Create(int id)
        {
            PromotionalVouchersViewModel model = new PromotionalVouchersViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id == 0)
                    {
                        model.pvouch = new VoucherPromo();

                        model.pvouch.ValidFrom = DateTime.Now.Date;
                        model.pvouch.ValidTo = model.pvouch.ValidFrom;
                        model.pvouch.VoucherTypeFk = -1; // avoid percentage (PK = 0) being selected by default
                    }
                    else
                    {
                        model.pvouch = db.VoucherPromo.Where(x => x.VoucherPromoId == id).First();
                        model.IsUsed = model.pvouch.IsUsed;
                        model.IsSingleUse = model.pvouch.IsSingleUse;
                        model.IsGlobal = model.pvouch.IsGlobal;
                        model.ForGeneralUse = model.pvouch.ForGeneralUse;
                        model.IsCustomerVoucher = string.IsNullOrEmpty(model.pvouch.AccountNumber) ? false : true;
                        model.IsForCustomer = model.pvouch.IsForCustomer;
                        model.IsForTrade = model.pvouch.IsForTrade;
                    }

                    model.AllWebsites = SelectListViewModel.GetAllWebsites();
                    model.AllVoucherTypes = SelectListViewModel.GetNgmdLookupSelectList("VoucherType");
                    model.AllVoucherPromoGroups = SelectListViewModel.GetAllVoucherPromoGroups();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public SaveReturn Save()
        {
            var sr = new SaveReturn();

            pvouch.IsGlobal = IsGlobal;
            pvouch.IsSingleUse = IsSingleUse;
            pvouch.IsUsed = IsUsed;
            pvouch.ForGeneralUse = ForGeneralUse;
            pvouch.IsForCustomer = IsForCustomer;
            pvouch.IsForTrade = IsForTrade;

            // check here if in DB, there is another voucher of same code and same website
            using (ngmdEntities db = new ngmdEntities())
            {
                if (db.VoucherPromo.Where(x =>
                                            x.VoucherCode == pvouch.VoucherCode &&
                                            x.WebsiteFk == pvouch.WebsiteFk &&
                                            x.VoucherPromoId != pvouch.VoucherPromoId) // pvouch.VoucherPromoId could be 0 on create
                                            .FirstOrDefault() != null)
                {
                    sr.IsSuccess = false;
                    sr.Message = "There is already a voucher with this voucher code and website";
                    return sr;
                }
            }

            try
            {
                if (pvouch.VoucherPromoId > 0)
                {
                    UpdateVoucher();
                }
                else
                {
                    AddNewVoucher();
                }

                if (SendEmail && !string.IsNullOrEmpty(CustomerEmail))
                {
                    string emailBody = GetEmailBody();

                    var supportEmail = SharedFunctions.GetConfigurationSetting("Website Application Variables", "supportEmailAddress", pvouch.WebsiteFk);

                    EmailUtilities.SendEmail("Customer Voucher", emailBody, true, MailPriority.Normal, new List<string> { CustomerEmail }, supportEmail);
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            sr.IsSuccess = true;
            return sr;
        }

        private string GetEmailBody()
        {
            string body;
            string websiteUrl;

            using (ngmdEntities db = new ngmdEntities())
            {
                body = db.cmsEntry
                              .Where(w => w.cmsSection.sectionName == "EmailData" && w.entryName == "CustomerVoucher" && w.cmsSection.websiteFK == pvouch.WebsiteFk)
                              .FirstOrDefault()
                              .cmsContent;

                websiteUrl = db.Website
                               .Where(w => w.WebsiteID == pvouch.WebsiteFk)
                               .FirstOrDefault()
                               .WebURL;
            }

            var replacements = new Dictionary<string, string>();
            replacements.Add("[vouchernumber]", pvouch.VoucherCode);
            replacements.Add("[url]", "https://" + websiteUrl + "/cvoucher/" + pvouch.VoucherCode);
            replacements.Add("[voucheramount]", Convert.ToString(pvouch.Amount));

            return SharedFunctions.DoReplacements(body, replacements);
        }

        private void AddNewVoucher()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(pvouch).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdateVoucher()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(pvouch).State = EntityState.Modified;
                db.SaveChanges();
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
                        VoucherPromo VoucherPromo = db.VoucherPromo.Find(id);
                        db.VoucherPromo.Remove(VoucherPromo);
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

        public List<SelectListItem> GetPromotionalGroups(int id)
        {
            List<SelectListItem> li = new List<SelectListItem>();

            using (var db = new ngmdEntities())
            {
                li = db.VoucherPromoGroup
                                             .Where(w => w.WebsiteFk == id)
                                             .Select(x => new SelectListItem
                                             {
                                                 Text = x.GroupName,
                                                 Value = x.VoucherPromoGroupId.ToString()
                                             })
                                             .ToList();
            }

            return li;
        }

        public string GetStockRef(int id)
        {
            using (var db = new ngmdEntities())
            {
                return db.configurationSetting
                              .Where(w => w.websiteFK == id && w.settingName == "CustomerVoucherStockRef")
                              .Select(x => x.settingValue)
                              .FirstOrDefault();
            }
        }
    }
}
