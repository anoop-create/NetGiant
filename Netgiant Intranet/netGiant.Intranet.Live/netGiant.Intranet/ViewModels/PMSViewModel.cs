using netGiant.Intranet.DataLayer;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using PagedList;

namespace netGiant.Intranet.ViewModels
{
    public class PMSViewModel
    {
        public PMSViewModel()
        {
            ActionLinks = new List<actionLink>();
        }
        
        public List<actionLink> ActionLinks { get; set; }
        public PagedList.IPagedList<product> products { get; set; }

        public PMSViewModel Get(int? page, int pageSize, int pageNumber, string search, string searchBy)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ActionLinks = db.actionLinks.ToList();

                List<product> list = new List<product>();

                list = (from p in db.product.Where(x => x.productStatusFK != db.productStatus.FirstOrDefault(
                        y => y.productStatusName.Equals("no status", System.StringComparison.InvariantCultureIgnoreCase)).productStatusID) 
                        orderby p.productID descending select p)
                        .Include(p => p.productGroup)
                        .Include(p => p.productStatus)
                        .Include(p => p.salesAreaGroup)
                        .Include(p => p.manufacturer)
                        .ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    switch (searchBy)
                    {
                        case "partNo":
                            products = list.Where(x => x.partNo.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        case "manufacturer":
                            products = list.Where(x => x.manufacturer.manufacturerName.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        case "unspsc":
                            products = list.Where(x => x.UNSPSCCode.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        case "productGroup":
                            products = list.Where(x => x.productGroup.productGroupName.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        case "productStatus":
                            products = list.Where(x => x.productStatus.productStatusName.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        case "salesAreaGroup":
                            products = list.Where(x => x.salesAreaGroup.salesAreaGroupName.ToLower().Contains(search.ToLower().Trim())).ToPagedList(pageNumber, pageSize);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    products = list.ToPagedList(pageNumber, pageSize);
                }
            }

            return this;
        }
    }
}