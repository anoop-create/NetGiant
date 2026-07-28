using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin
{
    public class AdminViewModel
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

    public class MenuViewModel
    {
        public IEnumerable<SelectListItem> AllLinkLevels { get; set; }
        public IQueryable<SelectListItem> ParentMenuItems { get; set; }
        public List<actionLink> MenuItems { get; set; }
        public actionLink ActionLink { get; set; }

        public MenuViewModel Get()
        {
            return Get(null);
        }

        public MenuViewModel Get(int? page)
        {
            //int pageSize = 100;
            //int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    MenuItems = db.actionLinks.OrderBy(x => x.parentLevelID).ToList();
                    //MenuItems = list.ToList();

                    foreach (actionLink item in MenuItems)
                    {
                        if (item.parentLevelID != 0)
                        {
                            item.parentLevelText = MenuItems.FirstOrDefault(i => i.actionLinkID == item.parentLevelID).actionLinkDesc;
                            int parentLevel = 99;
                            actionLink parentActionLink = item;
                            while (parentLevel != 1)
                            {
                                parentActionLink = MenuItems.FirstOrDefault(i => i.actionLinkID == parentActionLink.parentLevelID);
                                item.topParent = parentActionLink.actionLinkDesc;
                                parentLevel = parentActionLink.actionLinkLevel;
                            }

                        }
                        else
                        {
                            item.parentLevelText = "None";
                            item.topParent = item.actionLinkDesc;
                        }
                    }
                }
                MenuItems = MenuItems.OrderBy(x => x.topParent).ThenBy(x => x.actionLinkLevel).ToList();
                //MenuItems = list.ToPagedList(pageNumber, pageSize);
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
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
        public bool DeleteMenuItem(int menuId)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    actionLink al = db.actionLinks.Find(menuId);
                    db.actionLinks.Remove(al);
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
    }
}