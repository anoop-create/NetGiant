$(function () {
    // Diagnostic code for add to basket
    if (isCurrentPage('/product/') || isCurrentPage('/model/') || isCurrentPage('/products/')) {
        $.data(document.body, 'atb-status', '00');
    }

    // Add to basket
    $(document).on('click',
        '.atb-add',
        function () {
            try {
                $.data(document.body, 'atb-status', '01');
                var ref = $(this).attr('data-productid');
                var admindiscount = false;
                var price = 0;
                if (typeof ($(this).attr('data-admin-discount')) != 'undefined') {
                    admindiscount = $(this).attr('data-admin-discount');
                }
                if (admindiscount) {
                    if (typeof ($(this).attr('data-price')) != 'undefined') {
                        price = $(this).attr('data-price');
                    }
                }
                var thisparent = $('#basket > .content');
                var thisentry = $(this).closest('.atb-entry');
                var quickreorder = $(this).closest('#quick-order').length;
                var qty = "1";
                if (thisentry.find('input.atb-qty').length) {
                    qty = thisentry.find('input.atb-qty:first').val();
                }
                $.data(document.body, 'atb-status', '05');

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
                        isadmindiscount: admindiscount
                    },
                    async: false,
                    success: function (data) {
                        if (!data.savereturn.IsSuccess) {
                            launchPopup('IsInCheckout', 'popup');
                            return false;
                        }
                        $.data(document.body, 'atb-status', '06');
                        $('.basketQuantity').html(data.basketQuantity);
                        $('.basketTotal').html(data.basketTotal);
                        $('.basket-counter').html(data.basketQuantity);

                        var t = $('<section>').append($.parseHTML(data.savereturn.Html));
                        var h = t.find('div[data-productid="' + ref + '"]');

                        thisparent.find('.basket-entry, hr').remove();
                        $(data.savereturn.Html).insertAfter('#basketSummary');
                        if ($('.hdr-utility-menu.active').length > 0 && quickreorder == 0) {
                            // trigger dotdotdot
                            thisparent.prev().trigger('click');
                        } else {
                            // trigger the pop up
                            $('#basketItem, #mobileBasketItem').html(h);

                            if (quickreorder == 0) {
                                $('.basketMessage').css('right', '105px');
                            } else {
                                $('.basketMessage').css('right', '385px');
                            }
                            $('.basketMessage').animate({
                                opacity: "show",
                                right: "-=50px"
                            },
                                500).delay(3000).animate({
                                    opacity: "hide"
                                },
                                500,
                                function () {
                                    $(this).css('right', 105);
                                });
                            $('body').append('<div class="mobileBasketBackdrop hidden-lg hidden-md g-cur-p"/>');
                            $('.mobileBasketMessage').slideDown(500, function () {
                                $('.mobileBasketClose').show();
                            });
                        }

                        if ($('.atb-count').length > 0) {
                            var origqty = thisentry.find('.atb-count').html();
                            thisentry.find('.atb-count').html((parseInt(origqty) + parseInt(qty)).toString());
                            thisentry.find('.atb-count').parent().parent().removeClass('g-v-h');
                        }
                        $.data(document.body, 'atb-status', '09');
                        setDeferredImages();
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

    // Mega Menu Mobile manipulation
    if ($('#mobile-menu').is(":visible")) {
        $(document).on('click',
            '#dynamicNav a',
            function () {
                var elem = $(this);

                $("i", elem).toggleClass('fa-chevron-down fa-chevron-up');
                elem.siblings('div').toggleClass('g-d-n g-d-b');

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

    $(document).on('click',
        '.check-existing-label',
        function () {
            if ($('.check-existing').is(':checked')) {
                $('.check-existing').prop('checked', false).change();
            } else {
                $('.check-existing').prop('checked', true).change();
            }
        });


    // prevent submission of forms when pressing Enter key in a text input
    $('#signup-form').on('keypress', ':input:not(textarea):not([type=submit])', function (e) {
        if (e.which == 13) e.preventDefault();
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
                        if (data == 'False') {
                            $("#signup-form").validate().element("#signup-email");
                            $("#signup-form").validate().element("#signup-password");
                            if ($('#signup-email').valid() && $('#signup-password').valid()) {

                                removeErrorMessage('.signup-initial');

                                $('.signup-initial').hide();
                                $('.signup-details').fadeIn(500);
                            }
                        } else {
                            displayErrorMessage('.signup-initial', 'An account for this user already exists');
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
                    displayErrorMessage('.address-manual', 'Company name must be 30 characters or less.')
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

            $("head").append($("<link rel='stylesheet' type='text/css' href='https://services.postcodeanywhere.co.uk/css/captureplus-2.30.min.css?key=bw73-pp19-ec92-uj57' />"));

            $.getScript("https://services.postcodeanywhere.co.uk/js/captureplus-2.30.min.js?key=bw73-pp19-ec92-uj57", function () {
                capturePlus.listen("populate", function (address) {
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
                    if (location.href.toLowerCase().indexOf('checkout') != -1) {
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
            var emailAddress = $('#SignIn_UserName').val();
            $('#ident-modal').modal('hide');

            window.parent.launchPopup('ForgotPassword', 'password-modal', 'md', null, { backdrop: 'static' });
            if (emailAddress != undefined) {
                $('#password-reset-email').val(emailAddress);
            }
        });

    $(document).on('click',
        '.signin-forgot-password',
        function () {

            getPopupContent('ForgotPassword', null, function (sr) {
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

    //$(document).on('click',
    //    '#paypal-set-password',
    //    function () {
    //        $('.ident-set-password').hide();
    //        $('.ident').show();
    //        $('.ident-forgot-password').trigger('click');
    //    });

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

    $(document).on('click',
        '.basket-entry .delete',
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
                    productref: ref,
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
                        thisparent.find('.basket-entry, hr').remove();
                        $(data.savereturn.Html).insertAfter('#basketSummary');
                        if ($('.hdr-utility-menu.active').length > 0) {
                            thisparent.prev().trigger('click');
                        }
                        if (isCurrentPage('checkout/viewbasket')) {
                            $('#basketSummary .row[data-productid="' + ref + '"]').prev().remove();
                            $('#basketSummary .row[data-productid="' + ref + '"]').remove();
                        }

                        $('.basketQuantity').html(data.basketQuantity);
                        $('.basketTotal').html(data.basketTotal);
                        $('.basket-counter').html(data.basketQuantity);

                        setDeferredImages();

                        //See if we can find an entry in the current page
                        var prodentry = $('.atb-add[data-productid=' + ref + ']');
                        if (prodentry.length == 1) {
                            prodentry.closest('.atb-entry').find('.atb-count').html('0').parent().addClass('g-v-h');
                        }
                    }

                    renderPaypalButton();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Product/BasketDelete/", xhr, textStatus, thrownError);
                }
            });
        });

    $(document).on('click',
        '#apply-voucher',
        function () {

            var isValid = checkSessionExists("C_IsInCheckout") == true ? false : true;

            if (isValid) {
                $.ajax({
                    url: "/Checkout/ApplyVoucher/",
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        voucherCode: $('#voucher-code').val()
                    },
                    async: false,
                    success: function (data) {
                        if (data.savereturn.IsSuccess) {
                            location.href = "/checkout";
                        } else {
                            displayErrorMessage('.basket-voucher', data.savereturn.Message);
                        }
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/ApplyVoucher/", xhr, textStatus, thrownError);
                    }
                });
            } else {
                $('.IsInCheckout').val('true');
                location.href = '/checkout?pm=IsInCheckout';
            }
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
                        location.href = "/checkout";
                    }
                    refreshVbFields(data);
                    renderPaypalButton();
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/RemoveVoucher/", xhr, textStatus, thrownError);
                }
            });
        });

    // Portal Functions
    $(document).on('click',
        '#apply-discount',
        function () {
            $('#discount-atb').attr('data-price',
                $('#admin-discount').val() / $('#discount-atb').attr('data-vatm'));
            $('#discount-atb').trigger('click');
            refreshViewBasket();
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

    // Popup Processing
    if (window.location.search.indexOf('pm=') != -1) {
        var parms = window.location.search.split('&');
        var popupname = '';
        var size = 'lg';
        var replacements = '';
        for (i = 0; i < parms.length; i++) {
            if (parms[i].indexOf('pm=') != -1) {
                popupname = parms[i].split('=')[1];
            }
            if (parms[i].indexOf('sz=') != -1) {
                size = parms[i].split('=')[1];
            }
            if (parms[i].indexOf('rpl=') != -1) {
                replacements = decodeURI(parms[i].split('=')[1]).replace("$", "&").replace("_", "=");
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
            var popupid = typeof $(this).attr("data-popupid") == 'undefined' ? 'popup' : $(this).attr("data-popupid");
            var popupwidth = typeof $(this).attr("data-popupwidth") == 'undefined' ?
                '' :
                $(this).attr("data-popupwidth");
            var replacements = typeof $(this).attr("data-replacements") == 'undefined' ?
                '' :
                $(this).attr("data-replacements");

            launchPopup(popupname, popupid, popupwidth, replacements);
        });

    $(document).on('hidden.bs.modal',
        function (e) {
            // Provided it's OK to remove
            if (!$('#' + e.target.id).hasClass('donotremove')) {
                $('#' + e.target.id).remove();
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
    $(document).on('click',
        '.fp-save',
        function () {
            //get popup html
            //insert it
            //launch it
        });

    // Search related
    var autoCompTypingTimer;
    var resultClicked = false;
    if ($('#SearchApplication').val() != 1) {
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
            if (event.which == 13) {
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
            if (self.attr('id') == "keyword") {
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
                    if (self.attr('id') == "keyword") {
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
                if (self.attr('id') == "keyword") {
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
                    if (self.attr('id') == "keyword") {
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
        '#clearFilter',
        function () {
            $('.fltr-filters input[id^="att-"]').removeAttr('checked');
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

            $('.row-offcanvas.active').height($('.sidebar-offcanvas').height());

            var contentContainer = thisparent.find('.content');

            utilityDotDotDot(contentContainer);
        });

    $('[data-toggle="offcanvas-close"]').click(function () {
        $('.row-offcanvas').toggleClass('active');
        $('[data-toggle="offcanvas-open"]').removeClass('active');
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

    // Go To Top
    if ($('.msp_goToTop').length > 0) {
        var open = false;
        $(window).scroll(function () {
            if ($(window).scrollTop() > window.innerHeight) {
                $('.msp_goToTop').css('opacity', '1');
            } else {
                $('.msp_goToTop').css('opacity', '0');
            }
        });
        $('.msp_goToTop').css('opacity', '0');
        if ($(window).scrollTop() > window.innerHeight) {
            $('.msp_goToTop').css('opacity', '1');
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
            $.ajax({
                url: '/Misc/SuppressCustomerAlert',
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                },
                async: false,
                success: function (data) {
                    if (data.savereturn.IsSuccess) {
                        $('.ca-message').slideUp(600, function () {
                            $('.ca-message').addClass('g-d-n');
                        });
                    }
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Misc/SuppressCustomerAlert/", xhr, textStatus, thrownError);
                }
            });
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
    $(document).ready(function () {
        //setTimeout(function () { startTime() }, 500);
        startTime();
    });

    // moreLess is "View More" in collapsed mode and "View Less" in expanded mode
    $('.moreLess').each(function () {
        toggleCollapsedMode($(this),
            $(this).attr('data-num-items'),
            $(this).attr('data-buttclass'),
            $(this).attr('data-scroll-offset'),
            false);
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
        _gaq.push(['_trackEvent', '\Home Hub', 'Recently Viewed']);
    });
    $(document).on('click', '#my-printers', function (event) {
        _gaq.push(['_trackEvent', '\Home Hub', 'My Printers']);
    });
    $(document).on('click', '#quick-order', function (event) {
        _gaq.push(['_trackEvent', '\Home Hub', 'Recently Ordered Products']);
    });

    // Landing Page Popups
    if ($('#firsttimepopup').is(':visible')) {
        $('#firsttimepopup').trigger('click');
    }

    // Standard Stuff
    $('.selectpicker').selectpicker();
    $('[data-toggle="tooltip"]').tooltip();
    $("img.lazy").lazyload({threshold: 200});
    $('.navbar .lazy').trigger();
    $.stellar({
        horizontalScrolling: false,
        verticalScrolling: true,
        hideDistantElements: false
    });
    $('.dotdotdot').dotdotdot();

    // Tooltip
    if ($('.tooltip-highlight').length) {
        var selector = window.getSelection ? window : document;
        var x, y, wx, wy, ttw;

        getHighlightTooltip($('.tooltip-highlight').attr('data-tooltipname'));

        $('body').on('mousedown', function (e) {

            if ($(e.target).parents('.custom-tooltip').length > 0 || $(e.target).hasClass('custom-tooltip')) {
                return;
            }

            $('.custom-tooltip').hide();
        });

        $('.tooltip-highlight').on('mousedown', function (e) {

            wx = $(window).width();
            wy = $(window).height();
            ttw = $('.custom-tooltip').width();

            x = e.clientX;
            y = e.clientY + 10;
        });

        $('.tooltip-highlight').on('mouseup', function (e) {
            if (x == 0 || y == 0)
                return;

            // get the point between mousedown and mouseup on the x axis
            x += ((e.clientX - x) / 2) - (ttw / 1.7);
            // take into account any scroll
            y += window.pageYOffset;

            //no x overflow
            if (e.clientX + ttw > wx) {
                x = wx - ttw;
            }

            var response = selector.getSelection().toString().trim() == "" ? false : true;

            if (!response) {
                $('.custom-tooltip').hide();
                return;
            }

            $('.custom-tooltip').css({
                "top": (y + 20) + "px",
                "left": (x + 20) + "px",
                "position": "absolute"
            }).css('visibility', 'visible').hide().fadeIn(500);

            x = y = wx = wy = 0;
        });
    }

    // iframe height adjust amends the height of a parent iframe when content changes
    if ($('#iframe-height-adjust').length) {
        $('#iframe-height-adjust').each(function() {
            $(this.contentWindow).resize(function() {
                var id = $('#iframe-height-adjust').attr("data-containerid");
                var iframeid = $('#iframe-height-adjust').attr("data-iframeid");
                o = window.parent.document.getElementsByTagName('iframe')[0];
                if (iframeid != "") {
                    if (window.parent.document.getElementById(iframeid) != null) {
                        o = window.parent.document.getElementById(iframeid);
                    }
                }
                if (o != null) {
                    var newHeight = $(id).height();
                    o.style.height = newHeight + 'px';
                }
            });
        });
    }

    // Page Specific functions
    // Wizard Pages
    if ($('.wiz-widget').length) {
        $('.wiz-widget').on('change',
            '#wiz-manufacturer',
            function (event) {
                var manuId = $("#wiz-manufacturer").data("kendoDropDownList").value() == "" ? 0 : $("#wiz-manufacturer").data("kendoDropDownList").value();
                $.ajax({
                    url: '/Wizard/ChangeManufacturer',
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        typename: $('#wiz-cartridgetype').val(),
                        manufacturerId: manuId
                    },
                    async: false,
                    success: function (data) {
                        checkManufacturer(manuId);
                        populateManufacturer(data);
                        populatePopularPrinters(data);
                        populatePopularCartridges(data);
                        populateManuText(data);
                        populateEquipLinks(data);
                        $("img.lazy").lazyload();
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Wizard/ChangeManufacturer", xhr, textStatus, thrownError);
                    }
                });
            });

        $('.wiz-widget').on('change',
            '#wiz-family',
            function (event) {
                $.ajax({
                    url: '/Wizard/ChangeFamily',
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        type: $('#wiz-cartridgetype').val(),
                        manufacturerId: $("#wiz-manufacturer").data("kendoDropDownList").value(),
                        familyId: $("#wiz-family").data("kendoDropDownList").value()
                    },
                    async: false,
                    success: function (data) {
                        $("#wiz-equipment").data("kendoDropDownList").dataSource.data(data);
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Wizard/ChangeFamily", xhr, textStatus, thrownError);
                    }
                });
            });

        $('.wiz-widget').on('change',
            '#wiz-equipment',
            function (event) {
                if ($('#wiz-cartridge').length > 0) {
                    $.ajax({
                        url: '/Wizard/ChangeEquipment',
                        dataType: 'json',
                        traditional: true,
                        type: 'POST',
                        cache: false,
                        data: {
                            type: $('#wiz-cartridgetype').val(),
                            manufacturerId: $("#wiz-manufacturer").data("kendoDropDownList").value(),
                            familyId: $("#wiz-family").data("kendoDropDownList").value(),
                            equipmentId: $("#wiz-equipment").data("kendoDropDownList").value()
                        },
                        async: false,
                        success: function(data) {
                            $("#wiz-cartridge").data("kendoDropDownList").dataSource.data(data);
                            $("#wiz-cartridge").change();
                        },
                        error: function(xhr, textStatus, thrownError) {
                            logAjaxScriptError("/Wizard/ChangeEquipment", xhr, textStatus, thrownError);
                        }
                    });
                } else {
                    var equipId = $("#wiz-equipment").data("kendoDropDownList").value() == ""
                        ? 0
                        : $("#wiz-equipment").data("kendoDropDownList").value();
                    checkEquipment(equipId);
                }
            });

        // wiz-cartridge used as part of the extended wizard
        $('.wiz-widget').on('change',
            '#wiz-cartridge',
            function (event) {
                var productId = $("#wiz-cartridge").data("kendoDropDownList").value() == ""
                    ? 0
                    : $("#wiz-cartridge").data("kendoDropDownList").value();
                if (productId == "" || productId == "0") {
                    $('#wiz-find').attr('disabled', 'disabled');
                } else {
                    $('#wiz-find').removeAttr('disabled');
                }
            });

        $('.wiz-widget').on('click',
            '#wiz-find',
            function (event) {
                var equipment = $("#wiz-equipment").data("kendoDropDownList");
                var equipmentData = equipment.dataSource.view()[equipment.selectedIndex - 1];

                _gaq.push(['_trackEvent', 'Printer Wizard', 'Submit']);
                location.href = '/model/' +
                    $("#wiz-equipment").data("kendoDropDownList").text().replace(/ /g, '-') +
                    '-' + equipmentData.Data.data_ctype.replace('hp-range', 'toner-cartridges') + '/';
            });
    }
    if (isCurrentPage('/toner-cartridges/') || isCurrentPage('/ink-cartridges/') || isCurrentPage('/solid-ink-cartridges/') || isCurrentPage('/franking-cartridges/')) {
        $('.mini-product-container').jScrollPane({ showArrows: true });
    }

    // Product Pages
    if (isCurrentPage('/product/')) {

        $('.mini-product-container').jScrollPane({ showArrows: true });
        $(function () {
            $(".product-pdfs").mouseenter(function () {
                $(this).find('.content').removeClass('hide');
            }).mouseleave(function () {
                $(this).find('.content').addClass('hide');
            });


        });

        if (typeof (flixJsCallbacks) === "object") {
            flixJsCallbacks.setLoadCallback(function () {
                try {
                    $('.flix-data, .flix-container').remove();
                } catch (e) {
                }
            },
                'noshow');
        }

        $('#imageModal').on('show.bs.modal',
            function (event) {
                var clickedElem = $(event.relatedTarget);
                var clickedImage = clickedElem.attr('src');

                if (clickedImage.indexOf("mediapool.getthespec.com") != -1) {
                    clickedImage = clickedImage + "&V=HR";
                }

                $('.image-modal .image-slide-container').removeClass('active');
                $('.image-modal .large-image').attr('src', clickedImage);
                $('.image-modal .image-slides img').each(function (index, elem) {
                    if ($(this).attr('src') == clickedImage) {
                        $(this).parent().addClass('active');
                    }
                });
            });

        $(document).on('click',
            '.image-modal .image-slides .image-slide-container',
            function () {
                var imageUrl = $('img', this).attr('src');
                $('.image-modal .large-image').attr('src', imageUrl);
                $('.image-modal .image-slide-container').removeClass('active');
                $(this).addClass('active');
            });

        $(document).on('click',
            '.image-modal #previous-image',
            function () {
                var previousImage = $('.image-slide-container.active').prev('div');
                if (previousImage.length > 0) {
                    $('.image-modal .image-slide-container').removeClass('active');
                    previousImage.addClass('active');
                    $('.image-modal .large-image').attr('src', previousImage.find('img').attr('src'));
                }
            });

        $(document).on('click',
            '.image-modal #next-image',
            function () {
                var nextImage = $('.image-slide-container.active').next('div');
                if (nextImage.length > 0) {
                    $('.image-modal .image-slide-container').removeClass('active');
                    nextImage.addClass('active');
                    $('.image-modal .large-image').attr('src', nextImage.find('img').attr('src'));
                }
            });

        $(document).ready(adjustModal(110));
        $(window).resize(adjustModal(110));
    }

    $(document).on('click',
        '.prd-altImage',
        function () {
            var thistab = $(this);
            $('#prd-image').animate({
                opacity: "hide",
            },
                500,
                function () {
                    $('#prd-image').attr('src', thistab.attr('data-image'));
                }).animate({
                    opacity: "show"
                },
                500,
                function () {
                    $('.prd-altImage').removeClass('active');
                    thistab.addClass('active');
                });
        });

    // Products Pages
    if (isCurrentPage('/products/') || isCurrentPage('/printer-finder')) {
        $('.pg-entry').hover(
            function () {
                $(this).find('.pg-compare').removeClass('g-v-h');
            },
            function () {
                if ($(this).find('.fa').hasClass('fa-square-o')) {
                    $(this).find('.pg-compare').addClass('g-v-h');
                }
            }
        );

        $(document).on('click',
            '.pg-compare-select',
            function () {
                if ($(this).find('i').hasClass('fa-square-o')) {
                    if ($('.pg-compare-count:first').html() == '4') {
                        alert('4 only');
                        return false;
                    }
                    $(this).find('i').removeClass('fa-square-o').addClass('fa-check-square-o');
                } else {
                    $(this).find('i').removeClass('fa-check-square-o').addClass('fa-square-o');
                }
                $('.pg-compare-count').html($('.pg-products .fa-check-square-o').length);
                checkCompareCount();
            });
    }

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
                    if (ajaxurl != undefined) {
                        $.ajax({
                            url: ajaxurl,
                            dataType: 'json',
                            traditional: true,
                            type: 'POST',
                            cache: false,
                            async: true,
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

                // close any open forms
                closeMe.each(function () {
                    var closeSection = $(this);
                    closeSection.find('.collapsable-detail:first').slideUp(400,
                        function () {
                            if (ajaxurl != undefined) {
                                closeSection.find('.collapsable-detail:first').empty();
                            }
                            closeSection.find('.toggle-section:first').html('Open <i class="fa fa-chevron-down"></i>');
                        });
                });

                if (!isCurrentPage('/myaccount') && openMe) {
                    if (ajaxurl != undefined) {
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

    // Checkout Pages
    if (isCurrentPage('/checkout')) {

        $('input[type="submit"], button, a').on('click', function (e) {
            if (e.ctrlKey && e.shiftKey) {
                return false;
            }
        });

        if (typeof paypal != 'undefined' && !isCurrentPage('/checkout/stage2')) {
            renderPaypalButton();
        }
    }

    if (isCurrentPage('/checkout/stage1') || isCurrentPage('/misc/accountapplication')) {
        $(document).on('click',
            '#bill-manual-address',
            function () {
                $('#co-billadd-fields').removeClass('g-d-n');
                $('#co-acc-billadd-fields').removeClass('g-d-n');
                $('#bill-manual-address').addClass('g-d-n');
            });
    }

    if (isCurrentPage('/checkout/stage1')) {

        $(document).on('change', '#delivery-address-search, #billing-address-search', function () {
            if ($(this).attr("id") == "delivery-address-search") {
                sessionStorage.setItem("pcaIsDeliveryAddress", true);
            } else {
                sessionStorage.setItem("pcaIsDeliveryAddress", false);
            }
        });

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

        $('input', '#co-stage1').blur(function () {
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
                var cobilladdfields = $('.co-paym-button.selected').attr('data-id') == "AccountApplication" ? "#co-acc-billadd-fields" : "#co-billadd-fields";

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

                if (section === 'CreditDebit' || section === "PayPal") {
                    $('#co-stage1').attr('action', '/checkout/stage2');
                    $('#co-submit-button > button').html('Continue To Payment');
                } else {
                    $('#co-stage1').attr('action', '/checkout/stage3');
                    $('#co-submit-button > button').html('Place Order');
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
                    $('#co-po-ref').addClass('g-d-n');
                }

                if (section === "AccountApplication") {
                    $('#co-accountapplication > div:not(#co-company-id), #co-credit-tc, #co-credit-billing,  #co-acc-billadd-search, #co-acc-billadd-fields, #bill-acc-manual-address').find('input').each(function () {
                        $(this).prop('disabled', false);
                    });

                    $('#co-billadd-fields').find('input').each(function () {
                        $(this).prop('disabled', true);
                    });

                    if ($('#AccountApplicationDetails_CustomerType').val() == "2") {
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

                if (section === 'Account' || section === "PayPal") {
                    $('.co-bill-address').addClass('g-d-n');
                    $("html, body").animate({ scrollTop: $(document).height() }, 1000);
                } else {
                    var scrollToContainer = $('.co-paym-addinfo.selected');
                    if (scrollToContainer.length == 0) {
                        scrollToContainer = $('.co-bill-address').hasClass('g-d-n') ? $('.co-acc-bill-address') : $('#co-bill-search');
                    }
                    $('html,body').animate({
                        scrollTop: scrollToContainer.offset().top - 30
                    }, 1000);
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

            if (type == "2" || type == "3") {
                $('.co-paym-accapp').show();
                if (type == "3") {
                    $('#co-company-id').hide();
                    $('#co-company-id input').prop('disabled', true);
                } else {
                    $('#co-company-id').show();
                    $('#co-company-id input').prop('disabled', false);
                }
                $('#co-credit-tc input').prop('disabled', false);
            } else {
                $('.co-paym-accapp').hide();
                $('#co-accountapplication').find('input').each(function () {
                    $(this).prop('disabled', true);
                });
                $('#co-accountapplication').find('select').each(function () {
                    $(this).prop('disabled', true);
                });
                $('#co-credit-tc input').prop('disabled', true);
            }
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
                        //if (data.savereturn.IsSuccess) {
                        $('#delivery-options').fadeOut(300,
                            function () {
                                $('#delivery-options').html(data.savereturn.Html);
                                $('#delivery-options').fadeIn(300);
                                $("input:radio[name='CheckoutDetails.DeliveryServiceId']:checked").trigger('click');
                            });
                        //}
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Checkout/ChangePostCode/", xhr, textStatus, thrownError);
                    }
                });
            });

        $(document).on('click',
            '.delivery-method',
            function () {
                var delServiceId = $("input[name=CheckoutDetails\\.DeliveryServiceId]:checked").val();
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
                        if ($('#CheckoutDetails_PaymentMethod').val() != "") {
                            $('#co-submit-button').removeClass('g-d-n');
                            $('#co-privacy-notice').removeClass('g-d-n');
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
    }

    if (isCurrentPage('/checkout/stage2')) {
        $('input[type=radio][name="CheckoutDetails_SagePayCardId"]').change(function () {
            var cardid = this.value;
            var cardtype = $('#CardId_' + cardid).attr('data-cardtype');
            $.ajax({
                url: "/Checkout/SagePayChangeCard/",
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
                    $('#CheckoutDetails_CardType').val(cardtype);
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/SagePayChangeCard/", xhr, textStatus, thrownError);
                }
            });
        });

        $('input[name="CheckoutDetails_SagePayCardId"]').first().change();

        $('input#CheckoutDetails_SaveThisCard').change(function () {
            var saveTheCard = $('#CheckoutDetails_SaveThisCard').is(":checked");
            $.ajax({
                url: "/Checkout/SagePayChangeSaveCard/",
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    saveTheCard: saveTheCard
                },
                async: false,
                success: function (data) {
                    $('#SagePayIFrame').attr('src', $('#SagePayIFrame').attr('src'));
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Checkout/SagePayChangeSaveCard/", xhr, textStatus, thrownError);
                }
            });
        });

        $(document).on('click',
            '#co-place-order',
            function () {
                if ($(this).attr('type') == "button") {
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
            '[id^="deleteCard"]',
            function () {
                var cardid = $(this);
                $.ajax({
                    url: "/Checkout/SagePayRegistration/delete?cardid=" + cardid.attr('id').split('-')[1],
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    async: false,
                    success: function (data) {
                        cardid.closest('tr').remove();
                        // Ensure a card is selected
                        var cardselected = false;
                        $('[name="CheckoutDetails_SagePayCardId"]').each(function () {
                            if ($(this).is(':checked')) {
                                cardselected = true;
                            }
                        });
                        if (!cardselected) {
                            if ($('[name="CheckoutDetails_SagePayCardId"]:first').length > 0) {
                                $('[name="CheckoutDetails_SagePayCardId"]:first').prop('checked', true);
                                $('[name="CheckoutDetails_SagePayCardId"]:first').change();
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

        if ($('#co-section-paypal').length > 0) {

            var env = 'sandbox';
            if (isCurrentPage('https://www')) {
                env = 'production';
            }
            paypal.Button.render({
                env: env,
                style: {
                    size: 'medium',
                    color: 'gold',
                    shape: 'rect'
                },
                payment: function (resolve, reject) {

                    var CREATE_PAYMENT_URL = '/checkout/paypalpayment/';

                    paypal.request.post(CREATE_PAYMENT_URL).then(function (data) {
                        resolve(data.id);
                    }).catch(function (err) {
                        reject(err);
                    });
                },
                onAuthorize: function (data) {

                    // Note: you can display a confirmation page before executing

                    var EXECUTE_PAYMENT_URL = '/checkout/paypalexecute';

                    paypal.request.post(EXECUTE_PAYMENT_URL,
                        { paymentID: data.paymentID, payerID: data.payerID, paypalType: 'checkout' })
                        .then(function (data) {
                            //$('#co-stage2').submit();
                        }).catch(function (err) {
                            location.href = "/checkout/paypalerror/";
                        });
                }
            },
                '#paypal-button');
        }

        var iframeContent;
        var saveChecked = false;

        $('#chk-save').click(function (e) {
            saveChecked = true;
        });

        $('#SagePayIFrame').on('load', function () {
            if ($('#SagePayIFrame').attr('src') == '/Checkout/SagePayRegistration/new') {

                $('#show-saved-cards').show();
                $('#show-saved-cards').show();

                if (!saveChecked) {
                    if (iframeContent != null && $('#SagePayIFrame') != iframeContent) {
                        $('#show-saved-cards').hide();
                        $('#chk-save').hide();
                    }
                }

                iframeContent = $('#SagePayIFrame');
                saveChecked = false;
            } else {
                iframeContent = null;
            }
        });
    }

    // Handle back button behavoir in the checkout 
    if (isCurrentPage('/checkout/stage1')) {
        sessionStorage.costatus = "Started";
    }

    if (isCurrentPage('/checkout/stage2') || isCurrentPage('/checkout/stage3')) {
        if (sessionStorage.costatus != "Started") {
            // Redirect to basket
            location.href = '/checkout/';
        }
    }

    if (isCurrentPage('/checkout/stage3')) {
        sessionStorage.costatus = "Ended";
        if (!!navigator.userAgent.match(/Version\/[\d\.]+.*Safari/) == false) {
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

    // My Account Pages
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
    }

    // Sorting
    if (isCurrentPage('/search-results') || isCurrentPage('/products/') || isCurrentPage('/printer-finder')) {
        var entries;
        var container;

        if (isCurrentPage('/search-results')) {

            entries = $(".pl-entry");
            container = $('.pl-products > .clearfix').next();

        } else if (isCurrentPage('/products/')) {

            entries = $(".pg-entry");
            container = $('.pg-products');

        } else if (isCurrentPage('/printer-finder')) {

            entries = $(".pg-entry");
            container = $('.pg-products');
        }

        $(document).on('changed.bs.select',
            '.sortResults',
            function () {
                var $divs = entries;
                var sortMethod = $(this).val();
                var alphabeticallyOrderedDivs;

                if (sortMethod == 1 || sortMethod == 2) {
                    alphabeticallyOrderedDivs = $divs.sort(function (a, b) {
                        if (sortMethod == 1) {
                            return $(a).find(".productName").text().toUpperCase().
                                localeCompare($(b).find(".productName").text().toUpperCase());
                        } else {
                            return $(b).find(".productName").text().toUpperCase().
                                localeCompare($(a).find(".productName").text().toUpperCase());
                        }
                    });
                    //$(alphabeticallyOrderedDivs).insertAfter(container);
                    $(container).html(alphabeticallyOrderedDivs);
                } else if (sortMethod == 3 || sortMethod == 4) {
                    var numericallyOrderedDivs = $divs.sort(function (a, b) {
                        var aA = parseFloat($(a).find(".price").text());
                        var bB = parseFloat($(b).find(".price").text());
                        if (aA > bB)
                            return sortMethod == 3 ? 1 : -1;
                        if (aA < bB)
                            return sortMethod == 3 ? -1 : 1;
                        return 0;
                    });
                    //$(numericallyOrderedDivs).insertAfter(container);
                    $(container).html(numericallyOrderedDivs);
                } else {

                    location.reload();
                }

                $("img.lazy").lazyload();
                $('.pg-entry').hover(
                    function () {
                        $(this).find('.pg-compare').removeClass('g-v-h');
                    },
                    function () {
                        if ($(this).find('.fa').hasClass('fa-square-o')) {
                            $(this).find('.pg-compare').addClass('g-v-h');
                        }
                    }
                );

                return false;
            });
    }

    // Printer Finder Page
    if (isCurrentPage('/printer-finder')) {

        $('#pl-product-count').text($('.pg-entry').not('.g-d-n').length);

        $(document).on('click',
            '.wizardNext',
            function () {
                var monoOrColour = $("input:radio[name='checkboxColourOrMono']:checked").val();
                var paperSize = $("input:radio[name='checkboxPaperSize']:checked").val();
                var functionType = $("input:radio[name='checkboxFunctions']:checked").val();
                var twoSided = $("input:radio[name='checkboxTwoSided']:checked").val();
                var connectivity = $("input:radio[name='checkboxConnectivity']:checked").val();

                var filteredJson = jsonData.Printers.filter(function (row) {
                    if (monoOrColour.includes(row.Colour) &&
                        paperSize.includes(row.Pagesize) &&
                        functionType.includes(row.Function) &&
                        twoSided.includes(row.Duplex)) {

                        switch (connectivity) {
                            case 'WIFI':
                                if (row.Wifi == 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            case 'MOBILE':
                                if (row.Mobile == 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            case 'NETWORK':
                                if (row.Network == 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            default:
                                return true;
                        }
                    } else {
                        return false;
                    }
                });

                var productIds = [];
                $.each(filteredJson,
                    function (i, item) {
                        productIds.push(parseInt(item.StockRef));
                    });

                $('.pg-entry').each(function () {
                    $(this).removeClass('g-d-n');
                    var prodId = parseInt($(this).find('.atb-add').data('productid'));

                    if (productIds.indexOf(prodId) == -1) {
                        $(this).addClass('g-d-n');
                    }
                });

                $('#pl-product-count').text($('.pg-entry').not('.g-d-n').length);

                return false;
            });

        $(document).on('click',
            '.showPrinters',
            function () {
                $('html, body').animate({
                    scrollTop: $('#pl-product-count').offset().top - 20
                },
                    1000);
            });
    }

    // Grid Pages and Printer Finder Page
    if (isCurrentPage('/products/') || isCurrentPage('/printer-finder')) {
        $(document).ready(adjustModal(178));
        $(window).resize(adjustModal(178));
    }

    // Grid Pages
    if (isCurrentPage('/products/')) {
        checkCompareCount();

        $(document).on('mouseenter',
            '.compare-product',
            function () {
                $(this).find('.delete-compare').removeClass('g-d-n');
            });

        $(document).on('mouseleave',
            '.compare-product',
            function () {
                $(this).find('.delete-compare').addClass('g-d-n');
            });

        $(document).on('click',
            '.delete-compare',
            function () {
                var id = $(this).attr('data-productid');
                $('td[data-productid=' + id + ']').addClass('g-d-n');
            });
    }

    // Grid / Model / Search Pages
    if (isCurrentPage('/products/') || isCurrentPage('/model/') || isCurrentPage('/search-results')) {
        triggerFilter();
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
        liveagent.init('https://d.la2w2.salesforceliveagent.com/chat', '57220000000Cc76', '00D20000000mwfM');
    } catch (err) {

    }

    $(document).on('click',
        '.liveChatTrigger',
        function () {
            if ($('#liveagent_button_online1').length != 0) {
                $('#liveagent_button_online1').trigger('click');
            } else {
                $('#liveagent_button_online2').trigger('click');
            }
        });
});

// Functions

function findObject(obj, key, val) {
    var objects = [];
    for (var i in obj) {
        if (!obj.hasOwnProperty(i)) continue;
        if (typeof obj[i] == 'object') {
            objects = objects.concat(findObject(obj[i], key, val));
        } else if (i == key && obj[key] == val) {
            objects.push(obj);
        }
    }
    return objects;
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

        if (typeof ($("#signin-form [type='submit']").attr('disabled')) != 'undefined') {
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

        if (typeof ($("#signup-form [type='submit']").attr('disabled')) != 'undefined') {
            $("#signup-form [type='submit']").removeAttr('disabled');
        }
    }
}

function disableSubmit() {
    $(this).find("input[type='submit']").attr('disabled', true);
    setTimeout(function () {
        $(this).find("input[type='submit']").attr('disabled', false);
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
    }
    else if (response.savereturn.Message == "3") {

        getPopupContent('ForgotPassword', null, function (sr) {
            $('.ident-reset-password').append(sr.Html);

            $('#password-reset-email').val($('#SignIn_UserName').val());

            displayErrorMessage('.ident-reset-password', response.savereturn.Html);

            $('.ident-reset-password, .ident-reset-back').fadeIn(500);
            $('.ident').hide();
        });
    }
    else {
        displayErrorMessage('.ident', response.savereturn.Html);

        if (response.savereturn.Message == "1") {
            $('.check-existing').prop('checked', true).change();
        }
    }

    var attr = $(this).find("input[type='submit']").attr('disabled');

    if (attr == 'disabled' || attr == true) {
        $(this).find("input[type='submit']").attr('disabled', false);
    }
}

function newsletterSignUpComplete() {
    launchPopup('NewsletterConfirmation', 'popup', 'sm', '');
}

function myAccountUpdateComplete(data) {

    var formId = $(this).attr('id');
    var errClass = formId == 'updateDetails' ? '.update-details' : '.update-address';

    $('.validation-summary-errors').hide();

    if (!data.responseJSON.savereturn.IsSuccess) {
        $('#password-success').hide();

        if (data.responseJSON.savereturn.Message == 'Email') {
            displayErrorMessage(errClass, 'Email is already in use!');
        } else if (data.responseJSON.savereturn.Message == 'Password') {
            displayErrorMessage(errClass, 'Current password is incorrect.');
        }
    } else {
        removeErrorMessage(errClass);
        $('#password-success').show();
    }
}

function popupFormComplete(data) {
    //if (data.responseJSON.savereturn.IsSuccess) {
    // Close the popup
    $('#popup .fa-times').trigger('click');
    $('.modal').modal('hide');
}

function askAQuestionSuccess(data) {
    if (!data)
        return false;

    removeErrorMessage('.err-email, .err-question');

    var sr = data.savereturn;

    if (!sr.IsSuccess) {

        if (sr.Message == "Email") {
            displayErrorMessage('.err-email', sr.Html);
        }
        else if (sr.Message == "Question") {
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
        if (obj.parents('.checkbox-validation-error').length == 0) {
            obj.wrap('<div class="checkbox-validation-error"></div>');
        }
    } else {
        if (obj.parents('.checkbox-validation-error').length > 0) {
            obj.unwrap();
        }
    }
}

function tidyUpStage1() {

    $('select:enabled').each(function () {
        if (!$(this).valid() && $(this).hasClass('input-validation-error')) {
            $(this).prevAll('button').css('border', '2px solid #ff6666');
        } else {
            $(this).prevAll('button').css('border', '1px solid #ccc');
        }
    });

    if ($('#CheckoutDetails_PaymentMethod').val() == 'PayPal' && $('#IsAuthenticated').val() == '0') {
        if ($('#CheckoutDetails_BillingAddress_Line1').val() == '') {
            $('#CheckoutDetails_BillingAddress_Line1').val($('#CheckoutDetails_DeliveryAddress_Line1').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line2').val() == '') {
            $('#CheckoutDetails_BillingAddress_Line2').val($('#CheckoutDetails_DeliveryAddress_Line2').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line3').val() == '') {
            $('#CheckoutDetails_BillingAddress_Line3').val($('#CheckoutDetails_DeliveryAddress_Line3').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line4').val() == '') {
            $('#CheckoutDetails_BillingAddress_Line4').val($('#CheckoutDetails_DeliveryAddress_Line4').val());
        }
        if ($('#CheckoutDetails_BillingAddress_Line5').val() == '') {
            $('#CheckoutDetails_BillingAddress_Line5').val($('#CheckoutDetails_DeliveryAddress_Line5').val());
        }
        if ($('#CheckoutDetails_BillingAddress_PostCode').val() == '') {
            $('#CheckoutDetails_BillingAddress_PostCode').val($('#CheckoutDetails_DeliveryAddress_PostCode').val());
        }
    }
    return true;
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

function autocompleteSearch(query, element) {
    $.ajax({
        url: "/search/autocomplete?keyword=" + query
    }).done(function (data) {
        element.closest('form').next().html(data).removeClass('g-d-n');
    });
}

function applyFilter() {
    // Build the selector
    var selector = '';
    $('.fltr-filters .fltr-group').each(function () {
        var comma = '';
        var selector1 = '';

        $(this).find('input[id^="att"]').each(function () {
            if ($(this).is(':checked')) {
                var id = $(this).attr('id');
                var idarray = id.split('-');
                var att = 'data-att-' + idarray[1];
                selector1 += comma + '[data-att-' + idarray[1] + '*="#' + idarray[2] + '#"]';
                if (comma == '') {
                    comma = ',';
                }
            }
        });
        if (selector1 != '') {
            selector += ".filter('" + selector1 + "')";
        }
    });

    if (isCurrentPage('model/') || isCurrentPage('/search-results')) {

        // Set the selector
        var sel = eval("$('.pl-products > div > .pl-entry')" + selector);

        // Hide products and headers
        //$('.pl-products > div > .pl-entry').hide();
        //$('.pl-products > div > .pl-sub-banner').hide();

        // Show the selected products, headers and set counter
        sel.removeClass('g-d-n');
        sel.parent().find('.pl-sub-banner').removeClass("g-d-n");
        //$('#pl-product-count').html(sel.length);
    }
    if (isCurrentPage('products/')) {

        // Set the selector
        var sel = eval("$('.pg-products > .pg-entry')" + selector);

        // Hide products and headers
        //$('.pg-products > .pg-entry').hide();

        // Show the selected products, headers and set counter
        sel.removeClass("g-d-n");
        //$('#pg-product-count').html(sel.length);
        $("img.lazy").lazyload();
    }

    refreshFilterCounts();
}

function applyPriceFilter(sel) {
    var priceMin = $('#minPrice').val() == '' ? 0 : Number($('#minPrice').val());
    var priceMax = $('#maxPrice').val() == '' ? 9999999 : Number($('#maxPrice').val());

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
    if (type == 'password-change') {
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
    if (type == 'password-change') {
        $('#OldPassword').val('');
    } else {
        $('#Password').val('');
    }
    return ret;
}

function openUtilityBar(sectionname) {
    sectionname = typeof sectionname != 'undefined' ? sectionname : 'basket';
    $('#' + sectionname + ' >  .header').trigger('click');
}

function scrollToSelector(selector, furtherOffset) {
    furtherOffset = typeof furtherOffset !== 'undefined' ? furtherOffset : 0;
    var elem = $(selector);
    $('html, body').animate({
        scrollTop: $(elem).offset().top - 20 - furtherOffset
    },
        400);
}

function checkManufacturer(manu) {
    if (manu == "" || manu == "0") {
        $('#pop-brands').removeClass('g-d-n');
    } else {
        $('#pop-brands').addClass('g-d-n');
    }
    $('#wiz-find').attr('disabled', 'disabled');
}

function checkEquipment(equip) {
    if (equip == "" || equip == "0") {
        $('#wiz-find').attr('disabled', 'disabled');
    } else {
        $('#wiz-find').removeAttr('disabled');
    }
}

function populateManufacturer(data) {
    $('.wiz-manufacturer-image > img').attr('src',
        data.cdn +
        '/Images/BannerLogos/' +
        ($("#wiz-manufacturer").data("kendoDropDownList").text().replace(/ /g, '-').toLowerCase()) +
        '.jpg');
    if ($("#wiz-manufacturer").data("kendoDropDownList").text() == '') {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            $('#wiz-cartridgetype').val().replace(/ /g, '-').toLowerCase() +
            '.jpg');
    } else {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            ($("#wiz-manufacturer").data("kendoDropDownList").text().replace(/ /g, '-').toLowerCase()) +
            '.jpg');
    }
    var manuname = '';
    if ($("#wiz-manufacturer").data("kendoDropDownList").text() != '') {
        manuname = $("#wiz-manufacturer").data("kendoDropDownList").text();
    }
    $('.pw-manu-name').html(manuname);

    if ($('#pw-altcart-link').length > 0) {
        var oldbrand = $('#pw-altcart-link').attr('href').split('-cartridges')[1];
        if (oldbrand == '/') {
            $('#pw-altcart-link').attr('href', $('#pw-altcart-link').attr('href') + manuname + "/");
        } else {
            $('#pw-altcart-link').attr('href', $('#pw-altcart-link').attr('href').replace(oldbrand, manuname + "/"));
        }
    }
}

function populateFamily(data) {
    $('#wiz-family').empty();
    $('#wiz-family').append('<option value="0">Select Printer Family or Series</option>');
    $.each(data.familylist,
        function (i, family) {
            $('#wiz-family').append('<option value="' + family.Value + '">' + family.Text + '</option>');
        });
}

function populateEquipment(data) {
    $('#wiz-equipment').empty();
    $('#wiz-equipment').append('<option value="0">Select Printer Model</option>');
    $.each(data.equiplist,
        function (i, equip) {
            var atts = '';
            $.each(equip.Data,
                function (key, val) {
                    atts = atts + ' ' + key.replace(/\_/g, '-') + '="' + val + '"';
                });
            $('#wiz-equipment').append('<option value="' + equip.Value + '"' + atts + '>' + equip.Text + '</option>');
        });
}

function populatePopularPrinters(data) {
    $('#pop-printers').empty().append(data.popprint);
}

function populatePopularCartridges(data) {
    $('#pop-cartridges').empty().append(data.popcart);
    $('.mini-product-container').jScrollPane({ showArrows: true });
}

function populateManuText(data) {
    $('#manu-text').empty().append(data.manutext);
}

function populateEquipLinks(data) {
    $('#printer-links').empty().append(data.printerlinks);
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

                setDeferredImages();

                if (options) {
                    $('#' + popupid).modal(options);
                } else {
                    $('#' + popupid).modal('show');
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
        if (window.location.hostname == 'localhost') return;
        this.src = "//" + window.location.hostname + "/version1/cdn/Images/noimage.jpg";
    });

    $('.deferImage').each(function () {
        if ($(this).attr('src') != $(this).attr('data-original')) {
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
            name: tooltipname,
        },
        async: false,
        success: function (e) {
            if (e.savereturn.IsSuccess) {
                $('body').append(e.savereturn.Html);
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/Misc/HighlightTooltip/", xhr, textStatus, thrownError);
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
            $('.mini-product-container').jScrollPane({ showArrows: true });

            // moreLess is "View More" in collapsed mode and "View Less" in expanded mode
            // for e.g. order history
            section.find('.moreLess').each(function () {
                toggleCollapsedMode($(this),
                    $(this).attr('data-num-items'),
                    $(this).attr('data-buttclass'),
                    $(this).attr('data-scroll-offset'),
                    false);
            });
        });
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}

function isCurrentPage(pageUrl) {
    return window.location.href.toLowerCase().indexOf(pageUrl) > -1;
}

function adjustModal(offset) {
    var heightModal = $(window).height() - offset;
    $(".modal-scroll").css({ "height": heightModal, "overflow-y": "auto" });
}

function checkCompareCount() {
    if ($('.pg-products .fa-check-square-o').length == 0) {
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
    evt = (evt) ? evt : window.event;
    var charCode = (evt.which) ? evt.which : evt.keyCode;
    if (charCode > 31 && (charCode < 48 || charCode > 57) && charCode != 46) {
        return false;
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
    $('#CheckoutDetails_UseSavedCard').val('True');
    $('#SagePayIFrame').attr('src', '');
}

function deferScriptLoad(fileType, fileAddress, fileOptions, fileInsert) {
    //If the browser supports attachEvent (e.g. IE)
    if (window.attachEvent) {
        //Set the script to run onload
        window.attachEvent("onload", function () { async_load(fileType, fileAddress, fileOptions, fileInsert); });
        //If the browser does not support attachEvent (e.g. FireFox)
    } else {
        //Set the script to run onload
        window.addEventListener("load",
            function () { async_load(fileType, fileAddress, fileOptions, fileInsert); },
            false);
    }
}

function changeBasketQty(ref, qty) {
    if (qty == 0) {
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
                    $("#qty-" + ref.toString()).data("kendoNumericTextBox").destroy();
                    refreshVbFields(data);
                    renderPaypalButton();
                }
            },
            error: function (xhr, textStatus, thrownError) {
                logAjaxScriptError("/Checkout/BasketChangeQty/", xhr, textStatus, thrownError);
            }
        });
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
    $('#vbBasketDetails').html(data.savereturn.Html);
    $('.basketQuantity').html(data.basketQuantity);
    $('.basketTotal').html(data.basketTotal);
    $('.basket-counter').html(data.basketQuantity);
    var basketSummary = $.parseHTML(data.basketSummary);
    $('#basket').html($(basketSummary).next('#basket').html());
}

function startTime() {
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

    if (today.getHours() == 17 && today.getMinutes() == 30 && today.getSeconds() == 0) {
        //var dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        //var monthNames = [
        //    "January", "February", "March", "April", "May", "June", "July", "August", "September", "October",
        //    "November", "December"
        //];
        //var ordinalNames = [
        //    "", "st", "nd", "rd", "th", "th", "th", "th", "th", "th", "th", "th", "th", "th", "th", "th", "th", "th",
        //    "th", "th", "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th", "th", "st"
        //];
        switch (dayNumber) {
            case 0:
                break;
            case 4:
                dCutOffDate.setTime(dCutOffDate.getTime() + (24 * 60 * 60 * 1000));
                //dDeliveryDate.setTime(dDeliveryDate.getTime() + (72 * 60 * 60 * 1000));
                break;
            case 5:
                dCutOffDate.setTime(dCutOffDate.getTime() + (72 * 60 * 60 * 1000));
                //dDeliveryDate.setTime(dDeliveryDate.getTime() + (24 * 60 * 60 * 1000));
                break;
            case 6:
                break;
            default:
                dCutOffDate.setTime(dCutOffDate.getTime() + (24 * 60 * 60 * 1000));
            //dDeliveryDate.setTime(dDeliveryDate.getTime() + (24 * 60 * 60 * 1000));
        }
        //$(".cl_clockText1Italic").text(dayNames[dDeliveryDate.getDay()] +
        //    ' ' +
        //    dDeliveryDate.getDate() +
        //    ordinalNames[dDeliveryDate.getDate()] +
        //    ' ' +
        //    monthNames[dDeliveryDate.getMonth()]);
        //bIncrement = false;
    }

    countDown[0] = Math.floor(((dCutOffDate - today) / 1000) / 3600);
    countDown[1] = Math.floor((((dCutOffDate - today) / 1000) - (countDown[0] * 3600)) / 60);
    countDown[2] = Math.floor(((dCutOffDate - today) / 1000) - (countDown[0] * 3600) - (countDown[1] * 60));

    if (dCutOffDate < today) {
        countDown[0] = 0;
        countDown[1] = 0;
        countDown[2] = 0;
    }

    //if ($("#cl_clockTime1").length > 0) {
    //    // add a zero in front of numbers<10
    //    countDown[0] = checkTime(countDown[0]);
    //    countDown[1] = checkTime(countDown[1]);
    //    countDown[2] = checkTime(countDown[2]);
    //    $("#cl_clockTime1").html('<span class="cl_clockTextHM">' + countDown[0] + '</span><span class="cl_clockTextHM">' + countDown[1] + '</span><span class="cl_clockTextHM">' + countDown[2] + '</span>');
    //}

    if (countDown[0] == 1) {
        txtHour = " hour ";
    } else {
        txtHour = " hours ";
    }
    if (countDown[1] == 1) {
        txtMinute = " min ";
    } else {
        txtMinute = " mins ";
    }
    $(".cutoffCountdownFalse").html(countDown[0] + txtHour + ' ' + countDown[1] + txtMinute);

    setTimeout(function () { startTime() }, 500);
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
    var responseText = "";

    if (xhr == null || xhr.responseText == null || xhr.responseText == "") {
        return;
    } else {
        if (xhr.responseText.length > 5000) {
            responseText = xhr.responseText.substring(0, 5000);
        } else {
            responseText = xhr.responseText;
        }
    }

    var e = new Error(url + ": " + xhr.statusText.toString() +
        ", thrownError: " +
        thrownError.toString() +
        ", textStatus: " +
        textStatus.toString() +
        ", responseText: " +
        responseText.toString());
    logScriptError(e);
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
        async: false,
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

    if (triggered && visibleEntries != null) {
        console.log(visibleEntries.length);
        $(visibleEntries).find('img.lazy').lazyload();
    }

    refreshFilterCounts();
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

function renderPaypalButton() {

    var env = 'sandbox';
    if (isCurrentPage('https://www')) {
        env = 'production';
    }

    var isValid = true;

    paypal.Button.render({
        env: env,
        style: {
            size: 'responsive',
            color: 'gold',
            shape: 'rect',
            tagline: false
        },
        validate: function (actions) {
            isValid = checkSessionExists("C_IsInCheckout") == false ? true : false;
        },
        payment: function (resolve, reject) {
            if (isValid) {
                sessionStorage.costatus = "Started";
                var CREATE_PAYMENT_URL = '/checkout/paypalpayment/';
                paypal.request.post(CREATE_PAYMENT_URL).then(function (data) {
                    if (!data) {
                        $('.IsInCheckout').val('true');
                        location.href = "/checkout?pm=IsInCheckout";
                    } else {
                        if (data == "refresh") {
                            location.href = "/checkout";
                        } else {
                            resolve(data.id);
                        }
                    }
                }).catch(function (err) {
                    reject(err);
                });
            } else {
                $('.IsInCheckout').val('true');
                location.href = "/checkout?pm=IsInCheckout";
            }
        },
        onAuthorize: function (data) {
            // Note: you can display a confirmation page before executing

            observer.disconnect();

            var EXECUTE_PAYMENT_URL = '/checkout/paypalexecute';

            paypal.request.post(EXECUTE_PAYMENT_URL,
                { paymentID: data.paymentID, payerID: data.payerID, paypalType: 'viewBasket' })
                .then(function (data) {
                    if (data.state && data.state == "approved") {
                        $("#paypal-form").submit();
                    } else {
                        var rpl = 'errormessage_' + encodeURI(data.failure_reason);
                        location.href = "/checkout/?pm=PayPalError&sz=md&rpl=" + rpl;
                    }
                }).catch(function (err) {
                    var rpl = 'errormessage_' + encodeURI(data.failure_reason);
                    location.href = "/checkout/?pm=PayPalError&sz=md&rpl=" + rpl;
                });

            $('body').append('<div class="modal-backdrop fade" style="background-color: rgba(0, 0, 0, 0.5); opacity: 1;"><div class="g-ps-r"><div class="g-ps-f" style="top: 50%; left: 50%;"><i class="fa fa-circle-o-notch fa-spin fa-3x fa-fw g-fc-st"></i><div class="g-fc-st" style="margin-left: -30px; margin-top: 10px;">Processing Payment...</div></div></div></div>');
        }
    },
        '#paypal-button');

    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            if ($(mutation.removedNodes[0]).hasClass('paypal-checkout-sandbox') && $('.IsInCheckout').val() != 'true') {
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

function async_load(fileType, fileAddress, fileOptions, fileInsert) {
    //The code block being created (e.g. script or link)
    var s = document.createElement(fileType);
    //Finds the tag in the page and places the new code block before it
    var x = document.getElementsByTagName(fileType)[0];
    //If the file type is link (stylesheet)
    if (fileType == "link") {
        //Set the type, rel, href and media attributes of the code block
        s.type = "text/css";
        s.rel = "stylesheet";
        s.href = fileAddress;
        s.media = "print";
        x.parentNode.insertBefore(s, x);
        //If the file type is script
    } else if (fileType == "script") {
        //Set the type, async (only supported by HTML5) and src attributes of the code block
        s.type = "text/javascript";
        s.async = true;
        s.src = fileAddress;
        x.parentNode.insertBefore(s, x);
        //If the file type is img
    } else if (fileType == "img") {
        //Set the type, async (only supported by HTML5) and src attributes of the code block
        s.src = fileAddress;
        for (var prop in fileOptions) {
            s[prop] = fileOptions[prop];
        }
        x = document.getElementById(fileInsert);
        x.appendChild(s);
    }
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
        if ($(this).hasClass('details') && $(this).css('display') == 'block') {
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

if (isCurrentPage('/checkout/stage1') || isCurrentPage('/checkout/stage2')) {

    $(function () {
        if (checkSessionExists('C_IsInCheckout')) {
            $('.IsInCheckout').val('true');
            location.href = "/checkout?pm=IsInCheckout";
        } else {
            setSession("C_IsInCheckout", true);
        }
    });

    if (navigator.userAgent.match(/(iPad|iPhone|iPod)/g)) {
        window.addEventListener("pagehide", function () {
            if ($('.IsInCheckout').val() != 'true') {
                setSession("C_IsInCheckout", null);
            }
        });

        window.addEventListener("blur", function () {
            if (!$('iframe').is(':focus')) {
                setSession("C_IsInCheckout", null);
                location.href = "/checkout";
            }
        });
    }
    else {
        window.onbeforeunload = function () {
            if ($('.IsInCheckout').val() != 'true') {
                setSession("C_IsInCheckout", null);
            }
        };
    }
}

$(window).on('load', function () {
    setDeferredImages();
});

//-------------------------------------------------- Start of Google Certified Shops --------------------------------
//var gts = gts || [];

//gts.push(["id", "186270"]);
//gts.push(["badge_position", "USER_DEFINED"]);
//gts.push(["badge_container", "ft_gcs"]);
//gts.push(["locale", "en_GB"]);
//if (isCurrentPage('/product/')) {
//    var arrProdURL = location.href.split('-');
//    //alert(arrProdURL[arrProdURL.length - 2]);
//    //gts.push(["google_base_offer_id", "ITEM_GOOGLE_SHOPPING_ID"]);
//    gts.push(["google_base_offer_id", arrProdURL[arrProdURL.length - 2]]);
//}
//gts.push(["google_base_subaccount_id", "9099197"]);
//gts.push(["google_base_country", "GB"]);
//gts.push(["google_base_language", "en"]);

//(function () {
//    var gts = document.createElement("script");
//    gts.type = "text/javascript";
//    gts.async = true;
//    gts.src = "https://www.googlecommerce.com/trustedstores/api/js";
//    var s = document.getElementsByTagName("script")[0];
//    s.parentNode.insertBefore(gts, s);
//})();
//-------------------------------------------------- End of Google Certified Shops ----------------------------------