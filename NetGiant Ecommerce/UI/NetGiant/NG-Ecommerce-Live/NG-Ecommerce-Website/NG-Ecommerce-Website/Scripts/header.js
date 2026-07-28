
// Page Specific
// Checkout
if (window.location.href.toLowerCase().indexOf('/checkout/stage1') > -1) {
    capturePlus.listen("options", function (options) {
        options.bar = options.bar || {};
        options.bar.showLogo = false;
        options.bar.showCountry = false;
    });
    capturePlus.listen("load", function (control) {
        //custom code
        control.listen("populate", function (address) {
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
        });
    });
}