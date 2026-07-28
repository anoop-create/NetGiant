using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using static netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance.CMSViewModel;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class FaqViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public Faq Faq { get; set; }
        public List<TelerikFaq> FaqList { get; set; }

        public IQueryable<SelectListItem> AllPageScopes { get; set; }
        public IQueryable<SelectListItem> AllCategoryScopes { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllCartridgeTypes { get; set; }
        public IQueryable<SelectListItem> AllManufacturers { get; set; }

        public FaqViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public FaqViewModel GetFaqs()
        {
            FaqList = _ctx.Faq
                .Select(x => new TelerikFaq
                {
                    Id = x.FaqId,
                    Website = x.Website.FriendlyName,
                    PageScope = x.Lookup.LookupName,
                    CategoryScope = x.Lookup1.LookupName,
                    Product = x.product.productName,
                    PartNumber = x.product.partNo,
                    Manufacturer = x.Lookup.LookupName == "Product Page" ? x.product != null ? x.product.manufacturer.manufacturerName : ""
                    : x.Lookup.LookupName == "Model Page" ? x.eqEquipment != null ? x.eqEquipment.manufacturer.manufacturerName : ""
                    : x.ManufacturerFk != null ? x.manufacturer.manufacturerName : "",
                    CartridgeType = x.Lookup.LookupName == "Product Page" ? x.product != null ?
                            (_ctx.Lookup
                                .Where(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.product.eqProductMembership.FirstOrDefault().eqEquipment.eqCartridgeTypeFK)
                                .AsQueryable()
                                .FirstOrDefault()
                                .LookupName) : ""
                    : x.Lookup.LookupName == "Model Page" ? x.eqEquipment != null ?
                            (_ctx.Lookup
                                .Where(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.eqEquipment.eqCartridgeTypeFK)
                                .AsQueryable()
                                .FirstOrDefault()
                                .LookupName) : ""
                    : x.CartridgeTypeFk != null ?
                            (_ctx.Lookup
                                .Where(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.CartridgeTypeFk)
                                .AsQueryable()
                                .FirstOrDefault()
                                .LookupName) : "",
                    Model = x.eqEquipment.description,
                    Question = x.Question,
                    Answer = x.Answer,
                    IsActive = x.IsActive.ToString(),
                    GenerateSchema = x.GenerateSchema.ToString(),
                    Priority = x.Priority
                })
            .ToList();

            return this;
        }

        public FaqViewModel CreateFaq(int id)
        {
            AllPageScopes = SelectListViewModel.GetNgmdLookupSelectList("FAQ Page Scope", false, false);
            AllCategoryScopes = SelectListViewModel.GetNgmdLookupSelectList("FAQ Category Scope", false, false);
            AllCartridgeTypes = SelectListViewModel.GetNgmdLookupSelectList("CartridgeType");
            AllManufacturers = SelectListViewModel.GetAllEquipManufacturers();
            AllWebsites = SelectListViewModel.GetAllWebsites();

            AllPageScopes.Where(x => x.Text == "Universal").FirstOrDefault().Selected = true;

            if (id > 0)
            {
                Faq = _ctx.Faq
                     .Where(x => x.FaqId == id).FirstOrDefault();
            }
            else
            {
                Faq = new Faq();
                Faq.PageScopeFk = Int32.Parse(AllPageScopes.Where(x => x.Text == "Universal").FirstOrDefault().Value);
            }

            return this;
        }

        public SaveReturn SaveFaq()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {

                    if (Faq.FaqId > 0)
                    {
                        db.Entry(Faq).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(Faq).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return sr;
        }

        public SaveReturn DeleteFaq(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        Faq f = db.Faq.Where(x => x.FaqId == id).FirstOrDefault();
                        db.Entry(f).State = EntityState.Deleted;

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

        private string GetManufacturer(product p, eqEquipment eq)
        {
            if (p != null)
            {
                return p.manufacturer.manufacturerName;
            }
            if (eq != null)
            {
                return eq.manufacturer.manufacturerName;
            }
            return "";
        }

        public class TelerikFaq
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string PageScope { get; set; }
            public string CategoryScope { get; set; }
            public string Product { get; set; }
            public string PartNumber { get; set; }
            public string Manufacturer { get; set; }
            public string CartridgeType { get; set; }
            public string Model { get; set; }
            public string Question { get; set; }
            public string Answer { get; set; }
            public string IsActive { get; set; }
            public string GenerateSchema { get; set; }
            public int Priority { get; set; }
            }
    }
}
