using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ManufacturersViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ManufacturersViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikManufacturer> ManufacturerList { get; set; }
        public manufacturer Manufacturer { get; set; }
        public List<priorityProvider> PriorityProviders { get; set; }
        public List<provider> Providers { get; set; }

        public ManufacturersViewModel Get()
        {
            ManufacturerList = _ctx.manufacturer
                                   .Select(x => new TelerikManufacturer
                                   {
                                       Id = x.manufacturerID,
                                       Name = x.manufacturerName,
                                       LastUpdated = x.dateLastUpdate
                                   })
                                   .AsQueryable();
            return this;
        }

        public ManufacturersViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (id > 0)
                {
                    Manufacturer = db.manufacturer.Find(id);
                    PriorityProviders = ProviderViewModel.GetPriorityProviders(id);
                }
                else
                {
                    Manufacturer = new manufacturer();
                    PriorityProviders = new List<priorityProvider>();
                }

                Providers = ProviderViewModel.GetProvidersByType(2);
            }

            return this;
        }

        public bool Save(ManufacturersViewModel model)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.Manufacturer.dateLastUpdate = DateTime.Now;

                    if (model.Manufacturer.manufacturerID > 0)
                    {
                        db.Entry(model.Manufacturer).State = EntityState.Modified;
                    }
                    else
                    {
                        db.manufacturer.Add(model.Manufacturer);
                    }

                    db.SaveChanges();
                }

                UpdatePriorityProviders(model);
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        private void UpdatePriorityProviders(ManufacturersViewModel model)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                List<priorityProvider> priProvs = db.priorityProvider
                        .Where(x => x.manufacturerFK == model.Manufacturer.manufacturerID).ToList();

                if (model.PriorityProviders != null)
                {
                    List<int?> nonSelectedProviders = new List<int?>();

                    nonSelectedProviders = priProvs.Select(x => x.providerFK)
                        .Except(model.PriorityProviders.Select(x => x.providerFK)).ToList();

                    for (var i = 0; i < nonSelectedProviders.Count(); i++)
                    {
                        var providerFK = nonSelectedProviders[i];

                        db.priorityProvider.Remove(db.priorityProvider
                            .Where(x => x.providerFK == providerFK &&
                                x.manufacturerFK == model.Manufacturer.manufacturerID)
                            .FirstOrDefault());
                    }
                }
                else
                {
                    foreach (var prov in priProvs)
                    {
                        db.priorityProvider.Remove(prov);
                    }
                }

                if (model.PriorityProviders != null)
                {
                    foreach (var prov in model.PriorityProviders)
                    {
                        if (prov.id == 0)
                        {
                            db.Entry(prov).State = EntityState.Added;
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manufacturer man = db.manufacturer.Find(id);
                    db.manufacturer.Remove(man);
                    db.SaveChanges();
                }
                sr.IsSuccess = true;
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }
    }

    public class TelerikManufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
