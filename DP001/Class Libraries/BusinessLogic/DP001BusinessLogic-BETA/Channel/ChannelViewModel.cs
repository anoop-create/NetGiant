using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using DP001DataAccess.Utilities;
using System.Data.Entity.Validation;

namespace DP001BusinessLogic.ViewModels
{
    public class ChannelViewModel
    {
        public ChannelViewModel()
        {

        }

        public ChannelViewModel(int tenantId)
        {
            _tenantId = tenantId;
        }

        public List<Channel> ChannelList { get; set; }
        private int _tenantId;

        public ChannelViewModel GetChannels()
        {
            var crud = new CrudChannel();
            ChannelList = crud.Read(x => x.TenantFK == _tenantId);

            return this;
        }

        public Channel GetChannel(int channelId)
        {
            var crud = new CrudChannel();
            var channel = crud.Read(channelId);

            return channel;
        }

        public SaveReturn Clone(int channelId, int tenantId)
        {
            var sr = new SaveReturn();

            try
            {
                var crud = new CrudTenant();
                var originalChannel = crud.Read(tenantId).Channels.FirstOrDefault(x => x.ChannelID == channelId);
                var newChannel = CloneChannel(originalChannel);
                CloneFtpSettings(originalChannel, newChannel);
                CloneCustomFields(originalChannel, newChannel);
                CloneSchedule(originalChannel, newChannel);
                ClonePriceRules(originalChannel, newChannel);

                sr.IsSuccess = true;
                sr.Message = "Channel Successfully Cloned";
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        private Channel CloneChannel(Channel originalChannel)
        {
            try
            {
                using (var db = new DP001Entities())
                {
                    var newChannel = new Channel
                    {
                        TenantFK = originalChannel.TenantFK,
                        ChannelName = originalChannel.ChannelName + "-Cloned",
                        IsActive = originalChannel.IsActive,
                        JobInProgress = originalChannel.JobInProgress,
                        SLMinReviews = originalChannel.SLMinReviews,
                        SLMinRating = originalChannel.SLMinRating,
                        SLActiveTypeFK = originalChannel.SLActiveTypeFK,
                        OutputFileEmailAddress = originalChannel.OutputFileEmailAddress,
                        NotificationsEmailAddress = originalChannel.NotificationsEmailAddress,
                        RoundingGroupFK = originalChannel.RoundingGroupFK,
                        UseClientProductId = originalChannel.UseClientProductId
                    };

                    db.Entry(newChannel).State = EntityState.Added;
                    db.SaveChanges();

                    return newChannel;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to clone channel table entry for ChannelId {originalChannel.ChannelID}. Error: {ex.Message} ");
            }
        }

        private void CloneFtpSettings(Channel originalChannel, Channel newChannel)
        {
            try
            {
                using (var db = new DP001Entities())
                {
                    foreach (var ftp in originalChannel.FTPSettings)
                    {
                        if (ftp.Lookup.LookupName != "Output Inventory" && ftp.Lookup.LookupName != "Sales History")
                        {
                            var newFtp = new FTPSetting
                            {
                                ChannelFK = newChannel.ChannelID,
                                FileTypeFK = ftp.FileTypeFK,
                                Description = ftp.Description,
                                FTPServer = ftp.FTPServer,
                                FTPUser = ftp.FTPUser,
                                FTPPassword = ftp.FTPPassword,
                                FTPPath = ftp.FTPPath,
                                FTPFileName = ftp.FTPFileName,
                                FTPSummaryFileName = ftp.FTPSummaryFileName,
                                FTPZipFileName = ftp.FTPZipFileName,
                                UseSkuuudleLite = ftp.UseSkuuudleLite,
                                FTPProtocolFK = ftp.FTPProtocolFK
                            };

                            db.FTPSettings.Add(newFtp);
                            db.SaveChanges();

                            var supplier = ftp.Suppliers.FirstOrDefault();
                            if (supplier != null)
                            {
                                var newSupplier = new Supplier
                                {
                                    ChannelFK = newChannel.ChannelID,
                                    FTPSettingsFK = newFtp.FTPSettingsID,
                                    SupplierName = supplier.SupplierName,
                                    IsActive = supplier.IsActive
                                };

                                db.Suppliers.Add(newSupplier);
                                db.SaveChanges();
                            }

                            if (ftp.FieldMapping != null)
                            {
                                var newFieldMapping = new FieldMapping
                                {
                                    FTPSettingsFK = newFtp.FTPSettingsID,
                                    Brand = ftp.FieldMapping.Brand ?? "a",
                                    ManufacturerPartNo = ftp.FieldMapping.ManufacturerPartNo ?? "a",
                                    StockQuantity = ftp.FieldMapping.StockQuantity,
                                    Price = ftp.FieldMapping.Price,
                                    Description = ftp.FieldMapping.Description ?? "Copy",
                                    ClientProductID = ftp.FieldMapping.ClientProductID,
                                    LnKdManufacturer = ftp.FieldMapping.LnKdManufacturer,
                                    LnKdManufacturerPartNo = ftp.FieldMapping.LnKdManufacturerPartNo,
                                    ProductCategory = ftp.FieldMapping.ProductCategory,
                                    Competitor = ftp.FieldMapping.Competitor,
                                    IsKeyLine = ftp.FieldMapping.IsKeyLine,
                                    CustomField1 = ftp.FieldMapping.CustomField1,
                                    CustomField2 = ftp.FieldMapping.CustomField2,
                                    CustomField3 = ftp.FieldMapping.CustomField3,
                                    CustomField4 = ftp.FieldMapping.CustomField4,
                                    CustomField5 = ftp.FieldMapping.CustomField5,
                                    CustomField6 = ftp.FieldMapping.CustomField6,
                                    CustomField7 = ftp.FieldMapping.CustomField7,
                                    CustomField8 = ftp.FieldMapping.CustomField8,
                                    CustomField9 = ftp.FieldMapping.CustomField9,
                                    CustomField10 = ftp.FieldMapping.CustomField10,
                                    VariantOf = ftp.FieldMapping.VariantOf,
                                    Quantity = ftp.FieldMapping.Quantity,
                                    Period = ftp.FieldMapping.Period,
                                    Date = ftp.FieldMapping.Date,
                                    Price2 = ftp.FieldMapping.Price2
                                };

                                db.FieldMappings.Add(newFieldMapping);
                                db.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to clone Ftp settings for ChannelId {originalChannel.ChannelID}. Error: {ex.Message}");
            }
        }

        private void ClonePriceRules(Channel originalChannel, Channel newChannel)
        {
            try
            {
                using (var db = new DP001Entities())
                {
                    var originalPriceRules = db.PriceRules
                        .Include(x => x.Brand)
                        .Include(x => x.ProductInventory)
                        .Include(x => x.ProductCategory)
                        .Where(x => x.ChannelFK == originalChannel.ChannelID)
                        .ToList();

                    foreach (var originalRule in originalPriceRules)
                    {
                        var newPriceRule = new PriceRule
                        {
                            ChannelFK = newChannel.ChannelID,
                            RuleName = originalRule.RuleName,
                            RuleTypeFK = originalRule.RuleTypeFK,
                            BrandFK = originalRule.Brand != null ? CreateBrand(newChannel.ChannelID, originalRule.Brand.BrandName) : null,
                            ProductInventoryFK = originalRule.ProductInventory != null ? CreateProduct(newChannel.ChannelID, originalRule.ProductInventory) : null,
                            ProductCategoryFK = originalRule.ProductCategory != null ? CreateCategory(newChannel.ChannelID, originalRule.ProductCategory.CategoryName) : null,
                            MethodFK = originalRule.MethodFK,
                            IsBanding = originalRule.IsBanding,
                            IsActive = originalRule.IsActive,
                            IsTest = originalRule.IsTest,
                            BandName = string.IsNullOrEmpty(originalRule.BandName) ? "a" : originalRule.BandName,
                            BandStart = originalRule.BandStart,
                            BandEnd = originalRule.BandEnd,
                            UpliftIsPc = originalRule.UpliftIsPc,
                            CostUplift = originalRule.CostUplift,
                            MarginsArePc = originalRule.MarginsArePc,
                            DesiredMargin = originalRule.DesiredMargin,
                            MinMargin = originalRule.MinMargin,
                            MaxMargin = originalRule.MaxMargin,
                            BeatRate = originalRule.BeatRate,
                            Nudge = originalRule.Nudge,
                            FixedPriceOverride = originalRule.FixedPriceOverride,
                            AltPriceAdj1 = originalRule.AltPriceAdj1,
                            AltPriceAdj2 = originalRule.AltPriceAdj2,
                            AltPriceAdj3 = originalRule.AltPriceAdj3,
                            AltPriceAdj4 = originalRule.AltPriceAdj4,
                            AltPriceAdj5 = originalRule.AltPriceAdj5,
                            AltPriceAdj6 = originalRule.AltPriceAdj6,
                            AltPriceAdj7 = originalRule.AltPriceAdj7,
                            AltPriceAdj8 = originalRule.AltPriceAdj8,
                            AltPriceAdj9 = originalRule.AltPriceAdj9,
                            AltPriceAdj10 = originalRule.AltPriceAdj10,
                            CompatDiscount = originalRule.CompatDiscount,
                            ProductCount = originalRule.ProductCount,
                            AboveCounter = originalRule.AboveCounter,
                            BelowCounter = originalRule.BelowCounter,
                            MaxCounter = originalRule.MaxCounter,
                            MinCounter = originalRule.MinCounter,
                            RoundingGroupFK = originalRule.RoundingGroupFK,
                            AdjMinMargin1 = originalRule.AdjMinMargin1,
                            AdjMinMargin2 = originalRule.AdjMinMargin2,
                            AdjMinMargin3 = originalRule.AdjMinMargin3,
                            AdjMinMargin4 = originalRule.AdjMinMargin4,
                            AdjMinMargin5 = originalRule.AdjMinMargin5,
                            AdjMinMargin6 = originalRule.AdjMinMargin6,
                            AdjMinMargin7 = originalRule.AdjMinMargin7,
                            AdjMinMargin8 = originalRule.AdjMinMargin8,
                            AdjMinMargin9 = originalRule.AdjMinMargin9,
                            AdjMinMargin10 = originalRule.AdjMinMargin10,
                            AdjMaxMargin1 = originalRule.AdjMaxMargin1,
                            AdjMaxMargin2 = originalRule.AdjMaxMargin2,
                            AdjMaxMargin3 = originalRule.AdjMaxMargin3,
                            AdjMaxMargin4 = originalRule.AdjMaxMargin4,
                            AdjMaxMargin5 = originalRule.AdjMaxMargin5,
                            AdjMaxMargin6 = originalRule.AdjMaxMargin6,
                            AdjMaxMargin7 = originalRule.AdjMaxMargin7,
                            AdjMaxMargin8 = originalRule.AdjMaxMargin8,
                            AdjMaxMargin9 = originalRule.AdjMaxMargin9,
                            AdjMaxMargin10 = originalRule.AdjMaxMargin10,
                            CustomRuleField1 = originalRule.CustomRuleField1,
                            CustomRuleField2 = originalRule.CustomRuleField2,
                            CustomRuleField3 = originalRule.CustomRuleField3,
                            CustomRuleField4 = originalRule.CustomRuleField4,
                            CustomRuleField5 = originalRule.CustomRuleField5,
                            CustomRuleField6 = originalRule.CustomRuleField6,
                            CustomRuleField7 = originalRule.CustomRuleField7,
                            CustomRuleField8 = originalRule.CustomRuleField8,
                            CustomRuleField9 = originalRule.CustomRuleField9,
                            CustomRuleField10 = originalRule.CustomRuleField10
                        };

                        db.PriceRules.Add(newPriceRule);
                        db.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                throw new ApplicationException($"Failed to clone price rules for ChannelId {originalChannel.ChannelID}. Error: {ex.Message + string.Join("; ", errorMessages)}");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to clone price rules for ChannelId {originalChannel.ChannelID}. Error: {ex.Message}");
            }
        }

        private int? CreateBrand(int newChannelFk, string brandName)
        {
            var newBrand = new Brand
            {
                ChannelFK = newChannelFk,
                BrandName = brandName
            };

            var crudBrand = new CrudBrand();
            return crudBrand.Create(newBrand).BrandID;
        }

        private long? CreateCategory(int newChannelFk, string categoryName)
        {
            var newCategory = new ProductCategory
            {
                ChannelFK = newChannelFk,
                CategoryName = categoryName
            };

            var crudCategory = new CrudProductCategory();
            return crudCategory.Create(newCategory).ProductCategoryID;
        }

        private long? CreateProduct(int newChannelFk, ProductInventory originalProduct)
        {
            var crudBrand = new CrudBrand();
            var newProduct = new ProductInventory
            {
                ChannelFK = newChannelFk,
                BrandFK = CreateBrand(newChannelFk, originalProduct.Brand.BrandName) ?? 0,
                ManufacturerPartNo = originalProduct.ManufacturerPartNo,
                Description = originalProduct.Description,
                ClientProductID = originalProduct.ClientProductID,
                LnkdBrandFK = originalProduct.LnkdBrandFK != null ? CreateBrand(newChannelFk, crudBrand.Read(originalProduct.LnkdBrandFK ?? 0).BrandName) : null,
                LnkdManufacturerPartNo = originalProduct.LnkdManufacturerPartNo,
                ProductCategoryFK = originalProduct.ProductCategory != null ? CreateCategory(newChannelFk, originalProduct.ProductCategory.CategoryName) : null,
                Price = originalProduct.Price,
                AltPrice1 = originalProduct.AltPrice1,
                AltPrice2 = originalProduct.AltPrice2,
                AltPrice3 = originalProduct.AltPrice3,
                AltPrice4 = originalProduct.AltPrice4,
                AltPrice5 = originalProduct.AltPrice5,
                AltPrice6 = originalProduct.AltPrice6,
                AltPrice7 = originalProduct.AltPrice7,
                AltPrice8 = originalProduct.AltPrice8,
                AltPrice9 = originalProduct.AltPrice9,
                AltPrice10 = originalProduct.AltPrice10,
                CalculationOutcome = originalProduct.CalculationOutcome,
                BeatRateNumber = originalProduct.BeatRateNumber,
                StockQuantity = originalProduct.StockQuantity,
                CheapestCostPrice = originalProduct.CheapestCostPrice,
                CheapestCompetitorPrice = originalProduct.CheapestCompetitorPrice,
                GrossMarginPercent = originalProduct.GrossMarginPercent,
                GrossMarginValue = originalProduct.GrossMarginValue,
                CompetitorDifference = originalProduct.CompetitorDifference,
                DateLastUpdated = CommonDataFunctions.GetCurrentDateTime(),
                MaximumPrice = originalProduct.MaximumPrice,
                MinimumPrice = originalProduct.MinimumPrice,
                DesiredPrice = originalProduct.DesiredPrice,
                IsKeyLine = originalProduct.IsKeyLine,
                CompetitorCount = originalProduct.CompetitorCount,
                SupplierCount = originalProduct.SupplierCount,
                DatePriceChanged = originalProduct.DatePriceChanged,
                CustomProductField1 = originalProduct.CustomProductField1,
                CustomProductField2 = originalProduct.CustomProductField2,
                CustomProductField3 = originalProduct.CustomProductField3,
                CustomProductField4 = originalProduct.CustomProductField4,
                CustomProductField5 = originalProduct.CustomProductField5,
                CustomProductField6 = originalProduct.CustomProductField6,
                CustomProductField7 = originalProduct.CustomProductField7,
                CustomProductField8 = originalProduct.CustomProductField8,
                CustomProductField9 = originalProduct.CustomProductField9,
                CustomProductField10 = originalProduct.CustomProductField10,
                VariantOf = originalProduct.VariantOf,
                BeatenCompetitorPrice = originalProduct.BeatenCompetitorPrice,
                StatusFK = originalProduct.StatusFK,
                TargetMarginPercent = originalProduct.TargetMarginPercent
            };

            var crudProduct = new CrudProductInventory();
            return crudProduct.Create(newProduct).ProductInventoryID;
        }

        private void CloneCustomFields(Channel originalChannel, Channel newChannel)
        {
            try
            {
                using (var db = new DP001Entities())
                {
                    foreach (var customField in originalChannel.CustomFields)
                    {
                        var newCustomField = new CustomField
                        {
                            ChannelFK = newChannel.ChannelID,
                            CustFieldTypeFK = customField.CustFieldTypeFK,
                            DBFieldName = customField.DBFieldName,
                            UserFieldName = customField.UserFieldName
                        };

                        db.CustomFields.Add(newCustomField);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to clone Custom Fields for ChannelId {originalChannel.ChannelID}. Error: {ex.Message}");
            }
        }

        private void CloneSchedule(Channel originalChannel, Channel newChannel)
        {
            try
            {
                using (var db = new DP001Entities())
                {
                    foreach (var schedule in originalChannel.Schedules)
                    {
                        var newSchedule = new Schedule
                        {
                            ChannelFK = newChannel.ChannelID,
                            ScheduleName = schedule.ScheduleName,
                            RunTypeFK = schedule.RunTypeFK,
                            FrequencyFK = schedule.FrequencyFK,
                            DayOfWeek = schedule.DayOfWeek,
                            Time = schedule.Time,
                            IsActive = false
                        };

                        db.Schedules.Add(newSchedule);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to clone Schedule settings for ChannelId {originalChannel.ChannelID}. Error: {ex.Message}");
            }
        }
    }
}

