using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.DataLayer.NetgiantMembership;
using System.Data.Entity;
using System.Net.Mail;
using PagedList;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Configuration;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Review
{
    public class ReviewViewModel : CommonViewModel
    {
        public ReviewViewModel()
        {
            db = new ngmdEntities();
        }

        private ngmdEntities db;

        public IQueryable<TelerikReview> ReviewList { get; set; }
        public feefoFeedback OnefeefoFeedback { get; set; }

        public class TelerikReview
        {
            public int FeefoFeedbackId { get; set; }
            public DateTime FeedbackDate { get; set; }
            public string ProductName { get; set; }
            public string PartNo { get; set; }
            public string Rating { get; set; }
            public string Comment { get; set; }
            public string Website { get; set; }
            public string IsHidden { get; set; }
        }

        public ReviewViewModel GetReviews()
        {
            ReviewList = db.feefoFeedbacks
                .Join(
                    db.Website,
                    FB => FB.websiteFK,
                    WS => WS.WebsiteID,
                    (FB, WS) => new { FB, WS }
                )
                .Join(
                    db.product,
                    FB => FB.FB.productFK,
                    PR => PR.productID,
                    (FB, PR) => new TelerikReview
                    {
                        FeefoFeedbackId = FB.FB.feefoFeedbackID,
                        FeedbackDate = FB.FB.feedbackDate,
                        ProductName = PR.productName,
                        PartNo = PR.partNo,
                        Rating = FB.FB.productRating.ToString(),
                        Comment = FB.FB.productComment,
                        Website = FB.WS.WebsiteName,
                        IsHidden = FB.FB.isHidden == true ? "True" : "False"
                    }
                )
                .AsQueryable();

            return this;
        }

        public ReviewViewModel GetReviewForEdit(int id)
        {
            ReviewViewModel model = new ReviewViewModel();

            if (id > 0)
            {
                model.OnefeefoFeedback = db.feefoFeedbacks.Find(id);

                model.OnefeefoFeedback.ProductName = db.product.Find(model.OnefeefoFeedback.productFK).productName;
                model.OnefeefoFeedback.WebsiteName = db.Website.Find(model.OnefeefoFeedback.websiteFK).WebsiteName;
            }

            return model;
        }

        public void SaveisHidden()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    //get the data
                    feefoFeedback MyFFFB = db.feefoFeedbacks.Find(OnefeefoFeedback.feefoFeedbackID);
                    MyFFFB.isHidden = OnefeefoFeedback.isHidden;
                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
    }
}