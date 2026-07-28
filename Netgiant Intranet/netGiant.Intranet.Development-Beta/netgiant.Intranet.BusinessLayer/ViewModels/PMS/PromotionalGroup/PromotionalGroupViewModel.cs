using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;
using System;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.PromotionalGroup
{
    public class PromotionalGroupViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public PromotionalGroupViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikPromotionalGroup> PromotionalGroupList { get; set; }

        public void GetPromotionalGroups()
        {
            PromotionalGroupList = _ctx.promotionalGroup
                .Select(x => new TelerikPromotionalGroup
                {
                    Id = x.promotionalGroupId,
                    Name = x.promotionalGroupName,
                    FilterName = x.filterName,
                    Active = x.active
                });
        }

        public SaveReturn CreatePromotionalGroup(string name, string filter)
        {
            var sr = new SaveReturn();

            using(ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    var pg = db.Set<promotionalGroup>();
                    pg.Add(
                        new promotionalGroup {
                            promotionalGroupName = name,
                            filterName = filter,
                            active = true
                        });

                    db.SaveChanges();
                    sr.IsSuccess = true;
                }
                catch(Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }

        public SaveReturn UpdatePromotionalGroup(int id, string name, string filter)
        {
            var sr = new SaveReturn();

            using(ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    var pg = db.promotionalGroup.Find(id);
                    pg.promotionalGroupName = name;
                    pg.filterName = filter;

                    db.SaveChanges();

                    sr.IsSuccess = true;
                }
                catch(Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }

        public SaveReturn DeletePromotionalGroup(int id)
        {
            var sr = new SaveReturn();

            using(ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    var row = db.promotionalGroup.First(x => x.promotionalGroupId == id);
                    db.promotionalGroup.Remove(row);
                    db.SaveChanges();

                    sr.IsSuccess = true;
                }
                catch(Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }

        public SaveReturn SetPromotionalGroupActive(int id)
        {
            var sr = new SaveReturn();

            using(ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    var promo = db.promotionalGroup.Find(id);
                    promo.active = !promo.active;
                    db.Entry(promo).State = EntityState.Modified;
                    db.SaveChanges();

                    sr.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }
    }

    public class TelerikPromotionalGroup
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        [Required]
        [MaxLength(200)]
        public string FilterName { get; set; }
        public bool Active { get; set; }
    }
}
