using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.SCOM.Services;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ngBatchProcesses.BusinessObjects.Axis
{
    public class ProductFields
    {
        public static void CreateTempFieldValues(string desiredCSVPath, StandardFunctions stnFunc)
        {
            ProductFieldServices svcProductField = new ProductFieldServices();

            List<int> productIDs = svcProductField.GetProductsWithNoTempFields(desiredCSVPath);
            IEnumerable<FieldNameSE> fieldNames = svcProductField.GetAllFieldNames();
            IEnumerable<FieldValueSE> fieldValues = svcProductField.GetAllFieldValues();

            try
            {
                foreach (int productID in productIDs)
                {
                    foreach (FieldNameSE fieldName in fieldNames)
                    {
                        if (!fieldValues.Any(x => x.ProductFK == productID && x.FieldNameFK == fieldName.FieldNameID))
                        {
                            //Create temp field value for the product
                            FieldValueSE fv = new FieldValueSE();
                            fv.FieldValueText = "";
                            fv.FieldValueBool = null;
                            fv.FieldValueDouble = null;
                            fv.ProductFK = productID;
                            fv.WebsiteFK = null;
                            fv.FieldNameFK = fieldName.FieldNameID;
                            svcProductField.SaveFieldValue(fv);
                        }
                    }
                }

                stnFunc.AddToActivityLog("Created template fields for the products");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** unable to create temp fields for the products");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
            }
        }
    }
}
