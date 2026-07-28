using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class SupplierViewModel
    {
        public SupplierViewModel()
        {

        }

        public SupplierViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public List<Supplier> SupplierList { get; set; }
        public List<SelectListItem> SupplierSelectList { get; set; }
        public IQueryable<SupplierInventory> InventoryList { get; set; }
        public Supplier SupplierEntry { get; set; }
        public List<SupplierInventory> SearchResults { get; set; }

        public IQueryable<SupplierBrandMatching> BrandAliases { get; set; }
        public SupplierBrandMatching SupplierBrandMatchingEntry { get; set; }

        public IQueryable<SupplierMfpnMatching> MfpnAliases { get; set; }
        public SupplierMfpnMatching SupplierMfpnMatchingEntry { get; set; }
        public List<SelectListItem> MatchTypes { get; set; }
        public int ChannelID { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        public SupplierViewModel GetSupplierList()
        {
            var crud = new CrudSupplier();
            SupplierList = crud.Read(x => x.ChannelFK == _channelId);

            return this;
        }

        public SupplierViewModel GetInventory()
        {
            var crud = new CrudSupplierInventory();
            InventoryList = crud.ReadSupplierInventoryQuery(x => x.ChannelFK == _channelId, _ctx);

            return this;
        }

        public SupplierViewModel New()
        {
            SupplierEntry = new Supplier();

            return this;
        }

        public SupplierViewModel Edit(int SupplierId)
        {
            var crud = new CrudSupplier();

            SupplierEntry = crud.Read(x => x.ChannelFK == _channelId
                && x.SupplierID == SupplierId, 100)
                .FirstOrDefault();

            return this;
        }

        public void Create()
        {
            var crud = new CrudSupplier();
            crud.Create(SupplierEntry);
        }

        public void Update(Supplier supplierEntry)
        {
            var crud = new CrudSupplier();

            var isValid = crud.Read(x => x.ChannelFK == supplierEntry.ChannelFK
                && x.SupplierID == supplierEntry.SupplierID).Count > 0;

            if (isValid)
                crud.Update(supplierEntry);
        }

        public void Delete(int id)
        {
            var crud = new CrudSupplier();

            var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.SupplierID == id).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        public SupplierViewModel SearchInventory(string term, int brandFK)
        {
            var crud = new CrudSupplierInventory();
            SearchResults = crud.Read(x =>
                ((x.ManufacturerPartNo.Contains(term) ||
                x.Description.Contains(term)) &&
                x.BrandFK == brandFK &&
                x.ChannelFK == _channelId), 20);

            return this;
        }

        public Stream CreateExportFile()
        {
            var data = InventoryList.Select(x => new
            {
                Part_Number = x.ManufacturerPartNo,
                Description = x.Description,
                Product_Name = x.Description,
                Brand = x.Brand.BrandName,
                Stock = x.StockQuantity,
                Supplier_Name = x.Supplier.SupplierName,
                Price = x.Price.ToString()
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        //Brand Alias
        public SupplierViewModel GetBrandAliases()
        {
            var crud = new CrudSupplierBrandMatching();
            BrandAliases = crud.ReadQuery(x => x.Supplier.ChannelFK == _channelId, _ctx);

            return this;
        }

        public SupplierViewModel NewBrandAlias()
        {
            SupplierBrandMatchingEntry = new SupplierBrandMatching();
            GetSupplierList();
            SupplierSelectList = SupplierList.Select(x => new SelectListItem { Text = x.SupplierName, Value = x.SupplierID.ToString() }).ToList();

            return this;
        }

        public SaveReturn CreateBrandAlias(int channelId)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;
            _channelId = channelId;

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                GetSupplierList();
                var crud = new CrudSupplierBrandMatching();
                crud.Create(SupplierBrandMatchingEntry, channelId);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SupplierViewModel EditBrandAlias(int id)
        {
            var crud = new CrudSupplierBrandMatching();

            SupplierBrandMatchingEntry = crud.Read(x => x.SupplierBrandMatchingID == id && x.Supplier.ChannelFK == _channelId)
                .FirstOrDefault();

            if (SupplierBrandMatchingEntry != null)
            {
                GetSupplierList();
                SupplierSelectList = SupplierList.Select(x => new SelectListItem { Text = x.SupplierName, Value = x.SupplierID.ToString() }).ToList();
            }

            return this;
        }

        public SaveReturn UpdateBrandAlias(SupplierBrandMatching supplierBrandMatchingEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudSupplierBrandMatching();

                var isFound = crud.Read(x => x.SupplierBrandMatchingID == supplierBrandMatchingEntry.SupplierBrandMatchingID &&
                    x.Supplier.ChannelFK == ChannelID).Count > 0;

                if (isFound)
                {
                    crud.Update(supplierBrandMatchingEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public void DeleteBrandAlias(int id)
        {
            var crud = new CrudSupplierBrandMatching();

            var deleteRecord = crud.Read(x => x.SupplierBrandMatchingID == id && x.Supplier.ChannelFK == _channelId).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        //Mfpn Alias
        public SupplierViewModel GetMfpnAliases()
        {
            var crud = new CrudSupplierMfpnMatching();
            MfpnAliases = crud.ReadQuery(x => x.ChannelFK == _channelId, _ctx);

            return this;
        }

        public SupplierViewModel NewMfpnAlias()
        {
            SupplierMfpnMatchingEntry = new SupplierMfpnMatching();
            MatchTypes = SharedViewModel.GetLookupList("MfpnMatchType");

            return this;
        }

        public SaveReturn CreateMfpnAlias()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            //foreach (Supplier sup in SupplierList)
            //{
            //    //Unique Schedule name check
            //    if (sup.SupplierID == SupplierBrandMatchingEntry.SupplierFK)
            //    {
            //        sr.Message = "You cannot add a schedule with the same name as an existing schedule";
            //        isValid = false;
            //    }
            //}

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                //GetSupplierList();
                var crud = new CrudSupplierMfpnMatching();
                crud.Create(SupplierMfpnMatchingEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SupplierViewModel EditMfpnAlias(int id)
        {
            var crud = new CrudSupplierMfpnMatching();

            SupplierMfpnMatchingEntry = crud.Read(x => x.SupplierMfpnMatchingID == id && x.ChannelFK == _channelId)
                .FirstOrDefault();

            if (SupplierMfpnMatchingEntry != null)
            {
                MatchTypes = SharedViewModel.GetLookupList("MfpnMatchType");
            }

            return this;
        }

        public SaveReturn UpdateMfpnAlias(SupplierMfpnMatching supplierMfpnMatchingEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudSupplierMfpnMatching();

                var isFound = crud.Read(x => x.SupplierMfpnMatchingID == supplierMfpnMatchingEntry.SupplierMfpnMatchingID &&
                    x.ChannelFK == supplierMfpnMatchingEntry.ChannelFK).Count > 0;

                if (isFound)
                {
                    crud.Update(supplierMfpnMatchingEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public void DeleteMfpnAlias(int id)
        {
            var crud = new CrudSupplierMfpnMatching();

            var deleteRecord = crud.Read(x => x.SupplierMfpnMatchingID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }

}
