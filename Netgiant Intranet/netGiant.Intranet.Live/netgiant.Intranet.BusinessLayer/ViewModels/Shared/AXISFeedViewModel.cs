using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class AXISFeedViewModel
    {
        public enum AXISQueueLevel
        {
            ProductLevel,
            InventoryLevel,
            ProductFieldLevel
        }

        public AXISQueue axisQueue { get; set; }
        public IQueryable<AXISQueue> axisQueues { get; set; }

        public void Save(AXISQueue queue)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.AXISQueue.Add(queue);
                //db.SaveChanges();
            }
        }

        public static bool TestingTheTest()
        {
            Test t1 = new Test();
            t1.ID = 1;
            t1.Name = "Test1";

            Test t2 = new Test();
            t2.ID = 2;
            t2.Name = "Test2";
            t1 = t2;

            return Equals(t1, t2);
        }
    }

    public class Test
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }


}
