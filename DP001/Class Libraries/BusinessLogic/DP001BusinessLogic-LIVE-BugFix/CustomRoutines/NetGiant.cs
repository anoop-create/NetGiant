using DP001BusinessLogic.Pricing;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.CustomRoutines
{
    public class NetGiant
    {
        public static void Control(TargetUser targetUser, string targetFunction, Channel channel, object extras = null)
        {
            if (targetUser == TargetUser.NetGiant)
            {
                switch (targetFunction)
                {
                    case "BreakMargins":

                        BreakMargins(extras);
                        break;

                    default:
                        break;
                }
            }
        }

        private static void BreakMargins(object extras)
        {
            var priceRuleDetail = (PriceRuleDetail)extras;

            // Custom code here for NetGiant break margins
        }
    }
}
