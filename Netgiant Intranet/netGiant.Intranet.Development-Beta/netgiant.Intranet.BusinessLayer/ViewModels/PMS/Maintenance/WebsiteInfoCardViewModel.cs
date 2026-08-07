using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    // Manages dbo.WebsiteInfoCard - the basket-page sidebar widgets (sale banner,
    // "Free Next Day Delivery", "Trusted By 25,000+", "Exclusive Trade Pricing", etc),
    // one table discriminated by Category. Mirrors the CRUD pattern used by
    // CMSViewModel/CMSController for CMS Entries.
    public class WebsiteInfoCardViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public WebsiteInfoCardViewModel()
        {
            _ctx = new ngmdEntities();
        }

        // Keep this list in sync with the Category values seeded/used in the
        // Ecommerce site's BasketDetails.cshtml rendering.
        public static readonly List<string> Categories = new List<string>
        {
            "Banner",
            "Delivery",
            "Trust",
            "TradePricing"
        };

        public WebsiteInfoCard InfoCard { get; set; }
        public List<SelectListItem> WebsiteNameList { get; set; }
        public List<SelectListItem> CategoryList { get; set; }
        public IQueryable<TelerikInfoCard> InfoCardList { get; set; }

        public void GetInfoCardList()
        {
            InfoCardList = _ctx.WebsiteInfoCard
                .Select(x => new TelerikInfoCard
                {
                    Id = x.WebsiteInfoCardId,
                    Website = x.Website.FriendlyName,
                    Category = x.Category,
                    Title = x.Title,
                    BodyText = x.BodyText,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                })
                .AsQueryable();
        }

        public WebsiteInfoCardViewModel CreateEntry(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    InfoCard = db.WebsiteInfoCard
                        .Include(x => x.Website)
                        .Where(x => x.WebsiteInfoCardId == id).FirstOrDefault();
                }
            }
            else
            {
                InfoCard = new WebsiteInfoCard
                {
                    IsActive = true,
                    DisplayOrder = 0
                };
            }
            SetupSelectLists();

            return this;
        }

        public bool SaveEntry(string userName)
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (InfoCard.WebsiteInfoCardId > 0)
                    {
                        // Load the real row instead of attaching the posted object directly.
                        // The edit form has no fields for CreatedDate/CreatedBy, so a freshly
                        // model-bound InfoCard would have those at CLR defaults (CreatedDate =
                        // 0001-01-01) - marking THAT whole object Modified would try to write
                        // 0001-01-01 into a SQL DATETIME column (valid range starts 1753-01-01)
                        // and SaveChanges() would throw. Copying just the editable fields onto
                        // the tracked, DB-loaded entity avoids touching CreatedDate/CreatedBy at all.
                        WebsiteInfoCard existing = db.WebsiteInfoCard
                            .Where(x => x.WebsiteInfoCardId == InfoCard.WebsiteInfoCardId)
                            .FirstOrDefault();

                        if (existing == null)
                        {
                            throw new ApplicationException("Info card not found - it may have been deleted by someone else.");
                        }

                        existing.WebsiteId = InfoCard.WebsiteId;
                        existing.Category = InfoCard.Category;
                        existing.IconClass = InfoCard.IconClass;
                        existing.Title = InfoCard.Title;
                        existing.BodyText = InfoCard.BodyText;
                        existing.FindOutMoreContent = InfoCard.FindOutMoreContent;
                        existing.ImageUrl = InfoCard.ImageUrl;
                        existing.LinkUrl = InfoCard.LinkUrl;
                        existing.DisplayOrder = InfoCard.DisplayOrder;
                        existing.IsActive = InfoCard.IsActive;
                        existing.ModifiedDate = DateTime.Now;
                        existing.ModifiedBy = userName;
                    }
                    else
                    {
                        InfoCard.CreatedDate = DateTime.Now;
                        InfoCard.CreatedBy = userName;
                        db.Entry(InfoCard).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public SaveReturn DeleteEntry(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        WebsiteInfoCard e = db.WebsiteInfoCard.Where(x => x.WebsiteInfoCardId == id).FirstOrDefault();
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

        #region Utilities
        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            CategoryList = Categories.Select(c => new SelectListItem { Value = c, Text = c }).ToList();
        }

        public List<SelectListItem> GetWebsiteNames()
        {
            List<SelectListItem> oList;

            using (ngmdEntities db = new ngmdEntities())
            {
                oList = db.Website.OrderBy(x => x.FriendlyName).Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.FriendlyName
                }).ToList();
            }
            return oList;
        }
        #endregion

        #region Telerik Classes
        public class TelerikInfoCard
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string Category { get; set; }
            public string Title { get; set; }
            public string BodyText { get; set; }
            public int DisplayOrder { get; set; }
            public bool IsActive { get; set; }
        }
        #endregion
    }
}
