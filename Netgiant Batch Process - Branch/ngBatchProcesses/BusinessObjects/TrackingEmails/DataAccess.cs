using ngBatchProcesses.BusinessObjects.TrackingEmails.DataSet_ServerSBSTableAdapters;

namespace ngBatchProcesses.BusinessObjects.TrackingEmails
{
    class DataAccess
    {
        public DataSet_ServerSBS.ng_GetCustomerInfoFromCustOrdRefDataTable GetCustomerInformation(string custRef)
        {
            //Declare instance of the DataSet and execute the stored procedure - dbo.ng_getCustomerInfoFromCustOrdRef
            DataSet_ServerSBS ds1 = new DataSet_ServerSBS();
            ng_GetCustomerInfoFromCustOrdRefTableAdapter ta1 = new ng_GetCustomerInfoFromCustOrdRefTableAdapter();
            DataSet_ServerSBS.ng_GetCustomerInfoFromCustOrdRefDataTable dt1 = new DataSet_ServerSBS.ng_GetCustomerInfoFromCustOrdRefDataTable();
            ta1.Fill(dt1, custRef);
            ds1.Dispose();
            return dt1;
        }
    }
}
