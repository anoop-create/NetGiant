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
        if (e.which == 13) {
            doCustomerSearch();
        }
    });

    $('#customer-search').keypress(function (e) {
        if (e.which == 13) {
            doCustomerSearch();
        }
    });

    $(document).on('click',
        'tr',
        function () {
            $(this).removeClass('k-state-selected');
        });
});

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
    var data = this.dataItem(this.select());
    var websiteId = data.WebsiteId;
    var urlNode = window.location.host.split('.')[0];
    var destinationUrl = '';

    if (!urlNode.startsWith("localhost")) {
        switch (websiteId) {
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

    window.open('http://' + destinationUrl + '/Portal/Authenticate?userId=' + data.Record, '_blank');
}

function getAdditionalFilters() {
    return {
        keyword: $("#customer-search").val(),
        postcodeOnly: $('#postcodeOnly').is(':checked')
    }
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

function convertRecord(rec) {
    // remove '01/'
    var ret = rec.replace('01/', '');
    // remove user number
    ret = ret.substring(0, ret.length - 5);

    return ret;
}