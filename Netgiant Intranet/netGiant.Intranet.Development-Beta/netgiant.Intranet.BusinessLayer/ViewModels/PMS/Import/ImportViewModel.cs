using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProductImages;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.PromotionalGroup;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProviderInventory;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class ImportViewModel : CommonViewModel
    {
        public ImportViewModel()
        {
            Websites = SelectListViewModel.GetAllWebsites();
            Keys = new List<SelectListItem>
            {
                new SelectListItem { Text = "Alt Ref", Value = "1" },
                new SelectListItem { Text = "Stock Ref", Value = "2" }
            }
            .AsQueryable();
        }

        public IQueryable<SelectListItem> Websites { get; set; }
        public IQueryable<SelectListItem> Keys { get; set; }

        public ImportType Type { get; set; }
        public string FilePath { get; set; }
        public int WebsiteFk { get; set; }
        public int ImportKey { get; set; }

        public void ProcessImport(HttpPostedFileBase file, ImportType type)
        {
            SaveImportedFile(file);

            switch (type)
            {
                case ImportType.CrossSellingLink:
                    var crossSellingLink = new ImportCrossSellingLinkViewModel();
                    crossSellingLink.Import(FilePath);
                    break;
                case ImportType.Equipment:
                    var equipment = new ImportEquipmentViewModel();
                    equipment.Import(FilePath);
                    break;
                case ImportType.EquipmentNotes:
                    var equipmentnotes = new ImportEquipmentViewModel();
                    equipmentnotes.Import(FilePath);
                    break;
                case ImportType.Product:
                    var product = new ImportProductViewModel();
                    product.FilePath = FilePath;
                    product.WebsiteFK = WebsiteFk;
                    product.ImportPrimaryKey = ImportKey;
                    product.Import();
                    break;
                case ImportType.ProductCategoryCodes:
                    var categorycodes = new ImportCategoryCodeViewModel(FilePath);
                    categorycodes.Import();
                    break;
                case ImportType.ProductImages:
                    var productImages = new ImportProductImagesViewModel();
                    productImages.FilePath = FilePath;
                    productImages.Import();
                    break;
                case ImportType.PromotionalGroup:
                    var promotionalGroup = new ImportPromotionalGroupViewModel();
                    promotionalGroup.Import(FilePath);
                    break;
                case ImportType.ProviderInventory:
                    var providerInventory = new ImportProviderInventoryViewModel();
                    providerInventory.Import(FilePath);
                    break;
                case ImportType.ObsoleteItem:
                    var obsoleteItem = new ImportObsoleteItemViewModel(FilePath);
                    obsoleteItem.Import();
                    break;
                case ImportType.ProductAddon:
                    var productAddon = new ImportProductAddonViewModel();
                    productAddon.Import(FilePath);
                    break;
                default: break;
            }

            DeleteImportedFile(FilePath);
        }

        public void SaveImportedFile(HttpPostedFileBase file)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            string localDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();
            FilePath = localDirectory + "\\PMSTempData\\" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss_") + file.FileName;
            if (Directory.Exists(localDirectory))
            {
                file.SaveAs(FilePath);
            }
            else
            {
                localDirectory = "C:\\";
                FilePath = localDirectory + "\\PMSTempData\\" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss_") + file.FileName;
                file.SaveAs(FilePath);
            }

        }

        public void DeleteImportedFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    public enum ImportType
    {
        [Display(Name = "Cross Selling Link")]
        CrossSellingLink,
        [Display(Name = "Equipment")]
        Equipment,
        [Display(Name = "Equipment Notes")]
        EquipmentNotes,
        [Display(Name = "Product")]
        Product,
        [Display(Name = "Product Category Codes")]
        ProductCategoryCodes,
        [Display(Name = "Product Images")]
        ProductImages,
        [Display(Name = "Promotional Group")]
        PromotionalGroup,
        [Display(Name = "Provider Inventory")]
        ProviderInventory,
        [Display(Name = "Obsolete Items")]
        ObsoleteItem,
        [Display(Name = "Product Add On")]
        ProductAddon
    }
}
