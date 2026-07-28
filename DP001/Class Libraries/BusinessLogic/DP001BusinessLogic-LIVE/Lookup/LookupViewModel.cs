using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity.Validation;

namespace DP001BusinessLogic.ViewModels
{
    public class LookupViewModel
    {
        public LookupViewModel()
        {
            _ctx = new DP001Entities();
        }

        public IQueryable<Lookup> LookupList { get; set; }
        public IQueryable<LookupType> LookupTypeList { get; set; }
        public Lookup LookupEntry { get; set; }
        public LookupType LookupTypeEntry { get; set; }
        public List<SelectListItem> LookupTypes { get; set; }

        private DP001Entities _ctx;

        public LookupViewModel GetLookups()
        {
            var crud = new CrudLookup();
            LookupList = crud.ReadLookupsQuery(x => x.LookupID == x.LookupID, _ctx);

            return this;
        }

        public LookupViewModel GetLookupTypes()
        {
            var crud = new CrudLookup();
            LookupTypeList = crud.ReadLookupTypesQuery(x => x.LookupTypeID == x.LookupTypeID, _ctx);

            return this;
        }

        public LookupViewModel New()
        {
            LookupEntry = new Lookup();
            LookupTypes = SharedViewModel.GetLookupTypeList();

            return this;
        }

        public SaveReturn Create(Lookup lookup)
        {
            var saveReturn = new SaveReturn();
            var crud = new CrudLookup();
            bool isValid = true;

            //foreach (Lookup l in Channel.PriceRules)
            //{
            //    //Unique Rule name check
            //    if (l.LookupName.ToLower() == lookup.LookupName.ToLower())
            //    {
            //        saveReturn.Message = "You cannot add a lookup with the same name as an existing rule";
            //        isValid = false;
            //    }
            //}

            if (!isValid)
            {
                saveReturn.IsSuccess = false;
                return saveReturn;
            }

            try
            {
                //crud.Create(lookup);
                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
                saveReturn.InnerException = e.InnerException != null ? e.InnerException.ToString() : "";

                if (e is DbEntityValidationException)
                {
                    var entityException = (DbEntityValidationException)e;
                    var errorMessages = entityException.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                    saveReturn.EntityValidationError = string.Join("; ", errorMessages);
                }
            }

            return saveReturn;
        }
    }
}
