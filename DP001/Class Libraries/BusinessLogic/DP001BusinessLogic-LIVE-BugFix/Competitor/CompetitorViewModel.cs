using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.ViewModels
{
    public class CompetitorViewModel
    {
        public CompetitorViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public IQueryable<Competitor> CompetitorList { get; set; }
        public List<CompetitorInventory> CompetitorsList { get; set; }
        public IQueryable<CompetitorInventory> InventoryList { get; set; }
        public Competitor CompetitorEntry { get; set; }
        public List<CompetitorInventory> SearchResults { get; set; }
        private int _channelId;
        private DP001Entities _ctx;

        public CompetitorViewModel GetInventory()
        {
            var crud = new CrudCompetitorInventory();
            InventoryList = crud.ReadCompetitorInventoryQuery(x => x.ChannelFK == _channelId, _ctx);

            return this;
        }

        public Competitor GetCompetitor(int id)
        {
            var crudCompetitor = new CrudCompetitor();
            return crudCompetitor.Read(id);
        }

        public void GetCompetitors(int productID)
        {
            var crudCompetitors = new CrudCompetitorInventory();
            CompetitorsList = crudCompetitors.Read(x => x.ChannelFK == _channelId && x.ProductInventoryFK == productID);
        }

        public CompetitorViewModel GetCompetitorList()
        {
            var crud = new CrudCompetitor();
            CompetitorList = crud.ReadCompetitorQuery(x => x.ChannelFK == _channelId, _ctx);

            return this;
        }

        public SaveReturn Update(Competitor competitorEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudCompetitor();

                var isFound = crud.Read(x => x.ChannelFK == competitorEntry.ChannelFK
                    && x.CompetitorID == competitorEntry.CompetitorID).Count > 0;

                if (isFound)
                {
                    crud.Update(competitorEntry);
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

        public CompetitorViewModel SearchInventory(string term, int brandFK)
        {
            CrudCompetitorInventory crud = new CrudCompetitorInventory();
            SearchResults = crud.Read(x =>
                (x.ManufacturerPartNo.Contains(term) &&
                x.BrandFK == brandFK &&
                x.ChannelFK == _channelId), 20);

            return this;
        }

        public Stream CreateExportFile()
        {
            var data = InventoryList.Select(x => new
            {
                Part_Number = x.ProductInventory.ManufacturerPartNo,
                Competitor_Part_Number = x.ManufacturerPartNo,
                Brand = x.Brand.BrandName,
                Competitor_Name = x.Competitor.CompetitorName,
                Price = x.Price.ToString()
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }
    }
}
