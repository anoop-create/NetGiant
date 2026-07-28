using NGSBP.DataAccessLayer.DataUtilities;
using NGSBP.DataAccessLayer.SCOM.SImpleEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGSBP.DataAccessLayer.SCOM.Services
{
    public class KPIServices
    {
        public static List<KPISE> GetKPIData()
        {
            DataTable results = SQLUtilities.ExecuteStoredProcedureQuery("axisdiplomat", "ng_GetKPIValues");
            List<KPISE> kpiList = new List<KPISE>();

            foreach (DataRow row in results.Rows)
            {
                KPISE kpiSE = new KPISE();
                kpiSE.Orders = row["orders"].ToString();
                kpiSE.Sales = row["sales"].ToString();
                kpiSE.Cost = row["cost"].ToString();
                kpiSE.Vouchers = row["vouchers"].ToString();
                kpiSE.WebsiteID = row["website"].ToString();

                kpiList.Add(kpiSE);
            }

            return kpiList;
        }
    }
}
