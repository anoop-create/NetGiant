using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Orders
{
    public class SagePayViewModel
    {
        //Transaction Page Properties
        public List<SagePayTransactions> TransactionsList { get; set; }
        public SagePayTransactions Transaction { get; set; }
        public int TransactionsListCount { get; set; }
        public IQueryable<SelectListItem> StatusList { get; set; }
        //Tokens Page Properties
        public List<SagePayTokens> TokensList { get; set; }
        public SagePayTokens Token { get; set; }
        public int TokensListCount { get; set; }
        //Generic Page Properties
        public IQueryable<SelectListItem> WebsiteList { get; set; }

        public SagePayViewModel GetSagePayTransactionsData()
        {
            return GetSagePayTransactionsData(null, null, null, null, null, null, null, null);
        }

        public SagePayViewModel GetSagePayTransactionsData(string orderBy, string statusName, int? websiteID, string searchTerm,
            string searchBy, DateTime? DateFrom, DateTime? DateTo, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<SagePayTransactions> query = db.SagePayTransactions;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "id":
                            query = query.Where(x => x.doc_id.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "detail":
                            query = query.Where(x => x.protx_detail.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "uid":
                            query = query.Where(x => x.vm_uid.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(statusName))
                {
                    query = query.Where(x => x.protx_status == statusName);
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteID == websiteID);
                }

                if(DateFrom == null || DateTo == null)
                {
                    DateFrom = DateTime.Now.Date + new TimeSpan(0, 0, 0);
                    DateTo = DateTime.Now.Date + new TimeSpan(23, 59, 59);
                }

                query = query.Where(x => x.protx_time >= DateFrom && x.protx_time <= DateTo);

                switch (orderBy)
                {
                    case "idAsc":
                        query = query.OrderBy(x => x.protx_id);
                        break;
                    case "idDesc":
                        query = query.OrderByDescending(x => x.protx_id);
                        break;
                    case "timeAsc":
                        query = query.OrderBy(x => x.protx_time);
                        break;
                    case "timeDesc":
                        query = query.OrderByDescending(x => x.protx_time);
                        break;
                    case "statusAsc":
                        query = query.OrderBy(x => x.protx_status);
                        break;
                    case "statusDesc":
                        query = query.OrderByDescending(x => x.protx_status);
                        break;
                    case "detailAsc":
                        query = query.OrderBy(x => x.protx_detail);
                        break;
                    case "detailDesc":
                        query = query.OrderByDescending(x => x.protx_detail);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.websiteID);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.websiteID);
                        break;
                    case "uidAsc":
                        query = query.OrderBy(x => x.protx_uid);
                        break;
                    case "uidDesc":
                        query = query.OrderByDescending(x => x.protx_uid);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.protx_time)
                            .ThenBy(x => x.websiteID);
                        break;
                }

                TransactionsListCount = query.Count();
                TransactionsList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                WebsiteList = SelectListViewModel.AllWebsites();
                StatusList = GetStatus();
            }
            return this;
        }

        public SagePayTransactions GetProtxData(int protxID)
        {
            SagePayTransactions tran;

            using (ngmdEntities db = new ngmdEntities())
            {
                tran = db.SagePayTransactions.Find(protxID);
            }
            return tran;
        }

        private IQueryable<SelectListItem> GetStatus()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.SagePayTransactions.OrderBy(x => x.protx_status).Select(x => new SelectListItem
                {
                    Value = x.protx_status.ToString(),
                    Text = x.protx_status.ToString()
                }).Distinct().ToList().AsQueryable();
            }
            return query;
        }

        public SagePayViewModel GetSagePayTokensData()
        {
            return GetSagePayTokensData(null, null, null, null, null, null, null, null);
        }

        public SagePayViewModel GetSagePayTokensData(string orderBy, int? websiteID, string searchTerm, string searchBy,
            bool? showDeleted,DateTime? DateFrom, DateTime? DateTo, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<SagePayTokens> query = db.SagePayTokens;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "account":
                            query = query.Where(x => x.account.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "email":
                            query = query.Where(x => x.email.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "uid":
                            query = query.Where(x => x.uid.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "token":
                            query = query.Where(x => x.token.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteID == websiteID);
                }

                if (showDeleted == false || showDeleted == null)
                {
                    query = query.Where(x => x.deleted == 0);
                }

                if(DateFrom == null || DateTo == null)
                {
                    DateFrom = DateTime.Now.Date + new TimeSpan(0, 0, 0);
                    DateTo = DateTime.Now.Date + new TimeSpan(23, 59, 59);
                }

                query = query.Where(x => x.timestamp >= DateFrom && x.timestamp <= DateTo);

                switch (orderBy)
                {
                    case "accountAsc":
                        query = query.OrderBy(x => x.account);
                        break;
                    case "accountDesc":
                        query = query.OrderByDescending(x => x.account);
                        break;
                    case "emailAsc":
                        query = query.OrderBy(x => x.email);
                        break;
                    case "emailDesc":
                        query = query.OrderByDescending(x => x.email);
                        break;
                    case "uidAsc":
                        query = query.OrderBy(x => x.uid);
                        break;
                    case "uidDesc":
                        query = query.OrderByDescending(x => x.uid);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.websiteID);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.websiteID);
                        break;
                    case "tokenAsc":
                        query = query.OrderBy(x => x.token);
                        break;
                    case "tokenDesc":
                        query = query.OrderByDescending(x => x.token);
                        break;
                    case "timeAsc":
                        query = query.OrderBy(x => x.timestamp);
                        break;
                    case "timeDesc":
                        query = query.OrderByDescending(x => x.timestamp);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.timestamp)
                            .ThenBy(x => x.websiteID);
                        break;
                }

                TokensListCount = query.Count();
                TokensList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                WebsiteList = SelectListViewModel.AllWebsites();
            }
            return this;
        }

        public SagePayTokens GetCardDetails(int id)
        {
            SagePayTokens tokens;

            using (ngmdEntities db = new ngmdEntities())
            {
                tokens = db.SagePayTokens.Find(id);
            }
            return tokens;
        }
    }
}
