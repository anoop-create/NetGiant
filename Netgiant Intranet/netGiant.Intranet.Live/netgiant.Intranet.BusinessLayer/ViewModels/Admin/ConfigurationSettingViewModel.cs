using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin
{
    public class ConfigurationSettingViewModel
    {
        public List<configurationSetting> SettingList { get; set; }
        public configurationSetting Setting { get; set; }
        public int ConfigurationSettingsCount { get; set; }
        public IQueryable<SelectListItem> WebsiteList { get; set; }
        public IQueryable<SelectListItem> SectionList { get; set; }


        public ConfigurationSettingViewModel Get()
        {
            return Get(null, null, null, null, null, 1);
        }

        public ConfigurationSettingViewModel Get(string orderBy, string searchTerm, string searchBy, 
            int? websiteID, string sectionName, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<configurationSetting> query = db.configurationSetting
                    .Include(x => x.Website);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "name":
                            query = query.Where(x => x.settingName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "value":
                            query = query.Where(x => x.settingValue.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "description":
                            query = query.Where(x => x.description.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if(!string.IsNullOrEmpty(sectionName))
                {
                    query = query.Where(x => x.sectionName == sectionName);
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID);
                }

                switch (orderBy)
                {
                    case "sectionAsc":
                        query = query.OrderBy(x => x.sectionName)
                            .ThenBy(x => x.settingName);
                        break;
                    case "sectionDesc":
                        query = query.OrderByDescending(x => x.sectionName)
                            .ThenByDescending(x => x.settingName);
                        break;
                    case "nameAsc":
                        query = query.OrderBy(x => x.settingName);
                        break;
                    case "nameDesc":
                        query = query.OrderByDescending(x => x.settingName);
                        break;
                    case "valueAsc":
                        query = query.OrderBy(x => x.settingValue);
                        break;
                    case "valueDesc":
                        query = query.OrderByDescending(x => x.settingValue);
                        break;
                    case "descriptionAsc":
                        query = query.OrderBy(x => x.description);
                        break;
                    case "descriptionDesc":
                        query = query.OrderByDescending(x => x.description);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "dateLastUpdateAsc":
                        query = query.OrderBy(x => x.dateLastUpdate);
                        break;
                    case "dateLastUpdateDesc":
                        query = query.OrderByDescending(x => x.dateLastUpdate);
                        break;
                    default:
                        query = query.OrderBy(x => x.sectionName)
                            .ThenBy(x => x.settingName);
                        break;
                }

                ConfigurationSettingsCount = query.Count();
                SettingList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                WebsiteList = SelectListViewModel.AllWebsites();
                SectionList = GetSectionNames();
            }

            return this;
        }

        public ConfigurationSettingViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Setting = db.configurationSetting.Find(id);
                }
            }
            else
            {
                Setting = new configurationSetting();
            }

            WebsiteList = SelectListViewModel.AllWebsites();

            return this;
        }

        public void Save()
        {
            Setting.dateLastUpdate = DateTime.Now;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (Setting.configurationSettingID > 0)
                    {
                        db.Entry(Setting).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(Setting).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public void Delete(int configurationSettingID)
        {
            try
            {
                if (configurationSettingID > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        configurationSetting cs = db.configurationSetting.Find(configurationSettingID);
                        db.Entry(cs).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        private IQueryable<SelectListItem> GetSectionNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.configurationSetting.OrderBy(x => x.sectionName).Select(x => new SelectListItem
                {
                    Value = x.sectionName.ToString(),
                    Text = x.sectionName.ToString()
                }).Distinct().ToList().AsQueryable();
            }
            return query;
        }

    }
}
