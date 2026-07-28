using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Attributes;
using System;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(VoucherMetaData))]
    public partial class VoucherPromo
    {
    }

    public class VoucherMetaData
    {
        [Key]
        public int VoucherPromoId { get; set; }

        [Required(ErrorMessage = "Web Site is required")]
        public int WebsiteFk { get; set; }

        [Required(ErrorMessage = "Voucher Type is required")]
        public int VoucherTypeFk { get; set; }

        [Required(ErrorMessage = "Voucher Group is required")]
        public int VoucherPromoGroupFk { get; set; }

        [Required(ErrorMessage = "Voucher Code is required")]
        public string VoucherCode { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "Valid From is required")]
        public System.DateTime ValidFrom { get; set; }

        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "Valid To is required")]
        public System.DateTime ValidTo { get; set; }

        [Required(ErrorMessage = "Stock Ref is required")]
        public string StockRef { get; set; }

        [Required(ErrorMessage = "Min Basket Value is required")]
        public decimal MinBasketValue { get; set; }

        [Required(ErrorMessage = "Min Qual Value is required")]
        public decimal MinQualValue { get; set; }

        [RequiredIf("VoucherTypeFk == 1 || VoucherTypeFk == 2", ErrorMessage = "Amount is required")]
        public Nullable<decimal> Amount { get; set; }

        [RequiredIf("VoucherTypeFk == 0", ErrorMessage = "Percentage is required")]
        public Nullable<decimal> Percentage { get; set; }

        [RequiredIf("VoucherTypeFk == 3", ErrorMessage = "Gift Stock Ref is required")]
        public string GiftStockRef { get; set; }

        [RequiredIf("VoucherTypeFk == 4", ErrorMessage = "Multi Buy Qual No is required")]
        public Nullable<int> MultiBuyQualNo { get; set; }

        [RequiredIf("VoucherTypeFk == 4", ErrorMessage = "Multi Buy No Discounted is required")]
        public Nullable<int> MultiBuyNoDiscounted { get; set; }
    }
}
