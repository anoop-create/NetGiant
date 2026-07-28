using System;
using System.Web;

namespace BusinessLogic
{
    public class Delivery
    {
        /// <summary>
        /// Set the default delivery date
        /// </summary>
        public static void SetDeliveryDate()
        {
            DateTime currentDate = DateTime.Now;
            DateTime deliveryDate = new DateTime();
            TimeSpan cutOffTime = new TimeSpan(17, 30, 00);

            switch (currentDate.DayOfWeek)
            {
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(2);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(1);
                        }
                        break;
                    }
                case DayOfWeek.Thursday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(4);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(1);
                        }
                        break;
                    }
                case DayOfWeek.Friday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(4);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(3);
                        }
                        break;
                    }
                case DayOfWeek.Saturday:
                    {
                        deliveryDate = currentDate.AddDays(3);
                        break;
                    }
                case DayOfWeek.Sunday:
                    {
                        deliveryDate = currentDate.AddDays(2);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            HttpContext.Current.Session["D_StandardDeliveryDate"] = deliveryDate;
            HttpContext.Current.Session["D_SaturdayDeliveryDate"] = deliveryDate.AddDays((int)DayOfWeek.Saturday - (int)deliveryDate.DayOfWeek);
            HttpContext.Current.Session["D_StandardDeliveryDay"] = deliveryDate.DayOfWeek.ToString();

            string ext = "th";
            switch (deliveryDate.Day)
            {
                case 1:
                case 21:
                case 31:
                    {
                        ext = "st";
                        break;
                    }
                case 2:
                case 22:
                    {
                        ext = "nd";
                        break;
                    }
                case 3:
                case 23:
                    {
                        ext = "rd";
                        break;
                    }
            }
            HttpContext.Current.Session["D_StandardDeliveryMonthDay"] = deliveryDate.Day.ToString() + ext;
        }
    }
}
