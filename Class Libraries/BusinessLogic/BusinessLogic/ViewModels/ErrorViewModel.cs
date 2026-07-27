using System.Collections.Generic;
using System.Web;

namespace BusinessLogic.ViewModels
{
    public class ErrorViewModel : CommonViewModel
    {
        public ErrorViewModel(int id)
        {
            ErrorNumber = id;
            ErrorData = DataCache.GetSectionData("ErrorData");
            SetupErrorDetails();
        }

        public Dictionary<string, string> ErrorData { get; set; }
        public int ErrorNumber { get; set; }
        public string ErrorDescription { get; set; }
        public string ErrorDetail { get; set; }
        public int ResponseStatusCode { get; set; }

        private void SetupErrorDetails()
        {
            if (ErrorNumber >= 100 && ErrorNumber < 600)
            {
                ErrorDescription = HttpWorkerRequest.GetStatusDescription(ErrorNumber);
                ResponseStatusCode = ErrorNumber;
                return;
            }
            else if (ErrorNumber >= 1000 && ErrorNumber < 1100)
            {
                ErrorDescription = "SagePay Error";
                return;
            }
            else if(ErrorNumber >= 2000 && ErrorNumber < 2100)
            {
                ErrorDescription = "PayPal Error";
                return;
            }
            else if (ErrorNumber == 9000)
            {
                ErrorDescription = "We're doing some essential maintenance, please come back later";
                return;
            }
            else
            {
                ErrorDescription = "Unknown Error Code " + ErrorNumber;
                return;
            }
        }
    }
}
