/**
 * Structure of this document is as follows:
 *      1. Functions
 *      2. Immediate Code
 *
 * For use on the following pages
 *      home
 *      printerwizard
 *      cartridgeendurancetool
 */

function checkManufacturer(manu) {
    if (manu === "" || manu === "0") {
        $('#pop-brands').removeClass('g-d-n');
    } else {
        $('#pop-brands').addClass('g-d-n');
    }
    $('#wiz-find').attr('disabled', 'disabled');
}

function checkEquipment(equip) {
    if (equip === "" || equip === "0") {
        $('#wiz-find').attr('disabled', 'disabled');
    } else {
        $('#wiz-find').removeAttr('disabled');
    }
}

function populateBody(data) {
    $('#wizard-body').empty().append(data.body);
}

function populateManufacturer(data) {
    var manuname = "";
    if ($('#wiz-manufacturer').data('kendoDropDownList') === undefined) {
        manuname = $('#wiz-manufacturer option:selected').text();
    } else {
        if ($('#wiz-manufacturer').data('kendoDropDownList').text() !== '') {
            manuname = $('#wiz-manufacturer').data('kendoDropDownList').text();
        }
    }
    $('.wiz-manufacturer-image > img').attr('src',
        data.cdn +
        '/Images/BannerLogos/' +
        manuname.replace(/ /g, '-').toLowerCase() +
        '.jpg');
    if (manuname === '') {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            $('#wiz-cartridgetype').val().replace(/ /g, '-').toLowerCase() +
            '.jpg');
    } else {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            manuname.replace(/ /g, '-').toLowerCase() +
            '.jpg');
    }
    $('.pw-manu-name').html(manuname);

    if ($('#pw-altcart-link').length > 0) {
        var oldbrand = $('#pw-altcart-link').attr('href').split('-cartridges')[1];
        if (oldbrand === '/') {
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

function populateCartridge(data) {
    $('#wiz-cartridge').empty();
    $('#wiz-cartridge').append('<option value="0">Select Cartridge</option>');
    $.each(data.cartlist,
        function (i, cart) {
            var atts = '';
            $.each(cart.Data,
                function (key, val) {
                    atts = atts + ' ' + key.replace(/\_/g, '-') + '="' + val + '"';
                });
            $('#wiz-cartridge').append('<option value="' + cart.Value + '"' + atts + '>' + cart.Text + '</option>');
        });
}

function populatePopularPrinters(data) {
    if (data.popprintcount === 0) {
        $('#pop-printers-container').css('display', 'none');
    } else {
        $('#pop-printers-container').css('display', 'block');
    }

    $('#pop-printers').empty().append(data.popprint);
}

function populatePopularRanges(data) {
    if (data.poprangecount === 0) {
        $('#pop-ranges-container').css('display', 'none');
    } else {
        $('#pop-ranges-container').css('display', 'block');
    }
    $('#pop-ranges').empty().append(data.poprange);
}

function populatePopularCartridges(data) {
    if (data.popcartcount === 0) {
        $('#pop-cartridges-container').css('display', 'none');
    } else {
        $('#pop-cartridges-container').css('display', 'block');
    }

    $('#pop-cartridges').empty().append(data.popcart);
    $('.mini-product-container').jScrollPane({ showArrows: true });
}

function populatePrinterLinks(data) {
    $('#printer-links').empty().append(data.printerlinks);
}

function populateManuText(data) {
    $('#manu-text').empty().append(data.manutext);
}

function populateEquipLinks(data) {
    $('#printer-links').empty().append(data.printerlinks);
    $('#printer-links2').empty();
}

function filterFamily() {
    var params = {
        type: $('#cartridge-type-name').val(),
        search: null,
        manufacturerId: $("#wiz-manufacturer").data("kendoDropDownList").value()
    };

    var filter = $('#wiz-family').data('kendoDropDownList').dataSource.filter();

    if (filter && filter.filters[0].operator === "contains") {
        params.search = filter.filters[0].value;
    }

    return params;
}

function filterEquipment() {
    var params = {
        type: $('#cartridge-type-name').val(),
        search: null,
        familyId: $('#wiz-family').data('kendoDropDownList').value() === "" ? 0 : $('#wiz-family').data('kendoDropDownList').value(),
        manufacturerId: $("#wiz-manufacturer").data("kendoDropDownList").value()
    };

    var filter = $('#wiz-equipment').data('kendoDropDownList').dataSource.filter();

    if (filter && filter.filters[0].operator === "contains") {
        params.search = filter.filters[0].value;
    }

    return params;
}

$(function () {

    // Wizard Pages
    if ($('.wiz-widget').length) {
        $('.wiz-widget').on('change',
            '#wiz-manufacturer',
            function (event) {
                var manuId;
                if ($("#wiz-manufacturer").data("kendoDropDownList") === undefined) {
                    manuId = $('#wiz-manufacturer').val() === "" ? 0 : $('#wiz-manufacturer').val();
                } else {
                    manuId = $("#wiz-manufacturer").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-manufacturer").data("kendoDropDownList").value();
                }
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
                        if ($('#wiz-manufacturer').data('kendoDropDownList') === undefined) {
                            populateFamily(data);
                            populateEquipment(data);
                            $('.selectpicker').selectpicker('refresh');
                        } else {
                            $("#wiz-family").data("kendoDropDownList").select(0);
                        }
                        checkManufacturer(manuId);

                        //populateBody(data);

                        populateManufacturer(data);
                        populatePopularPrinters(data);
                        populatePopularRanges(data);
                        populatePopularCartridges(data);
                        populatePrinterLinks(data);
                        populateManuText(data);
                        populateEquipLinks(data);
                        // Site audit (May 2026) item 21: "several product images are showing
                        // the 1pxTrans.png placeholder (lazy load not triggering correctly in
                        // some contexts)" - this call was passing no options, which defaults
                        // jquery.lazyload's failure_limit to 0. That setting assumes images are
                        // laid out in strict top-to-bottom page order and stops checking further
                        // images the first time one isn't yet visible - fine for a normal page,
                        // but populatePopularCartridges() above renders its tiles inside a
                        // jScrollPane horizontal scroll strip (.mini-product-container), where
                        // visibility isn't top-to-bottom at all. The very next tile past the
                        // visible viewport edge would trip that check and every tile after it
                        // would never get its real src swapped in. site.js's own document-ready
                        // call (the one every other page relies on) already passes
                        // {threshold:200, failure_limit:999} for exactly this reason - matching
                        // it here so newly-injected wizard tiles behave the same way.
                        $("img.lazy").lazyload({ threshold: 200, failure_limit: 999 });
                        $('.pw-manu-name').html(manuname);
                    },
                    error: function (xhr, textStatus, thrownError) {
                        logAjaxScriptError("/Wizard/ChangeManufacturer", xhr, textStatus, thrownError);
                    }
                });
            });

        $('.wiz-widget').on('change',
            '#wiz-family',
            function (event) {
                var manuId;
                var familyId;
                if ($("#wiz-manufacturer").data("kendoDropDownList") === undefined) {
                    manuId = $('#wiz-manufacturer').val() === "" ? 0 : $('#wiz-manufacturer').val();
                    familyId = $('#wiz-family').val() === "" ? 0 : $('#wiz-family').val();
                } else {
                    manuId = $("#wiz-manufacturer").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-manufacturer").data("kendoDropDownList").value();
                    familyId = $("#wiz-family").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-family").data("kendoDropDownList").value();
                }
                $.ajax({
                    url: '/Wizard/ChangeFamily',
                    dataType: 'json',
                    traditional: true,
                    type: 'POST',
                    cache: false,
                    data: {
                        type: $('#wiz-cartridgetype').val(),
                        manufacturerId: manuId,
                        familyId: familyId
                    },
                    async: false,
                    success: function (data) {
                        if ($('#wiz-manufacturer').data('kendoDropDownList') === undefined) {
                            populateEquipment(data);
                            $('.selectpicker').selectpicker('refresh');
                        } else {
                            $("#wiz-equipment").data("kendoDropDownList").dataSource.data(data);
                        }
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
                    var manuId;
                    var familyId;
                    var equipId;
                    if ($("#wiz-manufacturer").data("kendoDropDownList") === undefined) {
                        manuId = $('#wiz-manufacturer').val() === "" ? 0 : $('#wiz-manufacturer').val();
                        familyId = $('#wiz-family').val() === "" ? 0 : $('#wiz-family').val();
                        equipId = $('#wiz-equipment').val() === "" ? 0 : $('#wiz-equipment').val();
                    } else {
                        manuId = $("#wiz-manufacturer").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-manufacturer").data("kendoDropDownList").value();
                        familyId = $("#wiz-family").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-family").data("kendoDropDownList").value();
                        equipId = $("#wiz-equipment").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-equipment").data("kendoDropDownList").value();
                    }
                    $.ajax({
                        url: '/Wizard/ChangeEquipment',
                        dataType: 'json',
                        traditional: true,
                        type: 'POST',
                        cache: false,
                        data: {
                            type: $('#wiz-cartridgetype').val(),
                            manufacturerId: manuId,
                            familyId: familyId,
                            equipmentId: equipId
                        },
                        async: false,
                        success: function (data) {
                            if ($('#wiz-equipment').data('kendoDropDownList') === undefined) {
                                populateCartridge(data);
                                $('.selectpicker').selectpicker('refresh');
                            } else {
                                $("#wiz-cartridge").data("kendoDropDownList").dataSource.data(data);
                                //$("#wiz-cartridge").change();
                            }
                        },
                        error: function (xhr, textStatus, thrownError) {
                            logAjaxScriptError("/Wizard/ChangeEquipment", xhr, textStatus, thrownError);
                        }
                    });
                } else {
                    if ($('#wiz-equipment').data('kendoDropDownList') === undefined) {
                        equipId = $('#wiz-equipment').val() === "" ? 0 : $('#wiz-equipment').val();
                    } else {
                        equipId = $("#wiz-equipment").data("kendoDropDownList").value() === ""
                            ? 0
                            : $("#wiz-equipment").data("kendoDropDownList").value();
                    }
                    checkEquipment(equipId);
                }
            });

        // wiz-cartridge used as part of the extended wizard
        $('.wiz-widget').on('change',
            '#wiz-cartridge',
            function (event) {
                var productId;
                if ($("#wiz-cartridge").data("kendoDropDownList") === undefined) {
                    productId = $('#wiz-cartridge').val() === "" ? 0 : $('#wiz-cartridge').val();
                } else {
                    productId = $("#wiz-cartridge").data("kendoDropDownList").value() === "" ? 0 : $("#wiz-cartridge").data("kendoDropDownList").value();
                }
                if (productId === "" || productId === "0") {
                    $('#wiz-find').attr('disabled', 'disabled');
                } else {
                    $('#wiz-find').removeAttr('disabled');
                }
            });

        $('.wiz-widget').on('click',
            '#wiz-find',
            function (event) {
                if (!$("#wiz-cartridge").length) {

                    var equip;
                    var ctype;
                    if ($('#wiz-equipment').data('kendoDropDownList') === undefined) {
                        ctype = $("#wiz-equipment option:selected").attr('data-ctype');
                        equip = $("#wiz-equipment option:selected").text();
                    } else {
                        var equipment = $("#wiz-equipment").data("kendoDropDownList");
                        var equipmentData = equipment.dataSource.view()[equipment.selectedIndex - 1];
                        ctype = equipmentData.Data.data_ctype.replace('hp-range', 'toner-cartridges').replace('toner-range', 'toner-cartridges').replace('ink-range', 'ink-cartridges');
                        equip = $("#wiz-equipment").data("kendoDropDownList").text();
                    }
                    location.href = '/model/' +
                        equip.replace(/ /g, '-') +
                        '-' + ctype.replace('hp-range', 'toner-cartridges') + '/';
                }
            });
    }

    if (isCurrentPage('/toner-cartridges/') || isCurrentPage('/ink-cartridges/') || isCurrentPage('/solid-ink-cartridges/') || isCurrentPage('/franking-cartridges/')) {
        $('.mini-product-container').jScrollPane({ showArrows: true });
    }
});