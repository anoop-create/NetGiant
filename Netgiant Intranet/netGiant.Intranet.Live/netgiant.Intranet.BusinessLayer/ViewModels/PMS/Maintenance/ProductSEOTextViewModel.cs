using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ProductSEOTextViewModel
    {
        public List<productSEOText> ProductSEOTextList { get; set; }
        public int ProductSEOTextListCount { get; set; }
        public productSEOText ProductSEOText { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> ProductTypeNameList { get; set; }
        public bool isMaintenance { get; set; }

        public ProductSEOTextViewModel GetProductSEOText()
        {
            return GetProductSEOText(null, null, null, null, null, 2, 2, null, 1);
        }

        public ProductSEOTextViewModel GetProductSEOText(string orderBy, string searchTerm, string searchBy, int? websiteID, 
            int? ProductTypeID, int? ownBrand, int? assembly, bool? Maintenance, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<productSEOText> query = db.productSEOText
                    .Include(x => x.Website)
                    .Include(x => x.productType);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "pnumber":
                            query = query.Where(x => x.paragraphNo.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "enumber":
                            query = query.Where(x => x.entryNo.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "ptext":
                            query = query.Where(x => x.paragraphText.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID);
                }

                if (ProductTypeID != null && ProductTypeID > 0)
                {
                    query = query.Where(x => x.productTypeFK == ProductTypeID);
                }

                if (ownBrand == 1)
                {
                    query = query.Where(x => x.isOwnBrand == true);
                }
                else if (ownBrand == 0)
                {
                    query = query.Where(x => x.isOwnBrand == false);
                }
                else if (ownBrand == null)
                {
                    query = query.Where(x => x.isOwnBrand == null);
                }

                if (assembly == 1)
                {
                    query = query.Where(x => x.isAssembly == true);
                }
                else if (assembly == 0)
                {
                    query = query.Where(x => x.isAssembly == false);
                }
                else if (assembly == null)
                {
                    query = query.Where(x => x.isAssembly == null);
                }

                if (Maintenance == true)
                {
                    query = query.Where(x => x.isMaintenance == true);
                }

                switch (orderBy)
                {
                    case "numberAsc":
                        query = query.OrderBy(x => x.paragraphNo);
                        break;
                    case "numberDesc":
                        query = query.OrderByDescending(x => x.paragraphNo);
                        break;
                    case "entryAsc":
                        query = query.OrderBy(x => x.entryNo);
                        break;
                    case "entryDesc":
                        query = query.OrderByDescending(x => x.entryNo);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "typeAsc":
                        query = query.OrderBy(x => x.productType.productTypeName);
                        break;
                    case "typeDesc":
                        query = query.OrderByDescending(x => x.productType.productTypeName);
                        break;
                    case "textAsc":
                        query = query.OrderBy(x => x.paragraphText);
                        break;
                    case "textDesc":
                        query = query.OrderByDescending(x => x.paragraphText);
                        break;
                    case "ownbrandAsc":
                        query = query.OrderBy(x => x.isOwnBrand);
                        break;
                    case "ownbrandDesc":
                        query = query.OrderByDescending(x => x.isOwnBrand);
                        break;
                    case "assemblyAsc":
                        query = query.OrderBy(x => x.isAssembly);
                        break;
                    case "assemblyDesc":
                        query = query.OrderByDescending(x => x.isAssembly);
                        break;
                    case "maintenanceAsc":
                        query = query.OrderBy(x => x.isMaintenance);
                        break;
                    case "maintenanceDesc":
                        query = query.OrderByDescending(x => x.isMaintenance);
                        break;
                    default:
                        query = query.OrderBy(x => x.Website.WebsiteName)
                            .ThenBy(x => x.productType.productTypeName)
                            .ThenBy(x => x.isOwnBrand)
                            .ThenBy(x => x.isAssembly)
                            .ThenBy(x => x.isMaintenance)
                            .ThenBy(x => x.paragraphNo)
                            .ThenBy(x => x.entryNo);
                        break;
                }

                ProductSEOTextListCount = query.Count();
                ProductSEOTextList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                SetupSelectLists();
            }
            return this;
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.Websites.OrderBy(x => x.WebsiteName).Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.WebsiteName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetProductTypeNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.productType.OrderBy(x => x.productTypeName).Select(x => new SelectListItem
                {
                    Value = x.productTypeID.ToString(),
                    Text = x.productTypeName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public ProductSEOTextViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ProductSEOText = db.productSEOText.Where(x => x.productSEOTextID == id).FirstOrDefault();
                }
            }
            else
            {
                ProductSEOText = new productSEOText();
            }
            SetupSelectLists();

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (ProductSEOText.productSEOTextID > 0)
                    {
                        db.Entry(ProductSEOText).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckSeoTextExists(db);
                        db.Entry(ProductSEOText).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        productSEOText en = db.productSEOText.Where(x => x.productSEOTextID == id).FirstOrDefault();
                        db.Entry(en).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        private void CheckSeoTextExists(ngmdEntities db)
        {
            productSEOText pst = new productSEOText();

            pst = db.productSEOText.Where(x => x.websiteFK == ProductSEOText.websiteFK &&
                x.productTypeFK == ProductSEOText.productTypeFK &&
                x.paragraphNo == ProductSEOText.paragraphNo &&
                x.entryNo == ProductSEOText.entryNo &&
                x.isOwnBrand == ProductSEOText.isOwnBrand &&
                x.isAssembly == ProductSEOText.isAssembly &&
                x.isMaintenance == ProductSEOText.isMaintenance).FirstOrDefault();

            if (pst != null)
                throw new Exception("Product SEO Text already exists for specified criteria.");
        }

        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            ProductTypeNameList = GetProductTypeNames();
        }
    }
}
