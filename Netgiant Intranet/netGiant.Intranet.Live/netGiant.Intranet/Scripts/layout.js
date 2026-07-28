$(document).ready(function () {

    var selectedParentLinkID;
    var sideBarActiveTabIndex = $('#accordion a.helperActive').parent().attr("index");

    if ($("#accordion").text().trim().length > 0) {
        $("#accordion").accordion({
            heightStyle: "content",
            active: parseInt(sideBarActiveTabIndex)
        });
    }

    informationBox();

    $('#goToTop').click(function () {
        $("html, body").animate({ scrollTop: $('html').offset().top }, 1200);
    });

    $(window).scroll(function () {
        if ($(this).scrollTop() > 700) {
            $('#goToTop').fadeIn();
        } else {
            $('#goToTop').fadeOut();
        }
    });

});

//Global Functions
function standardPaging(event, pageBtn, divPagingID, ajaxURL, divPartialViewID, optionsArray, sortType, sortDir) {

    var oldPageNumber = Number($('' + divPagingID + ' .active a').text());
    var currentPageNumber = 1;

    //If paging buttons clicked
    if (event.target.tagName == 'A' && event.target.classList.length == 0) {
        if (pageBtn.parent().hasClass('disabled')) {
            return false;
        }

        $('' + divPagingID + ' li').each(function () {
            $(this).removeClass('active')
        });

        if (pageBtn.parent().hasClass("PagedList-skipToPrevious")) {
            currentPageNumber = oldPageNumber - 1;
        } else if (pageBtn.parent().hasClass("PagedList-skipToNext")) {
            currentPageNumber = oldPageNumber + 1;
        } else if (pageBtn.parent().hasClass("PagedList-skipToLast")) {
            currentPageNumber = $('#PageCount').val();
        } else if (pageBtn.parent().hasClass("PagedList-skipToFirst")) {
            currentPageNumber = 1;
        } else {
            pageBtn.parent().addClass('active');
            currentPageNumber = $('' + divPagingID + ' .active a').text();
        }
    }

    optionsArray.push(currentPageNumber);

    $('body').append('<div id="updatingContainer"></div>');

    $.ajax({
        url: ajaxURL,
        dataType: 'html',
        traditional: true,
        type: 'POST',
        cache: false,
        data:
            {
                optionsArray: optionsArray,
                timestamp: $.now()
            },
        async: false,
        success: function (data) {
            $('' + divPartialViewID + '').empty();
            $('' + divPartialViewID + '').html(data);

            if (sortType != undefined) {
                if (sortType.length > 0) {
                    var sortImgClass = '';
                    var sortImgTitle = '';
                    if (sortDir.val() == "Asc") {
                        sortImgClass = 'msp_sortArrowAsc';
                        sortImgTitle = 'Ascending';
                    } else {
                        sortImgClass = 'msp_sortArrowDesc';
                        sortImgTitle = 'Descending';
                    }

                    $('.tableHeaderSort[value="' + sortType.val() + '"]').next().addClass(sortImgClass);
                    $('.tableHeaderSort[value="' + sortType.val() + '"]').next().attr("title", sortImgTitle);
                }
            }

            $('#updatingContainer').remove();

            if ($("#historyPage").length > 0) {
                $("#historyPage").val($('' + divPartialViewID + '').html());
            }
        },
        error: function () {
            $('#updatingContainer').remove();
        }
    });

    return false;
}

function setupSort(element) {
    var sortType = $("#activeSortType");
    var sortDir = $("#activeSortDir");

    if (sortType.val() == element.attr("value")) {

        if (sortDir.val() == 'Desc') {
            sortDir.val('Asc');
        } else {
            sortDir.val('Desc');
        }

    } else {
        sortType.val(element.attr("value"));
        sortDir.val('Asc');
    }
}

function jsonPopulateDropDown(url, dropDownId, name, selectedID) {

    $.getJSON(url, function (data) {
        $(dropDownId).empty();
        var optionsList = '<option value="" selected="true">Search by ' + name + '</option>';

        $.each(data, function (index) {
            var optVal = data[index].Value;
            var optText = data[index].Text;
            optionsList += '<option value="' + optVal + '">' + optText + '</option>';
        });

        $(dropDownId).removeAttr('disabled');
        $(dropDownId).html(optionsList);

        if (selectedID != 0 && selectedID != '' && selectedID != undefined) {
            $(dropDownId).val(selectedID);
        }
    });

}

function ajaxGet(url, resultContainer, obj) {

    $.ajax({
        url: url,
        dataType: 'html',
        traditional: true,
        type: 'GET',
        data: obj,
        async: false,
        cache: false,
        success: function (data) {
            $('' + resultContainer + '').empty();
            $('' + resultContainer + '').html(data);
        }
    });

}

function ajaxDelete(divDeleteDialog, url, id, divPartialView, optionsArray, sortType, sortDir) {

    optionsArray.push("1");

    $(divDeleteDialog).dialog({
        resizable: false,
        width: 600,
        modal: true,
        buttons: {
            "Delete": function () {

                $('body').append('<div id="updatingContainer"></div>');

                $.ajax({
                    url: url,
                    dataType: 'html',
                    traditional: true,
                    type: 'POST',
                    data: {
                        optionsArray: optionsArray,
                        timestamp: $.now()
                    },
                    async: false,
                    cache: false,
                    success: function (data) {
                        $('' + divPartialView + '').empty();
                        $('' + divPartialView + '').html(data);
                        informationBox();

                        if (sortType != undefined) {
                            if (sortType.length > 0) {
                                var sortImgClass = '';
                                if (sortDir.val() == "Asc") {
                                    sortImgClass = 'msp_sortArrowAsc';
                                } else {
                                    sortImgClass = 'msp_sortArrowDesc';
                                }

                                $('.tableHeaderSort[value="' + sortType.val() + '"]').next().addClass(sortImgClass);
                            }
                        }

                        if ($("#historyPage").length > 0) {
                            $("#historyPage").val($('' + divPartialView + '').html());
                        }

                        $('#updatingContainer').remove();
                    },
                    error: function () {
                        $('#updatingContainer').remove();
                    }
                });
                $(this).dialog("close");
            },
            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
}

function informationBox() {
    var infoBoxFlag = $("#infoBoxFlag");
    if (infoBoxFlag.length > 0) {
        $("body").append('<div id="informationContainer">' + infoBoxFlag.text() + '</div>');
        $("#informationContainer").slideDown(700).delay(1700).slideUp(700, function () {
            $("#informationContainer").remove();
        });
        infoBoxFlag.remove();
    }
}

function appendInfoBox(text) {
    if (text.length > 0) {
        $("body").append('<div id="informationContainer">' + text + '</div>');
        $("#informationContainer").slideDown(700).delay(1700).slideUp(700, function () {
            $("#informationContainer").remove();
        });
    }
}

function setupComboBox(sourceUrl) {
    $.widget("custom.combobox", {
        _create: function () {
            this.wrapper = $("<span>")
            .addClass("custom-combobox")
            .insertAfter(this.element);
            this.element.hide();
            this._createAutocomplete();
            this._createShowAllButton();
        },
        _createAutocomplete: function () {
            this.input = $("<input value='Start Typing...'>")
            .appendTo(this.wrapper)
            .addClass("custom-combobox-input ui-widget ui-widget-content ui-state-default ui-corner-left")
            .autocomplete({
                minLength: 0,
                source: $.proxy(this, "_source"),
                select: function (event, ui) {
                    $(this).val(ui.item.label);
                    $(this).parent().parent().find('.hiddenValue').val(ui.item.value).trigger('change');
                    return false;
                }
            })
            .focus(function () {
                $(this).val('');
                $(this).parent().parent().find('.hiddenValue').val(0);
            })
            .blur(function () {
                if ($(this).val() == '') {
                    $(this).val('Start Typing...');
                }
            })
            .tooltip({
                tooltipClass: "ui-state-highlight"
            });
        },
        _createShowAllButton: function () {
            var input = this.input,
            wasOpen = false;
            $("<a>")
            .attr("tabIndex", -1)
            .tooltip()
            .appendTo(this.wrapper)
            .button({
                icons: {
                    primary: "ui-icon-triangle-1-s"
                },
                text: false
            })
            .removeClass("ui-corner-all")
            .addClass("custom-combobox-toggle ui-corner-right")
            .mousedown(function () {
                wasOpen = input.autocomplete("widget").is(":visible");
            })
            .click(function () {
                input.focus();
                // Close if already visible
                if (wasOpen) {
                    return;
                }
                // Pass empty string as value to search for, displaying all results
                input.autocomplete("search", "");

            });
        },
        _source: function (request, response) {
            $.ajax({
                url: sourceUrl + '?searchTerm=' + request.term,
                dataType: "json",
                success: function (data) {
                    response(data);
                }
            });
        },
        _destroy: function () {
            this.wrapper.remove();
            this.element.show();
        }
    });
}

$(function () {
    $(document).tooltip({
        position: {
            my: "center bottom-20",
            at: "center top",
            using: function (position, feedback) {
                $(this).css(position);
                $("<div>")
                  .addClass("arrow")
                  .addClass(feedback.vertical)
                  .addClass(feedback.horizontal)
                  .appendTo(this);
            }
        }
    });
});


var blockNumber = 2;  //Infinate Scroll starts from second block
var noMoreData = false;
var inProgress = false;

function InfiniteScroll(url, optionsArray, clearData,
    clearElement, countElement, appendElement, sortType, sortDir) {

    inProgress = true;
    $("body").append('<div id="updatingContainer"></div>');
    optionsArray.push(blockNumber);

    $.ajax({
        type: 'POST',
        url: url,
        traditional: true,
        data:
        {
            optionsArray: optionsArray,
            timestamp: $.now()
        },
        success: function (data) {
            blockNumber++;
            if (clearData) { $(clearElement).remove(); }
            noMoreData = data.NoMoreData;
            $(countElement).text(data.Count + ' ');
            $(appendElement).append(data.HTMLString);
            $('#updatingContainer').remove();
            inProgress = false;
            appendInfoBox(data.InfoBoxMessage);

            if (sortType != undefined) {
                if (sortType.length > 0) {
                    var sortImgClass = '';
                    var sortImgTitle = '';
                    if (sortDir.val() == "Asc") {
                        sortImgClass = 'msp_sortArrowAsc';
                        sortImgTitle = 'Ascending';
                    } else {
                        sortImgClass = 'msp_sortArrowDesc';
                        sortImgTitle = 'Descending';
                    }

                    $('.tableHeaderSort').next().removeClass('msp_sortArrowAsc msp_sortArrowDesc');
                    $('.tableHeaderSort[value="' + sortType.val() + '"]').next().addClass(sortImgClass);
                    $('.tableHeaderSort[value="' + sortType.val() + '"]').next().attr("title", sortImgTitle);
                }
            }

        }
    });

}

function ajaxDeleteInfiniteScroll(divDeleteDialog, url, id, divPartialView, optionsArray, 
    sortType, sortDir, clearData, clearElement, countElement, appendElement) {

    optionsArray.push("1");

    $(divDeleteDialog).dialog({
        resizable: false,
        width: 600,
        modal: true,
        buttons: {
            "Delete": function () {

                $('body').append('<div id="updatingContainer"></div>');

                $.ajax({
                    url: url,
                    traditional: true,
                    type: 'POST',
                    data: {
                        optionsArray: optionsArray,
                        timestamp: $.now()
                    },
                    async: false,
                    cache: false,
                    success: function (data) {
                        informationBox();
                        blockNumber++;
                        if (clearData) { $(clearElement).remove(); }
                        noMoreData = data.NoMoreData;
                        $(countElement).text(data.Count + ' ');
                        $(appendElement).append(data.HTMLString);
                        $('#updatingContainer').remove();
                        inProgress = false;
                        appendInfoBox(data.InfoBoxMessage);

                        $('#updatingContainer').remove();

                        if (sortType != undefined) {
                            if (sortType.length > 0) {
                                var sortImgClass = '';
                                var sortImgTitle = '';
                                if (sortDir.val() == "Asc") {
                                    sortImgClass = 'msp_sortArrowAsc';
                                    sortImgTitle = 'Ascending';
                                } else {
                                    sortImgClass = 'msp_sortArrowDesc';
                                    sortImgTitle = 'Descending';
                                }

                                $('.tableHeaderSort').next().removeClass('msp_sortArrowAsc msp_sortArrowDesc');
                                $('.tableHeaderSort[value="' + sortType.val() + '"]').next().addClass(sortImgClass);
                                $('.tableHeaderSort[value="' + sortType.val() + '"]').next().attr("title", sortImgTitle);
                            }
                        }

                    },
                    error: function () {
                        $('#updatingContainer').remove();
                    }
                });
                $(this).dialog("close");
            },
            Cancel: function () {
                $(this).dialog("close");
            }
        }
    });
}

function resizeGrid() {
    if ($('#grid').length > 0) {
        var gridElement = $("#grid"),
            dataArea = gridElement.find(".k-grid-content"),
            gridHeight = $(window).height() - $('#grid').offset().top,
            otherElements = gridElement.children().not(".k-grid-content"),
            otherElementsHeight = 0;
        otherElements.each(function () {
            otherElementsHeight += $(this).outerHeight();
        });
        dataArea.height(gridHeight - otherElementsHeight);
    }
}

