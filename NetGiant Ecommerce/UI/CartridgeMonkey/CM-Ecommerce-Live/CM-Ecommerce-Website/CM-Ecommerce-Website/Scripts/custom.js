$(function() {
    if (window.location.pathname == '/' ||
        '/toner-cartridges/ink-cartridges/franking-cartridges/solid-ink-cartridges/'.indexOf(window.location.pathname) >
        -1) {
        $('.wiz-widget').on('change',
            '#wiz-manufacturer',
            function (event) {
                $('#pw-guide > .active').removeClass('active');
                if ($('#wiz-manufacturer').val() != 0) {
                    $('#pw-guide > .item2').addClass('active');
                } else {
                    $('#pw-guide > .item1').addClass('active');
                }
            });
        $('.wiz-widget').on('change',
            '#wiz-family',
            function (event) {
                $('#pw-guide > .active').removeClass('active');
                if ($('#wiz-family').val() != 0) {
                    $('#pw-guide > .item3').addClass('active');
                } else {
                    $('#pw-guide > .item2').addClass('active');
                }
            });
        $('.wiz-widget').on('change',
            '#wiz-equipment',
            function (event) {
                $('#pw-guide > .active').removeClass('active');
                if ($('#wiz-equipment').val() != 0) {
                    $('#pw-guide > .item4').addClass('active');
                } else {
                    if ($('#wiz-family').val() != 0) {
                        $('#pw-guide > .item3').addClass('active');
                    } else {
                        $('#pw-guide > .item2').addClass('active');
                    }
                }
            });
    }
});