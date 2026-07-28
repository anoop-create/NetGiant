using static netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.JobStatusCommonViewModel;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public interface IJobStatusCommonViewModel
    {
        bool SaveHadErrors { get; set; }
        void WriteJobStatusRecord(string jobStatus, string htmlNotes, SavingErrorType savingErrorType = SavingErrorType.Saving);
    }
}
