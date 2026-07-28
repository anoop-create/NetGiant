using Microsoft.Web.Administration;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Requests.RequestError;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class IISUtilities
    {
        public IISUtilities(Dictionary<string, string> parms)
        {
            Parms = parms;
        }

        public Dictionary<string, string> Parms { get; set; }

        public void Check_Restart()
        {
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    foreach (ApplicationPool appPool in serverManager.ApplicationPools)
                    {
                        switch (appPool.State)
                        {
                            case ObjectState.Stopped:
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = appPool.Name + " is found to be stopped. Attempting to restart.", ErrorCode = "ERROR" });
                                appPool.Start();
                                break;

                            case ObjectState.Starting:
                                // Do nothing — it's already starting
                                break;

                            case ObjectState.Started:
                                // Do nothing — it's already started
                                break;

                            case ObjectState.Stopping:
                                // Do nothing — wait until it's stopped
                                break;

                            default:
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = appPool.Name + " is found to be in an unknown state. Attempting to restart.", ErrorCode = "ERROR" });
                                appPool.Start();
                                break;
                        }
                    }

                    foreach (Site site in serverManager.Sites)
                    {
                        if (site.Name != "Default Web Site")
                        {
                            if (site.State != ObjectState.Started)
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = site.Name + " is found to be stopped. Attempting to restart.", ErrorCode = "ERROR" });
                                site.Start();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error in IISUtilities.Check_Restart: " + ex.Message, ErrorCode = "ERROR" });
            }
        }
    }
}
