using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DP001DataAccess.Utilities;
using System.Reflection;

namespace DP001BusinessLogic.ViewModels
{
    public class CustomFieldViewModel
    {
        public CustomFieldViewModel()
        {

        }

        public CustomFieldViewModel(int channelId)
        {
            _channelId = channelId;
        }

        public List<CustomField> CustomFieldList { get; set; }
        public CustomField CustomFieldEntry { get; set; }
        public List<SelectListItem> AllCustomFieldTypes { get; set; }
        private int _channelId;
        public TenantSetting Tenant { get; set; }
        public Channel Channel { get; set; }


        public CustomFieldViewModel GetCustomFields()
        {
            var crud = new CrudCustomField();
            CustomFieldList = crud.Read(_channelId);

            return this;
        }

        public CustomFieldViewModel New()
        {
            CustomFieldEntry = new CustomField();
            CustomFieldEntry.Lookup = new Lookup();

            AllCustomFieldTypes = SharedViewModel.GetLookupList("CustomFieldType");

            return this;
        }

        public CustomFieldViewModel Edit(int id)
        {
            var crud = new CrudCustomField();

            CustomFieldEntry = crud.Read(x => x.ChannelFK == _channelId
                && x.CustomFieldID == id)
                .FirstOrDefault();

            AllCustomFieldTypes = SharedViewModel.GetLookupList("CustomFieldType");

            return this;
        }

        public SaveReturn Create()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            foreach (CustomField cf in Channel.CustomFields)
            {
                //Unique Custom Field name check
                if (cf.UserFieldName.ToLower() == CustomFieldEntry.UserFieldName.ToLower())
                {
                    sr.Message = "You cannot add a custom field with the same name as an existing custom field";
                    isValid = false;
                }
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crudLookup = new CrudLookup();
                Lookup lookup = crudLookup.Read(x => x.LookupID == CustomFieldEntry.CustFieldTypeFK).FirstOrDefault();
                CustomFieldEntry.DBFieldName = FindAvailableSlot(lookup, Channel);
                var crud = new CrudCustomField();
                crud.Create(CustomFieldEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }
        
        public SaveReturn Update(CustomField customFieldEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            foreach (CustomField cf in Channel.CustomFields)
            {
                //Is this the existing custom entry
                if (cf.CustomFieldID == customFieldEntry.CustomFieldID)
                {
                    continue;
                }

                //Unique Custom Field name check
                if (cf.UserFieldName.ToLower() == customFieldEntry.UserFieldName.ToLower())
                {
                    sr.Message = "This custom name name already exists";
                    isValid = false;
                }
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudCustomField();

                var isFound = crud.Read(x => x.ChannelFK == customFieldEntry.ChannelFK
                    && x.CustomFieldID == customFieldEntry.CustomFieldID).Count > 0;

                if (isFound)
                {
                    crud.Update(customFieldEntry);
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
            var crud = new CrudCustomField();

            var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.CustomFieldID == id).FirstOrDefault();

            if (deleteRecord != null)
            {
                try
                {
                    if (deleteRecord.Lookup.LookupName == "Product Inventory Field")
                    {
                        //remove entry from field mappings table
                        FieldMapping fm = GetProdInvFieldMapping(_channelId);

                        if (fm != null)
                        {
                            PropertyInfo property = typeof(FieldMapping).GetProperty(deleteRecord.DBFieldName.Replace("Product", ""));
                            property.SetValue(fm, null, null);

                            CrudFieldMappings crudFieldMappings = new CrudFieldMappings();
                            crudFieldMappings.Update(fm);
                        }
                    }

                    crud.Delete(deleteRecord);
                    saveReturn.IsSuccess = true;
                }
                catch (Exception e)
                {
                    saveReturn.Message = e.Message;
                }

            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Custom Field not found or you do not have permission to delete it";
            }

            return saveReturn;
        }

        private string FindAvailableSlot(Lookup lookup, Channel channel)
        {
            string fieldname = "";
            string fieldnameRoot = "";
            switch (lookup.LookupName)
            {
                case "Product Inventory Field":
                    fieldnameRoot = "CustomProductField";
                    break;
                case "Price Adjustment Field":
                    fieldnameRoot = "AltPriceAdj";
                    break;
                case "Price Rule Field":
                    fieldnameRoot = "CustomRuleField";
                    break;
            }

            List<string> cfn = channel.CustomFields.Where(x => x.CustFieldTypeFK == lookup.LookupID).Select(x => x.DBFieldName).ToList();
            int i = 1;
            for (i = 1; i < 11; i++)
            {
                if (cfn.Contains(fieldnameRoot + i.ToString()))
                {
                    continue;
                }
                fieldname = fieldnameRoot + i.ToString();
                break;
            }
            return fieldname;
        }

        private FieldMapping GetProdInvFieldMapping(int channelId)
        {
            CrudFtpSetting crud = new CrudFtpSetting();
            FTPSetting ftp = crud.Read(x => x.ChannelFK == channelId && x.Lookup.LookupName == "Product Inventory").FirstOrDefault();

            return ftp.FieldMapping;
        }
    }
}
