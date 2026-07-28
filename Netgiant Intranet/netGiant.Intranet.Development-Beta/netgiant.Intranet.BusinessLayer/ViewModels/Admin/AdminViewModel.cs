using System;
using System.Linq;
using System.Collections.Generic;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Web.Mvc;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin
{
    public class AdminViewModel : CommonViewModel
    {
        public AdminViewModel()
        {
            ActionLinks = new List<actionLink>();
        }

        public List<actionLink> ActionLinks { get; set; }

        public AdminViewModel Get()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ActionLinks = db.actionLinks.ToList();
            }

            return this;
        }
    }

    public class MenuViewModel : CommonViewModel
    {
        public MenuViewModel()
        {
            _ctx = new ngmdEntities();
        }
        private ngmdEntities _ctx;

        public IEnumerable<SelectListItem> AllLinkLevels { get; set; }
        public IQueryable<SelectListItem> ParentMenuItems { get; set; }
        public List<actionLink> MenuItems { get; set; }
        public actionLink ActionLink { get; set; }
        public List<Telerik> MenuList { get; set; }

        public void GetMenuList()
        {
            MenuList = _ctx.actionLinks
                .Select(x => new Telerik
                {
                    Id = x.actionLinkID,
                    ParentId = x.parentLevelID,
                    TopParent = "", //x.topParent,
                    Parent = "", //x.parentLevelText,
                    LinkTitle = x.actionLinkDesc,
                    LinkLevel = x.actionLinkLevel,
                    LinkUrl = x.actionLinkURL,
                    ActionName = x.actionName,
                    ControllerName = x.controllerName,
                    Area = x.area,
                    Roles = x.roles,
                    Active = x.active
                }).ToList();

            foreach (Telerik item in MenuList)
            {
                if (item.ParentId != 0)
                {
                    item.Parent = MenuList.FirstOrDefault(i => i.Id == item.ParentId).LinkTitle;
                    int parentLevel = 99;
                    Telerik parentActionLink = item;
                    while (parentLevel != 1)
                    {
                        parentActionLink = MenuList.FirstOrDefault(i => i.Id == parentActionLink.ParentId);
                        item.TopParent = parentActionLink.LinkTitle;
                        parentLevel = parentActionLink.LinkLevel;
                    }
                }
                else
                {
                    item.Parent = "None";
                    item.TopParent = item.LinkTitle;
                }
            }
        }

        public MenuViewModel GetMenuDetails(int menuId)
        {
            if (menuId == 0)
            {
                ActionLink = new actionLink();
            }
            else
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ActionLink = db.actionLinks
                        .Where(x => x.actionLinkID == menuId)
                        .FirstOrDefault();
                }
            }
            GetAllLinkLevels();

            return this;
        }

        public Dictionary<string, string> GetParentItems(int childLevel)
        {
            return null;
        }

        public void GetAllLinkLevels()
        {
            List<SelectListItem> items = new List<SelectListItem>();

            items.Add(new SelectListItem { Text = "Top", Value = "1" });
            items.Add(new SelectListItem { Text = "Side", Value = "2" });
            items.Add(new SelectListItem { Text = "Detail", Value = "3" });

            AllLinkLevels = items;
        }

        public void GetParentMenuItems(int level)
        {

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<actionLink> list = db.actionLinks.Where(x => x.actionLinkLevel == level);

                    list = list.Where(x => x.actionLinkLevel == level);

                    ParentMenuItems = list.Select(x => new SelectListItem
                    {
                        Value = x.actionLinkID.ToString(),
                        Text = x.actionLinkDesc
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

        }

        /// <summary>
        /// Create or update a menu item
        /// </summary>
        /// <param name="mVm"></param>
        /// <returns></returns>
        public bool SaveMenuItem(MenuViewModel mVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    mVm.ActionLink.dateLastUpdate = DateTime.Now;

                    if (mVm.ActionLink.actionLinkID > 0)
                    {
                        db.Entry(mVm.ActionLink).State = EntityState.Modified;
                    }
                    else
                    {
                        db.actionLinks.Add(mVm.ActionLink);
                    }

                    db.SaveChanges();

                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }

        /// <summary>
        /// Deletes a menu item based on the id
        /// </summary>
        /// <param name="id">The menu Id</param>
        /// <returns></returns>
        public SaveReturn DeleteMenuItem(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        actionLink a = db.actionLinks.Where(x => x.actionLinkID == id).FirstOrDefault();
                        db.Entry(a).State = EntityState.Deleted;
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

        public class Telerik
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public string TopParent { get; set; }
            public string Parent { get; set; }
            public string LinkTitle { get; set; }
            public int LinkLevel { get; set; }
            public string LinkUrl { get; set; }
            public string ActionName { get; set; }
            public string ControllerName { get; set; }
            public string Area { get; set; }
            public string Roles { get; set; }
            public bool Active { get; set; }
        }
    }
}