using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FilterableAttributeViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public FilterableAttributeViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikFilterableAttribute> FilterableAttributeList { get; set; }

        public FilterableAttributeViewModel Get()
        {
            FilterableAttributeList = _ctx.filterableAttribute
                                          .Select(x => new TelerikFilterableAttribute
                                          {
                                              Id = x.filterableAttributeID,
                                              Name = x.attributeName
                                          })
                                          .AsQueryable();
            return this;
        }
    }

    public class TelerikFilterableAttribute
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
