using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using MoreLinq;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudPriceRule
    {
        public PriceRule Create(PriceRule obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.PriceRuleID == 0)
                {
                    string oldValues = "";
                    string newValues = BuildAuditString(obj);
                    var crudAudit = new CrudTenantAudit();
                    TenantAudit ta = crudAudit.BuildTenantAuditRecord(obj.ChannelFK, "A", "Price Rule - " + obj.RuleName, oldValues, newValues);
                    crudAudit.Create(ta);

                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public PriceRule Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.PriceRules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.ProductCategory)
                    .Include(x => x.Brand)
                    .Include(x => x.ProductInventory)
                    .Where(x => x.PriceRuleID == id)
                    .FirstOrDefault();
            }
        }

        public List<PriceRule> Read(Expression<Func<PriceRule, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.PriceRules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.ProductInventory)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public List<PriceRule> ReadList(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.PriceRules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.ProductCategory)
                    .Include(x => x.Brand)
                    .Where(x => x.PriceRuleID == id)
                    .ToList();
            }
        }

        public IPagedList<PriceRule> ReadPagedList(
            Expression<Func<PriceRule, bool>> where,
            int pageNumber,
            string sortOrder)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.PriceRules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Include(x => x.Brand)
                    .Include(x => x.ProductCategory)
                    .Include(x => x.ProductInventory)
                    .AsQueryable();

                query = query.Where(where);
                query = SetSortOrder(sortOrder, query);

                return query.ToPagedList(pageNumber, 200);
            }
        }

        public IQueryable<PriceRuleInfo> ReadPriceRulesQuery(
            Expression<Func<PriceRule, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.PriceRules
                .Include(x => x.Lookup)
                .Include(x => x.Lookup1)
                .Include(x => x.Brand)
                .Include(x => x.ProductCategory)
                .Include(x => x.ProductInventory)
                .Where(where)
                .GroupBy(m => new { m.ProductCategoryFK, m.RuleTypeFK, m.BrandFK, m.ProductInventoryFK, m.IsTest })
                .Select(x => new PriceRuleInfo { Rule = x.FirstOrDefault(), ProductCount = x.Sum(z => z.ProductCount) })
                .OrderBy(x => x.Rule.RuleName)
                .AsQueryable();

            return query;
        }

        public int GetTenantRuleCount(int tenantID)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.PriceRules
                    .Where(x => x.Channel.TenantFK == tenantID && x.IsActive)
                    .Count();
            }
        }

        public void Update(PriceRule obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var currObj = Read(obj.PriceRuleID);
                string oldValues = BuildAuditString(currObj);
                string newValues = BuildAuditString(obj);
                var crudAudit = new CrudTenantAudit();
                TenantAudit ta = crudAudit.BuildTenantAuditRecord(obj.ChannelFK, "C", "Price Rule - " + obj.RuleName, oldValues, newValues);
                crudAudit.Create(ta);

                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public bool Delete(PriceRule obj)
        {
            bool isSuccess = false;

            string oldValues = BuildAuditString(obj);
            string newValues = "";
            var crudAudit = new CrudTenantAudit();
            TenantAudit ta = crudAudit.BuildTenantAuditRecord(obj.ChannelFK, "D", "Price Rule - " + obj.RuleName, oldValues, newValues);
            crudAudit.Create(ta);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@PriceRuleID", SqlDbType.Int);
            sqlParm1.Value = obj.PriceRuleID;
            sqlParms.Add(sqlParm1);

            isSuccess = SQL.ExecuteStoredProcedure("DP001", "DeletePriceRule", sqlParms, obj.ChannelFK);

            return isSuccess;
        }

        private static IQueryable<PriceRule> SetSortOrder(string sortOrder, IQueryable<PriceRule> query)
        {
            switch (sortOrder)
            {
                case "type":
                    query = query.OrderBy(x => x.Lookup1.LookupName);
                    break;
                case "type_desc":
                    query = query.OrderByDescending(x => x.Lookup1.LookupName);
                    break;
                case "method":
                    query = query.OrderBy(x => x.Lookup.LookupName);
                    break;
                case "method_desc":
                    query = query.OrderByDescending(x => x.Lookup.LookupName);
                    break;
                case "category":
                    query = query.OrderBy(x => x.ProductCategory.CategoryName);
                    break;
                case "category_desc":
                    query = query.OrderByDescending(x => x.ProductCategory.CategoryName);
                    break;
                case "rulename":
                    query = query.OrderBy(x => x.RuleName);
                    break;
                case "rulename_desc":
                    query = query.OrderByDescending(x => x.RuleName);
                    break;
                case "brand":
                    query = query.OrderBy(x => x.Brand.BrandName);
                    break;
                case "brand_desc":
                    query = query.OrderByDescending(x => x.Brand.BrandName);
                    break;
                case "product":
                    query = query.OrderBy(x => x.ProductInventory.ManufacturerPartNo);
                    break;
                case "product_desc":
                    query = query.OrderByDescending(x => x.ProductInventory.ManufacturerPartNo);
                    break;
                default:
                    query = query.OrderBy(x => x.RuleName);
                    break;
            }

            return query;
        }

        public List<PriceRule> GetRelatedBands(PriceRule rule, int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var bands = db.PriceRules
                    .Where(x => x.RuleTypeFK == rule.RuleTypeFK && x.ChannelFK == channelId);

                switch (rule.Lookup1.LookupName)
                {
                    case "Category":
                        bands = bands
                            .Where(x => x.ProductCategoryFK == rule.ProductCategoryFK
                            && x.BrandFK == null
                            && x.ProductInventoryFK == null);
                        break;
                    case "Brand":
                        bands = bands
                            .Where(x => x.ProductCategoryFK == rule.ProductCategoryFK
                            && x.BrandFK == rule.BrandFK
                            && x.ProductInventoryFK == null);
                        break;
                    case "Product":
                        bands = bands
                            .Where(x => x.ProductCategoryFK == rule.ProductCategoryFK
                            && x.BrandFK == null
                            && x.ProductInventoryFK == rule.ProductInventoryFK);
                        break;
                    case "Universal":
                        bands = bands
                            .Where(x => x.ProductCategoryFK == null
                            && x.BrandFK == null
                            && x.ProductInventoryFK == null);
                        break;
                    default:
                        break;
                }

                return bands.OrderBy(x => x.BandStart).ToList();
            }
        }

        public string BuildAuditString(PriceRule pr)
        {
            string values;

            var crudLookup = new CrudLookup();

            values = "Channel|" + pr.ChannelFK.ToString();
            values += "#Rule Name|" + pr.RuleName;
            values += "#Rule Type|" + crudLookup.Read(x => x.LookupType.LookupTypeName == "PriceRuleType" && x.LookupID == pr.RuleTypeFK).FirstOrDefault().LookupName;
            values += "#Brand|" + pr.BrandFK.ToString();
            values += "#Product Category|" + pr.ProductCategoryFK.ToString();
            values += "#Method|" + crudLookup.Read(x => x.LookupType.LookupTypeName == "Method" && x.LookupID == pr.MethodFK).FirstOrDefault().LookupName;
            values += "#Is Banded|" + pr.IsBanding.ToString();
            values += "#Is Active|" + pr.IsActive.ToString();
            values += "#Is Test|" + pr.IsTest.ToString();
            values += "#Band Name|" + (pr.BandName != null ? pr.BandName : String.Empty);
            values += "#Band Start|" + (pr.BandStart == null ? "" : pr.BandStart.ToString());
            values += "#Band End|" + (pr.BandEnd == null ? "" : pr.BandEnd.ToString());
            values += "#Uplift Is %|" + pr.UpliftIsPc.ToString();
            values += "#Cost Uplift|" + pr.CostUplift.ToString();
            values += "#Margins Are %|" + pr.MarginsArePc.ToString();
            values += "#Desired Margin|" + pr.DesiredMargin.ToString();
            values += "#Min Margin|" + pr.MinMargin.ToString();
            values += "#Max Margin|" + pr.MaxMargin.ToString();
            values += "#Beat Rate|" + pr.BeatRate.ToString();
            values += "#Nudge|" + pr.Nudge.ToString();
            values += "#Fixed Price Override|" + pr.FixedPriceOverride.ToString();
            values += "#Alt PriceAdj 1|" + pr.AltPriceAdj1.ToString();
            values += "#Alt PriceAdj 2|" + pr.AltPriceAdj2.ToString();
            values += "#Alt PriceAdj 3|" + pr.AltPriceAdj3.ToString();
            values += "#Alt PriceAdj 4|" + pr.AltPriceAdj4.ToString();
            values += "#Alt PriceAdj 5|" + pr.AltPriceAdj5.ToString();
            values += "#Alt PriceAdj 6|" + pr.AltPriceAdj6.ToString();
            values += "#Alt PriceAdj 7|" + pr.AltPriceAdj7.ToString();
            values += "#Alt PriceAdj 8|" + pr.AltPriceAdj8.ToString();
            values += "#Alt PriceAdj 9|" + pr.AltPriceAdj9.ToString();
            values += "#Alt PriceAdj 10|" + pr.AltPriceAdj10.ToString();
            values += "#Compat Discount|" + pr.CompatDiscount.ToString();

            return values;
        }

    }
}
