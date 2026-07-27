$(function () {
    checkManufacturer($('#wiz-manufacturer').val());
    checkEquipment($('#wiz-equipment').val());

    $('.wiz-widget').on('change',
        '#wiz-manufacturer',
        function (event) {
            var manuId = $('#wiz-manufacturer').val() == "" ? 0 : $('#wiz-manufacturer').val();
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
                    populateFamily(data);
                    populateEquipment(data);
                    populatePopularPrinters(data);
                    populatePopularCartridges(data);
                    populateManuText(data);
                    populateEquipLinks(data);
                    $('.selectpicker').selectpicker('refresh');
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Wizard/ChangeManufacturer", xhr, textStatus, thrownError);
                }
            });
        });

    $('.wiz-widget').on('change',
        '#wiz-family',
        function (event) {
            var familyId = $('#wiz-family').val() == "" ? 0 : $('#wiz-family').val();
            $.ajax({
                url: '/Wizard/ChangeFamily',
                dataType: 'json',
                traditional: true,
                type: 'POST',
                cache: false,
                data: {
                    typename: $('#wiz-cartridgetype').val(),
                    manufacturerId: $('#wiz-manufacturer').val() == "" ? 0 : $('#wiz-manufacturer').val(),
                    familyId: familyId
                },
                async: false,
                success: function (data) {
                    populateEquipment(data);
                    $('.selectpicker').selectpicker('refresh');
                    $('#wiz-equipment').trigger('change');
                },
                error: function (xhr, textStatus, thrownError) {
                    logAjaxScriptError("/Wizard/ChangeFamily", xhr, textStatus, thrownError);
                }
            });
        });

    $('.wiz-widget').on('change',
        '#wiz-equipment',
        function (event) {
            var equipId = $('#wiz-equip').val() == "" ? 0 : $('#wiz-equip').val();
            checkEquipment($('#wiz-equipment').val());
        });

    $('.wiz-widget').on('click',
        '#wiz-find',
        function (event) {
            _gaq.push(['_trackEvent', 'Printer Wizard', 'Submit']);
            location.href = '/model/' +
                $('#wiz-equipment option:selected').text().replace(/ /g, '-') +
                '-' +
                $('#wiz-equipment option:selected').attr('data-ctype') +
                '/';
        });
});

function checkManufacturer(manu) {
    if (manu == "" || manu == "0") {
        $('#wiz-family').attr('disabled', 'disabled');
        $('#wiz-equipment').attr('disabled', 'disabled');
        $('#wiz-family-container').addClass('g-op-50p');
        $('#wiz-equipment-container').addClass('g-op-50p');
        $('#pop-brands').removeClass('g-d-n');
    } else {
        $('#wiz-family').removeAttr('disabled');
        $('#wiz-equipment').removeAttr('disabled');
        $('#wiz-family-container').removeClass('g-op-50p');
        $('#wiz-equipment-container').removeClass('g-op-50p');
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
        ($('#wiz-manufacturer option:selected').text().replace(/ /g, '-').toLowerCase()) +
        '.jpg');
    if ($('#wiz-manufacturer option:selected').val() == '') {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            $('#wiz-cartridgetype').val().replace(/ /g, '-').toLowerCase() +
            '.jpg');
    } else {
        $('.comm-title-image > img').attr('src',
            data.cdn +
            '/Images/BannerLogos/' +
            ($('#wiz-manufacturer option:selected').text().replace(/ /g, '-').toLowerCase()) +
            '.jpg');
    }
    var manuname = '';
    if ($('#wiz-manufacturer option:selected').val() != '') {
        manuname = $('#wiz-manufacturer option:selected').text();
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