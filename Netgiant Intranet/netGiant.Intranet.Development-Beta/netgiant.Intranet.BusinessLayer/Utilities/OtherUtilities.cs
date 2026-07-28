using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class OtherUtilities
    {
        public static string GetClientIPAddress(HttpRequestBase request)
        {
            // CloudFlare
            if (request.Headers["CDN-LOOP"] != null)
            {
                return request.Headers["CF-CONNECTING-IP"].Split(',')[0];
            }
            else
            {
                //Forwarded For
                if (request.Headers["X-FORWARDED-FOR"] != null)
                {
                    return request.Headers["X-FORWARDED-FOR"].Split(',')[0];
                }
            }

            //None
            return request.UserHostAddress.Split(',')[0];
        }

        public static bool IpAddressIsAllowed(string ip)
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"])
                    .Split(',');
                return addresses.Any(a => ip.Contains(a.Trim()));
            }
            return false;
        }

        public static void SetTlsVersion()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
        }
    }
}
