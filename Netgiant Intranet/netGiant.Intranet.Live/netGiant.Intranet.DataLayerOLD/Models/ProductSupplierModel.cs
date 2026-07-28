using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.DataLayer.Models
{
    public class ProductSupplierModel
    {
        public int providerID { get; set; }
        public string providerName { get; set; }
        public string altRef { get; set; }
        public string providerPartNo { get; set; }
        public double price { get; set; }
        public int quantity { get; set; }
        public DateTime inventoryUpdatedOn { get; set; }
        public DateTime priceUpdatedOn { get; set; }
        public int axisSupplierRef { get; set; }
        public bool? untrustedProvider { get; set; }
    }

    public class ProductPriceModel
    {
        public double Price { get; set; }
        public string WebsiteName { get; set; }
        public DateTime DateLastUpdate { get; set; }
    }
}
