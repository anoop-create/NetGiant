using System;
using System.Linq;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class AXISQueueViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public AXISQueueViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikAxisQueueItem> AxisQueueItemList { get; set; }

        public AXISQueueViewModel Get()
        {
            AxisQueueItemList = _ctx.AXISQueueDetails
                                    .Select(x => new TelerikAxisQueueItem
                                    {
                                        Id = x.AXISQueueDetailsID,
                                        EntityName = x.entityName,
                                        FieldName = x.fieldName,
                                        CreatedDate = x.createdDate,
                                        CompletedDate = x.completedDate,
                                        Type = x.CRUD,
                                        PartNo = x.AXISQueue.product.partNo
                                    })
                                    .AsQueryable();
            return this;
        }

        public void CreateQueueDetail(int productFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var axisQ = new AXISQueueDetails()
                {
                    AXISQueueFK = CreateQueueMain(productFK),
                    createdDate = DateTime.Now,
                    CRUD = "C",
                    entityName = "All",
                    fieldName = "All"
                };

                db.AXISQueueDetails.Add(axisQ);
                db.SaveChanges();
            }
        }

        private int CreateQueueMain(int productFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var queue = db.AXISQueue.Where(x => x.productFK == productFK).FirstOrDefault();

                if (queue == null)
                {
                    var axisQ = new AXISQueue()
                    {
                        productFK = productFK,
                        dateLastUpdated = DateTime.Now
                    };

                    db.AXISQueue.Add(axisQ);
                    db.SaveChanges();
                    queue = axisQ;
                }

                return queue.AXISQueueID;
            }
        }
    }

    public class TelerikAxisQueueItem
    {
        public int Id { get; set; }
        public string EntityName { get; set; }
        public string FieldName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Type { get; set; }
        public string PartNo { get; set; }
    }
}
