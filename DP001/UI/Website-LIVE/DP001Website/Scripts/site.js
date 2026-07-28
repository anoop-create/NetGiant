$(function () {
    switch (location.pathname) {
        case "/products":
            $('nav .dropdown:eq(1)').addClass('active');
            break;
        case "/suppliers":
            $('nav .dropdown:eq(1)').addClass('active');
            break;
        case "/competitors":
            $('nav .dropdown:eq(1)').addClass('active');
            break;
    }

    $('.sticky-header').stickyTableHeaders({
        //scrollableArea: $('section.page-form'),
        fixedOffset: 130
    });

    $('.help-popup').tooltip({ container: 'body' });

    $('*[data-autocomplete-url]')
        .each(function () {

            var dataUrl = $(this).data("autocomplete-url");
            var dataSuccess = $(this).data("ajax-success");
            var datahiddenId = $(this).data("ajax-hiddenid");

            $(this).autocomplete({
                source: function (request, response) {
                    var results = new Array();
                    $.ajax({
                        async: false,
                        cache: false,
                        type: "POST",
                        url: dataUrl,
                        data: {
                            "term": request.term,
                            "brandFK": $('#BrandFK').val()
                        },
                        success: function (data) {
                            results = getFunction(dataSuccess, ["xhr", "status"]).apply(this, arguments)
                        }
                    });
                    response(results);
                },
                select: function (event, ui) {
                    $('#' + datahiddenId).val(ui.item.Id);
                }
            }).focus(function () {
                $(this).val('');
                $('#' + datahiddenId).val('');
            });
        });

    /****************************************/
    /* Disable buttons when one is clicked  */
    /****************************************/
    //$(document).on("click", ".form-actions > button[value$='ubmit'], .form-actions > .form-delete, .form-actions > .form-cancel", function () {
    //    debugger;
    //    $(".form-actions > button[value$='ubmit']")
    //        .attr("disabled", "true");
    //    $(".form-actions > .form-delete")
    //        .data("href", $(this).attr("href"))
    //        .attr("href", "javascript:void(0)")
    //        .attr("disabled", "disabled");
    //});

    /**********************************/
    /* Mvc.Grid Helpers               */
    /**********************************/
    //$(document).on('focus', '.grid-filter-input', function (event) {
    //    event.stopPropagation();
    //});

    $(document).on('click', '.grid-header', function () {
        //alert($(this).find('.grid-filter').find('.grid-dropdown').is(":visible"));
        if (!$(this).find('.grid-filter').find('.grid-dropdown').is(":visible")) {


            $(this).find('.grid-filter').triggerHandler('click');
            $('.selectpicker').selectpicker();
            toggleVisibility('#' + $(this).find('.grid-filter').attr('data-name').replace(/\./g, '_') + 'SelectList');
            //alert($(this).width() / 2);
            var popleft = ($(this).width() * -1 / 2) - 150;
            var popbox = $(this).find('.grid-dropdown');
            var poparrow = $(this).find('.grid-dropdown-arrow');
            popbox.css('left', popleft);
            poparrow.css('left', popbox.width() / 2);
            if (popbox.offset().left < 0) {
                var absleft = ($(this).width() * -1) + 20;
                popbox.css('left', absleft);
                poparrow.css('left', '50px');
            }
            if (popbox.offset().left > $(window).width() - popbox.width()) {
                var absleft = (popbox.width() * -1) - 20;
                popbox.css('left', absleft);
                poparrow.css('left', '250px');
            }
        }
    });

    /**********************************/
    /* Off Canvas Helpers             */
    /**********************************/
    $(document).on('click', '.navbar-toggle', function () {
        //The JQuery event that sets 'active' class on and and off is registered before this event
        if ($('.row-offcanvas').hasClass('active')) {
            window.scrollTo(0, 0);
            //$("html, body").animate({ scrollTop: 0 }, "slow");
        }
    });

    $('[data-toggle="offcanvas"]').click(function () {
        $('.row-offcanvas').toggleClass('active');
        //$('.row-offcanvas > div:first').height($('.sidebar-offcanvas').height());
        $('.row-offcanvas.active').height($('.sidebar-offcanvas').height());
    });

    /**********************************/
    /* Notification Helpers           */
    /**********************************/
    $(document).on('click', '#notification-summary', function () {
        if ($('#notification-dropdown').is(':visible')) {
            $.ajax({
                async: false,
                cache: false,
                type: "POST",
                url: "/Log/Summary",
                success: function (data) {
                    $('#notification-dropdown').html(data.html);
                }
            });
        }
    });

});

//*************************//
// Functions               //
//*************************//

function getFunction(code, argNames) {
    var fn = window, parts = (code || "").split(".");
    while (fn && parts.length) {
        fn = fn[parts.shift()];
    }
    if (typeof (fn) === "function") {
        return fn;
    }
    argNames.push(code);
    return Function.constructor.apply(null, argNames);
}

function openDropForm(clickedElement, insertedClass, ajaxUrl, objData, toggleFooter) {
    if ($(insertedClass).length != 0) {
        var openElement = $(insertedClass).prev();
        var sameElement = false;
        //determine if the clicked element is the same as the open element
        if ($(clickedElement).is($(openElement))) {
            sameElement = true;
        }
        //close any open forms
        $(insertedClass).find('form').slideUp(800, function () {
            $(insertedClass).remove();
            //toggle the chevrons and unhide the table footer
            if (toggleFooter) {
                $(clickedElement).closest('table').find('tfoot > tr:first').show();
            }
            $('.fa-chevron-up').removeClass('fa-chevron-up').addClass('fa-chevron-down');
            //if a different form was opened                    
            if (!sameElement) {
                //open form
                loadDropForm(clickedElement, insertedClass, ajaxUrl, objData, toggleFooter);
            }
        })
    } else {
        //open form
        loadDropForm(clickedElement, insertedClass, ajaxUrl, objData, toggleFooter);
    }
}

function loadDropForm(clickedElement, insertedClass, ajaxUrl, objData, toggleFooter) {
    $.ajax({
        url: ajaxUrl,
        dataType: 'html',
        traditional: true,
        type: 'POST',
        cache: false,
        data: objData,
        async: false,
        success: function (data) {
            //hide the table footer
            if (toggleFooter) {
                $(clickedElement).closest('table').find('tfoot> tr:first').hide();
            }
            $(data).insertAfter(clickedElement);
            //validateRuleType();
            $(insertedClass).find('form').hide().slideDown(800, function () {
                //toggle the chevron
                $(clickedElement).find('.fa-chevron-down').removeClass('fa-chevron-down').addClass('fa-chevron-up');
                //re render helpers
                $('.help-popup').tooltip({ container: 'body' });
                $('.selectpicker').selectpicker();
                $('.timeentry').timeEntry({ show24Hours: true });
                $.validator.unobtrusive.parse(insertedClass + " form");
            });

        }
    });
}

function changeChannel(channel) {
    $.ajax({
        url: '/Channel/ChangeChannel',
        dataType: 'html',
        traditional: true,
        type: 'POST',
        cache: false,
        data: { newChannel: channel },
        async: false,
        success: function (data) {
            location.reload(true);
        }
    });
}

var cjip_check;
var cjip_isActive = false;

function checkJobInProgress(redirecturl, id) {
    id = typeof id !== 'undefined' ? id : '#repriceSpinner';
    cjip_check = setInterval(function () {
        checkJobInProgressRepeater(redirecturl, id);
    }, 1000);
}

function checkJobInProgressRepeater(redirecturl, id) {
    $.ajax({
        method: 'POST',
        url: '/Shared/CheckJobInProgress/',
        success: function (data) {
            if (!data.inProgress) {
                clearInterval(cjip_check);
                $('.repriceBtn').removeAttr("style");
                $(id).hide();
                $('#nextRunDate1').show();
                $('#nextRunDate2').hide();
                if (cjip_isActive) {
                    if (redirecturl != '') {
                        location.href = redirecturl;
                    }
                    else {
                        location.reload(true);
                    }
                }
            }
            else {
                $('.repriceBtn').css("pointer-events", "none").css("cursor", "default");
                $(id).show();
                $('#nextRunDate1').hide();
                $('#nextRunDate2').show();
                cjip_isActive = true;
            }
        }
    });
}

function triggerComplete(data) {
    if (data.responseJSON.inProgress) {
        $('.repriceBtn').attr("disabled", "disabled");
        $('#repriceSpinner').show();
        $('#nextRunDate1').hide();
        $('#nextRunDate2').show();
        checkJobInProgress('', '#repriceSpinner');
        $('.sub-menu .dropdown-toggle').trigger('click');
    }
}

function repriceComplete(data) {
    if (data.responseJSON.inProgress) {
        $('.repriceBtn').attr("disabled", "disabled");
        $('#repriceSpinner').show();
        $('#nextRunDate1').hide();
        $('#nextRunDate2').show();
        checkJobInProgress('/Reports/ComparisonStaging', '#repriceSpinner');
        $('.sub-menu .dropdown-toggle').trigger('click');
    }
}

function repriceCompleteCalculatePrices(data) {
    if (data.responseJSON.inProgress) {
        $('.repriceBtn').attr("disabled", "disabled");
        $('#repriceSpinner2').show();
        checkJobInProgress('/PriceRules', '#repriceSpinner2');
        $('.sub-menu .dropdown-toggle').trigger('click');
    }
}

var gtd_cachedTooltipData = Array();
function GetTooltipData(elem, url) {

    var id = elem.data('unique-id');

    if (id in gtd_cachedTooltipData) {
        return gtd_cachedTooltipData[id];
    }

    var localData = "";

    $.ajax(url, {
        data: { id: elem.attr('data-id') },
        async: false,
        success: function (data) {
            localData = data;
        }
    });

    gtd_cachedTooltipData[id] = localData;

    return localData;
}

function toggleFormFieldsState(div, action) {
    $(div + ' input').each(function () {
        $(this).prop('disabled', action);
    })
}

function toggleVisibility(id) {
    if ($(id).is(":visible")) {
        $(id).hide();
    } else {
        $(id).show();
    }
}

function reEnableActions(id) {
    //debugger;
    //id = typeof id !== 'undefined' ? id + " " : "";
    //$(id + ".form-actions > button")
    //    .each(function () {
    //        $(this).removeAttr("disabled");
    //});

    //$('.form-actions > a')
    //    .each(function () {
    //        $(this).attr("href", $(this).data("href"))
    //            .removeAttr("disabled");
    //});
}

function excludeItemFromInventory(obj) {
    $.ajax({
        async: false,
        cache: false,
        method: "POST",
        url: "/ProviderExclusion/Create/",
        data: {
            "competitorId": obj.closest('tr.grid-row').find('#x_CompetitorFK').val(),
            "brandName": obj.closest('tr.grid-row').find('#x_Brand_BrandName').val(),
            "manuPartNo": obj.closest('tr.grid-row').find('#x_ManufacturerPartNo').val(),
            "clientProductId": obj.closest('tr.grid-row').find('#x_ClientProductID').val(),
            "inventoryId": obj.closest('tr.grid-row').find('#x_CompetitorInventoryID').val()
        },
        success: function (data) {
            if (!data.isSuccess) {
                obj.closest('.exclCompetitorInv').find('.errmsg').html(data.msg);
                obj.prop("checked", false);
            } else {
                obj.closest('tr.grid-row').slideUp(800);
            }
        }
    });
}

function deleteExclusion(exclId) {
    $.ajax({
        async: false,
        cache: false,
        method: "POST",
        url: "/ProviderExclusion/Delete/",
        data: {
            "id": parseInt(exclId)
        },
        success: function (data) {
            if (!data.isSuccess) {
                $.alert({
                    title: 'Whoops!',
                    content: "Something went wrong and your exclusion wasn't deleted.",
                    confirm: function () {
                        //$.alert('Confirmed!'); // shorthand.
                    }
                });
            } else {
                location.reload(true);
            }
        }
    });
}
