using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ManufacturersViewModel
    {
        public IPagedList<manufacturer> manufacturersList { get; set; }
        public manufacturer manufacturer { get; set; }
        public List<priorityProvider> priorityProviders { get; set; }
        public List<provider> providers { get; set; }

        public ManufacturersViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<manufacturer> list = db.manufacturer.OrderBy(x => x.manufacturerName);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    list = list.Where(x => x.manufacturerName.ToLower().Contains(searchTerm.ToLower().Trim()));
                }

                switch (orderBy)
                {
                    case "manufacturerNameAsc":
                        list = list.OrderBy(x => x.manufacturerName);
                        break;
                    case "manufacturerNameDesc":
                        list = list.OrderByDescending(x => x.manufacturerName);
                        break;
                    case "dateLastUpdatedAsc":
                        list = list.OrderBy(x => x.dateLastUpdate);
                        break;
                    case "dateLastUpdatedDesc":
                        list = list.OrderByDescending(x => x.dateLastUpdate);
                        break;
                    default:
                        list = list.OrderBy(x => x.manufacturerName);
                        break;
                }

                manufacturersList = list.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public ManufacturersViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (id > 0)
                {
                    manufacturer = db.manufacturer.Find(id);
                    priorityProviders = ProviderViewModel.GetPriorityProviders(id);
                }
                else
                {
                    manufacturer = new manufacturer();
                    priorityProviders = new List<priorityProvider>();
                }

                providers = ProviderViewModel.GetProvidersByType(2);
            }

            return this;
        }

        public bool Save(ManufacturersViewModel manVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manVm.manufacturer.dateLastUpdate = DateTime.Now;

                    if (manVm.manufacturer.manufacturerID > 0)
                    {
                        db.Entry(manVm.manufacturer).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.manufacturer.Add(manVm.manufacturer);
                    }

                    db.SaveChanges();
                }

                UpdatePriorityProviders(manVm);
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        private void UpdatePriorityProviders(ManufacturersViewModel manVm)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                List<priorityProvider> priProvs = db.priorityProvider
                        .Where(x => x.manufacturerFK == manVm.manufacturer.manufacturerID).ToList();

                if (manVm.priorityProviders != null)
                {
                    List<int?> nonSelectedProviders = new List<int?>();

                    nonSelectedProviders = priProvs.Select(x => x.providerFK)
                        .Except(manVm.priorityProviders.Select(x => x.providerFK)).ToList();

                    for (var i = 0; i < nonSelectedProviders.Count(); i++)
                    {
                        var providerFK = nonSelectedProviders[i];

                        db.priorityProvider.Remove(db.priorityProvider
                            .Where(x => x.providerFK == providerFK &&
                                x.manufacturerFK == manVm.manufacturer.manufacturerID)
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

                if (manVm.priorityProviders != null)
                {
                    foreach (var prov in manVm.priorityProviders)
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

        public bool Delete(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manufacturer man = db.manufacturer.Find(id);
                    db.manufacturer.Remove(man);
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
    }
}
