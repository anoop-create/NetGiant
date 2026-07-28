using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.ViewModels
{
    public class FieldMappingsViewModel
    {
        public FieldMappingsViewModel(int channelId)
        {
            _channelId = channelId;
        }
        public FieldMappingsViewModel()
        {

        }

        public FTPSetting FtpSetting { get; set; }
        private int _channelId;

        public FieldMappingsViewModel GetFieldMappings(int ftpSettingsId)
        {
            var crudFtp = new CrudFtpSetting();

            var isValid = crudFtp.Read(x => x.ChannelFK == _channelId
                && x.FTPSettingsID == ftpSettingsId).Count > 0;

            if (isValid)
            {
                FtpSetting = crudFtp.ReadByKey(ftpSettingsId);
            }
            else
            {
                FtpSetting = new FTPSetting();
            }

            return this;
        }

        public void Update()
        {
            var crudFtp = new CrudFtpSetting();
            var crudMappings = new CrudFieldMappings();

            var isValid = crudFtp.Read(x => x.ChannelFK == FtpSetting.ChannelFK
                && x.FTPSettingsID == FtpSetting.FieldMapping.FTPSettingsFK).Count > 0;

            if (isValid)
                crudMappings.Update(FtpSetting.FieldMapping);
        }
    }
}
