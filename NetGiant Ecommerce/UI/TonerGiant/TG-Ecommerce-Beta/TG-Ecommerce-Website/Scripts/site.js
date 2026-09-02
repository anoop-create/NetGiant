// Structure of this document is as follows:
//      1. Functions
//      2. Immediate Code

//#region Functions

function isCurrentPage(pageUrl, isExact) {
    if (typeof isExact === 'undefined') { isExact = false; }

    if (isExact) {
        return window.location.href.toLowerCase().slice(pageUrl.length * -1);
    }
    return window.location.href.toLowerCase().indexOf(pageUrl) > -1;
}

function signinComplete(data) {
    if (data.responseJSON.issuccess) {

        removeErrorMessage('.signin');

        if (!data.responseJSON.redirecturl) {
            window.parent.location.reload();
        } else {
            window.parent.location = data.responseJSON.redirecturl;
        }
    } else {
        displayErrorMessage('.signin', 'Incorrect Username or Password');

        if (typeof $("#signin-form [type='submit']").attr('disabled') !== 'undefined') {
            $("#signin-form [type='submit']").removeAttr('disabled');
        }
    }
}

function signupComplete(data) {
    if (data.responseJSON.saveReturn.IsSuccess) {

        removeErrorMessage('.signup-address, .address-manual');

        window.parent.location.href = "/";
    } else {

        displayErrorMessage('.signup-address .address-manual', 'There was an issue creating the account');

        if (typeof $("#signup-form [type='submit']").attr('disabled') !== 'undefined') {
            $("#signup-form [type='submit']").removeAttr('disabled');
        }
    }
}

function disableSubmit() {
    $(this).find("input[type='submit'],button[type='submit']").attr('disabled', true);
    setTimeout(function () {
        $(this).find("input[type='submit'],button[type='submit']").attr('disabled', false);
    }, 500);
}

function identComplete(data) {
    var response = data.responseJSON;
    if (response.savereturn.IsSuccess) {
        removeErrorMessage('.ident');
        removeErrorMessage('.ident-set-password');
        // OK Submit the form
        $('#ident-modal').modal('hide');
        $('#CheckoutDetails_IsNewCustomer').val(response.signin.IsNewCustomer);
        $('#CheckoutDetails_Email').val(response.signin.UserName);
        $('#CheckoutDetails_Password').val(response.signin.Password);
        $('#co-form').submit();
    } else if (response.savereturn.Message === "3") {
        getPopupContent('ForgotPassword', null, function (sr) {
            $('.ident-reset-password').append(sr.Html);

            $('#password-reset-email').val($('#SignIn_UserName').val());

            displayErrorMessage('.ident-reset-password', response.savereturn.Html);

            $('.ident-reset-password, .ident-reset-back').fadeIn(500);
            $('.ident').hide();
        });
    } else {
        displayErrorMessage('.ident', response.savereturn.Html);

        if (response.savereturn.Message === "1") {
            $('.check-existing').prop('checked', true).change();
        }
    }

    var attr = $(this).find("input[type='submit']").attr('disabled');
    if (attr === 'disabled' || attr === true) {
        $(this).find("input[type='submit']").attr('disabled', false);
    }
}

function newsletterSignUpComplete() {
    launchPopup('NewsletterConfirmation', 'popup', 'sm', '');
}

function myAccountUpdateComplete(data) {
    var formId = $(this).attr('id');
    var errClass = formId === 'updateDetails' ? '.update-details' : '.update-address';

    $('.validation-summary-errors').hide();

    if (!data.responseJSON.savereturn.IsSuccess) {
        $('#password-success').hide();

        if (data.responseJSON.savereturn.Message === 'Email') {
            displayErrorMessage(errClass, 'Email is already in use!');
        } else if (data.responseJSON.savereturn.Message === 'Password') {
            displayErrorMessage(errClass, 'Current password is incorrect.');
        } else if (data.responseJSON.savereturn.Message === 'Authenticate') {
            displayErrorMessage(errClass, 'Error: Authentication failed.');
        }
    } else {
        removeErrorMessage(errClass);
        $('#password-success').show();
    }
}

function popupFormComplete(data) {
    $('#popup .fa-times').trigger('click');
    $('.modal').modal('hide');
}

function serverAction(action) {
    $.ajax({
        url: "/Misc/ServerAction/",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            action: action
        },
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                switch (action) {
                    case 1:
                        $('.cc-block').slideUp(600);
                        break;
                    case 2:
                        $('.ca-message').slideUp(600, function () {
                            $('.ca-message').addClass('g-d-n');
                        });
                        break;
                    default:
                        break;
                }
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/MyAccount/VerifyPassword/", xhr, textStatus, thrownError);
        }
    });
}

function askAQuestionSuccess(data) {
    if (!data)
        return false;

    removeErrorMessage('.err-email, .err-question');

    var sr = data.savereturn;

    if (!sr.IsSuccess) {

        if (sr.Message === "Email") {
            displayErrorMessage('.err-email', sr.Html);
        }
        else if (sr.Message === "Question") {
            displayErrorMessage('.err-question', sr.Html);
        }
    }
    else {
        $('.askAQuestion').hide();
        $('.askAQuestionSuccess').show();
    }
}

function savePrinterFormComplete(data) {
    $('#save-printer-text').
        html('<a class="second" href="/MyAccount/Index/MyPrinters"><span class="g-i"> View My Printers</span></a>');
    if (data.responseJSON.savereturn.IsSuccess) {
        $(".printerMessage > #printerItem").html(data.responseJSON.savereturn.Html);
        $('.printerMessage').animate({
            opacity: "show"
        },
            500).delay(3000).animate({
                opacity: "hide"
            },
                500,
                function () {
                    //$(this).css('right', 105);
                });
    }
    popupFormComplete(data);
}

function deletePrinterFormComplete(data) {
    if (data.responseJSON.savereturn.IsSuccess) {
        $('.myprinter-id-' + data.responseJSON.id).remove();
    }
    popupFormComplete(data);
}

function passwordResetRequestFormComplete(data) {
    if (data.responseJSON.savereturn.IsSuccess) {
        removeErrorMessage('.reset-password');
        $('.reset-confirmation').fadeIn(500);
        $('.reset-password, .signin-reset-back, .ident-reset-back').hide();
    } else {
        displayErrorMessage('.reset-password', 'An account for this email does not exist.');
    }
}

function validateCheckbox(obj) {
    if (!obj) return;

    if (obj.find('input.input-validation-error').length > 0) {
        if (obj.parents('.checkbox-validation-error').length === 0) {
            obj.wrap('<div class="checkbox-validation-error"></div>');
        }
    } else {
        if (obj.parents('.checkbox-validation-error').length > 0) {
            obj.unwrap();
        }
    }
}

function utilityDotDotDot(container) {
    container.dotdotdot({
        ellipsis: '...',
        after: 'div.keep',
        height: $(window).height() - 200,
        wrap: 'children',
        fallbackToLetter: false,
        callback: function (isTruncated, orgContent) {
            if (isTruncated) {
                $(this).find('.keep > .message').show();
            }
        }
    });
}

function applyFilter() {
    // Build the selector
    var selector = '';
    var sel;
    $('.fltr-filters .fltr-group').each(function () {
        var comma = '';
        var selector1 = '';

        $(this).find('input[id^="att"]').each(function () {
            if ($(this).is(':checked')) {
                var id = $(this).attr('id');
                var idarray = id.split('-');
                var att = 'data-att-' + idarray[1];
                selector1 += comma + '[data-att-' + idarray[1] + '*="#' + idarray[2] + '#"]';
                if (comma === '') {
                    comma = ',';
                }
            }
        });
        if (selector1 !== '') {
            selector += ".filter('" + selector1 + "')";
        }
    });

    if (isCurrentPage('model/') || isCurrentPage('/search-results')) {

        // Set the selector
        sel = eval("$('.pl-products > div > .pl-entry')" + selector);

        // Show the selected products, headers and set counter
        sel.removeClass('g-d-n');
        sel.parent().find('.pl-sub-banner').removeClass("g-d-n");
    }
    if (isCurrentPage('products/')) {

        // Set the selector
        sel = eval("$('.pg-products > .pg-entry')" + selector);

        // Show the selected products, headers and set counter
        sel.removeClass("g-d-n");
        $("img.lazy").lazyload();
    }

    refreshFilterCounts();
}

function applyAltFilter(filter) {
    // Build the regex
    var reg = '^';
    var filters = filter.split('-');
    $.each(filters, function (i, val) {
        if (val == "*") {
            reg = reg + '.';
        } else {
            reg = reg + val;
        }
    });
    reg = reg + '$';
    var regex = new RegExp(reg);

    if (isCurrentPage('model/') || isCurrentPage('/search-results')) {

        $('.pl-products > div > .pl-entry').each(function () {
            if (regex.test($(this).attr('data-alt-att'))) {
                $(this).removeClass('g-d-n');
                $(this).parent().find('.pl-sub-banner').removeClass("g-d-n");
            }
        });
    }

    refreshFilterCounts();
}

function applyPriceFilter(sel) {
    var priceMin = $('#minPrice').val() === '' ? 0 : Number($('#minPrice').val());
    var priceMax = $('#maxPrice').val() === '' ? 9999999 : Number($('#maxPrice').val());

    if (priceMin >= 0 && priceMax > 0) {
        $(sel).each(function () {
            if (!$(this).hasClass("g-d-n")) {
                var prodPrice = Number($(this).find('.price').text());
                if (prodPrice <= priceMax && prodPrice >= priceMin) {
                    $(this).removeClass("g-d-n");
                    $(this).parent().find('.pl-sub-banner').removeClass("g-d-n");
                } else {
                    $(this).addClass("g-d-n");
                }
            }
        });
    } else {
        $(sel).each(function () {
            if (!$(this).hasClass("g-d-n")) {
                $(this).parent().find('.pl-sub-banner').removeClass("g-d-n");
            }
        });
    }
}

function verifyPassword(type) {
    $('.validation-summary-errors').hide();

    var password = '';
    var ret = false;
    if (type === 'password-change') {
        password = $('#OldPassword').val();
    } else {
        password = $('#Password').val();
    }

    $.ajax({
        url: "/MyAccount/VerifyPassword/",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            password: password
        },
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                ret = true;
            } else {
                displayErrorMessage('.update-address', 'Current password is incorrect');
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/MyAccount/VerifyPassword/", xhr, textStatus, thrownError);
        }
    });
    if (type === 'password-change') {
        $('#OldPassword').val('');
    } else {
        $('#Password').val('');
    }
    return ret;
}

function openUtilityBar(sectionname) {
    sectionname = typeof sectionname !== 'undefined' ? sectionname : 'basket';
    $('#' + sectionname + ' >  .header').trigger('click');
}

function scrollToSelector(selector, furtherOffset, callback) {
    furtherOffset = typeof furtherOffset !== 'undefined' ? furtherOffset : 0;
    var elem = $(selector);
    $('html, body').animate({
        scrollTop: $(elem).offset().top - 20 - furtherOffset
    },
        400, callback);
}

function launchPopup(popupname, popupid, popupwidth, replacements, options) {
    if (!popupname) {
        return false;
    }

    // Close any existing popups
    $('.modal, .modal-backdrop').not('.donotremove').remove();

    $.ajax({
        url: "/Misc/Popup",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            popupname: popupname,
            popupid: popupid,
            popupwidth: popupwidth,
            replacements: replacements
        },
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                $('body').append(data.savereturn.Html);

                if (options) {
                    $('#' + popupid).modal(options);
                } else {
                    $('#' + popupid).modal('show');
                }

                setDeferredImages();
                if ($('.cutoffCountdownFalse').length) {
                    startTime();
                }

                if (data.savereturn.Html.indexOf('<form ') >= 0) {
                    $('#popup-form').validate({
                        errorContainer: "#popup-form .error-msg",
                        highlight: function (element, errorClass, validClass) {
                            var msg = $('.' + errorClass + '[for="' + $(element).attr('name') + '"]').
                                closest('.error-msg');
                            msg.find('i').remove();
                            msg.prepend('<i class="fa fa-exclamation-triangle fa-lg"></i>');
                        }
                    });
                }
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/Popup/", xhr, textStatus, thrownError);
        }
    });
}

function setDeferredImages() {
    $('.deferImage').on('error', function () {
        if (window.location.hostname === 'localhost') return;
        this.src = "https://" + window.location.hostname + "/version1/cdn/Images/noImage.jpg";
    });

    $('.deferImage').each(function () {
        if ($(this).attr('src') !== $(this).attr('data-original')) {
            $(this).attr('src', $(this).attr('data-original'));
        }
    });
}

function getPopupContent(popupname, replacements, callback) {
    if (!popupname)
        return;

    $.ajax({
        url: '/Misc/PopupContent',
        type: 'post',
        cache: false,
        data: {
            popupname: popupname,
            replacements: replacements
        },
        success: function (e) {
            if (callback) {
                callback(e.savereturn);
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError('/Misc/PopupContent/', xhr, textStatus, thrownError);
        }
    });
}

function getHighlightTooltip(tooltipname) {
    if (!tooltipname) {
        return false;
    }

    $.ajax({
        url: "/Misc/HighlightTooltip",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            name: tooltipname
        },
        async: false,
        success: function (e) {
            if (e.savereturn.IsSuccess) {
                $('body').append(e.savereturn.Html);
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/HighlightTooltip/ - " + tooltipname, xhr, textStatus, thrownError);
        }
    });
}

function collapsablePanelComplete(section, detail) {
    detail.slideDown(400,
        function () {
            //set button wording
            section.find('.toggle-section:first').html('Close <i class="fa fa-chevron-up"></i>');
            $('.selectpicker').selectpicker();
            $('[data-toggle="tooltip"]').tooltip();
            $('.mini-product-container').jScrollPane({ showArrows: true }); // <== Set height on mini-product-container to position left/right scrollbar

            // moreLess is "View More" in collapsed mode and "View Less" in expanded mode
            // for e.g. order history
            section.find('.moreLess').each(function () {
                toggleCollapsedMode($(this),
                    $(this).attr('data-num-items'),
                    $(this).attr('data-buttclass'),
                    $(this).attr('data-scroll-offset'),
                    false);
            });
            section.find('.atb-qty').each(function () {
                $(this).kendoNumericTextBox({
                    "change": function (e) { if (!this.value()) this.value(1); },
                    "spin": function (e) { if (!this.value()) this.value(1); },
                    "format": "#", "placeholder": "Enter quantity"
                });
            });
        });
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}

function adjustModal(offset) {
    var heightModal = $(window).height() - offset;
    $(".modal-scroll").css({ "height": heightModal, "overflow-y": "auto" });
}

function checkCompareCount() {
    if ($('.pg-products .fa-check-square-o').length === 0) {
        //disable compare button and set tooltip
        $('.compare').attr("disabled", "disabled");
        $('.compare').attr("title",
            "No products selected. Please hover over the product(s) you want to compare and check the compare box, then select the compare button. You can compare up to a maximum of 4 products each time.");
        $('.compare').removeAttr('data-toggle');
    } else {
        //enable compare button and set tooltip
        $('.compare').removeAttr("disabled");
        $('.compare').attr("title", "Click to compare");
        $('.compare').attr('data-toggle', 'modal');
    }
}

function imageError(elem, img) {
    elem.attr('data-imgerr', elem.attr('src'));
    elem.attr('src', img);
}

function isNumber(evt) {
    evt = evt ? evt : window.event;
    var charCode = evt.which ? evt.which : evt.keyCode;
    if (charCode > 31 && (charCode < 48 || charCode > 57) && charCode !== 46) {
        return false;
    }
    return true;
}

function changeBasketComplete(data, thisbutton) {
    if (!data.savereturn.IsSuccess) {
        launchPopup('IsInCheckoutAtb', 'popup');
        return false;
    }
    var ref = thisbutton.attr('data-productid');
    var quickreorder = thisbutton.closest('#quick-order').length;
    var itemtype = '1';
    if (typeof thisbutton.attr('data-itemtype') !== 'undefined') {
        itemtype = thisbutton.attr('data-itemtype');
    }

    $('.basketQuantity').html(data.basketQuantity);
    $('.basketTotal').html(data.basketTotal);
    $('.basket-counter').html(data.basketQuantity);

    var t = $('<section>').append($.parseHTML(data.basketSummary));
    var h = t.find('div[data-productid="' + ref + '"]');

    $('#minibasket-widget').replaceWith(data.basketSummary);
    if ($('#miniCartOverlay').hasClass('is-open') && quickreorder === 0) {
        // The mini-cart is the new floating-button/overlay widget (#cartFab/#miniCartOverlay
        // in MiniBasket.cshtml), not the old offcanvas tray - #minibasket-widget was just
        // replaced wholesale with fresh markup that starts closed by default, so re-open it
        // by re-adding the 'is-open' class rather than the old "trigger a click on .content"
        // approach, which doesn't exist in this markup at all.
        $('#miniCartOverlay').addClass('is-open');
        $('body').css('overflow', 'hidden');
    } else {
        // trigger the added to basket pop up
        $('#basketItem, #mobileBasketItem').html(h);

        if (quickreorder === 0) {
            $('.basketMessage').css('right', '105px');
        } else {
            $('.basketMessage').css('right', '385px');
        }
        $('.basketMessage').animate({
            opacity: 'show',
            right: '-=50px'
        },
            500).delay(3000).animate({
                opacity: 'hide'
            },
                500,
                function () {
                    $(this).css('right', 105);
                });
        //$('body').append('<div class="mobileBasketBackdrop hidden-lg hidden-md g-cur-p"/>');
        $('.mobileBasketMessage').slideDown(500, function () {
            $('.mobileBasketClose').show();
        });
    }

    if ($('.product-info-message').length > 0) {
        var productentry = $('.body-content button[data-productid="' + ref + '"]').closest('.atb-entry');
        productentry.find('.product-info-message').html(data.productInfoMessage).removeClass('g-v-h');
    }
    setDeferredImages();
    if (itemtype === '3') {
        refreshViewBasket();
    }
}
function refreshViewBasket() {
    $.ajax({
        url: "/Checkout/RefreshViewBasket/",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
        },
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                refreshVbFields(data);
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Checkout/RefreshViewBasket/", xhr, textStatus, thrownError);
        }
    });
}

function refreshVbFields(data) {

    var $basket = $('#vbBasketDetails');

    // =========================================================
    // PRESERVE PAYPAL - detach the actual rendered container
    // =========================================================
    var $paypal2 = $basket.find('#paypal-button2').detach();
    var $paypal3 = $basket.find('#paypal-button3').detach();


    // =========================================================
    // PRESERVE AMAZON
    // =========================================================
    var $amazonButton = $basket.find(
        '#AmazonPayButton, ' +
        '#amazonPayButton, ' +
        '#amazon-pay-button, ' +
        '.amazon-pay-button'
    ).detach();


    // =========================================================
    // SAVE COUNTDOWN VALUES
    // =========================================================
    var countdownValues = [];

    $basket.find('.cutoffCountdownFalse').each(function () {
        countdownValues.push($(this).text());
    });


    // =========================================================
    // REFRESH BASKET
    // =========================================================
    $basket.html(data.savereturn.Html);


    // =========================================================
    // RESTORE PAYPAL
    //
    // IMPORTANT:
    // Empty the NEW placeholder and append the OLD rendered DOM
    // =========================================================

    if ($paypal2.length) {
        var $newPaypal2 = $basket.find('#paypal-button2');

        if ($newPaypal2.length) {
            $newPaypal2.empty().append($paypal2.contents());
        }
    }

    if ($paypal3.length) {
        var $newPaypal3 = $basket.find('#paypal-button3');

        if ($newPaypal3.length) {
            $newPaypal3.empty().append($paypal3.contents());
        }
    }


    // =========================================================
    // RESTORE AMAZON
    // =========================================================

    if ($amazonButton.length) {

        var $newAmazonButton = $basket.find(
            '#AmazonPayButton, ' +
            '#amazonPayButton, ' +
            '#amazon-pay-button, ' +
            '.amazon-pay-button'
        ).first();

        if ($newAmazonButton.length) {
            $newAmazonButton.replaceWith($amazonButton);
        }
    }


    // =========================================================
    // RESTORE COUNTDOWN VALUES
    // =========================================================

    $basket.find('.cutoffCountdownFalse').each(function (index) {
        if (typeof countdownValues[index] !== 'undefined') {
            $(this).text(countdownValues[index]);
        }
    });


    // =========================================================
    // UPDATE TOTALS
    // =========================================================

    $('.basketQuantity').html(data.basketQuantity);
    $('.basketTotal').html(data.basketTotal);
    $('.basket-counter').html(data.basketQuantity);


    $('#minibasket-widget').replaceWith(data.basketSummary);

    setDeferredImages();


    // DO NOT call:
    // renderPaypalButtonV2();
    // updateCutoffCountdown();


    // =========================================================
    // REINITIALISE QUANTITY INPUTS
    // =========================================================

    $('input[id^="qty-"]').each(function () {
        $(this).kendoNumericTextBox({
            "change": changeBasketQty,
            "spin": changeBasketQty,
            "format": "#",
            "placeholder": "Enter quantity"
        });
    });
}

// Sets a basket line to an absolute quantity - used ONLY by the full basket page
// (BasketDetails.cshtml / BasketDetailsv2.cshtml: qty stepper buttons + the Kendo
// NumericTextBox Change/Spin handlers above). This was previously commented out entirely
// while its two call sites above (in refreshVbFields) still referenced it - meaning every
// call to refreshVbFields threw "changeBasketQty is not defined" and the Kendo qty spinner
// on the full basket page silently never got wired up. Restored to working order and left
// otherwise unchanged - the mini-cart has its own independent changeMiniBasketQty() below,
// so nothing here needs to know about the mini-cart's markup at all.
function changeBasketQty(productref, qty) {

    var isCheckout = isCurrentPage('/checkout');

    $.ajax({
        url: "/Product/BasketUpdateQty/",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,

        data: {
            productref: productref,
            qty: qty
        },

        async: false,

        success: function (data) {

            if (!data.savereturn.IsSuccess) {
                launchPopup('IsInCheckout', 'popup');
                return false;
            }

            if (isCheckout) {

                // Refresh basket details only.
                // Payment button DOM is preserved inside refreshVbFields().
                refreshViewBasket();

            } else {

                $('#minibasket-widget').replaceWith(data.basketSummary);

                $('.basketQuantity').html(data.basketQuantity);
                $('.basketTotal').html(data.basketTotal);
                $('.basket-counter').html(data.basketQuantity);

                setDeferredImages();
            }


            // IMPORTANT:
            // Do NOT add this:
            //
            // renderPaypalButtonV2();
            //
            // PayPal should remain as the existing button instance.
        },

        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError(
                "/Product/BasketUpdateQty/",
                xhr,
                textStatus,
                thrownError
            );
        }
    });
}

// Sets a basket line to an absolute quantity - used ONLY by the mini-cart's own qty
// stepper buttons (MiniBasket.cshtml: onclick="changeMiniBasketQty(...)"). Kept fully
// independent from changeBasketQty above (which the full basket page uses) so a mini-cart
// fix can never change basket-detail-page behaviour, and vice versa. No isCheckout branch
// is needed here because this function is never wired up to anything on that page.
function changeMiniBasketQty(productref, qty) {

    $.ajax({
        url: "/Product/BasketUpdateQty/",
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,

        data: {
            productref: productref,
            qty: qty
        },

        async: false,

        success: function (data) {

            if (!data.savereturn.IsSuccess) {
                launchPopup('IsInCheckout', 'popup');
                return false;
            }


            // Remember whether mini-cart was already open
            var wasOpen = $('#miniCartOverlay').hasClass('is-open');


            // Refresh only mini-basket HTML
            $('#minibasket-widget').replaceWith(data.basketSummary);


            // Update counts
            $('.basketQuantity').html(data.basketQuantity);
            $('.basketTotal').html(data.basketTotal);
            $('.basket-counter').html(data.basketQuantity);


            setDeferredImages();


            // Keep mini-cart open
            if (wasOpen) {

                $('#miniCartOverlay').addClass('is-open');

                $('body').css('overflow', 'hidden');
            }


            // IMPORTANT:
            // DO NOT CALL:
            //
            // renderPaypalButtonV2();
            //
            // This was causing PayPal to refresh every time
            // quantity was increased/decreased.
        },

        error: function (xhr, textStatus, thrownError) {

            logAjaxScriptError(
                "/Product/BasketUpdateQty/",
                xhr,
                textStatus,
                thrownError
            );
        }
    });
}

// Shared "You May Also Need" lookup - asks the server whether the current basket has any
// eligible add-on products (in stock, not already in the basket). Calls onEligible(html) if
// so, otherwise onNotEligible() (both optional). Used both by the mini-cart's Proceed to
// Checkout click and by every "Add to Basket" click site-wide (see .atb-add handler below).
function requestAddSellPopup(onEligible, onNotEligible) {
    $.ajax({
        url: "/Checkout/GetAddSellPopup/",
        dataType: 'json',
        type: 'POST',
        cache: false,
        // Was async: false (synchronous XHR). This call sits directly in the "tap Add to
        // Basket -> mini-cart/You May Also Need popup opens" chain on mobile (it's fired from
        // maybeShowAddSellPopupAfterAdd(), itself called from the .atb-add success handler,
        // right after that handler's own BasketAdd call - which had the same problem and was
        // already fixed). Two back-to-back main-thread-blocking synchronous XHRs in the same
        // click handler is exactly the kind of pattern iOS/WebKit's synchronous-XHR
        // restrictions can silently break: the backdrop/overlay markup gets appended to the
        // DOM, but the browser doesn't get a chance to reliably paint the popup panel that's
        // supposed to sit on top of it - matching the "black screen, nothing else visible"
        // report. onEligible/onNotEligible are already invoked from inside success/error below,
        // so removing async:false needs no caller changes.
        success: function (data) {
            if (data && data.hasAddSell) {
                if (onEligible) { onEligible(data.html); }
            } else if (onNotEligible) {
                onNotEligible();
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Checkout/GetAddSellPopup/", xhr, textStatus, thrownError);
            if (onNotEligible) { onNotEligible(); }
        }
    });
}

// Removes the popup and its transparent click-catcher backdrop together - the two always come
// and go as a pair, so every removal of one should also remove the other.
function removeAddSellPopup() {
    $('#you-may-also-need').remove();
    $('#you-may-also-need-backdrop').remove();
}

// Inserts the popup markup and tags it with where it came from - 'checkout' (closing/X/backdrop
// should still take the customer through to /checkout/, since that's what they were trying to
// do) vs 'addtocart' (closing should just dismiss it and leave them where they are).
// suppressMiniCart (optional): the customer is already looking at their basket - e.g. adding
// from the "You May Also Need" region inline on the basket page itself - so forcing the
// mini-cart flyout open on top of the basket page they're already viewing is redundant. See the
// .atb-add/.add-btn handlers below, which set this when the click originated from that region.
function showAddSellPopup(html, context, suppressMiniCart) {
    removeAddSellPopup();
    $('body').append(html);
    $('#you-may-also-need').attr('data-context', context);
    setDeferredImages();

    if (context === 'addtocart' && !suppressMiniCart) {
        // The popup is offering add-ons for what was just added - show the mini-cart open
        // behind/underneath it (even if it wasn't already open), on every device, so that:
        // add-on popup along with mini-cart should open together (the explicit requirement
        // this whole mini-cart/add-to-basket flow was built against from the start) is actually
        // true, and so that dismissing the popup lands the customer on an already-open mini-cart
        // instead of the bare page. The 'checkout' context (proceedToCheckout) doesn't need this
        // - that popup is only ever shown from inside an already-open mini-cart.
        //
        // This used to explicitly REMOVE is-open below 768px instead (mini-cart and popup were
        // mutually exclusive on mobile), on the theory that stacking the mini-cart's own dim
        // backdrop (.mini-cart-overlay, ~40% black) with this popup's own near-opaque backdrop
        // (#you-may-also-need-backdrop, #00000091, much higher z-index) was the cause of an
        // earlier "add to basket shows a black screen" report. That turned out to be wrong: the
        // actual cause was a completely different, unconditional legacy element
        // (.mobileBasketBackdrop in global.less, since disabled) that fired on every add-to-
        // basket regardless of add-ons or overlay stacking - see the mini-basket-progress doc's
        // "black screen culprit" pass. With that real cause fixed, there's no remaining reason to
        // keep the mini-cart and this popup mutually exclusive on mobile, and doing so only
        // fought the original requirement. The popup's own backdrop still visually sits on top
        // while it's showing (by design, via z-index) - this only changes the mini-cart's actual
        // open/closed *state* underneath, which is what matters once the popup is dismissed.
        $('#miniCartOverlay').addClass('is-open');
        $('body').css('overflow', 'hidden');
    }
}

// Header.cshtml always renders the site's nav-collapse wrapper with either "authenticated" or
// "not-authenticated" on it (see .navbar-collapse in Header.cshtml) - on every page, mobile and
// desktop alike. Reuse that existing marker rather than adding a new one.
function isNotAuthenticated() {
    return $('.navbar-collapse').hasClass('not-authenticated');
}

// FIX (2026-08-24, later same day): this used to check isNotAuthenticated() FIRST and, for a
// signed-out customer, skip the add-on eligibility check entirely and jump straight to the
// login popup - on the reasoning that basket's own Checkout button does the same (never shows
// an add-on popup for anyone). Reported bug: a guest with an add-on-eligible product in their
// basket clicked the mini-cart's Proceed to Checkout and got the login popup with no add-on
// popup at all - confirmed this was that exact behaviour firing as designed, not a detection
// bug (verified Header.cshtml only ever renders a single, correctly-set
// ".navbar-collapse ... not-authenticated" element - isNotAuthenticated() itself was reading
// it correctly). Per explicit confirmation this was tested as a guest, the intended behaviour
// is now: check for eligible add-ons regardless of sign-in state, same as an authenticated
// customer - a guest should see the same "You May Also Need" popup before being asked to log
// in, not skip straight past it. Login is deferred to whichever exit point is actually taken
// (see checkoutUrl() below), instead of short-circuited here before the add-on check ever runs.
function checkoutUrl() {
    // Guests need '?showlogin=1' appended so ViewBasket.cshtml runs its own Checkout button
    // action automatically on load (in-checkout guard -> submit the basket form if
    // authenticated, else show '#ident-modal', the "Secure Checkout" login popup) - that modal
    // only exists in the DOM on the checkout page itself, so it can't be shown from here
    // directly. Signed-in customers get the plain URL; the page's own guard already handles
    // them with no extra param needed.
    return isNotAuthenticated() ? '/checkout/?showlogin=1' : '/checkout/';
}

// Mini-cart "Proceed to Checkout" click. Checks for eligible "You May Also Need" add-on
// products first - regardless of sign-in state - and only then goes to /checkout/ (if there
// are none, or the popup has already been shown/skipped this visit), appending '?showlogin=1'
// via checkoutUrl() when the customer isn't signed in.
function proceedToCheckout() {
    if ($('#you-may-also-need').length) {
        // Popup already open/shown - a second click on Proceed skips straight through. Only
        // reachable on mobile now, since desktop never opens this popup from here at all.
        removeAddSellPopup();
        window.location.href = checkoutUrl();
        return;
    }

    // Desktop: no add-on popup on Proceed to Checkout - straight to checkout. 992 to match this
    // popup's own mobile/desktop CSS breakpoint (YouMayAlsoNeed.cshtml/style.css switch
    // #you-may-also-need's card-list mobile view in and out at "@media (max-width: 991px)") -
    // the same breakpoint maybeShowAddSellPopupAfterAdd() below already uses for the equivalent
    // add-to-basket-triggered popup, so both entry points now agree on desktop vs. mobile.
    if ($(window).width() >= 992) {
        window.location.href = checkoutUrl();
        return;
    }

    requestAddSellPopup(
        function (html) {
            showAddSellPopup(html, 'checkout');

            // Mobile's mini-cart tray isn't shown alongside the popup - close it. Unconditional
            // now (this code path is only ever reached below the 992px gate above).
            $('[data-toggle="offcanvas-close"]').trigger('click');
        },
        function () {
            window.location.href = checkoutUrl();
        }
    );
}


// Called after any successful "Add to Basket" click, from anywhere on the site (product page,
// category listing, the inline "You May Also Need" section on the basket page, etc.) - if the
// item(s) now in the basket have eligible add-ons, surface the same popup right away instead of
// only waiting for the mini-cart's Proceed to Checkout click.
// Always re-requests the popup, even if one is already showing - it used to bail out early
// whenever #you-may-also-need already existed ("don't stack a second copy"), but that also
// skipped re-checking eligibility for the item that was JUST added. Reported bug: add a product
// with eligible add-ons (popup opens), then add a second product with none of its own - the
// stale popup from the first product stayed open, showing add-ons that have nothing to do with
// what's now in the basket, because this function returned before ever asking the server again.
// requestAddSellPopup's own showAddSellPopup() already removes any existing popup before
// appending a fresh one, so re-requesting can't "stack" a second copy - and the onNotEligible
// callback here removes a stale popup outright when the current basket no longer qualifies.
function maybeShowAddSellPopupAfterAdd(suppressMiniCart) {
    // Mobile design: add-ons should only ever surface when the customer taps the mini-cart's
    // own "Proceed to Checkout" button (proceedToCheckout(), above - which already has its own
    // correct handling), never immediately after a plain Add to Basket. Below the breakpoint,
    // skip the popup entirely here. This also fixes a second symptom of the same bug: this
    // partial's CSS gives #you-may-also-need[data-context="addtocart"] a higher-specificity,
    // centered-floating-modal position/size rule that beats the plain mobile full-screen rule
    // (which is what makes the .ymn-proceed "Proceed to Checkout" footer button clearly
    // visible/usable) - so calling this on mobile was also rendering a cramped desktop-style
    // popup instead of the intended full-screen mobile layout. Skipping the call here means
    // mobile only ever reaches this popup via the 'checkout' context, so that mismatch can no
    // longer happen either.
    //
    // 992, not 768: matches the breakpoint YouMayAlsoNeed.cshtml/style.css actually use to swap
    // #you-may-also-need between its desktop carousel and mobile card-list views
    // ("@media (max-width: 991px)"), not the older 767px breakpoint this check was written
    // against before that CSS was widened to include tablet widths.
    if ($(window).width() < 992) {
        return;
    }

    requestAddSellPopup(function (html) {
        showAddSellPopup(html, 'addtocart', suppressMiniCart);
    }, function () {
        removeAddSellPopup();
    });
}

// Split out of startTime() below: does the actual calculation + DOM update for the "Order
// Within" countdown, with no side effect of scheduling another tick. startTime() (the original,
// still-recursive loop kicked off once on page load) calls this and then reschedules itself -
// unchanged. This standalone version exists so refreshVbFields() can force an immediate
// re-populate of a freshly re-rendered (and therefore blank) .cutoffCountdownFalse element right
// after a basket refresh, without also spinning up a second, parallel setTimeout loop alongside
// the one already running from page load - calling startTime() itself there instead would do
// exactly that, since it unconditionally reschedules itself every time it's invoked.
function updateCutoffCountdown() {
    //var currTime = new Array();
    var cutOffTime = new Array();
    var countDown = new Array();
    var today = new Date();

    // The following is used for Testing
    //today = new Date(2017, 06, 07, 11, 0, 0, 0);    // 11 O'Clock on a Friday
    //today = new Date(2017, 06, 07, 21, 0, 0, 0);    // 21 O'Clock on a Friday
    //today = new Date(2017, 06, 08, 11, 0, 0, 0);    // 11 O'Clock on a Saturday
    //today = new Date(2017, 06, 09, 11, 0, 0, 0);    // 11 O'Clock on a Sunday

    var txtHour;
    var txtMinute;
    var dCutOffDate;
    //var dDeliveryDate;

    var currTime = parseInt(today.getHours() +
        "" +
        ("0" + today.getMinutes()).substr(-2) +
        "" +
        ("0" + today.getSeconds()).substr(-2));
    var dayNumber = today.getDay();
    dCutOffDate = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 17, 30, 0, 0);
    if (currTime < 173000) {
        switch (dayNumber) {
            case 0:
                dCutOffDate.setDate(today.getDate() + 1);
                break;
            case 6:
                dCutOffDate.setDate(today.getDate() + 2);
                break;
            default:
                break;
        }
    } else {
        switch (dayNumber) {
            case 5:
                dCutOffDate.setDate(today.getDate() + 3);
                break;
            case 6:
                dCutOffDate.setDate(today.getDate() + 2);
                break;
            default:
                dCutOffDate.setDate(today.getDate() + 1);
                break;
        }
    }

    if (today.getHours() === 17 && today.getMinutes() === 30 && today.getSeconds() === 0) {
        switch (dayNumber) {
            case 0:
                break;
            case 4:
                dCutOffDate.setTime(dCutOffDate.getTime() + (24 * 60 * 60 * 1000));
                break;
            case 5:
                dCutOffDate.setTime(dCutOffDate.getTime() + (72 * 60 * 60 * 1000));
                break;
            case 6:
                break;
            default:
                dCutOffDate.setTime(dCutOffDate.getTime() + (24 * 60 * 60 * 1000));
        }
    }

    countDown[0] = Math.floor(((dCutOffDate - today) / 1000) / 3600);
    countDown[1] = Math.floor((((dCutOffDate - today) / 1000) - (countDown[0] * 3600)) / 60);
    countDown[2] = Math.floor(((dCutOffDate - today) / 1000) - (countDown[0] * 3600) - (countDown[1] * 60));

    if (dCutOffDate < today) {
        countDown[0] = 0;
        countDown[1] = 0;
        countDown[2] = 0;
    }

    if (countDown[0] === 1) {
        txtHour = " hour ";
    } else {
        txtHour = " hours ";
    }
    if (countDown[1] === 1) {
        txtMinute = " min ";
    } else {
        txtMinute = " mins ";
    }
    $('.cutoffCountdownFalse').html(countDown[0] + txtHour + ' ' + countDown[1] + txtMinute);
}

function startTime() {
    updateCutoffCountdown();
    setTimeout(function () { startTime(); }, 500);
}

function checkTime(i) {
    if (i < 10) {
        i = "0" + i;
    }
    return i;
}

function logAjaxFormError(xhr, textStatus, thrownError) {
    logAjaxScriptError(location.href, xhr, textStatus, thrownError);
}

function logAjaxScriptError(url, xhr, textStatus, thrownError) {
    // BUG FIX: this used to read "if (xhr || xhr.responseText || xhr.responseText) { return; }" -
    // that condition is true almost every time an ajax error callback fires (xhr is basically
    // always a truthy object), so this function returned immediately and NEVER logged anything,
    // for every failed ajax call across the whole site (28+ call sites use this same helper).
    // That means any real failure of e.g. /Product/BasketAdd/ (a bad session, a server error, a
    // network hiccup on a particular device) has been failing completely silently - no console
    // output, no server-side log entry, nothing visible to the user or to us - which matches
    // "tap Add to Basket, nothing happens at all" exactly if the request itself is failing.
    // Fixed to actually build and log the error, and to console.error it too so it's visible
    // immediately in on-device dev tools (e.g. Safari Web Inspector) without needing server logs.
    var responseText = "";

    if (xhr && xhr.responseText) {
        responseText = xhr.responseText.length > 5000
            ? xhr.responseText.substring(0, 5000)
            : xhr.responseText;
    }

    var message = url + ": " + (xhr && xhr.statusText ? xhr.statusText.toString() : "(no xhr)") +
        ", thrownError: " +
        (thrownError ? thrownError.toString() : "") +
        ", textStatus: " +
        (textStatus ? textStatus.toString() : "") +
        ", responseText: " +
        responseText;

    if (window.console && console.error) {
        console.error("[ajax error] " + message);
    }

    logScriptError(new Error(message));
}

function logScriptError(error) {
    $.ajax({
        url: '/Error/ScriptError/',
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        data: {
            url: location.href,
            description: error.message
        },
        // Was async: false. This now only runs when there's already been a failure (it's the
        // error-reporting path itself, freshly fixed above to actually get called at all) -
        // no reason to also block the main thread with a synchronous request here, especially
        // on iOS. This is fire-and-forget logging; nothing depends on its response.
        success: function (data) {

        }
    });
}

function triggerFilter(triggered) {
    var visibleEntries;

    if (isCurrentPage('/products/')) {
        $('.pg-products > .pg-entry').addClass("g-d-n");
        applyFilter();
        applyPriceFilter('.pg-products > .pg-entry');
        $('#pg-product-count').html($('.pg-products > .pg-entry:visible').length);
        visibleEntries = $('.pg-entry:visible');
    }
    if (isCurrentPage('/model/') || isCurrentPage('/search-results')) {
        $('.pl-products > div > .pl-entry').addClass("g-d-n");
        applyFilter();
        $('.pl-products > div > .pl-sub-banner').addClass("g-d-n");
        applyPriceFilter('.pl-products > div > .pl-entry');
        $('#pl-product-count').html($('.pl-products > div > .pl-entry:visible').length);
        visibleEntries = $('.pl-entry:visible');
    }

    if (triggered && visibleEntries !== null) {
        //console.log(visibleEntries.length);
        $(visibleEntries).find('img.lazy').lazyload();
    }

    refreshFilterCounts();
}

function triggerAltFilter(filter) {
    var visibleEntries;

    if (isCurrentPage('/model/') || isCurrentPage('/search-results')) {
        $('.pl-products > div > .pl-entry').addClass("g-d-n");
        $('.pl-products > div > .pl-sub-banner').addClass("g-d-n");
        applyAltFilter(filter);
        $('#pl-product-count').html($('.pl-products > div > .pl-entry:visible').length);
        visibleEntries = $('.pl-entry:visible');
    }

    if (triggered && visibleEntries !== null) {
        //console.log(visibleEntries.length);
        $(visibleEntries).find('img.lazy').lazyload();
    }
}

function refreshFilterCounts() {
    $('.prd-filters .checkbox').each(function () {
        var attArr = $(this).find('input:first').prop('id').split('-');
        var dataAtt = 'data-att-' + attArr[1];
        var dataAttVal = attArr[2];
        if (isCurrentPage('/products/')) {
            $(this).find('label > span').html('(' + $('.pg-entry[' + dataAtt + '="#' + dataAttVal + '#"]').not('.g-d-n').length + ')');
        }
        if (isCurrentPage('/model/') || isCurrentPage('/search-results')) {
            $(this).find('label > span').html('(' + $('.pl-entry[' + dataAtt + '="#' + dataAttVal + '#"]').not('.g-d-n').length + ')');
        }
    });
}

function backButtonUsed() {
    var ret = false;
    if (window.history && window.history.pushState) {

        $(window).on('popstate', function () {
            var hashLocation = location.hash;
            var hashSplit = hashLocation.split("#!/");
            var hashName = hashSplit[1];

            if (hashName !== '') {
                var hash = window.location.hash;
                if (hash === '') {
                    ret = true;
                }
            }
        });

        return ret;
    }
}

function renderPaypalButtonV2() {

    // Get the payment amount from the server
    var amt
    $.ajax({
        url: '/Checkout/PayPalGetAmount',
        method: 'POST',
        dataType: 'json',
        async: false,
        cache: false,
        data: {
            __RequestVerificationToken: $('[name="__RequestVerificationToken"]').val()
        },
        success: function (data) {
            if (data.IsSuccess) {
                amt = data.Html;
            } else {
                rpl = 'errormessage_' + encodeURI(data.Message);
                location.href = "/checkout/?pm=CheckoutError&sz=md&rpl=" + rpl;
            }
        },
        error: function (xhr, textStatus, thrownError) {
            setSession("C_IsInCheckout", null);
            logAjaxScriptError("/Checkout/ViewBasket", xhr, textStatus, thrownError);
        }
    });

    // #paypal-button2 now renders once in ViewBasket.cshtml and persists across basket
    // refreshes (it used to live inside the AJAX-replaced BasketDetails.cshtml partial, so a
    // fresh empty container was guaranteed every time this ran). Since this function can still
    // be called more than once per page view (add/remove/voucher etc. still call it), empty the
    // container first so paypal.Buttons().render() can't stack a second button on top of one
    // that's already there.
    $('#paypal-button2').empty();

    // FIX: because these containers persist outside #vbBasketDetails and this function has no
    // basket-empty check, removing the last item left a live, clickable PayPal button sitting on
    // top of an empty basket (the container itself is only ever added/removed by the server-side
    // GrandTotalIncVat > 0.01m check in ViewBasket.cshtml, which only runs on a real page load,
    // never on this AJAX refresh path). PayPalGetAmount already returns "0" for an empty basket
    // (Basket.GetBasketTotal on an empty B_BasketArray) - bail out here before rendering anything
    // rather than creating a PayPal button for a zero-value order.
    if (!amt || parseFloat(amt) <= 0) {
        return;
    }

    var paypalObject = buildPayPalObject(amt);

    paypalObject.fundingSource = paypal.FUNDING.PAYPAL;
    paypal.Buttons(paypalObject).render('#paypal-button2');
    // FIX: was also rendering a second, separate "Pay Later" branded button into #paypal-button3
    // (fundingSource: paypal.FUNDING.PAYLATER) - reported as appearing at the bottom of the
    // basket page and shouldn't be there. Removed; #paypal-button3's container (ViewBasket.cshtml)
    // and the SDK's enable-funding=paylater flag were removed to match.

    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if ($(mutation.removedNodes[0]).hasClass('paypal-checkout-sandbox') && $('.IsInCheckout').val() !== 'true') {
                setSession("C_IsInCheckout", null);
            }
        });
    });

    var config = {
        attributes: true,
        childList: true,
        characterData: true
    };

    observer.observe(document.body, config);
}

function buildPayPalObject(amt) {
    var paypalObject = {
        fundingSource: paypal.FUNDING.PAYPAL,
        // FIX: style.label was 'checkout', which renders PayPal's SDK-generated "Checkout with
        // PayPal" (or similar) text next to/instead of the plain logo. 'paypal' renders just the
        // PayPal logo/wordmark with no extra text - the other valid SDK values ('buynow', 'pay',
        // 'installment') all add their own different wording, so this is the only label option
        // that shows no additional text at all.
        style: {
            layout: 'vertical'
            , shape: 'rect'
            , label: 'paypal'
            , color: 'gold'
        },
        createOrder: function (data, actions) {
            if (!isCurrentPage('stage1')) {
                // Was "amount: { value: amt }" - amt is whatever renderPaypalButtonV2() fetched
                // at the moment this button was last rendered, closed over here. Now that the
                // button only renders once per page view (it used to be rebuilt on every basket
                // change, which kept amt current as an incidental side effect - see
                // ViewBasket.cshtml's comment on why it no longer does), a quantity/voucher
                // change after render would leave amt stale and the PayPal order would be
                // created for the wrong total. Fetch the current amount fresh right here, at the
                // moment the customer actually clicks the button, instead of trusting whatever
                // was captured at render time - this is correct regardless of how long ago the
                // button was rendered or how many times the basket has changed since.
                return $.ajax({
                    url: '/Checkout/PayPalGetAmount',
                    method: 'POST',
                    dataType: 'json',
                    cache: false,
                    data: {
                        __RequestVerificationToken: $('[name="__RequestVerificationToken"]').val()
                    }
                }).then(function (result) {
                    if (!result.IsSuccess) {
                        var rpl = 'errormessage_' + encodeURI(result.Message);
                        location.href = "/checkout/?pm=CheckoutError&sz=md&rpl=" + rpl;
                        return $.Deferred().reject().promise();
                    }
                    return actions.order.create({
                        intent: "AUTHORIZE",
                        purchase_units: [{
                            amount: {
                                value: result.Html
                            }
                        }],
                        payment_initiator: 'CUSTOMER'
                    });
                });
            }
            if (isCurrentPage('stage1')) {
                var isValid = ppIsValid()

                if (isValid == 'invalid') {
                    return actions.reject()
                }

                return fetch('/checkout/PayPalCreateOrderWithAddress', {
                    method: 'post',
                    body: isValid
                }).then(function (res) {
                    return res.json();
                }).then(function (orderData) {
                    return orderData.id;
                });
            }
        },

        onShippingChange: function (data, actions) {
            if (data.shipping_address.country_code !== "GB") {
                return actions.reject();
            }

            //stop NI postcodes
            if (data.shipping_address.postal_code.startsWith("BT")) {
                return actions.reject();
            }

            var selected = ""
            if (data.selected_shipping_option == null) {
                selected = "null"
            } else {
                selected = data.selected_shipping_option.id
            }

            var url = "/Checkout/PayPalDeliveryOptions?PostCode=" + data.shipping_address.postal_code
            url += "&PayPalID=" + data.orderID
            url += "&SelectedID=" + selected

            return fetch(url, {
                method: "GET"
            })
                .then(response => {
                    if (response.status != 200) {
                        setSession("C_IsInCheckout", null);

                        $("[id^=paypal-overlay-uid]").remove();

                        $.confirm({
                            title: 'PayPal Error',
                            content: 'An error was encountered',
                            buttons: {
                                OK: function () {
                                }
                            }
                        });
                    }
                })
        },

        onApprove: function (data, actions) {
            var myUrl = '/Checkout/PayPalCapture';
            if (isCurrentPage('stage1')) {
                myUrl = '/Checkout/PayPalCaptureStage1'
            }

            $('#paypal-button2').append('<div class="search-backdrop"><i class="fa fa-5x fa-circle-o-notch fa-spin g-ps-a g-m-a-0" style="bottom:50%; left:50%; margin-right:-50%; transform:translate(0, -50%);color:#FFFFFF"></i></div>')
            sessionStorage.costatus = "Started";
            $('.IsInCheckout').val('true');
            return actions.order.authorize().then(function (details) {
                var detailsJson = JSON.stringify(details);
                $.ajax({
                    url: myUrl,
                    method: 'POST',
                    dataType: 'json',
                    async: false,
                    cache: false,
                    data: {
                        __RequestVerificationToken: $('[name="__RequestVerificationToken"]').val(),
                        details: detailsJson,
                        paypaltype: 'viewBasket'
                    },
                    success: function (data) {
                        setSession("C_IsInCheckout", null);
                        if (data.IsSuccess) {
                            $("#paypal-form").submit();
                        } else {
                            rpl = 'errormessage_' + encodeURI(data.Message);
                            location.href = "/checkout/?pm=CheckoutError&sz=md&rpl=" + rpl;
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        setSession("C_IsInCheckout", null);
                        logAjaxScriptError("/Checkout/ViewBasket", xhr, textStatus, thrownError);
                    }
                });
            });
        },
        onCancel: function (data) {
            setSession("C_IsInCheckout", null);
            $('.search-backdrop').remove();
            $.confirm({
                title: 'PayPal Cancelled',
                content: 'You have cancelled the paypal operation',
                buttons: {
                    OK: function () {
                    }
                }
            });
        },
        onError: function (err) {
            setSession("C_IsInCheckout", null);
            $('.search-backdrop').remove();
            logAjaxScriptError("/Checkout/ViewBasket", xhr, textStatus, thrownError);
            $.confirm({
                title: 'PayPal Error',
                content: 'An error was encountered',
                buttons: {
                    OK: function () {
                    }
                }
            });
        }
    };
    return paypalObject;
}

function checkSessionExists(name) {
    var result;

    $.ajax({
        url: '/Misc/SessionExists',
        method: 'POST',
        dataType: 'json',
        async: false,
        cache: false,
        data: {
            name: name
        },
        success: function (e) {
            result = e.exists;
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/SessionExists", xhr, textStatus, thrownError);
        }
    });

    return result;
}

function setSession(name, value) {
    $.ajax({
        url: '/Misc/SetSession',
        method: 'POST',
        dataType: 'json',
        async: false,
        cache: false,
        data: {
            name: name,
            value: value
        },
        success: function (e) {

        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/SetSession", xhr, textStatus, thrownError);
        }
    });
    return null;
}

function displayNone_ChildrenAboveNumItems(thisObj, numItems) {
    thisObj.children().each(function (index) {
        if (index >= numItems) {
            $(this).addClass('g-d-n');

            // zg-d-t is a fake class, with no effect. g-d-t would intefere with g-d-n, causing the element to still be visible
            if ($(this).hasClass('g-d-t'))
                $(this).removeClass('g-d-t').addClass('zg-d-t');
        }
    });
}

function unDisplayNone_Children(thisObj) {
    thisObj.children().each(function () {
        if ($(this).hasClass('g-d-n')) {
            // toggle and detail container-fluids are siblings, not parent-child.
            // dont' want to expand every single detail container-fluid on view more
            if (!($(this).hasClass('details') && $(this).hasClass('container-fluid')))
                $(this).removeClass('g-d-n');

            // zg-d-t is a fake class, with no effect. Restore to g-d-t as appropriate, where adding g-d-n had replaced this
            if ($(this).hasClass('zg-d-t'))
                $(this).removeClass('zg-d-t').addClass('g-d-t');
        }
    });
}

function closeAnyChildContainerFluidDetails(thisObj) {
    // will only be 0 or 1 open items. Click close button if appropriate.
    var toClose = thisObj.find('.fa-chevron-up');

    toClose.each(function () {
        var closeButton = $(this).parent();
        if (closeButton != undefined)
            closeButton.trigger('click');
    });

    // above click causes a slideup - an animation within an animation
    // if there is 1 open details item, hide it immediately.
    // Initial Hidden = g-d-n
    // Showing = g-d-n + display:block
    // Rehidden = g-d-n + display:none
    thisObj.children('.container-fluid').each(function () {
        if ($(this).hasClass('details') && $(this).css('display') === 'block') {
            $(this).css('display', 'none');
        }
    });
}

function animateToExpandedMode(thisObj, nextElement) {
    closeAnyChildContainerFluidDetails(thisObj);

    var collapsedHeight = thisObj.height();

    // called to work out expandedHeight for animation
    unDisplayNone_Children(thisObj);

    // size will react automatically to elements adding or removing class 'g-d-n'
    // line should be placed here for repeated collapse/expand to work
    thisObj.css('height', 'auto');

    var expandedHeight = thisObj.height();

    thisObj.height(collapsedHeight).animate({ height: expandedHeight },
        2000, // "slow" = 600 ms
        function () {
            if (nextElement.is('button')) {
                nextElement.text("View Less");
            }

            // 'height', 'auto' is used to avoid problem with only partial details 
            // expansion on order history, whe user clicks Open
            thisObj.css('height', 'auto');
        }
    );
}

function animateToCollapsedMode(thisObj, nextElement, numItems, dataScrollOffset) {
    closeAnyChildContainerFluidDetails(thisObj);

    var expandedHeight = thisObj.height();

    // called 1st time to work out collapsedHeight for animation
    displayNone_ChildrenAboveNumItems(thisObj, numItems);

    // size will react automatically to elements adding or removing class 'g-d-n'
    // line should be placed here for repeated collapse/expand to work
    thisObj.css('height', 'auto');

    var collapsedHeight = thisObj.height();

    // repopulate non-collapsed items during hidden, or during animation from expanded to collapsed, they will appear as white space
    unDisplayNone_Children(thisObj);

    scrollToSelector(thisObj, dataScrollOffset); // move to top of now collapsed list, or would stay beyond bottom!

    thisObj.height(expandedHeight).animate({ height: collapsedHeight },
        2000, // "slow" = 600 ms
        function () {
            if (nextElement.is('button')) {
                nextElement.text("View More");
            }

            // called 2nd time after animation so that 'height', 'auto' has correct height
            displayNone_ChildrenAboveNumItems(thisObj, numItems);

            // 'height', 'auto' is used to avoid problem with only partial details 
            // expansion on order history, whe user clicks Open
            thisObj.css('height', 'auto');
        }
    );
}

function toggleCollapsedMode(thisObj, numItems, buttonMode, dataScrollOffset, onClick) {
    // thisObj is the div of moreLess

    if (thisObj.children().length <= numItems) // immediate children
        return;

    if (thisObj.height <= 0) // more than 1 entry produced by $('.moreLess').each
        return;

    if (!onClick)
        $('<button class="g-butt-second-reg ' + buttonMode + '" type="button">View More</button>').insertAfter(thisObj);

    var nextElement = $(thisObj).next();

    thisObj.toggleClass("collapsed"); // moreLess starts with no collapsed on initial layout

    if (thisObj.hasClass("collapsed")) {
        if (onClick) {
            // collapsing when clicking View Less - animate
            animateToCollapsedMode(thisObj, nextElement, numItems, dataScrollOffset, 80);
        }
        else {
            // collapsing on initial view layout - don't animate
            displayNone_ChildrenAboveNumItems(thisObj, numItems);
        }
    }
    else {
        // expanding - can only be from user click - animate
        animateToExpandedMode(thisObj, nextElement);
    }
}

function displayErrorMessage(classes, message) {
    $(classes + ' .error-message > p').text(message);
    $(classes + ' .error-message').show();
}

function removeErrorMessage(classes) {

    var cl = classes.split(", ");//.map((item) => item.trim());

    for (var i = 0; i < cl.length; i++) {
        $(cl[i] + ' .error-message > p').empty();
        $(cl[i] + ' .error-message').hide();
    }
}

function inIframe() {
    try {
        return window.self !== window.top;
    } catch (e) {
        return true;
    }
}

function closeWizDropDown(e) {
    $('#' + this.element[0].id + '_listbox').parent().scrollTop(0);
}

function isWebPSupported() {
    var elem = document.createElement('canvas');

    // The double negative in the following statement is intended
    if (!!(elem.getContext && elem.getContext('2d'))) {
        // was able or not to get WebP representation
        return elem.toDataURL('image/webp').indexOf('data:image/webp') === 0;
    }

    // very old browser like IE 8, canvas not supported
    return false;
}

function loadPca(n, t, i, r) {
    var u, f; n[i] = n[i] || {}, n[i].initial = { accountCode: "NETGI11112", host: "NETGI11112.pcapredict.com" }, n[i].on = n[i].on || function () { (n[i].onq = n[i].onq || []).push(arguments) }, u = t.createElement("script"), u.async = !0, u.src = r, f = t.getElementsByTagName("script")[0], f.parentNode.insertBefore(u, f)
}

//#endregion

$(window).on('load', function () {
    setDeferredImages();
});

//#region Immediate

$(function () {

    // Add to basket
    $(document).on('click',
        '.atb-add',
        function () {
            try {
                var ref = $(this).attr('data-productid');
                var itemtype = '1';
                if (typeof $(this).attr('data-itemtype') !== 'undefined') {
                    itemtype = $(this).attr('data-itemtype');
                }
                var price = 0;
                if (itemtype === '2') {  //= Admin Discount
                    if (typeof $(this).attr('data-price') !== 'undefined') {
                        price = $(this).attr('data-price');
                    }
                }
                var thisbutton = $(this);
                var thisentry = $(this).closest('.atb-entry');
                var qty = '1';
                if (thisentry.find('input.atb-qty').length) {
                    qty = thisentry.find('input.atb-qty:first').val();
                }
                // The customer is already on the basket page looking at their basket when this
                // button lives inside the inline "You May Also Need" add-on carousel there
                // (.you-may-need, BasketDetails.cshtml) - forcing the mini-cart flyout open on
                // top of the page they're already viewing is redundant, so that step is skipped
                // below for this specific origin only. Every other .atb-add button site-wide
                // (product pages, category listings, etc.) is untouched.
                // Same reasoning for #portalCsTools - the portal/trade "Customer Service Tools"
                // panel on the basket page (BasketDetails.cshtml), whose "Apply Discount" and
                // "Place Order On Hold" buttons both go through this same .atb-add handler
                // (#discount-atb is triggered programmatically by #apply-discount's own click
                // handler in checkout.js - $(this) here is still #discount-atb itself, so
                // .closest() sees it's inside the panel exactly as if it had been clicked
                // directly). A portal user using these is, by definition, already on the basket
                // page.
                var fromAddonRegion = $(this).closest('.you-may-need').length > 0 ||
                    $(this).closest('#portalCsTools').length > 0;

                $.ajax({
                    url: "/Product/BasketAdd/",
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        productref: ref,
                        productprice: price,
                        productqty: qty,
                        //isadmindiscount: admindiscount,
                        itemtype: itemtype
                    },
                    // Was async: false (synchronous XHR) - iOS/WebKit has progressively
                    // restricted and deprecated synchronous XHR on the main thread, which
                    // is a well-documented cause of "add to basket AJAX succeeds but the
                    // success callback's follow-up UI work (opening the mini-cart) never
                    // visibly runs" on iOS Safari specifically. The response is already
                    // handled entirely in the success callback below, so switching to the
                    // default async request changes nothing about call order - just removes
                    // the main-thread-blocking, iOS-fragile synchronous mode.
                    success: function (data) {
                        changeBasketComplete(data, thisbutton);
                        refreshViewBasket();
                        maybeShowAddSellPopupAfterAdd(fromAddonRegion);
                        // Was a bare openCart() call - that's a plain global function declared
                        // inside MiniBasket.cshtml's own inline <script>, re-defined every time
                        // that partial's markup is replaced via changeBasketComplete()'s
                        // $('#minibasket-widget').replaceWith(...) a few lines above. QA hit
                        // "Can't find variable: openCart" on a real iOS device - an uncaught
                        // ReferenceError thrown inside this async success callback, which the
                        // .atb-add handler's own try/catch does NOT cover (that only wraps the
                        // synchronous code that kicks off the $.ajax call, not this callback),
                        // so it silently aborted right here with zero visible feedback. Whatever
                        // the exact reason openCart wasn't defined at that moment (this file's
                        // one and only call site for it), the fix is to stop depending on that
                        // fragile global entirely: do the same two things openCart() does,
                        // directly, the same way changeBasketComplete()'s own re-open branch
                        // (a few lines above) already does it without calling openCart() either.
                        // Skipped entirely when fromAddonRegion is true - see the comment where
                        // that's set above.
                        if (!fromAddonRegion) {
                            var $miniCartOverlay = $('#miniCartOverlay');
                            if ($miniCartOverlay.length) {
                                $miniCartOverlay.addClass('is-open');
                                $('body').css('overflow', 'hidden');
                            } else if (window.console && console.error) {
                                console.error('[atb-add] #miniCartOverlay not found on this page - cannot open mini-cart');
                            }
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Product/BasketAdd/", xhr, textStatus, thrownError);
                    }
                });
            }
            catch (e) {
                logScriptError(e);
            }
        });
    $(document).on('click', '.add-btn', function () {
        try {
            var ref = $(this).attr('data-productid');
            var itemtype = $(this).attr('data-itemtype') || '1';
            var price = (itemtype === '2' && $(this).attr('data-price')) ? $(this).attr('data-price') : 0;

            var thisbutton = $(this);
            var thisentry = $(this).closest('.atb-entry');
            var qty = thisentry.find('input.atb-qty:first').val() || '1';
            // Same "already on the basket page" reasoning as the .atb-add handler above - this
            // button is the mobile-width rendering of the same inline add-on region
            // (#itemList, BasketDetails.cshtml), shown/hidden via CSS alongside the .you-may-need
            // desktop carousel rather than a separate server code path.
            var fromAddonRegion = $(this).closest('#itemList').length > 0;

            $.ajax({
                url: "/Product/BasketAdd/",
                dataType: 'json',
                type: 'POST',
                cache: false,
                data: {
                    productref: ref,
                    productprice: price,
                    productqty: qty,
                    itemtype: itemtype
                },
                success: function (data) {
                    changeBasketComplete(data, thisbutton);
                    refreshViewBasket();
                    maybeShowAddSellPopupAfterAdd(fromAddonRegion);

                    if (!fromAddonRegion) {
                        var $miniCartOverlay = $('#miniCartOverlay');
                        if ($miniCartOverlay.length) {
                            $miniCartOverlay.addClass('is-open');
                            $('body').css('overflow', 'hidden');
                        } else if (window.console && console.error) {
                            console.error('[add-btn] #miniCartOverlay not found on this page - cannot open mini-cart');
                        }
                    }
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketAdd/", xhr, textStatus, thrownError);
                }
            });
        } catch (e) {
            logScriptError(e);
        }
    });

    // Mega Menu Mobile manipulation
    if ($('#mobile-menu').is(":visible")) {
        $(document).on('click',
            '#dynamicNav .navbar-link',
            function () {
                var elem = $(this);

                if (typeof elem.attr('href') === "undefined") {
                    //mobile category link
                    $('#' + elem.attr('data-cat')).addClass('g-d-b').removeClass('g-d-n');
                    $('#' + elem.attr('data-cat')).height($('.navbar-collapse').height());
                }
            });
        $(document).on('click',
            '.navbar-slide-close',
            function () {
                var elem = $(this).closest('ul');
                $(elem).addClass('g-d-n').removeClass('g-d-b');

            });
        $(document).on('click',
            '#mobile-menu',
            function () {
                $('.navbar-slide').addClass('g-d-n').removeClass('g-d-b');
            });
    } else {
        $('#dynamicNav').hoverIntent(function (e) {
            $('body').append('<div class="modal-backdrop g-d-n g-op-50p"></div>');
            $('.modal-backdrop').fadeIn(150);
            $('.navbar-inverse .navbar-collapse').css('z-index', 1050);
        },
            function () {
                $('.modal-backdrop').fadeOut(150,
                    function () {
                        $('.modal-backdrop').remove();
                    });
                $('.navbar-inverse .navbar-collapse').css('z-index', 10);
            });
        $('#dynamicNav > li').hoverIntent(function (e) {
            var elem = $(this);
            elem.find('> div').removeClass('g-d-n').addClass('g-d-b');
            $('.lazy').lazyload();
        },
            function () {
                var elem = $(this);
                elem.find('> div').removeClass('g-d-b').addClass('g-d-n');
            });
    }

    // Login Stuff
    $(document).on('change',
        '.check-new',
        function () {
            if (!$(this).is(':checked')) {
                $('.check-existing').prop('checked', true);
                $('.ident-pass').fadeIn(500);
            } else {
                $(this).prop('checked', true);
                $('.check-existing').prop('checked', false);
                $('.ident-pass').hide();
            }
        });

    $(document).on('click',
        '.check-new-label',
        function () {
            if ($('.check-new').is(':checked')) {
                $('.check-new').prop('checked', false).change();
            } else {
                $('.check-new').prop('checked', true).change();
            }
        });

    $(document).on('click',
        '.check-existing-label',
        function () {
            if ($('.check-existing').is(':checked')) {
                $('.check-existing').prop('checked', false).change();
            } else {
                $('.check-existing').prop('checked', true).change();
            }
        });

    $(document).on('change',
        '.check-existing',
        function () {
            if (!$(this).is(':checked')) {
                $('.check-new').prop('checked', true);
                $('.ident-pass').hide();
            } else {
                $(this).prop('checked', true);
                $('.check-new').prop('checked', false);
                $('.ident-pass').fadeIn(500);
            }
        });

    // Prevent submission of forms when pressing Enter key in a text input
    $('#signup-form').on('keypress', ':input:not(textarea):not([type=submit])', function (e) {
        if (e.which === 13) e.preventDefault();
    });

    $(document).on('click',
        '#btn-signup-initial',
        function () {
            if ($('#signup-email').valid() && $('#signup-password').valid()) {
                // First check if a user exists for the email entered
                $.ajax({
                    async: false,
                    cache: false,
                    type: "POST",
                    url: "/MyAccount/UserExists",
                    data: {
                        email: $('#signup-email').val(),
                        __RequestVerificationToken: $('input[name=__RequestVerificationToken]').val()
                    },
                    success: function (data) {
                        if (data === 'False') {
                            $("#signup-form").validate().element("#signup-email");
                            $("#signup-form").validate().element("#signup-password");
                            if ($('#signup-email').valid() && $('#signup-password').valid()) {

                                removeErrorMessage('.signup-initial');

                                $('.signup-initial').hide();
                                $('.signup-details').fadeIn(500);
                            }
                        } else {
                            displayErrorMessage('.signup-initial', 'An account for this user already exists.');
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/MyAccount/UserExists/", xhr, textStatus, thrownError);
                    }
                });
            } else {
                if (!$("#signup-email").val() || !$("#signup-password").val()) {
                    displayErrorMessage('.signup-initial', 'Please complete the highlighted fields.');
                } else {
                    displayErrorMessage('.signup-initial', 'Please correct the highlighted fields.');
                }
            }
        });

    $(document).on('keyup',
        '#signup-email, #signup-password',
        function () {
            var form = $('#signup-form');
            form.validate();

            if (form.valid()) {
                removeErrorMessage('.signup-initial');
            }
        });

    $(document).on('click',
        '#btn-signin',
        function () {
            $('#signin-form').validate().element('#signin-email');
            $('#signin-form').validate().element('#signin-password');

            if ($('#signin-email').valid() && $('#signin-password').valid()) {
                removeErrorMessage('.signin');
            } else {

                if (!$('#signin-email').val() || !$('#signin-password').val()) {
                    displayErrorMessage('.signin', 'Please complete the highlighted fields.');
                } else {
                    displayErrorMessage('.signin', 'Please correct the highlighted fields.');
                }
            }
        });

    $(document).on('keyup',
        '#signin-email, #signin-password',
        function () {
            var form = $('#signin-form');
            form.validate();

            if (form.valid()) {
                removeErrorMessage('.signin');
            }
        });

    $(document).on('click',
        '#btn-ident-signin',
        function () {
            $('#ident-form').validate().element('#SignIn_UserName');
            $('#ident-form').validate().element('#SignIn_Password');

            if ($('#SignIn_UserName').valid() && $('#SignIn_Password').valid()) {
                removeErrorMessage('.ident');
            } else {
                if (!$('#SignIn_UserName').val() || !$('#SignIn_Password').val()) {
                    displayErrorMessage('.ident', 'Please complete the highlighted fields.');
                } else {
                    displayErrorMessage('.ident', 'Please correct the highlighted fields.');
                }
            }
        });

    $(document).on('keyup',
        '#SignIn_UserName, #SignIn_Password',
        function () {
            var form = $('#ident-form');
            form.validate();

            if (form.valid()) {
                removeErrorMessage('.ident');
            }
        });

    $(document).on('click',
        '#btn-signup-address-manual',
        function () {
            $('#signin-form').validate().element('#SignUp_Address_Line1');
            $('#signin-form').validate().element('#SignUp_Address_Line2');
            $('#signin-form').validate().element('#SignUp_Address_Line4');
            $('#signin-form').validate().element('#SignUp_Address_PostCode');

            if ($('#SignUp_Address_Line1').valid() && $('#SignUp_Address_Line2').valid() && $('#SignUp_Address_Line4').valid() && $('#SignUp_Address_PostCode').valid()) {
                removeErrorMessage('.address-manual');
            } else {
                if ($('#SignUp_Address_Line1').val().length > 30) {
                    displayErrorMessage('.address-manual', 'Company name must be 30 characters or less.');
                } else if (!$('#SignUp_Address_Line2').val() || !$('#SignUp_Address_Line4').val() || !$('#SignUp_Address_PostCode').val()) {
                    displayErrorMessage('.address-manual', 'Please complete the highlighted fields.');
                } else {
                    displayErrorMessage('.address-manual', 'Please correct the highlighted fields.');
                }
            }
        });

    $(document).on('keyup',
        '#SignUp_Address_Line2, #SignUp_Address_Line4, #SignUp_Address_PostCode',
        function () {
            var form = $('#signup-form');
            form.validate();

            if (form.valid()) {
                removeErrorMessage('.address-manual');
            }
        });

    $(document).on('click',
        '#btn-signup-details',
        function () {
            loadPca(window, document, "pca", "//NETGI11112.pcapredict.com/js/sensor.js");
            pca.on("load", function (type, id, control) {
                control.listen("populate", function (address) {
                    $('#signup3').trigger('click');
                });
            });

            $('#signup-form').validate().element('#signup-firstname');
            $('#signup-form').validate().element('#signup-surname');
            $('#signup-form').validate().element('#signup-telno');

            if ($('#signup-firstname').valid() && $('#signup-surname').valid() && $('#signup-telno').valid()) {
                removeErrorMessage('.signup-details');
                $('.signup-details').hide();
                $('.signup-address').fadeIn(500);
            } else {
                if (!$('#signup-firstname').val() || !$('#signup-surname').val() || !$('#signup-telno').val()) {
                    displayErrorMessage('.signup-details', 'Please complete the highlighted fields.');
                } else {
                    displayErrorMessage('.signup-details', 'Please correct the highlighted fields.');
                }
            }
            $('select:enabled').each(function () {
                if (!$(this).valid() && $(this).hasClass('input-validation-error')) {
                    $(this).prevAll('button').css('border', '2px solid #ff6666');
                } else {
                    $(this).prevAll('button').css('border', '1px solid #ccc');
                }
            });
        });

    $(document).on('keyup',
        '#signup-firstname, #signup-surname, #signup-telno',
        function () {
            var form = $('#signup-form');
            form.validate();

            if (form.valid()) {
                removeErrorMessage('.signup-details');
            }
        });

    $(document).on('click',
        '.signin-sidebar-btn, .signin-reset-back',
        function () {
            $('.signin, .signin-sidebar').fadeIn(500);
            $('.signup, .signup-sidebar, .signin-reset-password').hide();
            $('.signin-reset-password').empty();
        });

    $(document).on('click',
        '.ident-reset-back',
        function () {
            removeErrorMessage('.ident');
            $('.ident-reset-password').empty();
            $('.ident-set-password').hide();
            $('.ident').fadeIn(500);
        });

    $(document).on('click',
        '.signup-sidebar-btn',
        function () {
            $('.signup, .signup-sidebar, .signup-initial, .address-auto').fadeIn(500);
            $('.signin, .signin-sidebar, .reset-password, .reset-confirmation, .signup-details, .signup-address, .address-manual').hide();
        });

    $(document).on('click',
        '.address-back',
        function () {
            $('.signup-address, .address-manual').hide();
            $('.signup-details').fadeIn(500);
            $('.address-auto').delay(500).show();
        });

    $(document).on('click',
        '.details-back',
        function () {
            $('.signup-details').hide();
            $('.signup-initial').fadeIn(500);
        });

    $(document).on('click',
        '.enter-address',
        function () {
            $('.address-auto').hide();
            $('.address-manual').fadeIn(500);
        });

    $('#SignIn').on('hidden.bs.modal', function () {
        $('.signup, .signup-sidebar, .reset-password, .reset-confirmation').hide();
        $('.signin, .signin-sidebar').delay(500).show();
        $('#SignIn .signup input').not(":checkbox, :submit, :button, [name='__RequestVerificationToken']").val('');
        $('.error-message').hide();
        $('.input-validation-error').removeClass('input-validation-error');
    });

    $('#ident-modal').on('hidden.bs.modal', function () {
        $('.ident-reset-password').empty();
        $('.ident').delay(500).show();
        $('.error-message').hide();
        $('.input-validation-error').removeClass('input-validation-error');
    });

    $(document).on('click',
        '.logout',
        function () {
            $.ajax({
                async: false,
                cache: false,
                type: "POST",
                url: "/MyAccount/SignOut",
                success: function (data) {
                    if (location.href.toLowerCase().indexOf('checkout') !== -1) {
                        location.href = '/checkout/';
                    } else {
                        location.reload();
                    }
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/MyAccount/SignOut/", xhr, textStatus, thrownError);
                }
            });
        });

    $(document).on('click',
        '#forgot-password',
        function () {
            var emailAddress = $('#Email').val();
            $('#ident-modal').modal('hide');

            window.parent.launchPopup('ForgotPassword', 'password-modal', 'md', null, { backdrop: 'static' });
            if (emailAddress !== undefined) {
                $('#password-reset-email').val(emailAddress);
            }
        });

    $(document).on('click',
        '.signin-forgot-password',
        function () {

            getPopupContent('ForgotPassword', null, function (sr) {
                $('.signin-sidebar-btn').trigger('click');
                $('.signin-reset-password').append(sr.Html);

                $('#password-reset-email').val($('#signin-email').val());

                $('.signin-reset-password, .signin-reset-back').fadeIn(500);
                $('.signin').hide();
            });
        });

    $(document).on('click',
        '.ident-forgot-password',
        function () {
            getPopupContent('ForgotPassword', null, function (sr) {
                $('.ident-reset-password').append(sr.Html);

                $('#password-reset-email').val($('#SignIn_UserName').val());

                $('.ident-reset-password, .ident-reset-back').fadeIn(500);
                $('.ident').hide();
            });
        });

    $(document).on('click',
        '.reset-close',
        function () {
            $('.modal').modal('hide');
        });

    $(document).on('click',
        '.reset-password-btn',
        function (e) {
            $('#password-reset-form').validate({ errorPlacement: function (error, element) { } }).element('#password-reset-email');

            if ($('#password-reset-email').valid()) {
                removeErrorMessage('.reset-password');
            } else {
                $('#password-reset-email').addClass('input-validation-error');
                displayErrorMessage('.reset-password', 'Please enter a valid email address.');
            }
        });

    $(document).on('click',
        '#setup-new-account',
        function () {
            $('.ident-set-password').hide();
            $('.ident-actions').fadeIn(500);
        });

    $(document).on('click',
        '.signInLink',
        function () {
            var redirecturl = $(this).attr('data-url');
            $.ajax({
                url: "/Misc/AuthenticationCheck",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                },
                async: false,
                success: function (data) {
                    if (!data.isSuccess) {
                        $('#SignIn').modal('show');
                    } else {
                        window.parent.location = redirecturl;
                    }
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Misc/AuthenticationCheck/", xhr, textStatus, thrownError);
                }
            });
        });

    // Basket Operations
    $('.basketMessage').hoverIntent(function (e) {
        $('.basketMessage').stop(true);
    },
        function () {
            $('.basketMessage').animate({
                opacity: "hide"
            },
                500,
                function () {
                    $(this).css('right', 105);
                });
        });

    // Removes a basket line - used ONLY by the full basket page's own remove button
    // (BasketDetails.cshtml: <button class="basket-remove delete">). This entire success
    // callback was found commented out - meaning clicking remove on the full basket page
    // deleted the item server-side but never updated anything on screen. Restored to working
    // order (keeping the page's original "thisparent captured before replaceWith" approach
    // unchanged, since that's this handler's pre-existing behaviour, not something to fix
    // here) - the mini-cart now has its own independent .minibasket-remove handler below so
    // this one only ever needs to worry about the full basket page.
    $(document).on('click',
        '.basket-remove',
        function () {
            var ref = $(this).attr('data-productid');
            var thisparent = $(this).closest('.content');
            var isCheckout = false;
            if (isCurrentPage('/checkout')) {
                isCheckout = true;
            }
            $.ajax({
                url: "/Product/BasketDelete/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    productref: ref
                },
                async: false,
                success: function (data) {
                    if (!data.savereturn.IsSuccess) {
                        launchPopup('IsInCheckout', 'popup');
                        return false;
                    }
                    if (isCheckout) {
                        refreshViewBasket();
                    } else {
                        $('#minibasket-widget').replaceWith(data.basketSummary);
                        if ($('#miniCartOverlay').hasClass('is-open')) {
                            $('#miniCartOverlay').addClass('is-open');
                            $('body').css('overflow', 'hidden');
                        }

                        $('.basketQuantity').html(data.basketQuantity);
                        $('.basketTotal').html(data.basketTotal);
                        $('.basket-counter').html(data.basketQuantity);

                        setDeferredImages();

                        //See if we can find an entry in the current page
                        var prodentry = $('.body-content .atb-add[data-productid=' + ref + ']');
                        if (prodentry.length > 0) {
                            var thisentry = prodentry.closest('.atb-entry').first();
                            thisentry.find('.atb-count').html('0').parent().addClass('g-v-h');
                            if (thisentry.find('.product-info-message').length > 0) {
                                thisentry.find('.product-info-message').html(data.productInfoMessage).removeClass('g-v-h');
                                thisentry.find('.product-price-message').html(data.productPriceMessage);
                            }
                        }
                    }

                    renderPaypalButtonV2();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketDelete/", xhr, textStatus, thrownError);
                }
            });
        });

    // Removes a basket line - used ONLY by the mini-cart's own remove button
    // (MiniBasket.cshtml: <button class="minibasket-remove delete">). Kept fully
    // independent from .basket-remove above (which the full basket page uses) so a
    // mini-cart fix can never change basket-detail-page behaviour, and vice versa.
    $(document).on('click',
        '.minibasket-remove',
        function () {
            var ref = $(this).attr('data-productid');

            $.ajax({
                url: "/Product/BasketDelete/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    productref: ref
                },
                async: false,
                success: function (data) {
                    if (!data.savereturn.IsSuccess) {
                        launchPopup('IsInCheckout', 'popup');
                        return false;
                    }

                    var wasOpen = $('#miniCartOverlay').hasClass('is-open');

                    $('#minibasket-widget').replaceWith(data.basketSummary);

                    if (wasOpen) {
                        $('#miniCartOverlay').addClass('is-open');
                        $('body').css('overflow', 'hidden');
                    }

                    $('.basketQuantity').html(data.basketQuantity);
                    $('.basketTotal').html(data.basketTotal);
                    $('.basket-counter').html(data.basketQuantity);

                    setDeferredImages();

                    var prodentry = $('.body-content .atb-add[data-productid=' + ref + ']');
                    if (prodentry.length > 0) {
                        var thisentry = prodentry.closest('.atb-entry').first();
                        thisentry.find('.atb-count').html('0').parent().addClass('g-v-h');
                        if (thisentry.find('.product-info-message').length > 0) {
                            thisentry.find('.product-info-message').html(data.productInfoMessage).removeClass('g-v-h');
                            thisentry.find('.product-price-message').html(data.productPriceMessage);
                        }
                    }

                    renderPaypalButtonV2();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketDelete/", xhr, textStatus, thrownError);
                }
            });
        });

    $(document).on('click',
        '.atb-replace',
        function () {
            var ref = $(this).attr('data-productid');
            var price = 0;
            var qty = $(this).attr('data-qty');
            var refremove = $(this).attr('data-removeid');

            $.ajax({
                url: "/Product/BasketReplace/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    productref: ref,
                    productprice: price,
                    productqty: qty,
                    productrefremove: refremove
                },
                async: false,
                success: function (data) {
                    if (!data.savereturn.IsSuccess) {
                        launchPopup('IsInCheckout', 'popup');
                        return false;
                    }
                    refreshViewBasket();
                    renderPaypalButtonV2();
                    maybeShowAddSellPopupAfterAdd();

                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketReplace/", xhr, textStatus, thrownError);
                }
            });
        });
    // Mini-cart's own "Switch Now" button - independent of the shared .atb-replace used on the
    // basket-detail page (which goes through refreshViewBasket()). This one talks to
    // /Product/BasketReplace/ directly and swaps in data.basketSummary itself, so it needs its
    // own wasOpen/reopen handling for the overlay.
    $(document).on('click',
        '.minibasket-replace',
        function () {
            var ref = $(this).attr('data-productid');
            var price = 0;
            var qty = $(this).attr('data-qty');
            var refremove = $(this).attr('data-removeid');
            var wasOpen = $('#miniCartOverlay').hasClass('is-open');

            $.ajax({
                url: "/Product/BasketReplace/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    productref: ref,
                    productprice: price,
                    productqty: qty,
                    productrefremove: refremove
                },
                async: false,
                success: function (data) {
                    if (!data.savereturn.IsSuccess) {
                        launchPopup('IsInCheckout', 'popup');
                        return false;
                    }
                    $('#minibasket-widget').replaceWith(data.basketSummary);
                    $('.basketQuantity').html(data.basketQuantity);
                    $('.basketTotal').html(data.basketTotal);
                    $('.basket-counter').html(data.basketQuantity);
                    setDeferredImages();
                    if (wasOpen) {
                        $('#miniCartOverlay').addClass('is-open');
                        $('body').css('overflow', 'hidden');
                    }
                    renderPaypalButtonV2();
                    maybeShowAddSellPopupAfterAdd();

                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketReplace/", xhr, textStatus, thrownError);
                }
            });
        });
    $(document).on("click", "#btnSwitchAll", function () {
        $.ajax({
            url: "/Product/BasketReplaceAll/",
            type: "POST",
            dataType: "json",
            cache: false,

            success: function (data) {

                if (!data.savereturn.IsSuccess) {
                    launchPopup('IsInCheckout', 'popup');
                    return;
                }

                refreshViewBasket();
                renderPaypalButtonV2();
                maybeShowAddSellPopupAfterAdd();
            },

            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Product/BasketReplaceAll/", xhr, textStatus, thrownError);
            }
        });
    });
    // Mini-cart "Switch All" button - independent of the shared #btnSwitchAll used on the
    // basket-detail page (which goes through refreshViewBasket()). This one talks to
    // /Product/BasketReplaceAll/ directly and swaps in data.basketSummary itself, so it needs
    // its own wasOpen/reopen handling for the overlay.
    $(document).on('click', '#btnMiniBasketSwitchAll', function () {
        var wasOpen = $('#miniCartOverlay').hasClass('is-open');
        $.ajax({
            url: "/Product/BasketReplaceAll/",
            type: "POST",
            dataType: "json",
            cache: false,

            success: function (data) {

                if (!data.savereturn.IsSuccess) {
                    launchPopup('IsInCheckout', 'popup');
                    return;
                }

                $('#minibasket-widget').replaceWith(data.basketSummary);
                $('.basketQuantity').html(data.basketQuantity);
                $('.basketTotal').html(data.basketTotal);
                $('.basket-counter').html(data.basketQuantity);
                setDeferredImages();
                if (wasOpen) {
                    $('#miniCartOverlay').addClass('is-open');
                    $('body').css('overflow', 'hidden');
                }
                renderPaypalButtonV2();
                maybeShowAddSellPopupAfterAdd();
            },

            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Product/BasketReplaceAll/", xhr, textStatus, thrownError);
            }
        });
    });

    // Mini-cart promotion code - apply. Independent of the basket-detail page's own Apply
    // Voucher button/flow (id="apply-voucher", handled in checkout.js reading #voucher-code).
    // This used to share the "#apply-voucher" id with the mini-cart's old markup, which meant
    // this handler (reading the mini-cart's own field) fired redundantly every time the
    // basket-detail page's own button was clicked too. The mini-cart's button/field now have
    // their own dedicated class/id (.minibasket-apply-voucher / #minibasket-voucher-code), so
    // this handler is scoped to the mini-cart only and no longer collides with checkout.js's
    // handler for the basket-detail page.
    $(document).on('click', '.minibasket-apply-voucher', function () {
        var code = $.trim($('#minibasket-voucher-code').val());
        var $error = $('#minibasket-voucher-code-error');

        if (!code) {
            return false;
        }

        var wasOpen = $('#miniCartOverlay').hasClass('is-open');

        $.ajax({
            url: "/Checkout/ApplyVoucher/",
            dataType: 'json',
            traditional: true,
            type: 'POST',
            cache: false,
            data: {
                voucherCode: code
            },
            async: false,
            success: function (data) {
                if (!data || !data.savereturn || !data.savereturn.IsSuccess) {
                    if ($error.length) {
                        $error.text((data && data.savereturn && data.savereturn.Message) || 'Sorry, that code isn\'t valid.').removeClass('g-v-h');
                    }
                    return false;
                }

                $('#minibasket-widget').replaceWith(data.basketSummary);
                $('.basketQuantity').html(data.basketQuantity);
                $('.basketTotal').html(data.basketTotal);
                $('.basket-counter').html(data.basketQuantity);
                setDeferredImages();
                if (wasOpen) {
                    $('#miniCartOverlay').addClass('is-open');
                    $('body').css('overflow', 'hidden');
                }
                renderPaypalButtonV2();
            },
            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Checkout/ApplyVoucher/", xhr, textStatus, thrownError);
            }
        });
    });

    // Mini-cart promotion code - remove. Independent of the basket-detail page's own
    // ".remove-voucher" link (handled separately in checkout.js) - the mini-cart's own remove
    // button uses its own ".minibasket-voucher-remove" class, not shared with that page.
    $(document).on('click', '.minibasket-voucher-remove', function () {
        var wasOpen = $('#miniCartOverlay').hasClass('is-open');
        $.ajax({
            url: "/Checkout/RemoveVoucher/",
            dataType: 'json',
            traditional: true,
            type: 'POST',
            cache: false,
            async: false,
            success: function (data) {
                $('#minibasket-widget').replaceWith(data.basketSummary);
                $('.basketQuantity').html(data.basketQuantity);
                $('.basketTotal').html(data.basketTotal);
                $('.basket-counter').html(data.basketQuantity);
                setDeferredImages();
                if (wasOpen) {
                    $('#miniCartOverlay').addClass('is-open');
                    $('body').css('overflow', 'hidden');
                }
                renderPaypalButtonV2();
            },
            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Checkout/RemoveVoucher/", xhr, textStatus, thrownError);
            }
        });
    });

    // "You May Also Need" carousel on the full basket page - shows 3 (1 on mobile) at a time,
    // with left/right arrows stepping one card at a time. Arrows only render when there are
    // more than 3 add-ons (see BasketDetails.cshtml), so this just needs to move/disable them.
    function ymnCarouselStep($wrapper, direction) {
        var $viewport = $wrapper.find('.you-may-need-products');
        var $track = $wrapper.find('.you-may-need-track');
        var $items = $track.find('.need-product');

        if (!$items.length) {
            return;
        }

        var gap = parseFloat($track.css('gap')) || 0;
        var step = $items.first().outerWidth() + gap;
        var visibleCount = Math.max(1, Math.round($viewport.width() / step));
        var maxIndex = Math.max(0, $items.length - visibleCount);

        var currentIndex = parseInt($wrapper.attr('data-index'), 10) || 0;
        var newIndex = currentIndex + direction;
        if (newIndex < 0) { newIndex = 0; }
        if (newIndex > maxIndex) { newIndex = maxIndex; }

        $wrapper.attr('data-index', newIndex);
        $track.css('transform', 'translateX(-' + (newIndex * step) + 'px)');

        $wrapper.find('.need-arrow.prev').prop('disabled', newIndex === 0);
        $wrapper.find('.need-arrow.next').prop('disabled', newIndex === maxIndex);
    }

    $(document).on('click', '.need-arrow.prev', function () {
        ymnCarouselStep($(this).closest('.you-may-need-wrapper'), -1);
    });

    $(document).on('click', '.need-arrow.next', function () {
        ymnCarouselStep($(this).closest('.you-may-need-wrapper'), 1);
    });

    // Mini-cart "Proceed to Checkout" - may show the "You May Also Need" popup first if the
    // basket has in-stock add-on products linked to anything in it. Clicking Proceed a second
    // time (popup already open) skips straight to /checkout/.
    //
    // proceedToCheckout() itself has existed for a while, but this binding was missing - its
    // only caller anywhere in the codebase was an onclick="proceedToCheckout()" on the retired
    // BasketSummary.cshtml (see the comments in ProductController.cs/CheckoutController.cs
    // calling that file out as no longer the live mini-cart markup), so the actual mini-cart
    // widget's "Proceed to Checkout" button (MiniBasket.cshtml, .checkout-button with no
    // onclick) has been dead everywhere it's rendered - which, via the header, is every page
    // on the site - except by coincidence on the basket page itself, where ViewBasket.cshtml's
    // own page-scoped script binds a *different*, dedicated .checkout-button handler (the
    // in-checkout guard, then submit the real checkout form or show the "Secure Checkout"
    // login modal) that happens to also catch the mini-cart's button there since they share
    // the class. Skip entirely when that page-scoped handler is present (#vbBasketDetails only
    // exists in ViewBasket.cshtml) so the basket page keeps exactly that existing behaviour,
    // unchanged and not doubled up with the add-on popup check below.
    $(document).on('click', '.checkout-button', function () {
        if ($('#vbBasketDetails').length) {
            return;
        }
        proceedToCheckout();
    });

    // Header's Basket link (desktop, Header.cshtml's .hdr-basket) and mobile cart icon
    // (Header.cshtml's .basket-mobile wrapper) both used to navigate straight to /checkout/.
    // Per request, the floating "cart-fab" button was removed (MiniBasket.cshtml no longer
    // renders it) and these two links now open the mini-cart tray instead. Delegated, so it
    // keeps working through MiniBasket.cshtml's own AJAX-driven re-renders (which replace
    // #minibasket-widget, not these header links). Manipulates #miniCartOverlay directly
    // rather than calling the global openCart() - the same defensive choice made in the
    // .atb-add handler above, since that's more robust than depending on MiniBasket.cshtml's
    // inline <script> having (re)executed by the time this fires. Falls back to the original
    // navigation (does nothing here, so the browser's default click/navigation just proceeds)
    // if #miniCartOverlay genuinely isn't present on the page for some reason.
    $(document).on('click', '.open-mini-cart', function (e) {
        var $overlay = $('#miniCartOverlay');
        if ($overlay.length) {
            e.preventDefault();
            $overlay.addClass('is-open');
            $('body').css('overflow', 'hidden');
        }
    });

    // The popup's own "Proceed to Checkout" button (mobile only) always means checkout,
    // regardless of how the popup was opened. Uses checkoutUrl() (not a bare '/checkout/'), so
    // a guest who reached this popup without ever signing in - now the norm, since
    // proceedToCheckout() no longer skips the add-on check for signed-out customers - still
    // gets the login popup once they land on the checkout page, instead of a silent bare page.
    $(document).on('click', '#you-may-also-need .ymn-proceed', function () {
        removeAddSellPopup();
        window.location.href = checkoutUrl();
    });

    // The X/close button and the transparent backdrop behind the popup both "skip" it the same
    // way: only force a checkout redirect if the popup was opened from the mini-cart's Proceed
    // to Checkout click. If it was opened after a plain "Add to Basket" (from a product page,
    // category listing, etc.), skipping it should just dismiss it and leave the customer where
    // they were.
    //
    // Includes ".close-btn": the mobile card-list view (.ymn-mobile-view) has its own header
    // with its own close "x" (class="close-btn", id="closeBtn" - YouMayAlsoNeed.cshtml), styled
    // separately from the shared ".ymn-header"/".ymn-close" the desktop view uses, but with no
    // click handler of its own anywhere - tapping it did nothing at all. Added to this same
    // handler rather than given a separate one, since it should behave identically once tapped.
    $(document).on('click', '#you-may-also-need .ymn-close, #you-may-also-need .close-btn, #you-may-also-need-backdrop', function () {
        var context = $('#you-may-also-need').attr('data-context');
        removeAddSellPopup();
        if (context === 'checkout') {
            // checkoutUrl(), not a bare '/checkout/', for the same reason as the .ymn-proceed
            // handler above - a signed-out customer skipping/closing this popup still needs
            // '?showlogin=1' so the checkout page shows its login popup automatically.
            window.location.href = checkoutUrl();
        }
    });

    // Quietly add an item from the "You May Also Need" popup - refresh the mini-cart in place
    // without the usual "added to basket" toast/animation, and without closing the popup, so
    // the user can add more than one of the three items before proceeding.
    $(document).on('click', '.ymn-add', function () {
        var ref = $(this).attr('data-productid');
        var thisbutton = $(this);
        var wasOpen = $('#miniCartOverlay').hasClass('is-open');

        $.ajax({
            url: "/Product/BasketAdd/",
            dataType: 'json',
            traditional: true,
            type: 'POST',
            cache: false,
            data: {
                productref: ref,
                productprice: 0,
                productqty: 1,
                itemtype: '1'
            },
            async: false,
            success: function (data) {
                if (!data.savereturn.IsSuccess) {
                    launchPopup('IsInCheckout', 'popup');
                    return false;
                }

                $('#minibasket-widget').replaceWith(data.basketSummary);
                $('.basketQuantity').html(data.basketQuantity);
                $('.basketTotal').html(data.basketTotal);
                $('.basket-counter').html(data.basketQuantity);
                setDeferredImages();
                if (wasOpen) {
                    $('#miniCartOverlay').addClass('is-open');
                    $('body').css('overflow', 'hidden');
                }
                thisbutton.text('Added').prop('disabled', true);
            },
            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Product/BasketAdd/", xhr, textStatus, thrownError);
            }
        });
    });

    // Brand Wizard
    $('.hm-bw-links').hide();
    $(document).on('mouseenter',
        '.hm-brandWizardEntry',
        function () {
            $(this).find('.hm-bw-links').show();
        });

    $(document).on('mouseleave',
        '.hm-brandWizardEntry',
        function () {
            $(this).find('.hm-bw-links').hide();
        });

    // Launch SignIn Modal
    if (window.location.search.indexOf('signin=') !== -1) {
        $('#sign-in').trigger('click');
    }

    // Popup Processing
    if (window.location.search.indexOf('pm=') !== -1) {
        var parms = window.location.search.split('&');
        var popupname = '';
        var size = 'lg';
        var replacements = '';
        for (i = 0; i < parms.length; i++) {
            if (parms[i].indexOf('pm=') !== -1) {
                popupname = parms[i].split('=')[1];
            }
            if (parms[i].indexOf('sz=') !== -1) {
                size = parms[i].split('=')[1];
            }
            if (parms[i].indexOf('rpl=') !== -1) {
                replacements = decodeURIComponent(parms[i]).split('=')[1].replace(/\$/g, "&").replace(/\_/g, "=");
            }
        }
        // Lauch the popup
        launchPopup(popupname, 'popup', size, replacements);
    }
    $(document).on('click',
        '.popup',
        function () {
            // get popup html
            var popupname = $(this).attr("data-popupname");
            var popupid = typeof $(this).attr("data-popupid") === 'undefined' ? 'popup' : $(this).attr("data-popupid");
            var popupwidth = typeof $(this).attr("data-popupwidth") === 'undefined' ?
                '' :
                $(this).attr("data-popupwidth");
            var replacements = typeof $(this).attr("data-replacements") === 'undefined' ?
                '' :
                $(this).attr("data-replacements");

            launchPopup(popupname, popupid, popupwidth, replacements);
        });

    $(document).on('shown.bs.modal',
        function (e) {
            //$('#' + e.target.id + ' .lazy').lazyload();
            $('.lazy', e.target).lazyload();
        });

    $(document).on('hidden.bs.modal',
        function (e) {
            // Provided it's OK to remove
            //if (!$('#' + e.target.id).hasClass('donotremove')) {
            if (!$(e.target).hasClass('donotremove')) {
                //$('#' + e.target.id).remove();
                $(e.target).remove();
            }
            // Remove right-padding added to body tag by the popup plugin
            $('body').removeAttr('style');
        });

    // Compare Custom Popup
    $('.custom-popup').on('show.bs.modal',
        function (event) {

            var productIds = [];

            $('.pg-compare-select > .fa-check-square-o').each(function () {
                productIds.push($(this).data('id'));
            });

            var idString = productIds.join();
            var url = event.relatedTarget.dataset.url;
            var container = $(event.relatedTarget.dataset.container);
            container.html(
                '<div class="text-center g-m-t-20"><i class="fa fa-spinner fa-pulse fa-3x fa-fw"></i></div>');

            if (idString.length > 0) {
                $.ajax({
                    url: url,
                    data: { productsToCompare: idString },
                    method: 'POST'
                }).done(function (data) {
                    container.html(data);
                });
            } else {
                container.html(
                    '<div class="text-center g-m-t-20">Please hover over the product(s) you want to compare and check the compare box, then select the compare button. You can compare up to a maximum of 4 products each time.</div>');
            }
        });

    // Favourite Printers
    $('.printerMessage').hoverIntent(function (e) {
        $('.printerMessage').stop(true);
    },
        function () {
            $('.printerMessage').animate({
                opacity: "hide"
            },
                500,
                function () {
                });
        });

    // Search related
    var autoCompTypingTimer;
    var resultClicked = false;
    if ($('#SearchApplication').val() !== '1') {
        // Elastic
        $('#keyword, #wizardSearch').keyup(function () {
            var self = $(this);
            var query = self.val();
            if (query.length > 2) {
                autoCompTypingTimer = setTimeout(function () {
                    autocompleteSearch(query, self);
                },
                    500);
            } else {
                clearTimeout(autoCompTypingTimer);
                self.closest('form').next().addClass('g-d-n');
            }
        }).keypress(function (event) {
            var self = $(this);
            if (event.which === 13) {
                self.closest('form').next().addClass('g-d-n');
            }
        }).keydown(function () {
            var self = $(this);
            clearTimeout(autoCompTypingTimer);
        }).focus(function () {
            var self = $(this);
            var query = $(this).val();
            $('body').append('<div class="search-backdrop g-d-n"></div>');
            $('.search-backdrop').fadeIn(150);
            if (self.attr('id') === "keyword") {
                $('.hdr-search, .hdr-search > .autocomplete-results').css('z-index', 1050);
            } else {
                $('.wizardSearch-container').css('z-index', 1050);
            }
            if (query.length > 2) {
                autocompleteSearch(query, self);
            }
        }).blur(function () {
            var self = $(this);
            $('.search-backdrop').fadeOut(150,
                function () {
                    $('.search-backdrop').remove();
                    $('#sli_autocomplete').css('display', 'none');
                    $('.js-sli-close-area').addClass('is-hidden');
                    if (self.attr('id') === "keyword") {
                        $('.hdr-search, .hdr-search > .autocomplete-results').css('z-index', 10);
                    } else {
                        $('.wizardSearch-container').css('z-index', 10);
                    }
                });
            if (!resultClicked) {
                self.closest('form').next().empty().addClass('g-d-n');
            }
            resultClicked = false;
        });
    } else {
        // SLI
        $('#keyword, #wizardSearch')
            .focus(function () {
                var self = $(this);
                if (self.attr('id') === "keyword") {
                    $('.hdr-search, .hdr-search > .autocomplete-results').css('z-index', 1050);
                } else {
                    $('.wizardSearch-container').css('z-index', 1050);
                }
                $('body').append('<div class="search-backdrop g-d-n"></div>');
                $('.search-backdrop').fadeIn(150);
            })
            .blur(function () {
                var self = $(this);
                $('.search-backdrop').fadeOut(150, function () {
                    if (self.attr('id') === "keyword") {
                        $('.hdr-search, .hdr-search > .autocomplete-results').css('z-index', 10);
                    } else {
                        $('.wizardSearch-container').css('z-index', 10);
                    }
                    $('#sli_autocomplete').css('display', 'none');
                    $('.js-sli-close-area').addClass('is-hidden');
                    $('.search-backdrop').remove();
                });
            });
    }

    $(document).on('mousedown',
        '.autocomplete-results a',
        function () {
            resultClicked = true;
        });

    $(document).on("click",
        "#opensearch-mobile",
        function () {
            $(".hdr-search").toggleClass('hidden-xs').toggleClass('hidden-sm');
            $("#keyword").focus();
        });

    // Filters
    $(document).on('click',
        '.fltr-filters input[type="checkbox"]',
        function () {
            triggerFilter(true);
        });
    $(document).on('click',
        '.fltr-filters button',
        function () {
            triggerAltFilter($(this).attr('name').split('_')[1]);
        });

    $(document).on('click',
        '#clearFilter',
        function () {
            $('.fltr-filters input[id^="att-"]').prop('checked', false);
            $('#minPrice, #maxPrice').val('');
            applyFilter();
            $('#filter-price').trigger('click');
        });

    $(document).on('click',
        '.toggle-filter',
        function () {
            if ($('.prd-filters').hasClass('gm-d-n')) {
                $('.prd-filters').slideDown(600,
                    function () {
                        $('.prd-filters').removeClass('gm-d-n');
                    });
            } else {
                $('.prd-filters').slideUp(600,
                    function () {
                        $('.prd-filters').addClass('gm-d-n');
                    });
            }
        });

    $(document).on('click',
        '#filter-price',
        function () {

            if (isCurrentPage('/products/')) {
                $('.pg-products > .pg-entry').addClass('g-d-n');
                applyFilter();
                applyPriceFilter('.pg-products > .pg-entry');
                $('#pg-product-count').html($('.pg-products > .pg-entry:visible').length);
            }
            if (isCurrentPage('/model/') || isCurrentPage('/search-results')) {
                $('.pl-products > div > .pl-entry').addClass('g-d-n');
                applyFilter();
                $('.pl-products > div > .pl-sub-banner').addClass('g-d-n');
                applyPriceFilter('.pl-products > div > .pl-entry');
                $('#pl-product-count').html($('.pl-products > div > .pl-entry:visible').length);
            }
        });

    // Utility Menu
    $(document).on('mouseenter',
        '[data-toggle="offcanvas-open"] > .header',
        function () {
            $('#utility-bar > .tab-closed > span').html($(this).attr('data-label'));
            $('#utility-bar > .tab-closed').css('top', $(this).parent().position().top).show();
        });

    $(document).on('mouseleave',
        '[data-toggle="offcanvas-open"] > .header',
        function () {
            $('#utility-bar > .tab-closed').hide();
        });

    $(document).on('click',
        '[data-toggle="offcanvas-open"] > .header',
        function () {
            if (!$('.row-offcanvas').hasClass('active')) {
                $('.row-offcanvas').addClass('active');
            }
            $('[data-toggle="offcanvas-open"]').removeClass('active');
            var thisparent = $(this).parent();
            thisparent.toggleClass('active');
            var i = $('[data-toggle="offcanvas-open"]').index(thisparent);
            $('.tab-open').css('top', i * 40);
            thisparent.find('.content').animate({
                height: $(window).height() - 160
            });

            //$('.row-offcanvas.active').height($('.sidebar-offcanvas').height());

            var contentContainer = thisparent.find('.content');

            utilityDotDotDot(contentContainer);

            // Spec: "transparent overlay added to the background of the screen, so that the
            // tray has more definition" - dim the rest of the page while the mini-cart is open.
            // if (!$('.offcanvas-backdrop').length) {
            //     $('body').append('<div class="offcanvas-backdrop"></div>');
            // }
        });

    $(document).on('click', '.offcanvas-backdrop', function () {
        $('[data-toggle="offcanvas-close"]').trigger('click');
    });

    $('[data-toggle="offcanvas-close"]').click(function () {
        $('.row-offcanvas').toggleClass('active');
        $('[data-toggle="offcanvas-open"]').removeClass('active');
        $('.offcanvas-backdrop').remove();
    });

    $(document).on('click',
        '.open-utility',
        function () {
            if ($('#' + $(this).attr('data-utility')).hasClass('active')) {
                $('#utility-bar > .tab-open').trigger('click');
            } else {
                $('#' + $(this).attr('data-utility') + ' > .header').trigger('click');
            }
        });

    $(document).on('click',
        '.mobileBasketBackdrop, .btn-continue, .mobileBasketClose',
        function () {
            $('.mobileBasketMessage').hide();
            $('.mobileBasketBackdrop').remove();
        });

    // Image Zoom
    $(document).on('click',
        '#image-zoom',
        function () {
            $('#prd-image').trigger('click');
        });

    $('#imageModal').on('shown.bs.modal', function () {
        $('#imageModal .lazy').lazyload();
    });

    // Go To Top
    if ($('.msp_goToTop').length) {
        var open = false;
        $(window).scroll(function () {
            if ($(window).scrollTop() > window.innerHeight) {
                $('.msp_goToTop').css('display', 'block');
            } else {
                $('.msp_goToTop').css('display', 'none');
            }
        });
        $('.msp_goToTop').css('display', 'none');
        if ($(window).scrollTop() > window.innerHeight) {
            $('.msp_goToTop').css('display', 'block');
        }
    }

    $(document).on('click',
        '.msp_goToTop',
        function () {
            $("html, body").animate({ scrollTop: 0 }, "800");
        });

    // FeeFo Link
    $(document).on('click', '#feefolink', function () {
        var sitename = $(this).attr('data-sitename');
        window.open('http://ww2.feefo.com/en-gb/reviews/' + sitename + '#?timeFrame=ALL&amp;sort=newest', 'feefo', 'width=1100,height=600,scrollbars=yes,resizable=no,toolbar=no,menubar=no,location=no');
        return false;
    });

    // Customer Alert
    $(document).on('click',
        '.ca-suppress',
        function () {
            serverAction(2);
        });

    // SLI URL Logging
    $(document).on('click',
        '.pl-entry',
        function () {
            var attr = $(this).attr('data-logurl');
            if (typeof attr !== typeof undefined && attr !== false) {
                if (attr.length > 0) {
                    $.ajax({
                        url: attr,
                        dataType: 'json',
                        traditional: true,
                        type: 'GET',
                        cache: false,
                        async: false,
                        error: function (xhr, textStatus, thrownError) {
                            logAjaxScriptError(attr, xhr, textStatus, thrownError);
                        }
                    });
                }
            }
        });

    // Countdown Timer
    if ($('.cutoffCountdownFalse').length) {
        startTime();
    }

    // moreLess is "View More" in collapsed mode and "View Less" in expanded mode
    if ($('.moreLess').length) {
        $('.moreLess').each(function () {
            toggleCollapsedMode($(this),
                $(this).attr('data-num-items'),
                $(this).attr('data-buttclass'),
                $(this).attr('data-scroll-offset'),
                false);
        });
    }

    $(document).on('click', '.slideToggle', function () {
        var section = $(this).closest('.collapsable-section').find('#' + $(this).attr('data-section'));
        var action = $(this).attr('data-action');
        var item = $(this);
        $(section).slideToggle(500, function () {
            if (action === "toggleChevron") {
                if (item.find('.fa-chevron-down').length) {
                    item.find('.fa-chevron-down').removeClass('fa-chevron-down').addClass('fa-chevron-up');
                } else {
                    item.find('.fa-chevron-up').removeClass('fa-chevron-up').addClass('fa-chevron-down');
                }
            }
            $("img.lazy").lazyload();
        });
    });

    $(document).on('click', '.moreLess + button', function () {
        toggleCollapsedMode($(this).prev('.moreLess'),
            $(this).prev('.moreLess').attr('data-num-items'),
            $(this).prev('.moreLess').attr('data-buttclass'),
            $(this).prev('.moreLess').attr('data-scroll-offset'),
            true);
    });

    // Google Analytics Tracking
    $(document).on('click', '#recent', function (event) {
        gtag("event", "select_content", {
            content_type: "Home Hub - Recently Viewed",
        });
    });
    $(document).on('click', '#my-printers', function (event) {
        gtag("event", "select_content", {
            content_type: "Home Hub - My Printers",
        });
    });
    $(document).on('click', '#quick-order', function (event) {
        gtag("event", "select_content", {
            content_type: "Home Hub - Recently Ordered Products",
        });
    });

    // Landing Page Popups
    if ($('#firsttimepopup').length) {
        $('#firsttimepopup').modal('show');
    }

    // Standard Stuff
    $('.selectpicker').selectpicker();
    $('[data-toggle="tooltip"]').tooltip();
    $("img.lazy").lazyload({
        threshold: 200,
        failure_limit: 999
    });
    if ($('.dotdotdot:visible').length) {
        $('.dotdotdot').dotdotdot();
    }

    // WebP background images
    if ($('#loadbg').length) {
        if (isWebPSupported()) {
            $('#loadbg').css('background-image', 'url("' + $('#loadbg').attr('data-bgimg').replace('.jpg', '.webp') + '")');
        } else {
            $('#loadbg').css('background-image', 'url("' + $('#loadbg').attr('data-bgimg') + '")');
        }
    }

    // iframe height adjust amends the height of a parent iframe when content changes
    if ($('#iframe-height-adjust').length) {
        $('#iframe-height-adjust').each(function () {
            $(this.contentWindow).resize(function () {
                var id = $('#iframe-height-adjust').attr("data-containerid");
                var iframeid = $('#iframe-height-adjust').attr("data-iframeid");
                o = window.parent.document.getElementsByTagName('iframe')[0];
                if (iframeid !== "") {
                    if (window.parent.document.getElementById(iframeid) !== null) {
                        o = window.parent.document.getElementById(iframeid);
                    }
                }
                // The following test yields different results if identity operator (!==) is used
                if (o != null) {
                    var newHeight = $(id).height();
                    o.style.height = newHeight + 'px';
                }
            });
        });
    }

    $(document).on('click', '.prd-altImage', function () {
        var thistab = $(this);
        $('#prd-image').animate({
            opacity: "hide"
        },
            500,
            function () {
                $('#prd-image').attr('src', thistab.attr('data-image'));
            }).animate({
                opacity: "show"
            },
                500,
                function () {
                    $('.prd-altImage').closest($('div')).removeClass('active');
                    thistab.closest($('div')).addClass('active');
                });
    });

    // Simple Accordion
    $(".accordion_title").on("click", function (e) {
        e.preventDefault();
        var $this = $(this);

        if (!$this.hasClass("accordion-active")) {
            $(".accordion_content").slideUp(400);
            $(".accordion_title").removeClass("accordion-active");
            $('.accordion_arrow').removeClass('accordion_rotate');
        }

        $this.toggleClass("accordion-active");
        $this.next().slideToggle();
        $('.accordion_arrow', this).toggleClass('accordion_rotate');
    });

    // Collapsable Panels
    if (isCurrentPage('/myaccount') || isCurrentPage('/printer-finder')) {
        $(document).on('click',
            '.collapsable-sections .toggle-section',
            function () {
                var sections = $(this).closest('.collapsable-sections');
                var section = $(this).closest('.collapsable-section');
                var detail = section.find('.collapsable-detail');
                var ajaxurl = section.attr('data-url');
                var openMe = $(this).find('i').hasClass('fa-chevron-down');
                var closeMe = $();

                if (sections.children('.collapsable-section').find('.fa-chevron-up').length) {
                    closeMe = sections.children('.collapsable-section').find('.fa-chevron-up:first').closest('.collapsable-section');
                }

                sections.find('.toggle-section').removeClass('selected');
                $(this).addClass('selected');

                section.find('.toggle-section:first').html('Loading <i class="fa fa-spinner fa-spin"></i>');

                if (openMe) {
                    if (ajaxurl !== undefined) {
                        $.ajax({
                            url: ajaxurl,
                            dataType: 'json',
                            traditional: true,
                            type: 'GET',
                            cache: false,
                            async: true,
                            success: function (data) {
                                detail.html(data.responseHtml);
                                collapsablePanelComplete(section, detail);
                            },
                            error: function (xhr, textStatus, thrownError) {
                                logAjaxScriptError(ajaxurl, xhr, textStatus, thrownError);
                                detail.html("OOPS! We found an error. Status: " + xhr.status.toString() + ' ' + thrownError.toString());
                                collapsablePanelComplete(section, detail);
                            }
                        });
                    } else {
                        collapsablePanelComplete(section, detail);
                    }
                }

                // close any open forms
                closeMe.each(function () {
                    var closeSection = $(this);
                    closeSection.find('.collapsable-detail:first').slideUp(400,
                        function () {
                            if (ajaxurl !== undefined) {
                                closeSection.find('.collapsable-detail:first').empty();
                            }
                            closeSection.find('.toggle-section:first').html('Open <i class="fa fa-chevron-down"></i>');
                        });
                });

                if (!isCurrentPage('/myaccount') && openMe) {
                    if (ajaxurl !== undefined) {
                        $.ajax({
                            url: ajaxurl,
                            dataType: 'json',
                            traditional: true,
                            type: 'POST',
                            cache: false,
                            async: false,
                            success: function (data) {
                                detail.html(data.responseHtml);
                                collapsablePanelComplete(section, detail);
                            },
                            error: function (xhr, textStatus, thrownError) {
                                logAjaxScriptError(ajaxurl, xhr, textStatus, thrownError);
                            }
                        });
                    } else {
                        collapsablePanelComplete(section, detail);
                    }
                }
            });

        $(document).on('click',
            '.collapsable-section .toggle-order',
            function () {
                var section = $(this).closest('.collapsable-section');
                var openMe = $(this).closest('.container-fluid');
                var closeMe = section.find('.fa-chevron-up').closest('.container-fluid');
                var attemptToOpen = true;
                if ($(this).html().indexOf('Close') >= 0) {
                    attemptToOpen = false;
                }

                // close any open orders
                closeMe.each(function () {
                    var elem = $(this);
                    $(this).next('.details').slideUp(400,
                        function () {
                            elem.find('.toggle-order').html('Open <i class="fa fa-chevron-down"></i>');
                        });
                });

                if (attemptToOpen) {
                    openMe.next('.details').slideDown(400,
                        function () {
                            // set button wording
                            openMe.find('.toggle-order').html('Close <i class="fa fa-chevron-up"></i>');
                        });
                }
            });

        $(document).on('click',
            '.collapsable-go-to-next',
            function () {
                var section = $(this).closest('.collapsable-section');
                var detail = $(this).closest('.collapsable-detail');
                detail.slideUp(400,
                    function () {
                        section.find('.toggle-section').html('Open <i class="fa fa-chevron-down"></i>');
                    });
                section.next().next().find('.collapsable-detail').slideDown(400,
                    function () {
                        section.next().next().find('.toggle-section').html('Close <i class="fa fa-chevron-up"></i>');
                    });
            });
    }

    // Cookie Consent
    if ($('.cc-block > div').length) {
        $('.cc-block').delay(3000).slideDown(600);
    }

    // Page Specific
    if (isCurrentPage('misc/accountapplication') || isCurrentPage('misc/tradeapplication')) {
        loadPca(window, document, "pca", "//NETGI11112.pcapredict.com/js/sensor.js");
        pca.on("options", function (type, id, options) {
            options.bar = options.bar || {};
            options.bar.showLogo = false;
            options.bar.showCountry = false;
        });
        pca.on("load", function (type, id, control) {
            //custom code
            control.listen("populate", function (address) {
                if ($('#AccountApplicationDetails_BillingAddress_PostCode').val() != '') {
                    $('#co-acc-billadd-fields').removeClass('g-d-n');
                    $('#bill-manual-address').addClass('g-d-n');

                    // Increase the parent iframe height
                }
            });
        });
    }

    if (isCurrentPage('/myaccount')) {
        $(document).on('mouseenter',
            '.mini-product-entry',
            function () {
                $(this).find('.delete-printer').removeClass('g-d-n');
            });

        $(document).on('mouseleave',
            '.mini-product-entry',
            function () {
                $(this).find('.delete-printer').addClass('g-d-n');
            });

        $(document).on('click', 'button', function () {
            $('.error-message').hide();
            $('.validation-summary-errors').show();
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
                        cardid.closest('div.row').remove();
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/OpayoDeleteCard?id=" + cardid.attr('data-id') + "&tokenId=" + cardid.attr('data-tokenId'), xhr, textStatus, thrownError);
                    }
                });
            });

        //$(document).on('click',
        //    '[id^="deleteCard"]',
        //    function () {
        //        var cardid = $(this);
        //        $.confirm({
        //            title: 'Delete Saved Card',
        //            content: 'Are you sure you wish to delete your saved card ending in ',
        //            buttons: {
        //                confirm: function() {                            
        //                    $.ajax({
        //                        url: "/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1],
        //                        dataType: 'json',
        //                        traditional: true,
        //                        type: 'POST',
        //                        cache: false,
        //                        async: false,
        //                        success: function (data) {
        //                            $.alert({
        //                                title: 'Delete Saved Card',
        //                                content: 'Your saved card has been successfully deleted'
        //                            });
        //                            cardid.closest('.row').remove();
        //                        },
        //                        error: function (xhr, textStatus, thrownError) {
        //                            logAjaxScriptError("/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1], xhr, textStatus, thrownError);
        //                        }
        //                    });
        //                },
        //                cancel: function () {

        //                }
        //            }
        //        });
        //    });
    }
});

// Liveagent
$(function () {
    if (!window._laq) {
        window._laq = [];
    }
    window._laq.push(function () {
        if ($('#liveagent_button_online1').length) {
            liveagent.showWhenOnline($('#liveagent_id').val(), document.getElementById('liveagent_button_online1'));
            liveagent.showWhenOffline($('#liveagent_id').val(), document.getElementById('liveagent_button_offline1'));
        }
        if ($('#liveagent_button_online2').length) {
            liveagent.showWhenOnline($('#liveagent_id').val(), document.getElementById('liveagent_button_online2'));
            liveagent.showWhenOffline($('#liveagent_id').val(), document.getElementById('liveagent_button_offline2'));
        }
    });

    try {
        liveagent.init('https://d.la1-c1-par.salesforceliveagent.com/chat', '57220000000Cc76', '00D20000000mwfM');
    } catch (err) {
        // empty
    }

    $(document).on('click',
        '.liveChatTrigger',
        function () {
            if ($('#liveagent_button_online1').length !== 0) {
                $('#liveagent_button_online1').trigger('click');
            } else {
                $('#liveagent_button_online2').trigger('click');
            }
        });

    // ECMA 6 Support
    try {
        new Function("(y => y)");
        // Load Ecma6
        $.getScript('/scripts/ecma6.js', function () { execEcma6(); });
    }
    catch (e) { }
});

//#endregion
