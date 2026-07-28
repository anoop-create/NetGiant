$(function () {
    if ($('.k-grid').length) {
        resizeGrid();
        loadGridFilters();
    }

    $(document).on('click',
        '.telerik-filter > a',
        function () {
            var gridname = $('.telerik-filter').attr('data-gridname');
            var options = localStorage[gridname + 'Options'];
            var grid = $('#' + gridname).data("kendoGrid");

            eval($(this).attr('data-script'));
        });
    //$('.lazy').lazyload();
    // Image Zoom
    $(document).on('click',
        '#image-zoom',
        function () {
            $('#prd-image').trigger('click');
        });
});

function saveGridFilters() {
    var sessionName = getFilterSessionName();
    var grid = $('.k-grid').data('kendoGrid');

    if (grid.dataSource._filter) {
        sessionStorage.setItem(sessionName, kendo.stringify(grid.dataSource.filter()));
    }
}

function loadGridFilters() {
    var sessionName = getFilterSessionName();
    var grid = $('.k-grid').data('kendoGrid');
    var filters = JSON.parse(sessionStorage.getItem(sessionName));
    var gridfilters = grid.dataSource._filter ? grid.dataSource._filter.filters.length : 0;

    if (filters && filters.filters.length > gridfilters) {
        $.confirm({
            title: 'Grid Filters',
            content: 'Would you like to keep the filters you used previously?',
            animation: 'zoom',
            buttons: {
                Keep: function () {
                    grid.dataSource.filter(filters);
                },
                Remove: function () {
                    sessionStorage.removeItem(sessionName);
                }
            }
        });
    }
}

function getFilterSessionName() {
    return $('.k-grid').attr('id') + 'Filters';
}

function deleteGridRow(url, data) {
    $('body').append('<div id="updatingContainer"></div>');

    $.ajax({
        type: "POST",
        url: url,
        data: data,
        success: function (data) {
            if (data.saveReturn.IsSuccess) {
                $('.k-grid').data('kendoGrid').dataSource.read();
                $('.k-grid').data('kendoGrid').refresh();
            } else {
                $.alert({
                    title: 'Error!',
                    content: data.saveReturn.Message
                });
            }
        },
        error: function (e) {
            $.alert({
                title: 'Error!',
                content: e.responseText
            });
        }
    });

    $('#updatingContainer').remove();
}

function getWeekday(d, day_number) {
    d = new Date(d);
    var day = d.getDay(),
        diff = d.getDate() - day + (day <= day_number ? (day_number - 7) : (day_number));
    return new Date(d.setDate(diff));
}

function getMonthday(d, day_number) {
    d = new Date(d);
    if (d.getDate() <= day_number) {
        d.setMonth(d.getMonth() - 1);
    }
    return new Date(d.setDate(day_number));
}

function addDay(d, days) {
    d = new Date(d);
    
    return new Date(d.setDate(d.getDate() + days));
}

function addMonth(d, months) {
    d = new Date(d);

    return new Date(d.setMonth(d.getMonth() + months));
}

function getTheSubstring(value, len) {
    if (!value) return "";
    if (value.length > len) {
        return kendo.toString(value.substring(0, len)) + "...";
    } else {
        return kendo.toString(value);
    }
}

function ajaxPost(url, data) {
    $.ajax({
        type: "POST",
        url: url,
        data: data,
        success: function (data) {
                $.alert({
                    title: data.saveReturn.IsSuccess ? 'OK' : 'Error!',
                    content: data.saveReturn.Message
                });
            location.refresh();
        },
        error: function (e) {
            $.alert({
                title: 'Error!',
                content: e.responseText
            });
        }
    });
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
                $('body').append(data.savereturn.ReturnData);

                if (options) {
                    $('#' + popupid).modal(options);
                } else {
                    $('#' + popupid).modal('show');
                }

                //setDeferredImages();
                if ($('.cutoffCountdownFalse').length) {
                    startTime();
                }

                if (data.savereturn.ReturnData.indexOf('<form ') >= 0) {
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
        error: function (e) {
            $.alert({
                title: 'Error!',
                content: e.responseText
            });
        }
    });
}

function launchPopupView(url, popupid, popupwidth, options) {
    if (!url) {
        return false;
    }

    // Close any existing popups
    $('.modal, .modal-backdrop').not('.donotremove').remove();

    $('body').append('<section class="modal fade" id="' + popupid
        + '" tabindex="-1" role="dialog" aria-labelledby="myModalLabel">'
        + '<div class="modal-dialog modal-' + popupwidth + '" role="document">'
            + '<div class="modal-content"></div>'
            + '</div>'
        + '</section>');

    $('.modal-content').load(url, function () {
        if (options) {
            $('#' + popupid).modal(options);
        } else {
            $('#' + popupid).modal('show');
        }
    });
}

// Generic popup launcher for a CMS entry
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

// Generic popup launcher for a partial view
$(document).on('click',
    '.popup-view',
    function () {
        // get popup html
        var url = $(this).attr("data-url");
        var popupid = typeof $(this).attr("data-popupid") === 'undefined' ? 'popup' : $(this).attr("data-popupid");
        var popupwidth = typeof $(this).attr("data-popupwidth") === 'undefined' ?
            'md' :
            $(this).attr("data-popupwidth");
        var options = typeof $(this).attr("data-options") === 'undefined' ? '' : $(this).attr("data-options");

        launchPopupView(url, popupid, popupwidth, options);
    });

