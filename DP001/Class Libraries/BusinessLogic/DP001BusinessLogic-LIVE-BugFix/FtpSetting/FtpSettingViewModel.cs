using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class FtpSettingViewModel
    {
        public FtpSettingViewModel()
        {

        }

        public FtpSettingViewModel(int channelId)
        {
            _channelId = channelId;
        }

        public List<FTPSetting> FtpSettingList { get; set; }
        public FTPSetting FTPSettingEntry { get; set; }
        private int _channelId;
        public Channel Channel { get; set; }
        public List<SelectListItem> FileTypeList { get; set; }
        public List<SelectListItem> Protocols { get; set; }

        public FtpSettingViewModel GetFtpSettings()
        {
            var crud = new CrudFtpSetting();
            FtpSettingList = crud.Read(_channelId);

            return this;
        }

        public FtpSettingViewModel New(string feedType)
        {
            FTPSettingEntry = new FTPSetting();
            FTPSettingEntry.Lookup = new Lookup();
            FTPSettingEntry.Suppliers.Add(new Supplier());
            FileTypeList = SharedViewModel.GetLookupList("FileType");
            Protocols = SharedViewModel.GetLookupList("FTPProtocol");
            FTPSettingEntry.FileTypeFK = Int32.Parse(FileTypeList.Find(x => x.Text == feedType + " Inventory").Value);
            FTPSettingEntry.Lookup.LookupName = feedType + " Inventory";

            return this;
        }

        public FtpSettingViewModel Edit(int id)
        {
            var crud = new CrudFtpSetting();

            FTPSettingEntry = crud.Read(x => x.ChannelFK == _channelId
                && x.FTPSettingsID == id)
                .FirstOrDefault();

            if (FTPSettingEntry != null)
            {

                FileTypeList = SharedViewModel.GetLookupList("FileType");
                Protocols = SharedViewModel.GetLookupList("FTPProtocol");

                var outputInt = 0;
                bool isInt = false;
                if (FTPSettingEntry.Lookup.LookupName != "Output Inventory" && FTPSettingEntry.Lookup.LookupName != "Additional Inventory")
                {
                    isInt = int.TryParse(FTPSettingEntry.FieldMapping.ManufacturerPartNo, out outputInt);
                }

                if (isInt)
                {
                    FTPSettingEntry.FileHasHeadings = false;
                }
            }

            return this;
        }

        public SaveReturn Create()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            foreach (FTPSetting ftp in Channel.FTPSettings)
            {
                //Unique Feed name check
                if (ftp.Description.ToLower() == FTPSettingEntry.Description.ToLower())
                {
                    sr.Message = "You cannot add a feed with the same name as an existing feed";
                    isValid = false;
                }
            }

            if (!FTPSettingEntry.FileHasHeadings)
            {
                if (!ValueIsInt(FTPSettingEntry.FieldMapping.Brand) ||
                    !ValueIsInt(FTPSettingEntry.FieldMapping.ManufacturerPartNo) ||
                    !ValueIsInt(FTPSettingEntry.FieldMapping.Description) ||
                    !ValueIsInt(FTPSettingEntry.FieldMapping.Price) ||
                    !ValueIsInt(FTPSettingEntry.FieldMapping.StockQuantity))
                {
                    sr.Message = "You can only use column indexes as your file does not have field headings";
                    isValid = false;
                }
            }

            string[] fileParts = FTPSettingEntry.FTPFileName.Split('.');
            List<string> validExtensions = new List<string>();

            var crudLookup = new CrudLookup();
            string fileTypeName = crudLookup.Read(x => x.LookupID == FTPSettingEntry.FileTypeFK).FirstOrDefault().LookupName;
            if (fileTypeName == "Output Inventory")
            {
                validExtensions.Add("tsv");
                validExtensions.Add("txt");
            }
            else
            {
                validExtensions.Add("csv");
                validExtensions.Add("tsv");
                validExtensions.Add("txt");
            }
            if (!validExtensions.Contains(fileParts[fileParts.Length - 1].ToLower()))
            {
                sr.Message = "The file name field must have a file extension of one of the following: " + String.Join(", ", validExtensions);
                isValid = false;
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudFtpSetting();
                crud.Create(FTPSettingEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Update(FTPSetting ftpSettingEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            foreach (FTPSetting ftp in Channel.FTPSettings)
            {
                if (ftp.FTPSettingsID == ftpSettingEntry.FTPSettingsID)
                {
                    continue;
                }
                //Unique Feed name check
                if (ftp.Description.ToLower() == ftpSettingEntry.Description.ToLower())
                {
                    sr.Message = "You cannot add a feed with the same name as an existing feed";
                    isValid = false;
                }
            }

            if (!ftpSettingEntry.FileHasHeadings)
            {
                if (!ValueIsInt(ftpSettingEntry.FieldMapping.Brand) ||
                    !ValueIsInt(ftpSettingEntry.FieldMapping.ManufacturerPartNo) ||
                    !ValueIsInt(ftpSettingEntry.FieldMapping.Description) ||
                    !ValueIsInt(ftpSettingEntry.FieldMapping.Price) ||
                    !ValueIsInt(ftpSettingEntry.FieldMapping.StockQuantity))
                {
                    sr.Message = "You can only use column indexes as your file does not have field headings";
                    isValid = false;
                }
            }

            string[] fileParts = FTPSettingEntry.FTPFileName.Split('.');
            List<string> validExtensions = new List<string>();
            if (FTPSettingEntry.Lookup.LookupName == "Output Inventory")
            {
                validExtensions.Add("tsv");
                validExtensions.Add("txt");
            }
            else
            {
                validExtensions.Add("csv");
                validExtensions.Add("tsv");
                validExtensions.Add("txt");
            }
            if (!validExtensions.Contains(fileParts[fileParts.Length - 1].ToLower()))
            {
                sr.Message = "The file name field must have a file extension of one of the following: " + String.Join(", ", validExtensions);
                isValid = false;
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudFtpSetting();

                var isFound = crud.Read(x => x.ChannelFK == ftpSettingEntry.ChannelFK
                    && x.FTPSettingsID == ftpSettingEntry.FTPSettingsID).Count > 0;

                if (isFound)
                {
                    crud.Update(ftpSettingEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist or you do not have permission to change it";
                }
            }
            catch(Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Delete(int id)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            var crud = new CrudFtpSetting();

            try {
                var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.FTPSettingsID == id).FirstOrDefault();
                if (deleteRecord != null)
                {
                    sr.IsSuccess = crud.Delete(deleteRecord);
                    if (!sr.IsSuccess)
                    {
                        sr.Message = "There was a problem when trying to delete the FTP Setting, the problem has been reported to technical support";
                    }
                }
                else
                {
                    sr.Message = "Record does not exist or you do not have permission to delete it";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        private bool ValueIsInt(string valueIn)
        {
            var outputInt = 0;
            return int.TryParse(valueIn, out outputInt);
        }
    }
}
