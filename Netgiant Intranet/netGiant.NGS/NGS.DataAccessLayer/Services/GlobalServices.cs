using System;
using System.Configuration;

namespace NGS.DataAccessLayer.Services
{   
    [Serializable]
    public class GlobalServices
    {
        public string ConnectionString
        {
            get
            {
                return GetConnectionString();
            }
        }

        public string MembershipConnectionString
        {
            get
            {
                return GetMembershipConnectionString();
            }
        }
        
        private string GetConnectionString()
        {
            string cs = string.Empty;
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            foreach (ConnectionStringSettings connectionString in machineConfig.ConnectionStrings.ConnectionStrings)
            {
                if (connectionString.Name == "netgiantmasterdata")
                {
                    cs = connectionString.ToString();
                }
            }

            return cs;
        }

        private string GetMembershipConnectionString()
        {
            string cs = string.Empty;
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            foreach (ConnectionStringSettings connectionString in machineConfig.ConnectionStrings.ConnectionStrings)
            {
                if (connectionString.Name == "netgiantmembership")
                {
                    cs = connectionString.ToString();
                }
            }

            return cs;
        }
    }
}
