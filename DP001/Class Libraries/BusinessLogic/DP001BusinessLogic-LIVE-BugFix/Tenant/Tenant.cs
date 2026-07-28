using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using DP001BusinessLogic.Pricing;
using System.IO;
using DP001BusinessLogic.CustomRoutines;

namespace DP001BusinessLogic
{
    public class Tenant
    {
        public delegate bool SupplierDelegate(Channel channel);
        public delegate bool ProductDelegate(Channel channel);
        public delegate void OutputPricesDelegate(Channel channel, MemoryStream stream);
        public delegate MemoryStream CreateInMemoryCsvDelegate(List<PriceRuleDetail> priceRules, Channel channel);

        public delegate void CustomDelegate(
            TargetUser targetUser, 
            string targetFunction, 
            Channel channel, 
            object extras);

        public SupplierDelegate LoadSupplierInventory { get; set; }
        public ProductDelegate LoadProductInventory { get; set; }
        public OutputPricesDelegate OutputPrices { get; set; }
        public CreateInMemoryCsvDelegate CreateCsv { get; set; }

        public CustomDelegate CustomRoutines { get; set; }

        public void SetupTenantDelegates(Channel channel)
        {
            switch (channel.TenantSetting.Lookup.LookupName)
            {
                case "SaaS System":
                    LoadSupplierInventory = new SupplierDelegate(Inventories.LoadSupplierDataFromFtp);
                    LoadProductInventory = new ProductDelegate(Inventories.LoadProductDataFromFtp);
                    CreateCsv = new CreateInMemoryCsvDelegate(Engine.CreateInMemoryCsv);
                    OutputPrices = new OutputPricesDelegate(Engine.OutputPrices);
                    break;
                case "SAP Anywhere":
                    LoadSupplierInventory = new SupplierDelegate(Inventories.LoadSapASupplierDataFromApi);
                    LoadProductInventory = new ProductDelegate(Inventories.LoadSapAProductDataFromApi);
                    //OutputPrices = new OutputPricesDelegate(Engine.OutputPricesToApi);
                    CreateCsv = new CreateInMemoryCsvDelegate(Engine.CreateInMemoryCsv);
                    OutputPrices = new OutputPricesDelegate(Engine.OutputPrices);
                    break;
                case "SITC":
                    LoadSupplierInventory = new SupplierDelegate(Inventories.LoadSupplierDataFromFtp);
                    LoadProductInventory = new ProductDelegate(Inventories.LoadProductDataFromFtp);
                    CreateCsv = new CreateInMemoryCsvDelegate(StockInTheChannel.CreateInMemoryCsv);
                    OutputPrices = new OutputPricesDelegate(StockInTheChannel.OutputPricesToSitc);
                    break;
            }

            SetupCustomDelegate(channel);
        }

        private void SetupCustomDelegate(Channel channel)
        {
            if (channel.TenantSetting.Description == "Netgiant")
            {
                CustomRoutines = new CustomDelegate(NetGiant.Control);
            }
            else if (channel.TenantSetting.Description == "Ekm")
            {
                CustomRoutines = new CustomDelegate(Ekm.Control);
            }
        }

        public TenantSetting GetTenantRecord(int tenantFK)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.TenantSettings
                    .Include("FTPSettings.FieldMapping")
                    .Include(x => x.Lookup)
                    .Include(x => x.Channels)
                    .Where(x => x.TenantID == tenantFK)
                    .FirstOrDefault();
            }
        }

        public TenantSetting GetTenantFromChannel(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.TenantSettings
                    .Where(x => x.Channels.Any(y => y.ChannelID == channelId))
                    .FirstOrDefault();
            }
        }

        public bool SetJobInProgress(int channelId, bool inProgress)
        {
            var crud = new CrudChannel();
            bool isSuccess = false;
            Channel channel = new Channel();

            channel = crud.Read(channelId);

            channel.JobInProgress = inProgress;
            try
            {
                crud.Update(channel);
                isSuccess = true;
            }
            catch (Exception)
            {
            }

            return isSuccess;
        }

        public bool CheckJobInProgress(int channelId)
        {
            var crud = new CrudChannel();
            return crud.Read(channelId).JobInProgress;
        }

        public Channel GetChannelRecord(int channelFK)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Channels
                    .Include(x => x.TenantSetting)
                    .Include(x => x.Suppliers)
                    .Include("FTPSettings.FieldMapping")
                    .Include("FTPSettings.Lookup")
                    .Include(x => x.TenantSetting.Lookup)
                    .Include(x => x.Lookup)
                    .Where(x => x.ChannelID == channelFK)
                    .FirstOrDefault();
            }
        }
    }
}
