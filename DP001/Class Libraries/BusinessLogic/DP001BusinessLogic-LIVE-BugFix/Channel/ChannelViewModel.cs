using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class ChannelViewModel
    {
        public ChannelViewModel()
        {

        }

        public ChannelViewModel(int tenantId)
        {
            _tenantId = tenantId;
        }

        public List<Channel> ChannelList { get; set; }
        private int _tenantId;

        public ChannelViewModel GetChannels()
        {
            var crud = new CrudChannel();
            ChannelList = crud.Read(x => x.TenantFK == _tenantId);

            return this;
        }

        public Channel GetChannel(int channelId)
        {
            var crud = new CrudChannel();
            var channel = crud.Read(channelId);

            return channel;
        }
    }
}
