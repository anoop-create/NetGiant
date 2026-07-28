using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.DataSuppliers
{
    public class DataSupplierLookupViewModel
    {
        public List<ds_productView> DataSupplierLookupList { get; set; }
        public int DataSupplierLookupListCount { get; set; }
        public ds_productView ProductView { get; set; }
        public List<ds_searchableView> SearchableViewList { get; set; }
        public List<ds_featureView> FeatureViewList { get; set; }

        public DataSupplierLookupViewModel GetDataSupplierLookup()
        {
            return GetDataSupplierLookup(null, null, null, 1);
        }

        public DataSupplierLookupViewModel GetDataSupplierLookup(string orderBy, string searchTerm, string searchBy, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<ds_productView> query = db.ds_productView;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "partno":
                            query = query.Where(x => x.partno.ToString().Contains(searchTerm.Trim()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "prodidAsc":
                        query = query.OrderBy(x => x.prodID);
                        break;
                    case "prodidDesc":
                        query = query.OrderByDescending(x => x.prodID);
                        break;
                    case "partnoAsc":
                        query = query.OrderBy(x => x.partno);
                        break;
                    case "partnoDesc":
                        query = query.OrderByDescending(x => x.partno);
                        break;
                    case "manufacturerAsc":
                        query = query.OrderBy(x => x.manufacturer);
                        break;
                    case "manufacturerDesc":
                        query = query.OrderByDescending(x => x.manufacturer);
                        break;
                    case "datasupplierAsc":
                        query = query.OrderBy(x => x.dataSupplierID);
                        break;
                    case "datasupplierDesc":
                        query = query.OrderByDescending(x => x.dataSupplierID);
                        break;
                    case "modelAsc":
                        query = query.OrderBy(x => x.model);
                        break;
                    case "modelDesc":
                        query = query.OrderByDescending(x => x.model);
                        break;
                }

                DataSupplierLookupListCount = query.Count();
                DataSupplierLookupList = query
                    .Take(blockSize)
                    .ToList();
            }
            return this;
        }

        public DataSupplierLookupViewModel Details(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ProductView = db.ds_productView.Where(x => x.prodID == id).FirstOrDefault();
                SearchableViewList = db.ds_searchableView.Where(x => x.partNo == ProductView.partno 
                    && x.manufacturer == ProductView.manufacturer 
                    && x.dataSupplierID == ProductView.dataSupplierID).ToList();
                FeatureViewList = db.ds_featureView.Where(x => x.partNo == ProductView.partno
                    && x.manufacturer == ProductView.manufacturer
                    && x.dataSupplierID == ProductView.dataSupplierID).ToList(); 
            }
            return this;
        }
    }
}
