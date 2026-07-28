using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.DataSuppliers
{
    public class DataSupplierLookupViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public DataSupplierLookupViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikDataSupplierLookup> DataSupplierLookupList { get; set; }
        public ds_productView ProductView { get; set; }
        public List<ds_searchableView> SearchableViewList { get; set; }
        public DataSupplierLookupViewModel Get()
        {
            DataSupplierLookupList = _ctx.ds_productView
                                         .Select(x => new TelerikDataSupplierLookup
                                         {
                                             ProductId = x.prodID,
                                             PartNo = x.partno,
                                             Manufacturer = x.manufacturer,
                                             DataSupplier = (DataSupplier)x.dataSupplierID,
                                             Model = x.model
                                         })
                                         .AsQueryable();
            return this;
        }

        public DataSupplierLookupViewModel Details(string id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ProductView = db.ds_productView
                    .Where(x => x.prodID == id)
                    .FirstOrDefault();
                SearchableViewList = db.ds_searchableView
                    .Where(x => x.partNo == ProductView.partno
                    && x.manufacturer == ProductView.manufacturer
                    && x.dataSupplierID == ProductView.dataSupplierID)
                    .ToList();
                //FeatureViewList = db.ds_featureView
                //    .Where(x => x.partNo == ProductView.partno
                //    && x.manufacturer == ProductView.manufacturer
                //    && x.dataSupplierID == ProductView.dataSupplierID)
                //    .ToList();
            }
            return this;
        }
    }

    public enum DataSupplier
    {
        None,
        OpenRange,
        CNet,
        NetGiant
    }

    public class TelerikDataSupplierLookup
    {
        public string ProductId { get; set; }
        public string PartNo { get; set; }
        public string Manufacturer { get; set; }
        public DataSupplier DataSupplier { get; set; }
        public string Model { get; set; }
    }
}
