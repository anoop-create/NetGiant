using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace netGiant.Api.BusinessLayer.Models
{
    [DataContract(Namespace="")]
    public class ItemSearch
    {
        public ItemSearch()
        {
            Products = new List<ProductSearch>();
            Equipment = new List<EquipmentSearch>();
            Categories = new List<CategorySearch>();
        }

        [DataMember(Name="Products")]
        public List<ProductSearch> Products { get; set; }

        [DataMember(Name = "Equipment")]
        public List<EquipmentSearch> Equipment { get; set; }

        [DataMember(Name = "Categories")]
        public List<CategorySearch> Categories { get; set; }

        [DataMember(Name = "ProductIDs")]
        public string ProductIDs { get; set; }

        [DataMember(Name = "EquipmentIDs")]
        public string EquipmentIDs { get; set; }

        [DataContract(Namespace = "", Name = "ProductSearch")]
        public class ProductSearch
        {
            [DataMember(Name = "ID")]
            public string ID { get; set; }

            [DataMember(Name = "AxisID")]
            public string AxisID { get; set; }

            [DataMember(Name = "PartNo")]
            public string PartNo { get; set; }

            [DataMember(Name = "ProductName")]
            public string ProductName { get; set; }

            [DataMember(Name = "ProductImage")]
            public string ProductImage { get; set; }

            [DataMember(Name = "LuceneRank")]
            public string LuceneRank { get; set; }
        }

        [DataContract(Namespace = "", Name = "EquipmentSearch")]
        public class EquipmentSearch
        {
            [DataMember(Name = "ID")]
            public string ID { get; set; }

            [DataMember(Name = "EquipmentName")]
            public string EquipmentName { get; set; }

            [DataMember(Name = "CartridgeTypeID")]
            public string CartridgeTypeID { get; set; }

            [DataMember(Name = "Manufacturer")]
            public string Manufacturer { get; set; }

            [DataMember(Name = "ThumbnailUrl")]
            public string ThumbnailUrl { get; set; }

            [DataMember(Name = "ProductCount")]
            public string ProductCount { get; set; }

            [DataMember(Name = "LuceneRank")]
            public string LuceneRank { get; set; }
        }

        [DataContract(Namespace = "", Name = "CategorySearch")]
        public class CategorySearch
        {
            [DataMember(Name = "CategoryName")]
            public string CategoryName { get; set; }

            [DataMember(Name = "AxisCode")]
            public string AxisCode { get; set; }
        }
    }
}
