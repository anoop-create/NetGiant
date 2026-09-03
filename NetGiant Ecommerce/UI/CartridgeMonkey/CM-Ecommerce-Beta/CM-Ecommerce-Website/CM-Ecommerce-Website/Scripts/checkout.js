// Structure of this document is as follows:
//      1. Functions
//      2. Immediate Code

// For use on the following pages
//      viewbasket
//      misc/accountapplication
//      stage1
//      stage2
//      stage3

function setPasswordComplete(data) {
    location.href = "/";
}

function tidyUpStage1() {
    $('select:enabled').each(function () {
        if (!$(this).valid() && $(this).hasClass('input-validation-error')) {
            $(this).prevAll('button').css('border', '2px solid #ff6666');
        } else {
            $(this).prevAll('button').css('border', '1px solid #ccc');
        }
    });

    if ($('#CheckoutDetails_PaymentMethod').val() === 'PayPal' && $('#IsAuthenticated').val() === '0') {
        if ($('#CheckoutDetails_BillingAddress_Line1').val() === '') {
            $('#CheckoutDetails_BillingAddress_Line1').val($('#CheckoutDetails_DeliveryAddress_Line1').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line2').val() === '') {
            $('#CheckoutDetails_BillingAddress_Line2').val($('#CheckoutDetails_DeliveryAddress_Line2').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line3').val() === '') {
            $('#CheckoutDetails_BillingAddress_Line3').val($('#CheckoutDetails_DeliveryAddress_Line3').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line4').val() === '') {
            $('#CheckoutDetails_BillingAddress_Line4').val($('#CheckoutDetails_DeliveryAddress_Line4').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line5').val() === '') {
            $('#CheckoutDetails_BillingAddress_Line5').val($('#CheckoutDetails_DeliveryAddress_Line5').val());
        }
        if ($('#CheckoutDetails_BillingAddress_PostCode').val() === '') {
            $('#CheckoutDetails_BillingAddress_PostCode').val($('#CheckoutDetails_DeliveryAddress_PostCode').val());
        }
    }
    return true;
}

function showSavedCards(message) {
    if (typeof message === 'undefined') {
        message = '';
    }
    $('#saved-error').html(message);
    $('#saved-cards').removeClass('g-d-n');
    $('#card-entry').addClass('g-d-n');
    $('#saved-different').html('Saved');
    $('#chk-save').removeClass('g-d-n');
    $('#co-place-order').removeClass('g-d-n').attr('type', 'button');
    $('#co-place-savedcard-order').removeClass('g-d-n');
    $('#CheckoutDetails_UseASavedCard').val('True');
    $('#SagePayIFrame').attr('src', '');
}

function changeBasketQty(e, qty) {
    var ref;
    if (qty === undefined) {
        ref = e.sender.element[0].id.replace('qty-', '');
        qty = e.sender._value;
    } else {
        ref = e;
    }

    if (qty === 0) {
        $('.delete[data-productid="@bc.StockRef"]').trigger('click');
    } else {
        $.ajax({
            url: "/Checkout/BasketChangeQty/",
            dataType: 'json',
            traditional: true,
            type: 'POST',
            cache: false,
            data: {
                productref: ref,
                productqty: qty || 1
            },
            async: false,
            success: function (data) {
                if (data.savereturn.IsSuccess) {
                    //$("#qty-" + ref.toString()).data("kendoNumericTextBox").destroy();
                    refreshVbFields(data);
                    refreshViewBasket();
                    renderPaypalButtonV2();
                }
            },
            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Checkout/BasketChangeQty/", xhr, textStatus, thrownError);
            }
        });
    }
}

function findObject(obj, key, val) {
    var objects = [];
    for (var i in obj) {
        if (!obj.hasOwnProperty(i)) continue;
        if (typeof obj[i] === 'object') {
            objects = objects.concat(findObject(obj[i], key, val));
        } else if (i === key && obj[key].toString() === val) {
            objects.push(obj);
        }
    }
    return objects;
}

function ppIsValid() {
    var jsonBuilderObject = {} //json to go back to server 
    var myFormElements = $('#co-stage1')[0] //all form items in co-stage1
    var isValid = true //ready to be set to false if anything is invalid
    var firstInvalidItem = '' //the item to position to for fixing
    var listOfErrors = '' //the list of errors in html for the error box
    var itemsToCheck = [
        '#CheckoutDetails_Password',
        '#CheckoutDetails_DeliveryAddress_PostCode',
        '#CheckoutDetails_DeliveryAddress_Line5',
        '#CheckoutDetails_DeliveryAddress_Line4',
        '#CheckoutDetails_DeliveryAddress_Line3',
        '#CheckoutDetails_DeliveryAddress_Line2',
        '#CheckoutDetails_DeliveryAddress_Line1',
        '#CheckoutDetails_RecipientName_Surname',
        '#CheckoutDetails_RecipientName_Firstname',
        '#AccountApplicationDetails_CustomerType',
        '#CheckoutDetails_TelephoneNumber',
        '#CheckoutDetails_Name_Surname',
        '#CheckoutDetails_Name_Firstname'
    ]

    for (var i = 0; i < itemsToCheck.length; i++) {
        $(itemsToCheck[i]).removeClass('input-validation-error') //gets the text boxes
        $('[data-id=' + itemsToCheck[i].replace('#', '') + ']').removeClass('input-validation-error') //gets the dropdowns

        if (!$(itemsToCheck[i]).valid()) {
            var formValidator = $('#co-stage1').validate();
            var errorWords = formValidator.errorList[0].message

            listOfErrors = '<li>' + errorWords + '</li>' + listOfErrors

            isValid = false //we have one invalid
            firstInvalidItem = itemsToCheck[i] //the one to scroll to
            $(itemsToCheck[i]).addClass('input-validation-error') //gets the text boxes
            $('[data-id=' + itemsToCheck[i].replace('#', '') + ']').addClass('input-validation-error') //gets the dropdowns
        }
    }

    if (isValid) {
        for (var i = 0; i < myFormElements.length; i++) {
            jsonBuilderObject[myFormElements[i].id] = myFormElements[i].value //wrap all the form into simple object
        }
        return JSON.stringify(jsonBuilderObject) //return the json to site.js paypal button createOrder
    }
    if (!isValid) {
        $('html, body').scrollTop($(firstInvalidItem).offset().top) //go to the first error to fix

        listOfErrors = '</ul>' + listOfErrors + '</ul>'

        $('#PayPalValidation')
            .removeClass('g-d-n')
            .addClass('validation-summary-errors')
            .html(listOfErrors) //show the PAYPAL validation errors box

        $('#co-stage1').find("[data-valmsg-summary=true]")
            .removeClass("validation-summary-errors")
            .addClass("validation-summary-valid")
            .find("ul")
            .empty() //hide the regular validator

        return "invalid" //tell the paypal button we're not valid yet
    }
}

function callStage2b(result, thisform) {
    $.ajax({
        url: "/Checkout/Stage2b",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            UseASavedCard: thisform.find('input#CheckoutDetails_UseASavedCard').val(),
            SaveThisCard: thisform.find('input#CheckoutDetails_SaveThisCard').prop('checked'),
            BrowserColorDepth: thisform.find('input#BrowserColorDepth').val(),
            BrowserScreenHeight: thisform.find('input#BrowserScreenHeight').val(),
            BrowserScreenWidth: thisform.find('input#BrowserScreenWidth').val(),
            MerchantSessionKey: thisform.find('input#CheckoutDetails_MerchantSessionKey_merchantSessionKey').val(),
            CardIdentifier: thisform.find('input#CardIdentifier').val() //result.cardIdentifier
        },
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                if (data.savereturn.Message == 'Authorised') {
                    window.location.href = "/Checkout/Stage3";
                } else {
                    launchPopup('ThreeDAuth', 'threeDSPopup', 'md');
                }
            }
            if (!data.savereturn.IsSuccess) {
                if (data.savereturn.Message == 'Rejected') {
                    refreshSessionKey();
                    $.confirm({
                        title: 'There has been an error',
                        content: 'Please check your card details including CVV have been entered correctly.',
                        buttons: {
                            OK: function () { }
                        }
                    });
                } else {
                    window.location.href = '/Checkout/ViewBasket?pm=CheckoutError&rpl=errormessage_' + data.savereturn.Html;
                }
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/Popup/", xhr, textStatus, thrownError);
        }
    });
}

function refreshSessionKey() {
    $.ajax({
        url: "/Checkout/OpayoRefreshKey",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
        },
        async: false,
        success: function (data) {
            $('input[name="CheckoutDetails.MerchantSessionKey.merchantSessionKey"]').val(data.savereturn.Message);
            $('input[name="CardIdentifier"]').val('');
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/Popup/", xhr, textStatus, thrownError);
        }
    });
}

function opayoBreakout(message) {
    $('#threeDSPopup').trigger('click');
    $.confirm({
        title: 'An error was encountered',
        content: message,
        buttons: {
            OK: function () { }
        }
    });
}

$(function () {

    $(document).on('click',
        '#apply-voucher',
        function () {

            // Same empty-code guard as the mini-cart's .minibasket-apply-voucher handler in
            // site.js, so clicking Apply with a blank field behaves identically in both widgets.
            var voucherCode = $.trim($('#voucher-code').val());
            if (!voucherCode) {
                return false;
            }

            var isValid = checkSessionExists("C_IsInCheckout") === true ? false : true;

            if (isValid) {
                $.ajax({
                    url: "/Checkout/ApplyVoucher/",
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        voucherCode: voucherCode
                    },
                    async: false,
                    success: function (data) {
                        if (data.savereturn.IsSuccess) {
                            if (!data) {
                                location.href = "/checkout/";
                            }
                            //refreshVbFields(data);
                            refreshViewBasket();
                            renderPaypalButtonV2();
                            $('#error-message').hide();
                        } else {
                            $('#error-message > p').text(data.savereturn.Message);
                            $('#error-message').show();
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/ApplyVoucher/", xhr, textStatus, thrownError);
                    }
                });
            }
            else {
                $('.IsInCheckout').val('true');
                location.href = '/checkout?pm=IsInCheckout';
            }
        });

    $(document).on('click',
        '#apply-discount',
        function () {
            $('#discount-atb').attr('data-price',
                $('#admin-discount').val() / $('#discount-atb').attr('data-vatm'));
            $('#discount-atb').trigger('click');
            refreshViewBasket();
        });

    $(document).on('click',
        '.remove-voucher',
        function () {
            $.ajax({
                url: "/Checkout/RemoveVoucher/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                async: false,
                success: function (data) {
                    if (!data) {
                        location.href = "/checkout/";
                    }
                    //refreshVbFields(data);
                    refreshViewBasket();
                    renderPaypalButtonV2();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/RemoveVoucher/", xhr, textStatus, thrownError);
                }
            });
        });

    $(document).on('click',
        '#admn-clear-basket',
        function () {
            $.ajax({
                url: "/Checkout/ClearBasket/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                async: false,
                success: function (data) {
                    refreshViewBasket();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/ClearBasket/", xhr, textStatus, thrownError);
                }
            });
        });

    if (isCurrentPage('/checkout')) {

        $('input[type="submit"], button, a').on('click', function (e) {
            if (e.ctrlKey && e.shiftKey) {
                return false;
            }
        });

        if (typeof paypal !== 'undefined' && !isCurrentPage('/checkout/stage2')) {
            renderPaypalButtonV2();
        }
    }

    if (isCurrentPage('/checkout/stage1') || isCurrentPage('/misc/accountapplication') || isCurrentPage('/misc/tradeapplication')) {
        $(document).on('click',
            '#bill-manual-address',
            function () {
                $('#co-billadd-fields').removeClass('g-d-n');
                $('#co-acc-billadd-fields').removeClass('g-d-n');
                $('#bill-manual-address').addClass('g-d-n');
            });
    }

    if (isCurrentPage('/checkout/stage1')) {

        loadPca(window, document, "pca", "//NETGI11112.pcapredict.com/js/sensor.js");
        pca.on("options", function (type, id, options) {
            options.bar = options.bar || {};
            options.bar.showLogo = false;
            options.bar.showCountry = false;
        });
        var deliveryId;
        pca.on("load", function (type, id, control) {
            if (control.fields[0].element.indexOf('DeliveryAddress') >= 0) {
                sessionStorage.setItem("pcaDeliveryId", control.key);
            }
            control.listen("populate", function (address, address2, id) {
                var isDeliveryAddress = sessionStorage.getItem("pcaDeliveryId") === id;

                if ($('#CheckoutDetails_DeliveryAddress_PostCode').val() != '' && isDeliveryAddress) {
                    //$('#CheckoutDetails_DeliveryAddress_PostCode').change();
                    $('#co-deladd-fields').removeClass('g-d-n');
                    $('#del-manual-address').addClass('g-d-n');
                    $('.enter-delivery-label').hide();
                    $('#delivery-options').addClass('g-b-rlt-1-p');
                    $('#CheckoutDetails_DeliveryAddress_PostCode').trigger('change');
                }
                if (address.PostalCode === $('#CheckoutDetails_DeliveryAddress_PostCode').val()) {
                    $('#address-search').val('');
                    $('#co-section-delivery #co-deladd-fields').removeClass('g-d-n');
                    $('#del-manual-address').addClass('g-d-n');

                    $.ajax({
                        url: "/Checkout/GetDeliveryOptions/",
                        dataType: 'json',
                        traditional: true,
                        type: 'POST',
                        cache: false,
                        data: {
                            countrycode: address.CountryIso3,
                            postcode: address.PostalCode
                        },
                        async: false,
                        success: function (data) {
                            $('#co-section-delivery-options #options').html(data.savereturn.Html);
                            $('#co-section-delivery-options #options').show();
                        }
                    });
                }

                var section = $('.co-paym-button.selected').attr('data-id');

                if ($('#CheckoutDetails_BillingAddress_PostCode').val() != '' && !isDeliveryAddress) {
                    if (section === "AccountApplication") {
                        $('#AccountApplicationDetails_BillingAddress_Line1').val($('#CheckoutDetails_BillingAddress_Line1').val());
                        $('#AccountApplicationDetails_BillingAddress_Line2').val($('#CheckoutDetails_BillingAddress_Line2').val());
                        $('#AccountApplicationDetails_BillingAddress_Line3').val($('#CheckoutDetails_BillingAddress_Line3').val());
                        $('#AccountApplicationDetails_BillingAddress_Line4').val($('#CheckoutDetails_BillingAddress_Line4').val());
                        $('#AccountApplicationDetails_BillingAddress_Line5').val($('#CheckoutDetails_BillingAddress_Line5').val());
                        $('#AccountApplicationDetails_BillingAddress_PostCode').val($('#CheckoutDetails_BillingAddress_PostCode').val());
                        $('#co-acc-billadd-fields').removeClass('g-d-n');
                        $('#co-billadd-fields').addClass('g-d-n');
                        $('#bill-manual-address').addClass('g-d-n');
                    } else {
                        $('#co-acc-billadd-fields').addClass('g-d-n');
                        $('#co-billadd-fields').removeClass('g-d-n');
                        $('#bill-manual-address').addClass('g-d-n');
                    }
                }

                //sessionStorage.removeItem("pcaDeliveryId");
            });
        });

        //$(document).on('change keypress', '#delivery-address-search, #billing-address-search', function (e) {
        //    if (e.type == 'change' || (e.type == 'keypress' && e.which == 13)) {
        //        if ($(this).attr("id") === "delivery-address-search") {
        //            sessionStorage.setItem("pcaIsDeliveryAddress", true);
        //        } else {
        //            sessionStorage.setItem("pcaIsDeliveryAddress", false);
        //        }
        //    }
        //});

        $('select', '#co-stage1').change(function () {
            if (!$(this).valid()) {
                $(this).prevAll('button').css('border', '2px solid #ff6666');
            } else {
                $(this).prevAll('button').css('border', '1px solid #ccc');
            }
        });

        $('div.checkbox-validate > input[type=checkbox]').change(function () {
            validateCheckbox($(this).parent());
        });

        $('input:not(".no-validation")', '#co-stage1').blur(function () {
            //$('input', '#co-stage1').blur(function () {
            $(this).valid();
        });

        $('form').submit(function () {
            if (!$(this).valid()) {
                $('div.checkbox-validate').each(function () {
                    validateCheckbox($(this));
                });
            }
        });

        $(document).on('change',
            '#multi-address input[type="radio"][name="multiaddress"]',
            function () {
                //populate fields from Json
                var addObj = $.parseJSON($('#additional-addresses').html());
                var address = findObject(addObj, 'Id', $(this).val());
                if (address.length > 0) {
                    $('#CheckoutDetails_DeliveryAddress_Line1').val(address[0].Line1);
                    $('#CheckoutDetails_DeliveryAddress_Line2').val(address[0].Line2);
                    $('#CheckoutDetails_DeliveryAddress_Line3').val(address[0].Line3);
                    $('#CheckoutDetails_DeliveryAddress_Line4').val(address[0].Line4);
                    $('#CheckoutDetails_DeliveryAddress_Line5').val(address[0].Line5);
                    $('#CheckoutDetails_DeliveryAddress_PostCode').val(address[0].PostCode);
                    $('#CheckoutDetails_DeliveryAddress_PostCode').trigger('change');
                }
            });

        $(document).on('click',
            '#del-manual-address',
            function () {
                $('#co-deladd-fields').removeClass('g-d-n');
                $('#del-manual-address').addClass('g-d-n');
            });

        $(document).on('click',
            '#multi-address button:last',
            function () {
                // Last button in div #multi-address is Add Address. Only do address stuff on this.
                // Another button here is View More/Less, which would not do the address stuff.
                $('#multi-address').addClass('g-d-n');
                $('#single-address').removeClass('g-d-n');
                $('#address-search').focus();
            });

        $(document).on('click',
            '#co-billadd-add',
            function () {
                $('#co-billadd-fields').addClass('g-d-n');
                $('#co-billadd-search').removeClass('g-d-n');
            });

        $(document).on('click',
            '#same-address',
            function () {
                var cobilladdfields = $('.co-paym-button.selected').attr('data-id') === "AccountApplication" ? "#co-acc-billadd-fields" : "#co-billadd-fields";

                if ($('#same-address').is(':checked')) {
                    //populate from delivery address
                    $('#AccountApplicationDetails_BillingAddress_Line1').val($('#CheckoutDetails_DeliveryAddress_Line1').val());
                    $('#AccountApplicationDetails_BillingAddress_Line2').val($('#CheckoutDetails_DeliveryAddress_Line2').val());
                    $('#AccountApplicationDetails_BillingAddress_Line3').val($('#CheckoutDetails_DeliveryAddress_Line3').val());
                    $('#AccountApplicationDetails_BillingAddress_Line4').val($('#CheckoutDetails_DeliveryAddress_Line4').val());
                    $('#AccountApplicationDetails_BillingAddress_Line5').val($('#CheckoutDetails_DeliveryAddress_Line5').val());
                    $('#AccountApplicationDetails_BillingAddress_PostCode').val($('#CheckoutDetails_DeliveryAddress_PostCode').val());
                    $('#CheckoutDetails_BillingAddress_Line1').val($('#CheckoutDetails_DeliveryAddress_Line1').val());
                    $('#CheckoutDetails_BillingAddress_Line2').val($('#CheckoutDetails_DeliveryAddress_Line2').val());
                    $('#CheckoutDetails_BillingAddress_Line3').val($('#CheckoutDetails_DeliveryAddress_Line3').val());
                    $('#CheckoutDetails_BillingAddress_Line4').val($('#CheckoutDetails_DeliveryAddress_Line4').val());
                    $('#CheckoutDetails_BillingAddress_Line5').val($('#CheckoutDetails_DeliveryAddress_Line5').val());
                    $('#CheckoutDetails_BillingAddress_PostCode').val($('#CheckoutDetails_DeliveryAddress_PostCode').val());

                    $(cobilladdfields).removeClass('g-d-n');
                } else {
                    //clear billing address
                    $('#AccountApplicationDetails_BillingAddress_Line1').val('');
                    $('#AccountApplicationDetails_BillingAddress_Line2').val('');
                    $('#AccountApplicationDetails_BillingAddress_Line3').val('');
                    $('#AccountApplicationDetails_BillingAddress_Line4').val('');
                    $('#AccountApplicationDetails_BillingAddress_Line5').val('');
                    $('#AccountApplicationDetails_BillingAddress_PostCode').val('');
                    $('#CheckoutDetails_BillingAddress_Line1').val('');
                    $('#CheckoutDetails_BillingAddress_Line2').val('');
                    $('#CheckoutDetails_BillingAddress_Line3').val('');
                    $('#CheckoutDetails_BillingAddress_Line4').val('');
                    $('#CheckoutDetails_BillingAddress_Line5').val('');
                    $('#CheckoutDetails_BillingAddress_PostCode').val('');

                    $(cobilladdfields).removeClass('g-d-n');
                }
            });

        $(document).on('submit', '#co-stage1', function () {
            if ($(this).valid()) {
                $(this).find(':submit').attr('disabled', 'disabled');
            }
        });

        $(document).on('click',
            '.co-paym-button',
            function () {
                $(this).find(':radio').prop("checked", true);

                $('.co-paym-button').removeClass('selected');
                $(this).addClass('selected');
                var section = $(this).attr('data-id');
                $('#CheckoutDetails_PaymentMethod').val(section);

                if (section === 'CreditDebit') {
                    $('#co-stage1').attr('action', $('#stage2Page').val());
                    $('#co-submit-button > button').html('Continue To Payment');
                    $('#co-paypal-button').addClass('g-d-n');//hide PP button
                    $('#PayPalValidation').addClass('g-d-n');//hide PP validation show errors box
                    $('#PayPalValidation').removeClass('validation-summary-errors');//hide PP validation show errors box
                } else {
                    $('#co-stage1').attr('action', '/checkout/stage3');
                    $('#co-submit-button > button').html('Place Order');
                    $('#co-paypal-button').addClass('g-d-n');//hide PP button
                    $('#PayPalValidation').addClass('g-d-n');//hide PP validation show errors box
                    $('#PayPalValidation').removeClass('validation-summary-errors');//hide PP validation show errors box
                }
                $('.co-paym-addinfo').removeClass('selected');
                $('#co-' + section.toLowerCase()).addClass('selected');
                $('#co-po-ref').removeClass('g-d-n');
                $('#co-submit-button').removeClass('g-d-n');
                $('#co-privacy-notice').removeClass('g-d-n');
                $('#co-bill-search').removeClass('g-d-n');
                $('#bill-manual-address').removeClass('g-d-n');

                if ($('#same-address').is(':checked')) {
                    $('#same-address').click();
                }

                if (section === "PayPal") {
                    $('#co-paypal-button').removeClass('g-d-n');//show PP button
                    $('#co-submit-button').addClass('g-d-n');//hide regular button
                    $('#co-bill-search').addClass('g-d-n');//hide the billing address
                    $('#co-stage1').find("[data-valmsg-summary=true]")
                        .removeClass("validation-summary-errors")
                        .addClass("validation-summary-valid")
                        .find("ul")
                        .empty();//hide the regular validator
                }

                if (section === "AccountApplication") {
                    $('#co-accountapplication > div:not(#co-company-id), #co-credit-tc, #co-credit-billing,  #co-acc-billadd-search, #co-acc-billadd-fields, #bill-acc-manual-address').find('input').each(function () {
                        $(this).prop('disabled', false);
                    });

                    $('#co-billadd-fields').find('input').each(function () {
                        $(this).prop('disabled', true);
                    });

                    if ($('#AccountApplicationDetails_CustomerType').val() === "2") {
                        $('#co-company-id').find('input').each(function () {
                            $(this).prop('disabled', false);
                        });
                    }

                    $('#co-accountapplication').find('select').each(function () {
                        $(this).prop('disabled', false);
                    });
                    $('#co-submit-button > button').html('Submit Application & Order');
                    $('#co-credit-tc').removeClass('g-d-n');
                    $('#co-privacy-notice').addClass('g-d-n');
                    $('#co-credit-billing').removeClass('g-d-n');
                    $('#co-credit-paym').removeClass('g-d-n');
                    $('.co-bill-address').addClass('g-d-n');
                    $('.co-acc-bill-address').removeClass('g-d-n');
                } else {
                    $('#co-accountapplication, #co-credit-tc, #co-credit-billing, #co-acc-billadd-search, #co-acc-billadd-fields, #bill-acc-manual-address').find('input').each(function () {
                        $(this).prop('disabled', true);
                    });

                    $('#co-billadd-fields').find('input').each(function () {
                        $(this).prop('disabled', false);
                    });

                    $('#co-accountapplication').find('select').each(function () {
                        $(this).prop('disabled', true);
                    });
                    $('#co-credit-tc').addClass('g-d-n');
                    $('#co-privacy-notice').removeClass('g-d-n');
                    $('#co-credit-billing').addClass('g-d-n');
                    $('#co-credit-paym').addClass('g-d-n');
                    $('.co-bill-address').removeClass('g-d-n');
                    $('.co-acc-bill-address').addClass('g-d-n');
                }

                if (sessionStorage.getItem("applyForCreditClicked") !== 'true') {
                    if (section === 'Account' || section === "PayPal") {
                        $('.co-bill-address').addClass('g-d-n');
                        $("html, body").animate({ scrollTop: $(document).height() }, 1000);
                    } else {
                        var scrollToContainer = $('.co-paym-addinfo.selected');
                        if (scrollToContainer.length === 0) {
                            scrollToContainer = $('.co-bill-address').hasClass('g-d-n') ? $('.co-acc-bill-address') : $('#co-bill-search');
                        }
                        $('html,body').animate({
                            scrollTop: scrollToContainer.offset().top - 30
                        }, 1000);
                    }
                }
            });

        $(document).on('change', '#AccountApplicationDetails_CustomerType', function () {
            var type = $(this).val();

            $('#co-bill-search').addClass('g-d-n');
            $('#co-po-ref').addClass('g-d-n');
            $('#co-submit-button').addClass('g-d-n');
            $('#co-privacy-notice').addClass('g-d-n');
            $('.co-bill-address').addClass('g-d-n');
            $('.co-acc-bill-address').addClass('g-d-n');
            $('#co-phone').removeClass('selected');
            $('#co-bacs').removeClass('selected');
            $('#co-accountapplication').removeClass('selected');
            $('.co-paym-button').removeClass('selected');
            $('.co-paym-button > input[type=radio]').prop('checked', false);
            $('#co-credit-tc').addClass('g-d-n');
            $('#co-privacy-notice').removeClass('g-d-n');
            $('#co-credit-billing').addClass('g-d-n');
            $('#co-submit-button').addClass('g-d-n');
            $('#co-privacy-notice').addClass('g-d-n');
            $('#co-credit-paym').addClass('g-d-n');
            $('#co-paypal-button').addClass('g-d-n');//hide PP button

            $('.co-paym .co-paym-accapp').hide();
            if ((type === "2" && parseInt($('#BasketTotals_GrandTotalExcVat').val()) > parseInt($('#lowerSpendLimit').val()) || type === "3")) {
                $('.co-paym .co-paym-accapp').show();
                if (type === "3") {
                    $('.co-paym .co-paym-accapp').show();
                    $('#co-company-id').hide();
                    $('#co-company-id input').prop('disabled', true);
                } else {
                    $('#co-company-id').show();
                    $('#co-company-id input').prop('disabled', false);
                }
                $('#co-credit-tc input').prop('disabled', false);
            } else {
                $('#co-accountapplication').find('input').each(function () {
                    $(this).prop('disabled', true);
                });
                $('#co-accountapplication').find('select').each(function () {
                    $(this).prop('disabled', true);
                });
                $('#co-credit-tc input').prop('disabled', true);
            }
        });

        $(document).on('click', '#applyForCredit',
            function () {
                sessionStorage.setItem("applyForCreditClicked", true);
                if ($('#applyForCredit').is(':checked')) {
                    $('.co-paym-button[data-id="AccountApplication"]').trigger('click');
                } else {
                    $('.co-paym-button[data-id="CreditDebit"]').trigger('click');
                }
                sessionStorage.setItem("applyForCreditClicked", false);
            });

        $(document).on('change',
            '#CheckoutDetails_DeliveryAddress_PostCode',
            function () {
                var postcode = $('#CheckoutDetails_DeliveryAddress_PostCode').val();
                $.ajax({
                    url: "/Checkout/ChangePostCode",
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        postcode: postcode
                    },
                    async: false,
                    success: function (data) {
                        if (data.savereturn.IsSuccess) {
                            $('#delivery-options').fadeOut(300,
                                function () {
                                    $('#delivery-options').html(data.savereturn.Html);
                                    $('#delivery-options').fadeIn(300);
                                    $("input:radio[name='CheckoutDetails.DeliveryServiceId']:checked").trigger('click');
                                });
                        } else {
                            $.confirm({
                                title: 'Postcode',
                                content: data.savereturn.Message,
                                buttons: {
                                    OK: function () {
                                        $('#CheckoutDetails_DeliveryAddress_Line1').val('');
                                        $('#CheckoutDetails_DeliveryAddress_Line2').val('');
                                        $('#CheckoutDetails_DeliveryAddress_Line3').val('');
                                        $('#CheckoutDetails_DeliveryAddress_Line4').val('');
                                        $('#CheckoutDetails_DeliveryAddress_Line5').val('');
                                        $('#CheckoutDetails_DeliveryAddress_PostCode').val('');
                                        $('#co-deladd-fields').addClass('g-d-n');
                                    }
                                }
                            });
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/ChangePostCode/", xhr, textStatus, thrownError);
                    }
                });
            });

        $(document).on('click',
            '.delivery-method',
            function () {
                var delServiceId = $("input[name='CheckoutDetails.DeliveryServiceId']:checked").val();
                $.ajax({
                    url: "/Checkout/ChangeDeliveryMethod",
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        deliveryServiceId: delServiceId
                    },
                    async: false,
                    success: function (data) {
                        $('#orderSummaryContainer').html(data.savereturn.Html);
                        if ($('#CheckoutDetails_PaymentMethod').val() !== "") {
                            $('#co-submit-button').removeClass('g-d-n');
                            $('#co-privacy-notice').removeClass('g-d-n');
                        }

                        if ($("input[name='radioPaymentMethod']:checked").parent().attr('data-id') == 'PayPal') {
                            $('#co-submit-button').addClass('g-d-n');
                        }

                        if (data.basketTotal < 0.01) {
                            $('#paym-required').addClass('g-d-n');
                            $('.co-paym-addinfo, .co-paym-button').removeClass('selected');
                            $('#co-po-ref, #co-bacs, #co-phone, #co-paypal').addClass('g-d-n');
                            $('.co-bill-address').addClass('g-d-n');
                            $('#co-submit-button > button').text('Complete Order');
                        } else {
                            $('#paym-required').removeClass('g-d-n');
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/ChangeDeliveryMethod/", xhr, textStatus, thrownError);
                    }
                });
            });

        // Handle back button behavoir in the checkout 
        sessionStorage.costatus = "Started";
    }

    if (isCurrentPage('/checkout/stage2', true)) {
        //$('input[type=radio][name="CheckoutDetails_SagePayCardId"]').change(function () {
        //    var cardid = this.value;
        //    var cardtype = $('#CardId_' + cardid).attr('data-cardtype');
        //    $.ajax({
        //        url: "/Checkout/SagePayChangeCard/",
        //        dataType: 'json',
        //        traditional: true,
        //        type: 'POST',
        //        cache: false,
        //        data: {
        //            id: cardid,
        //            cardtype: cardtype
        //        },
        //        async: false,
        //        success: function (data) {
        //            if (data.IsSuccess) {
        //                $('#CheckoutDetails_CardType').val(cardtype);
        //            } else {
        //                location.href = '/checkout?pm=SessionExpired';
        //            }
        //        },
        //        error: function (xhr, textStatus, thrownError) {
        //            logAjaxScriptError("/Checkout/SagePayChangeCard/", xhr, textStatus, thrownError);
        //        }
        //    });
        //});

        //$('input[name="CheckoutDetails_SagePayCardId"]').first().change();

        //$('input#CheckoutDetails_SaveThisCard').change(function () {
        //    var saveTheCard = $('#CheckoutDetails_SaveThisCard').is(":checked");
        //    $.ajax({
        //        url: "/Checkout/SagePayChangeSaveCard/",
        //        dataType: 'json',
        //        traditional: true,
        //        type: 'POST',
        //        cache: false,
        //        data: {
        //            saveTheCard: saveTheCard
        //        },
        //        async: false,
        //        success: function (data) {
        //            $('#SagePayIFrame').attr('src', $('#SagePayIFrame').attr('src'));
        //        },
        //        error: function (xhr, textStatus, thrownError) {
        //            logAjaxScriptError("/Checkout/SagePayChangeSaveCard/", xhr, textStatus, thrownError);
        //        }
        //    });
        //});

        //$(document).on('click',
        //    '#co-place-order',
        //    function () {
        //        if ($(this).attr('type') === "button") {
        //            $('#card-entry').removeClass('g-d-n');
        //            $('#chk-save').addClass('g-d-n');
        //            $('#saved-cards').addClass('g-d-n');
        //            $('#saved-different').html('Different');
        //            $('#co-place-order').addClass('g-d-n');
        //            $('#SagePayIFrame').attr('src', '/Checkout/SagePayRegistration/saved');
        //            $('#show-saved-cards').hide();
        //        }
        //    });

        //$(document).on('click',
        //    '#add-card',
        //    function () {
        //        $('#CheckoutDetails_UseASavedCard').val('False');
        //        $('#saved-cards').addClass("g-d-n");
        //        $('#card-entry').removeClass("g-d-n");
        //        $('#SagePayIFrame').attr('src', $('#SagePayRegistrationUrl').val());
        //        $('#co-place-order').addClass('g-d-n');
        //    });

        //$(document).on('click',
        //    '#show-saved-cards',
        //    function () {
        //        showSavedCards('');
        //    });

        //$(document).on('click',
        //    '[id^="deleteCard"]',
        //    function () {
        //        var cardid = $(this);
        //        $.ajax({
        //            url: "/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1],
        //            dataType: 'json',
        //            traditional: true,
        //            type: 'POST',
        //            cache: false,
        //            async: false,
        //            success: function (data) {
        //                cardid.closest('tr').remove();
        //                // Ensure a card is selected
        //                var cardselected = false;
        //                $('[name="CheckoutDetails_SagePayCardId"]').each(function () {
        //                    if ($(this).is(':checked')) {
        //                        cardselected = true;
        //                    }
        //                });
        //                if (!cardselected) {
        //                    if ($('[name="CheckoutDetails_SagePayCardId"]:first').length > 0) {
        //                        $('[name="CheckoutDetails_SagePayCardId"]:first').prop('checked', true);
        //                        $('[name="CheckoutDetails_SagePayCardId"]:first').change();
        //                    } else {
        //                        // No more cards to select
        //                        $('#add-card').trigger('click');
        //                    }
        //                }
        //            },
        //            error: function (xhr, textStatus, thrownError) {
        //                logAjaxScriptError("/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1], xhr, textStatus, thrownError);
        //            }
        //        });
        //    });

        //var iframeContent;
        //var saveChecked = false;

        //$('#chk-save').click(function (e) {
        //    saveChecked = true;
        //});

        //$('#SagePayIFrame').on('load', function () {
        //    if ($('#SagePayIFrame').attr('src').toLowerCase() === '/checkout/sagepayregistration/new') {

        //        $('#show-saved-cards').show();

        //        if (!saveChecked) {
        //            // The following test yields different results if identity operator (!==) is used
        //            if (iframeContent != null && $('#SagePayIFrame') != iframeContent) {
        //                $('#show-saved-cards').hide();
        //                $('#chk-save').hide();
        //            }
        //        }

        //        iframeContent = $('#SagePayIFrame');
        //        saveChecked = false;
        //    } else {
        //        iframeContent = null;
        //    }
        //});
    }

    if (isCurrentPage('/checkout/stage2a')) {
        $('input[type=radio][name="CheckoutDetails_OpayoCardId"]').change(function () {
            var cardid = this.value;
            var cardtype = $('#CardId_' + cardid).attr('data-cardtype');
            $.ajax({
                url: "/Checkout/OpayoChangeCard/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    id: cardid,
                    cardtype: cardtype
                },
                async: false,
                success: function (data) {
                    if (data.IsSuccess) {
                        $('#CheckoutDetails_CardType').val(cardtype);
                    } else {
                        location.href = '/checkout?pm=SessionExpired';
                    }
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/OpayoChangeCard/", xhr, textStatus, thrownError);
                }
            });
        });

        $('input[name="CheckoutDetails_SagePayCardId"]').first().change();

        $('input#CheckoutDetails_SaveThisCard').change(function () {
            var saveTheCard = $('#CheckoutDetails_SaveThisCard').is(":checked");
            $.ajax({
                url: "/Checkout/OpayoChangeSaveCard/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    saveTheCard: saveTheCard
                },
                async: false,
                success: function (data) {

                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/OpayoChangeSaveCard/", xhr, textStatus, thrownError);
                }
            });
        });

        $(document).on('click',
            '#co-place-order',
            function () {
                if ($(this).attr('type') === "button") {
                    $('#card-entry').removeClass('g-d-n');
                    $('#chk-save').addClass('g-d-n');
                    $('#saved-cards').addClass('g-d-n');
                    $('#saved-different').html('Different');
                    $('#co-place-order').addClass('g-d-n');
                    $('#SagePayIFrame').attr('src', '/Checkout/SagePayRegistration/saved');
                    $('#show-saved-cards').hide();
                }
            });

        $(document).on('click',
            '#add-card',
            function () {
                $('#CheckoutDetails_UseASavedCard').val('False');
                $('#saved-cards').addClass("g-d-n");
                $('#card-entry').removeClass("g-d-n");
                $('#SagePayIFrame').attr('src', $('#SagePayRegistrationUrl').val());
                $('#co-place-order').addClass('g-d-n');
            });

        $(document).on('click',
            '#show-saved-cards',
            function () {
                showSavedCards('');
            });

        $(document).on('click',
            '[id^="deleteOpayoCard"]',
            function () {
                var cardid = $(this);
                $.ajax({
                    url: "/Checkout/OpayoDeleteCard?id=" + cardid.attr('data-id') + "&tokenId=" + cardid.attr('data-tokenId'),
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    async: false,
                    success: function (data) {
                        cardid.closest('tr').remove();
                        // Ensure a card is selected
                        var cardselected = false;
                        $('[name="CheckoutDetails_OpayoCardId"]').each(function () {
                            if ($(this).is(':checked')) {
                                cardselected = true;
                            }
                        });
                        if (!cardselected) {
                            if ($('[name="CheckoutDetails_OpayoCardId"]:first').length > 0) {
                                $('[name="CheckoutDetails_OpayoCardId"]:first').prop('checked', true);
                                $('[name="CheckoutDetails_OpayoCardId"]:first').change();
                            } else {
                                // No more cards to select
                                $('#add-card').trigger('click');
                            }
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1], xhr, textStatus, thrownError);
                    }
                });
            });

        //var iframeContent;
        var saveChecked = false;

        $('#chk-save').click(function (e) {
            saveChecked = true;
        });

        var validColorDepths = [48, 32, 24, 16, 15, 8, 4, 2, 1];
        var colorDepth = 24;
        if ($.inArray(screen.colorDepth, validColorDepths) != -1) {
            colorDepth = screen.colorDepth;
        }
        $('.browserColorDepth').val(colorDepth);
        $('.browserScreenHeight').val(screen.height);
        $('.browserScreenWidth').val(screen.width);

        $(document).on('hidden.bs.modal',
            '#threeDSPopup',
            function (e) {
                refreshSessionKey();
            }
        );

        // New Card
        $(document).on('click',
            '#co-submit-card',
            function (e) {
                e.preventDefault(); // to prevent form submission

                // Manually check Expiry Date
                var mm = parseInt($('input[data-card-details="expiry-date-mm"]').val());
                var yy = parseInt($('input[data-card-details="expiry-date-yy"]').val());
                var thismm = (new Date).getMonth() + 1;
                var thisyy = (new Date).getFullYear() - 2000;
                if (yy < thisyy || (yy == thisyy && mm < thismm)) {
                    $.confirm({
                        title: 'An error was encountered',
                        content: 'The expiry date you have entered is in the past.',
                        buttons: {
                            OK: function () { }
                        }
                    });
                    return;
                }

                var thisform = $('#co-stage2-card');
                sagepayOwnForm({ merchantSessionKey: thisform.find('input#CheckoutDetails_MerchantSessionKey_merchantSessionKey').val() }).tokeniseCardDetails({
                    cardDetails: {
                        cardholderName: $('input[data-card-details="cardholder-name"]').val(),
                        cardNumber: $('input[data-card-details="card-number"]').val(),
                        expiryDate: $('input[data-card-details="expiry-date-mm"]').val() + $('input[data-card-details="expiry-date-yy"]').val(),
                        securityCode: $('input[data-card-details="security-code-b"]').val()
                    },
                    onTokenised: function (result) {
                        if (result.success) {
                            thisform.find('input[name="CardIdentifier"]').val(result.cardIdentifier);
                            callStage2b(result, thisform);
                        } else {
                            $.confirm({
                                title: 'An error was encountered',
                                content: 'Please check that you have entered your card details, including your security code, correctly.',
                                buttons: {
                                    OK: function () { }
                                }
                            });
                        }
                    }
                });
            }
        );

        // Use Saved Card
        $(document).on('click',
            '#co-submit-savedCard',
            function (e) {
                e.preventDefault(); // to prevent form submission

                var thisform = $('#co-stage2-savedCard');
                var token = $('input[name="CheckoutDetails_OpayoCardId"]:checked').attr("data-tokenId");
                thisform.find('#CardIdentifier').val(token);
                sagepayOwnForm({ merchantSessionKey: thisform.find('input#CheckoutDetails_MerchantSessionKey_merchantSessionKey').val() }).activateReusableCardIdentifier({
                    reusableCardIdentifier: token
                    , securityCode: $('input[data-card-details="security-code-a"]').val()
                    , onActivated: function (result) {
                        if (result.success === true) {
                            callStage2b(result, thisform);
                        } else {
                            $.confirm({
                                title: 'An error was encountered',
                                content: 'Please check that you have entered your card details, including your security code, correctly.',
                                buttons: {
                                    OK: function () { }
                                }
                            });
                        }
                    }
                });
            }
        );
    }

    if (isCurrentPage('/checkout/stage2') || isCurrentPage('/checkout/stage3')) {
        if (sessionStorage.costatus !== "Started") {
            // Redirect to basket
            location.href = '/checkout/';
        }
    }

    if (isCurrentPage('/checkout/stage3')) {
        sessionStorage.costatus = "Ended";
        if (!!navigator.userAgent.match(/Version\/[\d\.]+.*Safari/) === false) {
            if (window.history && window.history.pushState) {
                $(window).on('popstate',
                    function () {
                        var hashLocation = location.hash;
                        var hashSplit = hashLocation.split("#!/");
                        var hashName = hashSplit[1];

                        if (hashName !== '') {
                            var hash = window.location.hash;
                            if (hash === '') {
                                launchPopup('Stage3BackButton', 'popup', 'md', '');
                                //alert('Using the back button from this page will result i.');
                                window.history.pushState('forward', null, '/');
                            }
                        }
                    });

                window.history.pushState('forward', null, '/');
            }
        }
    }
});