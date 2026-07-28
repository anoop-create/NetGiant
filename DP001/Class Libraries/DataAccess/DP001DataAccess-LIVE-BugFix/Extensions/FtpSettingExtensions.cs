using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(FtpSettingExtensions))]
    public partial class FTPSetting
    {
        //public string DownloadPath { get; set; }
        public bool FileHasHeadings { get; set; } = true;
    }

    public class FtpSettingExtensions
    {
        [Required(ErrorMessage = "File Type is required")]
        [Display(Name = "File Type")]
        public int FileTypeFK { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Server is required")]
        [Display(Name = "FTP Server")]
        public string FTPServer { get; set; }

        [Required(ErrorMessage = "UserName is required")]
        [Display(Name = "FTP Username")]
        public string FTPUser { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "FTP Password")]
        public string FTPPassword { get; set; }

        [Display(Name = "FTP Path")]
        public string FTPPath { get; set; }

        [Required(ErrorMessage = "File Name is required")]
        [Display(Name = "FTP File Name")]
        public string FTPFileName { get; set; }

        [Display(Name = "FTP Summary File Name")]
        public string FTPSummaryFileName { get; set; }

        [Display(Name = "Skuuudle Lite Zip File Name")]
        public string FTPZipFileName { get; set; }

    }
}
