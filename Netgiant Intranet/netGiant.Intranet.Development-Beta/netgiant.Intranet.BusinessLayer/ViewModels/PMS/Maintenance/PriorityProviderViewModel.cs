using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class PriorityProviderViewModel : CommonViewModel
    {
        public List<priorityProvider> PriorityProviderList { get; set; }
        public int PriorityProviderListCount { get; set; }
        public priorityProvider PriorityProvider { get; set; }
        public IQueryable<SelectListItem> ProviderNameList { get; set; }
        public IQueryable<SelectListItem> ManufacturerNameList { get; set; }

        public class TelerikPriorityProvider
        {
            public int ID { get; set; }
            public string Provider { get; set; }
            public string Manufacturer { get; set; }
        }

        public IQueryable<TelerikPriorityProvider> PriorityProviderList2 { get; set; }

        public PriorityProviderViewModel GetPriorityProvider()
        {
            ngmdEntities db = new ngmdEntities();
            PriorityProviderList2 = db.priorityProvider
                .Select(x => new TelerikPriorityProvider
                {
                    ID = x.id,
                    Provider = x.provider.providerName,
                    Manufacturer = x.manufacturer.manufacturerName

                })
                .AsQueryable();
                        
            return this;
        }

        public PriorityProviderViewModel GetPriorityProvider(string orderBy, string searchTerm, string searchBy,
            int? providerID, int? manufacturerID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            ngmdEntities db = new ngmdEntities();

                IQueryable<priorityProvider> query = db.priorityProvider
                    .Include(x => x.provider)
                    .Include(x => x.manufacturer);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "id":
                            query = query.Where(x => x.id.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "provider":
                            query = query.Where(x => x.provider.providerName.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "manufacturer":
                            query = query.Where(x => x.manufacturer.manufacturerName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (providerID != null && providerID > 0)
                {
                    query = query.Where(x => x.providerFK == providerID);
                }

                if (manufacturerID != null && manufacturerID > 0)
                {
                    query = query.Where(x => x.manufacturerFK == manufacturerID);
                }

                switch (orderBy)
                {
                    case "idAsc":
                        query = query.OrderBy(x => x.id);
                        break;
                    case "idDesc":
                        query = query.OrderByDescending(x => x.id);
                        break;
                    case "providerAsc":
                        query = query.OrderBy(x => x.providerFK);
                        break;
                    case "providerDesc":
                        query = query.OrderByDescending(x => x.providerFK);
                        break;
                    case "manufacturerAsc":
                        query = query.OrderBy(x => x.manufacturerFK);
                        break;
                    case "manufacturerDesc":
                        query = query.OrderByDescending(x => x.manufacturerFK);
                        break;
                    default:
                        query = query.OrderBy(x => x.id);
                        break;
                }

                PriorityProviderListCount = query.Count();
                PriorityProviderList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
                ProviderNameList = GetProviderNames();
                ManufacturerNameList = GetManufacturerNames();

            return this;
        }

        private IQueryable<SelectListItem> GetProviderNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.provider.OrderBy(x => x.providerName).Select(x => new SelectListItem
                {
                    Value = x.providerID.ToString(),
                    Text = x.providerName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetManufacturerNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.manufacturer.OrderBy(x => x.manufacturerName).Select(x => new SelectListItem
                {
                    Value = x.manufacturerID.ToString(),
                    Text = x.manufacturerName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }
    }
}
