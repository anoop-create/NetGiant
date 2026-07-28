using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using Customer = netGiant.Intranet.DataLayer.CustomerData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Ngmd = netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace netGiant.Intranet.BusinessLayer.ViewModels.CrMS
{
    public class LookupViewModel : CommonViewModel
    {
        public LookupViewModel()
        {
            _ctx = new Customer.customerEntities();
            _ngmd = new Ngmd.ngmdEntities();
        }

        public IQueryable<TelerikL> LookupList { get; set; }
        public IQueryable<TelerikLT> LookupTypeList { get; set; }
        public Customer.Lookup CustomerLookupEntry { get; set; }
        public Ngmd.Lookup NgmdLookupEntry { get; set; }
        public Customer.LookupType CustomerLookupTypeEntry { get; set; }
        public Ngmd.LookupType NgmdLookupTypeEntry { get; set; }
        private Customer.customerEntities _ctx;
        private Ngmd.ngmdEntities _ngmd;

        public List<SelectListItem> CustomerTypeNameList { get; set; }
        public List<SelectListItem> NgmdTypeNameList { get; set; }
        public LookupScope Scope { get; set; }

        public LookupViewModel GetLookups()
        {
            var ngmdlist = _ngmd.Lookup
                            .Include(x => x.LookupType)
                            .Select(x => new TelerikL
                            {
                                LookupTypeId = x.LookupType.LookupTypeId,
                                TypeName = x.LookupType.LookupTypeName,
                                LookupId = x.LookupId,
                                AltLookupId = x.AltLookupId,
                                Name = x.LookupName,
                                Sequence = x.Sequence ?? 0,
                                LookupScope = LookupScope.NetgiantMasterData
                            })
                            .ToList();

             var customerlist = _ctx.Lookup
                             .Include(x => x.LookupType)
                             .Select(o => new TelerikL
                             {
                                 LookupTypeId = o.LookupType.LookupTypeID,
                                 TypeName = o.LookupType.LookupTypeName,
                                 LookupId = o.LookupID,
                                 AltLookupId = o.AltLookupId,
                                 Name = o.LookupName,
                                 Sequence = o.Sequence ?? 0,
                                 LookupScope = LookupScope.CustomerData
                             })
                             .ToList();

            LookupList = ngmdlist.Concat(customerlist).AsQueryable();

            return this;
        }

        public LookupViewModel GetLookupTypes()
        {
            var ngmdlist = _ngmd.LookupType
                            .Select(x => new TelerikLT
                            {
                                LookupTypeId = x.LookupTypeId,
                                TypeName = x.LookupTypeName,
                                LookupScope = LookupScope.NetgiantMasterData
                            })
                            .ToList();

            var customerlist = _ctx.LookupType
                            .Select(o => new TelerikLT
                            {
                                LookupTypeId = o.LookupTypeID,
                                TypeName = o.LookupTypeName,
                                LookupScope = LookupScope.CustomerData
                            })
                            .ToList();

            LookupTypeList = ngmdlist.Concat(customerlist).AsQueryable();

            return this;
        }

        public LookupViewModel CreateLookupType(int id, LookupScope scope)
        {
            CreateLookupType();

            if (scope == LookupScope.CustomerData)
            {
                using (var db = new Customer.customerEntities())
                {
                    CustomerLookupTypeEntry = db.LookupType
                        .FirstOrDefault(x => x.LookupTypeID == id);
                }
            }
            else
            {
                using (var db = new Ngmd.ngmdEntities())
                {
                    NgmdLookupTypeEntry = db.LookupType
                        .FirstOrDefault(x => x.LookupTypeId == id);
                }
            }

            return this;
        }

        public LookupViewModel CreateLookup()
        {
            CustomerLookupEntry = new Customer.Lookup();
            NgmdLookupEntry = new Ngmd.Lookup();
            SetupSelectLists();

            return this;
        }

        public LookupViewModel CreateLookup(int id, LookupScope scope)
        {
            CreateLookup();

            if (scope == LookupScope.CustomerData)
            {
                using (var db = new Customer.customerEntities())
                {
                    CustomerLookupEntry = db.Lookup
                        .Include(x => x.LookupType)
                        .FirstOrDefault(x => x.LookupID == id);
                }
            }
            else
            {
                using (var db = new Ngmd.ngmdEntities())
                {
                    NgmdLookupEntry = db.Lookup
                        .Include(x => x.LookupType)
                        .FirstOrDefault(x => x.LookupId == id);
                }
            }

            return this;
        }

        public LookupViewModel CreateLookupType()
        {
            CustomerLookupTypeEntry = new Customer.LookupType();
            NgmdLookupTypeEntry = new Ngmd.LookupType();
            //SetupSelectLists();

            return this;
        }

        public bool SaveCustomerLookup()
        {
            bool success = true;
            try
            {
                using (var db = new Customer.customerEntities())
                {
                    if (CustomerLookupEntry.LookupID > 0)
                    {
                        db.Entry(CustomerLookupEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if Lookup already exists
                        CheckCustomerLookupExists(db);
                        db.Entry(CustomerLookupEntry).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }                
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool SaveCustomerLookupType()
        {
            bool success = true;
            try
            {
                using (var db = new Customer.customerEntities())
                {
                    if (CustomerLookupTypeEntry.LookupTypeID > 0)
                    {
                        db.Entry(CustomerLookupTypeEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if LookupType already exists
                        CheckCustomerLookupTypeExists(db);
                        db.Entry(CustomerLookupTypeEntry).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool SaveNgmdLookup()
        {
            bool success = true;
            try
            {
                using (var db = new Ngmd.ngmdEntities())
                {
                    if (NgmdLookupEntry.LookupId > 0)
                    {
                        db.Entry(NgmdLookupEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if Lookup already exists
                        CheckNgmdLookupExists(db);
                        db.Entry(NgmdLookupEntry).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }

                // Refresh cache
                DataCache.GetNgmdLookups(x => true, true);
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool SaveNgmdLookupType()
        {
            bool success = true;
            try
            {
                using (var db = new Ngmd.ngmdEntities())
                {
                    if (NgmdLookupTypeEntry.LookupTypeId > 0)
                    {
                        db.Entry(NgmdLookupTypeEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if Lookup already exists
                        CheckNgmdLookupTypeExists(db);
                        db.Entry(NgmdLookupTypeEntry).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public SaveReturn DeleteLookup(int id, LookupScope scope)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    if (scope == LookupScope.CustomerData)
                    {
                        using (var db = new Customer.customerEntities())
                        {
                            var e = db.Lookup.FirstOrDefault(x => x.LookupID == id);
                            db.Entry(e).State = EntityState.Deleted;
                            db.SaveChanges();
                            sr.IsSuccess = true;
                        }
                    }
                    else
                    {
                        using (var db = new Ngmd.ngmdEntities())
                        {
                            var e = db.Lookup.FirstOrDefault(x => x.LookupId == id);
                            db.Entry(e).State = EntityState.Deleted;
                            db.SaveChanges();
                            sr.IsSuccess = true;
                        }

                        // Refresh cache
                        DataCache.GetNgmdLookups(x => true, true);
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

        public SaveReturn DeleteLookupType(int id, LookupScope scope)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    if (scope == LookupScope.CustomerData)
                    {
                        using (var db = new Customer.customerEntities())
                        {
                            var e = db.LookupType.FirstOrDefault(x => x.LookupTypeID == id);
                            db.Entry(e).State = EntityState.Deleted;
                            db.SaveChanges();
                            sr.IsSuccess = true;
                        }
                    }
                    else
                    {
                        using (var db = new Ngmd.ngmdEntities())
                        {
                            var e = db.LookupType.FirstOrDefault(x => x.LookupTypeId == id);
                            db.Entry(e).State = EntityState.Deleted;
                            db.SaveChanges();
                            sr.IsSuccess = true;
                        }
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

        public void SetupSelectLists()
        {
            CustomerTypeNameList = GetCustomerTypeNames();
            NgmdTypeNameList = GetNgmdTypeNames();
        }

        private void CheckCustomerLookupExists(Customer.customerEntities db)
        {
            var l = new Customer.Lookup();

            l = db.Lookup.FirstOrDefault(x => x.LookupTypeFK == CustomerLookupEntry.LookupTypeFK &&
                                         x.LookupName == CustomerLookupEntry.LookupName);

            if (l != null)
                throw new Exception("Lookup already exists.");
        }

        private void CheckCustomerLookupTypeExists(Customer.customerEntities db)
        {
            var l = new Customer.LookupType();

            l = db.LookupType.FirstOrDefault(x => x.LookupTypeName == CustomerLookupTypeEntry.LookupTypeName);

            if (l != null)
                throw new Exception("Lookup already exists.");
        }

        private void CheckNgmdLookupExists(Ngmd.ngmdEntities db)
        {
            var l = new Ngmd.Lookup();

            l = db.Lookup.FirstOrDefault(x => x.LookupTypeFk == NgmdLookupEntry.LookupTypeFk &&
                                         x.LookupName == NgmdLookupEntry.LookupName);

            if (l != null)
                throw new Exception("Lookup already exists.");
        }

        private void CheckNgmdLookupTypeExists(Ngmd.ngmdEntities db)
        {
            var l = new Ngmd.LookupType();

            l = db.LookupType.FirstOrDefault(x => x.LookupTypeName == NgmdLookupTypeEntry.LookupTypeName);

            if (l != null)
                throw new Exception("Lookup already exists.");
        }

        public List<SelectListItem> GetCustomerTypeNames()
        {
            List<SelectListItem> oList;

            using (var db = new Customer.customerEntities())
            {
                oList = db.LookupType
                    .OrderBy(x => x.LookupTypeName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.LookupTypeID.ToString(),
                        Text = x.LookupTypeName.ToString()
                    }).ToList();
            }
            return oList;
        }

        public List<SelectListItem> GetNgmdTypeNames()
        {
            List<SelectListItem> oList;

            using (var db = new Ngmd.ngmdEntities())
            {
                oList = db.LookupType
                    .OrderBy(x => x.LookupTypeName)
                    .Select(x => new SelectListItem
                    {
                        Value = x.LookupTypeId.ToString(),
                        Text = x.LookupTypeName.ToString()
                    }).ToList();
            }
            return oList;
        }

        public class TelerikL
        {
            public int LookupTypeId { get; set; }
            public int LookupId { get; set; }
            public int? AltLookupId { get; set; }
            public string TypeName { get; set; }
            public string Name { get; set; }
            public short Sequence { get; set; }
            public LookupScope LookupScope { get; set; }
        }

        public class TelerikLT
        {
            public int LookupTypeId { get; set; }
            public string TypeName { get; set; }
            public LookupScope LookupScope { get; set; }
        }

        public enum LookupScope
        {
            NetgiantMasterData,
            CustomerData
        }
    }
}
