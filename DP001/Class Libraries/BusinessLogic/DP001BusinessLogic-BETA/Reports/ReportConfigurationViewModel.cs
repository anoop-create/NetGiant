using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class ReportConfigurationViewModel
    {
        public ReportConfigurationViewModel(int channelId)
        {
            _channelId = channelId;
        }

        public ReportConfigurationViewModel()
        {

        }

        public List<ReportConfiguration> ReportConfigList { get; set; }
        public ReportConfiguration ReportConfig { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public string CurrentUserEmail { get; set; }
        private int _channelId;

        public ReportConfigurationViewModel GetReportLinks(int tenantFk, string userId)
        {
            ReportConfigList = CrudReportConfiguration.Read(x =>
                (x.Lookup.LookupName == "Shared" && x.TenantFk == tenantFk) ||
                (x.Lookup.LookupName == "Private" && x.UserId == userId && x.TenantFk == tenantFk));

            return this;
        }

        public SaveReturn Create()
        {
            var saveReturn = new SaveReturn();

            try
            {
                saveReturn.ReturnData = new ExpandoObject();
                saveReturn.ReturnData.ReportConfigId = CrudReportConfiguration.Create(ReportConfig).ReportConfigurationId;
                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }

        public ReportConfigurationViewModel Edit(int id)
        {
            ReportConfig = CrudReportConfiguration.Read(x => x.ReportConfigurationId == id).FirstOrDefault();
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");

            return this;
        }

        public SaveReturn Update()
        {
            var saveReturn = new SaveReturn();

            try
            {
                CrudReportConfiguration.Update(ReportConfig);
                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }

        public SaveReturn Delete()
        {
            var saveReturn = new SaveReturn();

            try
            {
                CrudReportConfiguration.Delete(ReportConfig);
                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }
    }
}
