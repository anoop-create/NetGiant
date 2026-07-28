// Structure of this document is as follows:
//      1. Functions
//      2. Immediate Code

// For use on the following pages
//      index

function doCustomerSearch() {
    var grid = $("#grid").data("kendoGrid");
    var searchterm = $('#customer-search').val();
    if ($('#postcodeOnly:checked').length) {
        searchterm = searchterm.replace(' ', '');
    }
    grid.dataSource.read({ keyword: searchterm });
    $('#search-term').text($('#customer-search').val());
}

var Grid_OnRowSelect = function (e) {
    var eventTarget = event.target ? $(event.target) : $(event.srcElement);
    if (!eventTarget.closest('td').hasClass('exclude-from-click-event')) {
        var data = this.dataItem(this.select());
        var websiteid = data.WebsiteId;

        launchUser(websiteid, data.Record, "Index", "Home");
    }
};

function launchUser(websiteid, userid, action, controller) {
    var urlNode = window.location.host.split('.')[0];
    if (!urlNode.startsWith("localhost")) {
        switch (websiteid) {
            case 1:
                destinationUrl = urlNode + (urlNode.length > 0 ? '.tonergiant.co.uk' : 'tonergiant.co.uk');
                break;
            case 2:
                destinationUrl = urlNode + (urlNode.length > 0 ? '.cartridgemonkey.com' : 'cartridgemonkey.com');
                break;
            case 3:
                destinationUrl = urlNode + (urlNode.length > 0 ? '.netgiant.com' : 'netgiant.com');
                break;
        }
    } else {
        destinationUrl = urlNode;
    }

    window.open('http://' + destinationUrl + '/Portal/Authenticate?userId=' + userid + "&act=" + action + '&cont=' + controller, '_blank');
}

function getAdditionalFilters() {
    return {
        keyword: $("#customer-search").val(),
        postcodeOnly: $('#postcodeOnly').is(':checked')
    };
}

function resizeGrid() {
    if ($('#grid').length > 0) {
        var gridElement = $("#grid"),
            dataArea = gridElement.find(".k-grid-content"),
            gridHeight = $(window).height() - $('#grid').offset().top - 5,
            otherElements = gridElement.children().not(".k-grid-content"),
            otherElementsHeight = 0;
        otherElements.each(function () {
            otherElementsHeight += $(this).outerHeight();
        });
        dataArea.height(gridHeight - otherElementsHeight);
    }
}

function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function sendTrackingLink(id) {
    $.ajax({
        url: "/Portal/OrderTrackingSendEmail/" + id,
        dataType: 'json',
        traditional: true,
        type: 'POST',
        cache: false,
        async: false,
        success: function (data) {
            if (data.savereturn.IsSuccess) {
                $.confirm({
                    title: 'Tracking Email',
                    content: 'Email has been sent.',
                    buttons: {
                        OK: function () {
                        }
                    }
                });
            } else {
                $.confirm({
                    title: 'Tracking Email',
                    content: 'There was an error. Email not sent.',
                    buttons: {
                        OK: function () {
                        }
                    }
                });
            }
        },
        error: function (xhr, textStatus, thrownError) {
            logAjaxScriptError("/MyAccount/VerifyPassword/", xhr, textStatus, thrownError);
        }
    });
}

function convertRecord(rec) {
    // remove '01/'
    var ret = rec.replace('01/', '');
    // remove user number
    ret = ret.substring(0, ret.length - 5);

    return ret;
}

$(function () {
    resizeGrid();

    $(document).on('click',
        '#search-button',
        function () {
            if ($('#customer-grid').length) {
                doCustomerSearch();
            } else {
                // Need to load the grid page
                //location.href = '/portal/index?srch=' + $('#customer-search').val() + '&pco=' + $('#postcodeOnly').is(':checked');
                var form = $('<form id="portal-search" action="/portal/index" method="POST">' +
                    '<input type="hidden" name="srch" value="' + $('#customer-search').val() + '" />' +
                    '<input type="hidden" name="pco" value="' + $('#postcodeOnly').is(':checked') + '" />' +
                    '</form>');
                $(document.body).append(form);
                $('#portal-search').submit();
            }
        });

    $(document).on('keyup',
        '#customer-search',
        function (e) {
            if (e.which !== 32) {
                var value = $(this).val();
                var noWhitespaceValue = value.replace(/\s+/g, '');
                var noWhitespaceCount = noWhitespaceValue.length;
                if (noWhitespaceCount > 3) {
                    doCustomerSearch();
                }
            }
        });

    $('#customer-search').keypress(function (e) {
        if (e.which === 13) {
            doCustomerSearch();
        }
    });

    //$('#customer-search').keypress(function (e) {
    //    if (e.which === 13) {
    //        doCustomerSearch();
    //    }
    //});

    $(document).on('click',
        'tr',
        function () {
            $(this).removeClass('k-state-selected');
        });

    $(document).on('click',
        '.record-options',
        function (event) {
            var w = $(this).attr('data-site');
            var u = $(this).attr('data-id');
            var t = $(this).attr('data-tracking');

            var v = '<a href="/portal/addvoucher?acc=' + u.split('-')[0] + '&site=' + w + '" class="g-p-5 g-fs-sm primary">Add Voucher</a>';
            var r = '<a href="javascript: launchUser(' + w + ', \'' + u + '\', \'Return\', \'MyAccount\')" class="g-p-5 g-fs-sm primary">Add Return</a>';
            var ot = '';
            if (t == 'true') {
                var ot = '<a href="/portal/OrderTrackingList?acc=' + u.split('-')[0] + '" class="g-p-5 g-fs-sm primary">Order Tracking</a>';
            }
            if ($(this).find('div').length === 0) {
                $('.record-options > div').remove();
                $(this).append('<div class="g-bc-s g-b-1-p g-ps-a g-p-5 g-m-l-m5"><div>' + v + '</div><div>' + r + '</div><div>' + ot + '</div></div>');
            } else {
                $('.record-options > div').remove();
            }
        });
});
