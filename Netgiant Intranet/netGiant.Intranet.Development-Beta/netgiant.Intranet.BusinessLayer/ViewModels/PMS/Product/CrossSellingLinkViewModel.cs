using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class CrossSellingLinkViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public CrossSellingLinkViewModel()
        {
            AllManufacturers = SelectListViewModel.GetAllManufacturers();
            AllCrossSellingLinkTypes = SelectListViewModel.GetNgmdLookupSelectList("CrossSellingLinkType");
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikCrossSellingLink> CrossSellingLinkList { get; set; }
        public crossSellingLink CrossSellingLink { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public IQueryable<SelectListItem> AllCrossSellingLinkTypes { get; set; }
        public IQueryable<SelectListItem> ProductList { get; set; }
        public List<crossSellingLink> CrossSellingLinkForExport { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public bool TwoWayLink { get; set; }

        private string _searchTerm;
        private string _searchBy;
        private string _orderBy;
        private int? _manufacturerFK;

        public CrossSellingLinkViewModel Get()
        {
            CrossSellingLinkList = _ctx.crossSellingLink
                                       .Select(x => new TelerikCrossSellingLink
                                       {
                                           Id = x.crossSellingLinkID,
                                           PartNoA = x.product.partNo,
                                           ProductNameA = x.product.productName,
                                           PartNoB = x.product1.partNo,
                                           ProductNameB = x.product1.productName,
                                           Type = (_ctx.Lookup
                                            .Where(y => y.LookupType.LookupTypeName == "CrossSellingLinkType" && y.AltLookupId == x.crossSellingLinkTypeFK)
                                            .AsQueryable()
                                            .FirstOrDefault()
                                            .LookupName),
                                       })
                                       .AsQueryable();
            return this;
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
            model.ProductList = SelectListViewModel.GetAllProductsPartNoDesc();

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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn { IsSuccess = true };
            try
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
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }

        public void CreateCrossSellingLinkCSVFile(List<TelerikCrossSellingLink> cslList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\CrossSellingLinkTrackingExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikCrossSellingLink csl in cslList)
                {
                    InsertCSVData(writer, csl);
                }
            }

        }

        private void InsertCSVData(CsvFileWriter writer, TelerikCrossSellingLink item)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(item.Id.ToString());
            newRow.Add(item.ProductNameA);
            newRow.Add(item.PartNoA);
            newRow.Add(item.ProductNameB);
            newRow.Add(item.PartNoB);
            newRow.Add(item.Type);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Cross Selling Link Id");
            firstRow.Add("Product Name A");
            firstRow.Add("Part Number A");            
            firstRow.Add("Product Name B");
            firstRow.Add("Part Number B");
            firstRow.Add("Type");

            writer.WriteRow(firstRow);
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

    public class TelerikCrossSellingLink
    {
        public int Id { get; set; }
        public string PartNoA { get; set; }
        public string ProductNameA { get; set; }
        public string PartNoB { get; set; }
        public string ProductNameB { get; set; }
        public string Type { get; set; }
    }
}
