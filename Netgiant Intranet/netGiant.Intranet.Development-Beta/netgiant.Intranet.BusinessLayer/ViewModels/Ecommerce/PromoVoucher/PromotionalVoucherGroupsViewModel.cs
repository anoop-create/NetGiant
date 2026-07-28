using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher
{
    public class PromotionalVoucherGroupsViewModel : HelperViewModel
    {
        public PromotionalVoucherGroupsViewModel()
        {
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;
        public VoucherPromoGroup pvouchgrp { get; set; }
        public IQueryable<TelerikPromotionalVoucherGroups> PromotionalVoucherGroupsList { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }

        public PromotionalVoucherGroupsViewModel GetPromotionalVoucherGroups()
        {
            PromotionalVoucherGroupsList = _ctx.VoucherPromoGroup
                .Select(x => new TelerikPromotionalVoucherGroups
                {
                    PromotionalVoucherGroupWebsite = x.Website.FriendlyName,
                    PromotionalVoucherGroupId = x.VoucherPromoGroupId,
                    PromotionalVoucherGroupName = x.GroupName
                })
                .AsQueryable();

            return this;
        }

        public class TelerikPromotionalVoucherGroups
        {
            public string PromotionalVoucherGroupWebsite { get; set; }
            public int PromotionalVoucherGroupId { get; set; }
            public string PromotionalVoucherGroupName { get; set; }
        }

        public static PromotionalVoucherGroupsViewModel Create(int id)
        {
            PromotionalVoucherGroupsViewModel model = new PromotionalVoucherGroupsViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id == 0)
                    {
                        model.pvouchgrp = new VoucherPromoGroup();
                    }
                    else
                    {
                        model.pvouchgrp = db.VoucherPromoGroup.Where(x => x.VoucherPromoGroupId == id).First();
                    }

                    model.AllWebsites = SelectListViewModel.GetAllWebsites();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void Save()
        {
            try
            {
                if (pvouchgrp.VoucherPromoGroupId > 0)
                {
                    UpdatePromotionalVoucherGroup();
                }
                else
                {
                    AddNewPromotionalVoucherGroup();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        private void AddNewPromotionalVoucherGroup()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(pvouchgrp).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdatePromotionalVoucherGroup()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(pvouchgrp).State = EntityState.Modified;
                db.SaveChanges();
            }
        }


        public void Delete(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    VoucherPromoGroup pvouchgrp = db.VoucherPromoGroup.Find(id);
                    db.VoucherPromoGroup.Remove(pvouchgrp);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
    }
}
