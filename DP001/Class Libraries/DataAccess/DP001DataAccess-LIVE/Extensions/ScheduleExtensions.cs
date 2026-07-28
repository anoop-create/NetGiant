using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ScheduleExtensions))]
    public partial class Schedule
    {
        public int SortedDay { get; set; }
        public TimeSpan SortedTime { get; set; }
        public int RunDay { get; set; }
        public string FrequencyName { get; set; }
    }

    public class ScheduleExtensions
    {
        [Required(ErrorMessage = "Schedule Name is required")]
        [Display(Name = "Schedule Name")]
        public string ScheduleName { get; set; }

        [Required(ErrorMessage = "Run Type is required")]
        [Display(Name = "Run Type")]
        public int RunTypeFK { get; set; }

        [Required(ErrorMessage = "Frequency is required")]
        [Display(Name = "Frequency")]
        public int FrequencyFK { get; set; }

        [RequiredIf("FrequencyName == 'Weekly'", ErrorMessage = "Day Of Week is required")]
        [Display(Name = "Day Of Week")]
        public Nullable<int> DayOfWeek { get; set; }

        [Required(ErrorMessage = "Time is required, the format is HH:MM")]
        [Display(Name = "Time")]
        public Nullable<System.TimeSpan> Time { get; set; }
    }
}
