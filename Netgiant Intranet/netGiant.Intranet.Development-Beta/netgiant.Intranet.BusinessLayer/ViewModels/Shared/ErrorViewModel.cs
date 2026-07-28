using System;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class ErrorViewModel : CommonViewModel
    {
        public ErrorViewModel(Exception ex)
        {
            Message = ex.Message;
            InnerException = ex.InnerException;
            StackTrace = ex.StackTrace;
        }

        public string Message { get; set; }
        public Exception InnerException { get; set; }
        public string StackTrace { get; set; }
    }
}
