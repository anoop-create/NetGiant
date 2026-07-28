using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PagedList;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FilterableAttributeViewModel
    {
        public IPagedList<filterableAttribute> filterableAttributesList { get; set; }
        public filterableAttribute filterableAttribute { get; set; }

        public FilterableAttributeViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<filterableAttribute> list = db.filterableAttribute.OrderBy(x => x.attributeName);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    list = list.Where(x => x.attributeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                }

                switch (orderBy)
                {
                    case "attributeNameAsc":
                        list = list.OrderBy(x => x.attributeName);
                        break;
                    case "attributeNameDesc":
                        list = list.OrderByDescending(x => x.attributeName);
                        break;
                    default:
                        list = list.OrderBy(x => x.attributeName);
                        break;
                }

                filterableAttributesList = list.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public FilterableAttributeViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    filterableAttribute = db.filterableAttribute.Find(id);
                }
                else
                {
                    filterableAttribute = new filterableAttribute();
                }
            }

            return this;
        }
    }
}
