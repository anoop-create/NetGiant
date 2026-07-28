using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DP001DataAccess.Utilities;

namespace DP001BusinessLogic.ViewModels
{
    public class ScheduleViewModel
    {
        public ScheduleViewModel()
        {

        }

        public ScheduleViewModel(int channelId)
        {
            _channelId = channelId;
        }

        public List<Schedule> ScheduleList { get; set; }
        public Schedule ScheduleEntry { get; set; }
        public List<SelectListItem> AllFrequencies { get; set; }
        public List<SelectListItem> AllRunTypes { get; set; }
        public List<SelectListItem> Days = new List<SelectListItem>();
        public string NextRunDate { get; set; }
        private int _channelId;
        public Channel Channel { get; set; }
        public TenantSetting Tenant { get; set; }

        public ScheduleViewModel GetSchedules()
        {
            var crud = new CrudSchedule();
            ScheduleList = crud.Read(_channelId);

            return this;
        }

        public ScheduleViewModel New()
        {
            ScheduleEntry = new Schedule();
            ScheduleEntry.Lookup = new Lookup();
            ScheduleEntry.Lookup1 = new Lookup();

            AllFrequencies = SharedViewModel.GetLookupList("Frequency");
            AllRunTypes = SharedViewModel.GetLookupList("RunType");
            GetDaysOfWeek();

            return this;
        }

        public ScheduleViewModel Edit(int id)
        {
            var crud = new CrudSchedule();

            ScheduleEntry = crud.Read(x => x.ChannelFK == _channelId
                && x.ScheduleID == id)
                .FirstOrDefault();

            AllFrequencies = SharedViewModel.GetLookupList("Frequency");
            AllRunTypes = SharedViewModel.GetLookupList("RunType");
            GetDaysOfWeek();

            return this;
        }

        public SaveReturn Create()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            foreach (Schedule sch in Channel.Schedules)
            {
                //Unique Schedule name check
                if (sch.ScheduleName.ToLower() == ScheduleEntry.ScheduleName.ToLower())
                {
                    sr.Message = "You cannot add a schedule with the same name as an existing schedule";
                    isValid = false;
                }
            }
            //Schedule limit check
            if (ScheduleEntry.IsActive)
            {
                if (activeScheduleCountExceeded(1))
                {
                    sr.Message = "The schedule cannot be added as adding it will exceed your schedule limit";
                    isValid = false;
                }
            }

            //If runtype is Load Sales History - check they don't have any already. Can only have a maximum of one.
            if (CheckSalesHistorySchedules())
            {
                sr.Message = "You cannot add another Sales History schedule. You can only have a maximum of 1.";
                isValid = false;
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudSchedule();
                crud.Create(ScheduleEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Update(Schedule scheduleEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;
            int add1 = 0;

            foreach (Schedule sch in Channel.Schedules)
            {
                //Has the existing rule been activated
                if (sch.ScheduleID == scheduleEntry.ScheduleID
                    && ScheduleEntry.IsActive
                    && sch.IsActive == false)
                {
                    add1 = 1;
                }

                //Is this the existing rule
                if (sch.ScheduleID == scheduleEntry.ScheduleID)
                {
                    continue;
                }

                //Unique Schedule name check
                if (sch.ScheduleName.ToLower() == scheduleEntry.ScheduleName.ToLower())
                {
                    sr.Message = "This schedule name already exists";
                    isValid = false;
                }
            }
            //Schedule limit check
            if (scheduleEntry.IsActive)
            {
                if (activeScheduleCountExceeded(add1))
                {
                    sr.Message = "Unable to activate schedule as activating will exceed your schedule limit";
                    isValid = false;
                }
            }

            //If runtype is Load Sales History - check they don't have any already. Can only have a maximum of one.
            if (CheckSalesHistorySchedules())
            {
                sr.Message = "You cannot add another Sales History schedule. You can only have a maximum of 1.";
                isValid = false;
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudSchedule();

                var isFound = crud.Read(x => x.ChannelFK == scheduleEntry.ChannelFK
                    && x.ScheduleID == scheduleEntry.ScheduleID).Count > 0;

                if (isFound)
                {
                    crud.Update(scheduleEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist or you do not have persmission to change it";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Delete(int id)
        {
            var saveReturn = new SaveReturn();
            var crud = new CrudSchedule();

            var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.ScheduleID == id).FirstOrDefault();

            if (deleteRecord != null)
            {
                crud.Delete(deleteRecord);
                saveReturn.IsSuccess = true;
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Schedule not found or you do not have permission to delete it";
            }

            return saveReturn;
        }

        public void FindNextRun()
        {
            if (!Channel.IsActive)
            {
                NextRunDate = "Channel is not active";
                return;
            }

            if (Channel.JobInProgress)
            {
                NextRunDate = "Pricing currently in progress";
                return;
            }

            if (ScheduleList.Count == 0)
            {
                NextRunDate = "No schedules found";
                return;
            }

            TimeSpan now = CommonDataFunctions.GetCurrentDateTime().TimeOfDay;
            int today = (int)CommonDataFunctions.GetCurrentDateTime().DayOfWeek;

            foreach (Schedule sch in ScheduleList)
            {
                if (sch.IsActive)
                {
                    if (sch.Lookup1.LookupName == "Daily")
                    {
                        if (sch.Time > now)
                        {
                            sch.SortedDay = -10;
                            sch.RunDay = today;
                        }
                        else
                        {
                            sch.SortedDay = -9;
                            sch.RunDay = today + 1;
                        }
                    }
                    if (sch.Lookup1.LookupName == "Weekly")
                    {
                        if (sch.Time > now)
                        {
                            sch.SortedDay = (int)sch.DayOfWeek - today;
                            sch.RunDay = (int)sch.DayOfWeek;
                        }
                        else
                        {
                            sch.SortedDay = (int)sch.DayOfWeek - today;
                            sch.RunDay = (int)sch.DayOfWeek + 1;
                        }
                    }
                    sch.SortedTime = (TimeSpan)sch.Time - now;
                }
            }

            List<Schedule> sortedSchedules = ScheduleList.Where(x => x.IsActive).OrderBy(x => x.SortedDay).ThenBy(x => x.SortedTime).ToList();

            if (sortedSchedules.Count > 0)
            {
                NextRunDate = Enum.GetName(typeof(DayOfWeek), sortedSchedules.FirstOrDefault().RunDay) + " at " + sortedSchedules.FirstOrDefault().Time.ToString();
            }
            else
            {
                NextRunDate = "No schedules found";
            }
        }

        private void GetDaysOfWeek()
        {
            int i = 0;
            foreach (DayOfWeek d in Enum.GetValues(typeof(DayOfWeek)))
            {
                DayOfWeek x = d;
                Days.Add(new SelectListItem { Value = i.ToString(), Text = d.ToString() });
                i++;
            }
        }

        private bool activeScheduleCountExceeded(int add1)
        {
            bool exceeded = true;
            var crud = new CrudSchedule();

            int scheduleCount = crud.GetTenantScheduleCount(Tenant.TenantID);
            if (scheduleCount + add1 <= Tenant.Contract.ScheduleCount)
            {
                exceeded = false;
            }

            return exceeded;
        }

        private bool CheckSalesHistorySchedules()
        {
            var crud = new CrudLookup();
            var scheduleRunType = crud.Read(x => x.LookupID == ScheduleEntry.RunTypeFK).FirstOrDefault().LookupName;

            if (ScheduleEntry.ScheduleID == 0)
            {
                if (scheduleRunType == "Load Sales History")
                {
                    if (Channel.Schedules.Any(x => x.Lookup.LookupName == "Load Sales History"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
