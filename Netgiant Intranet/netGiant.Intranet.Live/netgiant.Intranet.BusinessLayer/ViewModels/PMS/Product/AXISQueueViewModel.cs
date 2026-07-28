using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Web.Mvc;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class AXISQueueViewModel
    {   
        public AXISQueueDetails axisQueueDetails { get; set; }
        public PagedList.IPagedList<AXISQueueDetails> listAXISQueueDetails { get; set; }

        public AXISQueueViewModel Get()
        {
            return Get(null, "", "", "");
        }
        
        public AXISQueueViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<AXISQueueDetails> list = db.AXISQueueDetails.Include("AXISQueue.product");

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "partNo":
                                list = list.Where(x => x.AXISQueue.product.partNo.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "entityNameAsc":
                            list = list.OrderBy(x => x.entityName);
                            break;
                        case "entityNameDesc":
                            list = list.OrderByDescending(x => x.entityName);
                            break;
                        case "fieldNameAsc":
                            list = list.OrderBy(x => x.fieldName);
                            break;
                        case "fieldNameDesc":
                            list = list.OrderByDescending(x => x.fieldName);
                            break;
                        case "createdDateAsc":
                            list = list.OrderBy(x => x.createdDate);
                            break;
                        case "createdDateDesc":
                            list = list.OrderByDescending(x => x.createdDate);
                            break;
                        case "completedDateAsc":
                            list = list.OrderBy(x => x.completedDate);
                            break;
                        case "completedDateDesc":
                            list = list.OrderByDescending(x => x.completedDate);
                            break;
                        case "crudAsc":
                            list = list.OrderBy(x => x.CRUD);
                            break;
                        case "crudDesc":
                            list = list.OrderByDescending(x => x.CRUD);
                            break;
                        case "partNoAsc":
                            list = list.OrderBy(x => x.AXISQueue.product.partNo);
                            break;
                        case "partNoDesc":
                            list = list.OrderByDescending(x => x.AXISQueue.product.partNo);
                            break;
                        default:
                            list = list.OrderByDescending(x => x.createdDate);
                            break;
                    }

                    listAXISQueueDetails = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch(Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

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
}
