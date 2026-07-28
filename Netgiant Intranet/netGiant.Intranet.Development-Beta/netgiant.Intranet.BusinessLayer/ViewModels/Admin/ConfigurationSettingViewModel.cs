using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Linq;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin
{
    public class ConfigurationSettingViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ConfigurationSettingViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public configurationSetting ConfigurationSetting { get; set; }
        public IQueryable<SelectListItem> WebsiteList { get; set; }
        public IQueryable<TelerikConfigurationSetting> ConfigurationSettingList { get; set; }

        public void GetConfigurationSettingList()
        {
            ConfigurationSettingList = _ctx.configurationSetting.Select(x => new TelerikConfigurationSetting
            {
                Id = x.configurationSettingID,
                Section = x.sectionName,
                Name = x.settingName,
                Value = x.settingValue,
                Description = x.description,
                Website = x.Website.FriendlyName ?? "N/A",
                DateLastUpdated = x.dateLastUpdate
            })
            .AsQueryable();
        }

        public ConfigurationSettingViewModel CreateConfigurationSetting(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ConfigurationSetting = db.configurationSetting.Find(id);
                }
            }
            else
            {
                ConfigurationSetting = new configurationSetting();
            }

            WebsiteList = SelectListViewModel.GetAllWebsites();

            return this;
        }

        public bool Save()
        {
            bool success = true;
            ConfigurationSetting.dateLastUpdate = DateTime.Now;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (ConfigurationSetting.configurationSettingID > 0)
                    {
                        db.Entry(ConfigurationSetting).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckConfigurationSettingExists(db);
                        db.Entry(ConfigurationSetting).State = EntityState.Added;
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

        private void CheckConfigurationSettingExists(ngmdEntities db)
        {
            var result = db.configurationSetting.Where(w =>
                                                        w.websiteFK == ConfigurationSetting.websiteFK &&
                                                        w.sectionName == ConfigurationSetting.sectionName &&
                                                        w.settingName == ConfigurationSetting.settingName).FirstOrDefault();

            if (result != null) throw new Exception("Configuration Setting already exists with this name");
        }

        public SaveReturn DeleteConfigurationSetting(int id)
        {
            SaveReturn sr = new SaveReturn();
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        configurationSetting s = db.configurationSetting.Where(x => x.configurationSettingID == id).FirstOrDefault();
                        db.Entry(s).State = EntityState.Deleted;
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

        public SaveReturn SetSelectionToTrue(Expression<Func<configurationSetting, bool>> where)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    List<configurationSetting> lcs = db.configurationSetting
                        .Where(where)
                        .ToList();
                    foreach (configurationSetting cs in lcs)
                    {
                        cs.settingValue = "True";
                        db.Entry(cs).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                    sr.IsSuccess = true;
                    sr.Message = "Configuration Settings updated";
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public class TelerikConfigurationSetting
        {
            public int Id { get; set; }
            public string Section { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public string Description { get; set; }
            public string Website { get; set; }
            public DateTime? DateLastUpdated { get; set; }
        }
    }
}
