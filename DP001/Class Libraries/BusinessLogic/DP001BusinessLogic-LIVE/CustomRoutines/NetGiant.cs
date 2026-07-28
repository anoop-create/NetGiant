using DP001BusinessLogic.Pricing;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using DP001DataAccess.Utilities;
using System.Data.Entity;
using System.Runtime.CompilerServices;

namespace DP001BusinessLogic.CustomRoutines
{
    public class NetGiant
    {
        static NetGiant()
        {
            Initialize();
        }

        private static void Initialize()
        {
            var crud = new CrudLookup();

            var lookupProductField = crud.Read(x => x.LookupType.LookupTypeName == "CustomFieldType" && x.LookupName == "Product Inventory Field").FirstOrDefault();
            if (lookupProductField != null)
                _customProductFieldTypeId = lookupProductField.LookupID;

            var lookupPriceRuleField = crud.Read(x => x.LookupType.LookupTypeName == "CustomFieldType" && x.LookupName == "Price Rule Field").FirstOrDefault();
            if (lookupPriceRuleField != null)
                _customPriceRuleFieldTypeId = lookupPriceRuleField.LookupID;
        }

        private static List<CustomField> _customFields;
        private static int _customProductFieldTypeId;
        private static int _customPriceRuleFieldTypeId;
        private static Channel _channel;

        public static string GetCustomMethodOperatorField(string customMethodName, Channel channel)
        {
            var crudCustomField = new CrudCustomField();
            _customFields = crudCustomField.Read(x => x.ChannelFK == channel.ChannelID);
            _channel = channel;

            switch (customMethodName)
            {
                case "Assembly":
                    var customEntry = _customFields.FirstOrDefault(
                        x => x.UserFieldName == "Base Price" && x.Lookup.LookupName == "Product Inventory Field");

                    if (customEntry != null)
                        return
                            customEntry.DBFieldName;
                    break;
            }

            return "";
        }

        public static void Control(TargetUser targetUser, string targetFunction, Channel channel, object extras = null)
        {
            if (targetUser != TargetUser.NetGiant) return;

            var crudCustomField = new CrudCustomField();
            _customFields = crudCustomField.Read(x => x.ChannelFK == channel.ChannelID);
            _channel = channel;

            switch (targetFunction)
            {
                case "BreakMargins":
                    BreakMargins(extras);
                    break;
                case "RelatedDiscount":
                    RelatedDiscount(extras);
                    break;
                case "Assembly":
                    DoAssembly(extras);
                    break;
                default:
                    break;
            }
        }

        private static void BreakMargins(dynamic extras)
        {
            try
            {
                var ruleDetails = extras.priceRuleDetail;
                var basePrice = GetProductCustomFieldValue("Base Price", ruleDetails.Product);
                var break2MinMarginPrice = Math.Round(Engine.CalculateMargin(extras.costUpliftPrice, ruleDetails.Rule.AdjMinMargin2), 2);

                //if minimum has been hit
                if (ruleDetails.Product.AltPrice2 == break2MinMarginPrice)
                {
                    if (ruleDetails.Product.AltPrice2 > 0)
                    {
                        ruleDetails.Product.AltPrice1 =
                            Math.Round(((ruleDetails.Product.AltPrice2 + ruleDetails.Product.Price)/2), 2);
                    }

                    if (ruleDetails.Product.AltPrice2 > ruleDetails.Product.Price || ruleDetails.Product.AltPrice1 > ruleDetails.Product.Price)
                    {
                        ruleDetails.Product.AltPrice1 = ruleDetails.Product.Price;
                        ruleDetails.Product.AltPrice2 = ruleDetails.Product.Price;
                    }
                }

                if (ruleDetails.Product.AltPrice1 == 0 && ruleDetails.Product.AltPrice2 == 0)
                {
                    ruleDetails.Product.AltPrice1 = ruleDetails.Product.Price;
                    ruleDetails.Product.AltPrice2 = ruleDetails.Product.Price;
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "NetGiant Custom Routine Error. Could not calculate break margins for product - " +
                    extras.priceRuleDetail.Product.ManufacturerPartNo + ". Error: " + e.Message, "Error");
            }
        }

        private static void RelatedDiscount(dynamic extras)
        {
            try
            {
                var ruleDetails = extras.priceRuleDetail;
                ruleDetails.Components.FinalPrice = 0;
                var calculationOutcomes = (List<Lookup>)extras.calculationOutcomes;
                var salesFrequency = GetProductCustomFieldValue("Sales Frequency", ruleDetails.Product);
                var salesFrequenceLimit = GetPriceRuleCustomFieldValue("Sales Frequency Limit", ruleDetails.Rule);

                if (salesFrequency < salesFrequenceLimit)
                {
                    var compatibleOverrideValue = GetPriceRuleCustomFieldValue("Comp. Override Value £", ruleDetails.Rule);
                    var compatibleOverridePercent = GetPriceRuleCustomFieldValue("Comp. Override Margin %", ruleDetails.Rule);

                    var marginOverrideValue = ruleDetails.Components.CostUpliftPrice + compatibleOverrideValue;
                    var marginOverridePercent = ruleDetails.Components.CostUpliftPrice / (1 - (compatibleOverridePercent / 100));

                    ruleDetails.Components.FinalPrice = Math.Round(Math.Max(marginOverrideValue, marginOverridePercent), 2);
                    ruleDetails.Components.CalculationOutcome =
                        calculationOutcomes.Find(x => x.LookupName == "Low Sales").LookupID;

                    return;
                }

                var minMarginValue = GetPriceRuleCustomFieldValue("Min Margin Value £", ruleDetails.Rule);
                var maxMarginValue = GetPriceRuleCustomFieldValue("Max Margin Value £", ruleDetails.Rule);
                var maxMarginPercent = Math.Round(ruleDetails.Components.MaxMargin, 2);
                var compatDiscount = ruleDetails.Rule.CompatDiscount;
                var discountedPrice = Engine.CalculatePercentDiscount(ruleDetails.Rule.BasePrice, -compatDiscount);
                var minLimit = Math.Round(minMarginValue + ruleDetails.Components.CostUpliftPrice, 2);
                var maxLimit = Math.Round(maxMarginValue + ruleDetails.Components.CostUpliftPrice, 2);
                var maxOutcome = "";

                if (maxMarginPercent < maxLimit)
                {
                    maxLimit = maxMarginPercent;
                    maxOutcome = "Maximum";
                    //Whoops! We've gone below the minimum limit!
                    if (maxLimit < minLimit)
                    {
                        ruleDetails.Components.FinalPrice = minLimit;
                        ruleDetails.Components.CalculationOutcome = calculationOutcomes.Find(x => x.LookupName == "Minimum Margin Value").LookupID;
                        return;
                    }
                }
                else
                {
                    maxLimit = Math.Round(maxMarginValue + ruleDetails.Components.CostUpliftPrice, 2);
                    maxOutcome = "Maximum Margin Value";
                }

                //Is the maximum limit lower than the minimum limit
                if (minLimit > maxLimit)
                {
                    ruleDetails.Components.FinalPrice = 0;
                    ruleDetails.Components.CalculationOutcome = calculationOutcomes.Find(x => x.LookupName == "Unable to Price").LookupID;
                    return;
                }

                if (discountedPrice > minLimit && discountedPrice < maxLimit)
                {
                    ruleDetails.Components.FinalPrice = Math.Round(discountedPrice, 2);
                    ruleDetails.Components.CalculationOutcome = calculationOutcomes.Find(x => x.LookupName == "Relational Discount").LookupID;
                }
                else
                {
                    if (discountedPrice < minLimit)
                    {
                        ruleDetails.Components.FinalPrice = minLimit;
                        ruleDetails.Components.CalculationOutcome = calculationOutcomes.Find(x => x.LookupName == "Minimum Margin Value").LookupID;
                    }
                    if (discountedPrice > maxLimit)
                    {
                        ruleDetails.Components.FinalPrice = maxLimit;
                        ruleDetails.Components.CalculationOutcome = calculationOutcomes.Find(x => x.LookupName == maxOutcome).LookupID;
                    }
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "NetGiant Custom Routine Error. Could not calculate relational discount for product - " +
                    extras.priceRuleDetail.Product.ManufacturerPartNo + ". Error: " + e.Message, "Error");
            }
        }
        
        private static void DoAssembly(dynamic extras)
        {
            try
            {
                var ruleDetails = extras.priceRuleDetail;
                var calculationOutcomes = (List<Lookup>)extras.calculationOutcomes;
                var basePrice = GetProductCustomFieldValue("Base Price", ruleDetails.Product);
                var assemblyDiscount = GetPriceRuleCustomFieldValue("Assembly Discount %", ruleDetails.Rule);
                var discountedPrice = Math.Round(basePrice - (basePrice * (assemblyDiscount / 100)), 2);
                var break2MinMarginPrice = Math.Round(Engine.CalculateMargin(ruleDetails.Components.CostUpliftPrice, ruleDetails.Rule.AdjMinMargin2), 2);
                var desAltPrice = Engine.CalculatePercentDiscount(discountedPrice, ruleDetails.Rule.AltPriceAdj2);

                if (desAltPrice > break2MinMarginPrice)
                {
                    ruleDetails.Components.FinalPrice = discountedPrice;
                    ruleDetails.Components.CalculationOutcome =
                        calculationOutcomes.Find(x => x.LookupName == "Assembly Discount").LookupID;
                }
                else
                {
                    ruleDetails.Components.FinalPrice = break2MinMarginPrice;
                    ruleDetails.Components.CalculationOutcome =
                        calculationOutcomes.Find(x => x.LookupName == "Minimum").LookupID;
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "NetGiant Custom Routine Error. Could not calculate assembly discount for product - " +
                    extras.priceRuleDetail.Product.ManufacturerPartNo + ". Error: " + e.Message, "Error");
            }
        }

        private static decimal GetProductCustomFieldValue(string fieldName, dynamic product)
        {
            var firstOrDefault = _customFields.FirstOrDefault(x => x.UserFieldName == fieldName);
            if (firstOrDefault == null) return 0;

            var property = typeof(ProductInventory).GetProperty(firstOrDefault.DBFieldName);
            return (decimal)property.GetValue(product, null);
        }

        private static decimal GetPriceRuleCustomFieldValue(string fieldName, dynamic priceRule)
        {
            var firstOrDefault = _customFields.FirstOrDefault(x => x.UserFieldName == fieldName && x.CustFieldTypeFK == _customPriceRuleFieldTypeId);
            if (firstOrDefault == null) return 0;

            var property = typeof(GetProductPriceRules_Result).GetProperty(firstOrDefault.DBFieldName);
            return (decimal)property.GetValue(priceRule, null);
        }
    }
}
