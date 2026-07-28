using netGiant.Intranet.DataLayer;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class CrossSellingLinkViewModel
    {
        public CrossSellingLinkViewModel()
        {
            AllManufacturers = SelectListViewModel.AllManufacturers();
            AllCrossSellingLinkTypes = SelectListViewModel.GetCrossSellingLinkTypes();
        }

        public IPagedList<crossSellingLink> CrossSellingLinkList { get; set; }
        public crossSellingLink CrossSellingLink { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public IQueryable<SelectListItem> AllCrossSellingLinkTypes { get; set; }
        public IQueryable<SelectListItem> ProductList { get; set; }
        public bool TwoWayLink { get; set; }

        private string _searchTerm;
        private string _searchBy;
        private string _orderBy;
        private int? _manufacturerFK;

        public CrossSellingLinkViewModel Get()
        {
            return Get(1, null, null, null, null);
        }

        public CrossSellingLinkViewModel Get(int pageNumber, string searchTerm,
            string searchBy, string orderBy, int? manufacturerFK)
        {
            _searchTerm = searchTerm;
            _searchBy = searchBy;
            _orderBy = orderBy;
            _manufacturerFK = manufacturerFK;

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<crossSellingLink> query = db.crossSellingLink
                    .Include(x => x.product)
                    .Include(x => x.product1)
                    .Include(x => x.crossSellingLinkType);

                query = SetWhereClause(query);
                query = SetOrderByClause(query);

                CrossSellingLinkList = query.ToPagedList(pageNumber, 50);
            }

            return this;
        }

        private IQueryable<crossSellingLink> SetWhereClause(IQueryable<crossSellingLink> q)
        {
            if (!string.IsNullOrEmpty(_searchTerm))
            {
                switch (_searchBy)
                {
                    case "partNo":
                        q = q.Where(x => x.product.partNo.ToLower().Contains(_searchTerm) ||
                            x.product1.partNo.ToLower().Contains(_searchTerm));
                        break;
                    case "productName":
                        q = q.Where(x => x.product.productName.ToLower().Contains(_searchTerm) ||
                            x.product1.productName.ToLower().Contains(_searchTerm));
                        break;
                }
            }

            if (_manufacturerFK != null && _manufacturerFK > 0)
                q = q.Where(x => x.product.manufacturerFK == _manufacturerFK);

            return q;
        }

        private IQueryable<crossSellingLink> SetOrderByClause(IQueryable<crossSellingLink> q)
        {
            switch (_orderBy)
            {
                case "aPartNoAsc":
                    q = q.OrderBy(x => x.product.partNo);
                    break;
                case "aPartNoDesc":
                    q = q.OrderByDescending(x => x.product.partNo);
                    break;
                case "bPartNoAsc":
                    q = q.OrderBy(x => x.product1.partNo);
                    break;
                case "bPartNoDesc":
                    q = q.OrderByDescending(x => x.product1.partNo);
                    break;
                case "aProductNameAsc":
                    q = q.OrderBy(x => x.product.productName);
                    break;
                case "aProductNameDesc":
                    q = q.OrderByDescending(x => x.product.productName);
                    break;
                case "bProductNameAsc":
                    q = q.OrderBy(x => x.product1.productName);
                    break;
                case "bProductNameDesc":
                    q = q.OrderByDescending(x => x.product1.productName);
                    break;
                case "typeAsc":
                    q = q.OrderBy(x => x.crossSellingLinkType.crossSellingLinkTypeName);
                    break;
                case "typeDesc":
                    q = q.OrderByDescending(x => x.crossSellingLinkType.crossSellingLinkTypeName);
                    break;
                default:
                    q = q.OrderBy(x => x.product.partNo);
                    break;
            }

            return q;
        }

        public static CrossSellingLinkViewModel Create(int id)
        {
            CrossSellingLinkViewModel model = new CrossSellingLinkViewModel();

            using (ngmdEntities db = new ngmdEntities())
            {
                if (id > 0)
                {
                    model.CrossSellingLink = db.crossSellingLink.Include(x => x.product).Include(x => x.product1)
                        .Where(x => x.crossSellingLinkID == id).FirstOrDefault();

                    crossSellingLink twoWay = db.crossSellingLink.Where(x => x.aProductFK == model.CrossSellingLink.bProductFK &&
                                            x.bProductFK == model.CrossSellingLink.aProductFK).FirstOrDefault();

                    if (twoWay != null)
                        model.TwoWayLink = true;

                }
                else
                {
                    model.CrossSellingLink = new crossSellingLink();
                }
            }

            model.ProductList = SelectListViewModel.AllProductsPartNoDesc();

            return model;
        }

        public void Save()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                crossSellingLink csl = db.crossSellingLink.Where(x => x.aProductFK == CrossSellingLink.bProductFK &&
                        x.bProductFK == CrossSellingLink.aProductFK).FirstOrDefault();

                if (CrossSellingLink.crossSellingLinkID > 0)
                {
                    db.Entry(CrossSellingLink).State = EntityState.Modified;

                    if (TwoWayLink)
                    {
                        if (csl == null)
                        {
                            CreateTwoWayLink(db);
                        }
                    }
                    else
                    {
                        if (csl != null)
                        {
                            db.Entry(csl).State = EntityState.Deleted;
                        }
                    }

                }
                else
                {
                    db.Entry(CrossSellingLink).State = EntityState.Added;

                    if (TwoWayLink)
                    {
                        if (csl == null)
                        {
                            CreateTwoWayLink(db);
                        }
                    }
                }

                db.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                crossSellingLink csl = db.crossSellingLink.Find(id);

                if (csl != null)
                {
                    db.Entry(csl).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
        }

        private void CreateTwoWayLink(ngmdEntities db)
        {
            crossSellingLink newCsl = new crossSellingLink();
            newCsl.aProductFK = CrossSellingLink.bProductFK;
            newCsl.bProductFK = CrossSellingLink.aProductFK;
            newCsl.crossSellingLinkTypeFK = CrossSellingLink.crossSellingLinkTypeFK;
            db.Entry(newCsl).State = EntityState.Added;
        }
    }
}
