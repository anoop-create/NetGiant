using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.ComponentModel;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class OpenRangeAttributeViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public OpenRangeAttributeViewModel()
        {
            _ctx = new ngmdEntities();
        }

        //public IQueryable<TelerikOpenRangeReplacement> OpenRangeReplacementList { get; set; }
        public ReplacementType replacementType { get; set; }

        //public void GetReplacementAttributes()
        //{
        //    OpenRangeReplacementList =
        //        (from r in _ctx.or_replacements
        //         from s in _ctx.or_searchables.Where(s => s.searchableID == r.SearchableId).DefaultIfEmpty()
        //         where r.Deleted == false
        //         select new
        //         {
        //             ReplacementId = r.ReplacementId,
        //             SearchableId = r.SearchableId,
        //             Type = r.Type,
        //             Original = r.Original,
        //             Replacement = r.Replacement,
        //             NameId = (decimal?)s.nameID ?? 0,
        //             ProductId = r.Type == 1 ? s.prodID : 0
        //         })
        //        .AsEnumerable()
        //        .Select(x => new TelerikOpenRangeReplacement
        //        {
        //            Id = x.ReplacementId,
        //            TypeId = x.Type,
        //            Type = GetTypeDescription((ReplacementType)x.Type),
        //            NameId = (int)x.NameId,
        //            Original = x.Original,
        //            Replacement = x.Replacement,
        //            ProductId = x.ProductId
        //        }).AsQueryable();
        //}

        //public int GetSearchableId(int type, int nameId, int productId = 0)
        //{
        //    var result = 0;

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            if (type == 1)
        //            {
        //                result = db.or_searchables.Where(x => x.prodID == productId && x.nameID == nameId).Select(x => x.searchableID).FirstOrDefault();
        //            }

        //            if (type == 2)
        //            {
        //                result = db.or_searchables.Where(x => x.nameID == nameId).Select(x => x.searchableID).FirstOrDefault();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new Exception(ex.Message);
        //        }
        //    }

        //    return result;
        //}

        //public SaveReturn CreateReplacement(int type, int searchableId, string original, string replacement)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            if(type != 2)
        //            {
        //                bool filterExists = (from f in db.filterableAttribute
        //                                     where f.attributeName == replacement
        //                                     select f).Count() > 0 ? true : false;

        //                if(!filterExists)
        //                {
        //                    var filter = db.Set<filterableAttribute>();
        //                    filter.Add(new filterableAttribute
        //                    {
        //                        attributeName = replacement,
        //                        dateLastUpdate = DateTime.Now
        //                    });
        //                }
        //            }

        //            var norm = db.Set<or_replacements>();
        //            norm.Add(new or_replacements
        //            {
        //                SearchableId = searchableId,
        //                Type = type,
        //                Original = original,
        //                Replacement = replacement,
        //                Deleted = false
        //            });

        //            db.SaveChanges();
        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn SaveReplacement(int id, int type, int searchableId, string original, string replacement)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            var r = db.or_replacements.Find(id);
        //            r.SearchableId = searchableId;
        //            r.Type = type;
        //            r.Original = original;
        //            r.Replacement = replacement;

        //            db.SaveChanges();
        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn DeleteReplacement(int id)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            var r = db.or_replacements.Find(id);
        //            r.Deleted = true;

        //            db.SaveChanges();

        //            sr.IsSuccess = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn GetAttributeNames()
        //{
        //    var sr = new SaveReturn();

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            sr.ReturnData =
        //                (from s in db.or_searchables
        //                group s by  new { s.nameID, s.name } into g
        //                select new { nameId = g.Key.nameID, name = g.Key.name })
        //                .OrderBy(o => o.name)
        //                .ToList();

        //            sr.IsSuccess = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            sr.IsSuccess = true;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn GetAttributeValues(int nameId)
        //{
        //    var sr = new SaveReturn();

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            sr.ReturnData =
        //                (from s in db.or_searchables
        //                 where s.nameID == nameId
        //                 group s by new { s.value } into g
        //                 select new { value = g.Key.value })
        //                 .OrderBy(o => o.value)
        //                 .ToList();
                        
        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn GetAttributeProducts(int nameId)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            sr.ReturnData = (from os in db.or_searchables
        //                             join op in db.or_products on os.prodID equals op.prodID
        //                             join p in db.product on op.partno equals p.partNo
        //                             where os.nameID == nameId
        //                             select new
        //                             {
        //                                 ProductId = os.prodID,
        //                                 ProductName = p.productName
        //                             })
        //                             .OrderBy(o => o.ProductName)
        //                             .ToList();
        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        public string GetTypeDescription(Enum value)
        {
            var enumType = value.GetType();
            var field = enumType.GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length == 0 ? value.ToString() : ((DescriptionAttribute)attributes[0]).Description;
        }
    }

    public class TelerikOpenRangeReplacement
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public int NameId { get; set; }
        public string Original { get; set; }
        public string Replacement { get; set; }
        public int ProductId { get; set; }
    }

    public enum ReplacementType
    {
        [Description("Name (All)")]
        NameAll, 
        [Description("Name (Product)")]
        NameSpecific,
        [Description("Value")]
        ValueAll
    }
}
