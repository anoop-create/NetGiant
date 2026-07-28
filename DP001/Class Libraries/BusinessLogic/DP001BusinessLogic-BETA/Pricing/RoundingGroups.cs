using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.Pricing
{
    public class RoundingGroups
    {
        public static void SetGroup1Prices(ProductInventory product)
        {
            product.Price = Group1Calculation(product.Price);
            product.AltPrice1 = Group1Calculation(product.AltPrice1 ?? 0);
            product.AltPrice2 = Group1Calculation(product.AltPrice2 ?? 0);
            product.AltPrice3 = Group1Calculation(product.AltPrice3 ?? 0);
            product.AltPrice4 = Group1Calculation(product.AltPrice4 ?? 0);
            product.AltPrice5 = Group1Calculation(product.AltPrice5 ?? 0);
            product.AltPrice6 = Group1Calculation(product.AltPrice6 ?? 0);
            product.AltPrice7 = Group1Calculation(product.AltPrice7 ?? 0);
            product.AltPrice8 = Group1Calculation(product.AltPrice8 ?? 0);
            product.AltPrice9 = Group1Calculation(product.AltPrice9 ?? 0);
            product.AltPrice10 = Group1Calculation(product.AltPrice10 ?? 0);
        }

        private static decimal Group1Calculation(decimal price)
        {
            decimal roundedPrice = 0;

            if (price > 0)
            {
                if (price.IsBetween(0, 10))
                {
                    roundedPrice = Math.Round((Math.Ceiling(price / (decimal)0.1) * (decimal)0.1) - (decimal)0.01, 2);
                }
                else if (price.IsBetween(10, 100))
                {
                    var decimalPart = price - Math.Floor(price);
                    if (decimalPart < (decimal)0.6)
                    {
                        roundedPrice = Math.Floor(price) + (decimal)0.59;
                    }
                    else
                    {
                        roundedPrice = Math.Floor(price) + (decimal)0.99;
                    }
                }
                else if (price.IsBetween(100, 999999999))
                {
                    var decimalPart = price - Math.Floor(price);
                    roundedPrice = Math.Floor(price) + (decimal)0.99;
                }


                // Old Rules - Removed 20/10/2016 as Per Anthony McMahon request. Change is above
                //if (price.IsBetween(0, 10))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / (decimal)0.1) * (decimal)0.1) - (decimal)0.01, 2);
                //}
                //else if (price.IsBetween(10, 20))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / (decimal)0.5) * (decimal)0.5) - (decimal)0.01, 2);
                //}
                //else if (price.IsBetween(20, 100))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / 1) * 1) - (decimal)0.01, 2);
                //}
                //else if (price.IsBetween(100, 250))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / 5) * 5) - (decimal)0.01, 2);
                //}
                //else if (price.IsBetween(250, 1000))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / 10) * 10) - (decimal)0.01, 2);
                //}
                //else if (price.IsBetween(1000, 999999999))
                //{
                //    roundedPrice = Math.Round((Math.Ceiling(price / 50) * 50) - (decimal)0.01, 2);
                //}
            }

            return roundedPrice;
        }
    }

    public static class BetweenExtension
    {
        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                && Comparer<T>.Default.Compare(item, end) < 0;
        }
    }
}


