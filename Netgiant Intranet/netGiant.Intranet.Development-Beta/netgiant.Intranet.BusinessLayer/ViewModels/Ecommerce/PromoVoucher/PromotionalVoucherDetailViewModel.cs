using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher
{
    public class PromotionalVoucherDetailViewModel : CommonViewModel
    {
        public VoucherPromo VoucherPromoDetail { get; set; }
        public string VoucherType { get; set; }   

        public PromotionalVoucherDetailViewModel GetPromotionalVoucherDetail(int promoId)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                VoucherPromoDetail = db.VoucherPromo
                            .Include(x => x.VoucherPromoGroup)
                            .Include(x => x.Website)
                            .Where(x => x.VoucherPromoId == promoId)
                            .FirstOrDefault();
            }

            VoucherType = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "VoucherType" & x.AltLookupId == VoucherPromoDetail.VoucherTypeFk).FirstOrDefault().LookupName;

            return this;
        }
    }
}
