using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher
{
    public class PromotionalVoucherGroupDetailViewModel : CommonViewModel
    {
        public VoucherPromoGroup VoucherPromoGroupDetail { get; set; }

        public PromotionalVoucherGroupDetailViewModel GetPromotionalVoucherGroupDetail(int promoGroupId)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                VoucherPromoGroupDetail = db.VoucherPromoGroup
                    .Include(x => x. Website)
                    .Where(x => x.VoucherPromoGroupId == promoGroupId)
                    .FirstOrDefault();
            }

            return this;
        }
    }
}
