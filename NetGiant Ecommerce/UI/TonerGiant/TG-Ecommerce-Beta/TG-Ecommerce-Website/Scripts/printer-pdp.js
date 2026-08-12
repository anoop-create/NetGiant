// Printer PDP - page-scoped behaviour (accordion toggle, consumables tab switching, sticky
// header show/hide on scroll). Only loaded on printer product pages (see the conditional
// <script> include in Views/Product/Index.cshtml, gated on Model.IsPrinterProduct) - never
// runs on any existing consumable product page, so nothing here can affect them.
//
// Deliberately kept separate from the shared Scripts/site.js bundle: everything below reads
// only the new ppdp-* markup added for this feature, and the one shared behaviour it needs
// (Add to Basket) is delivered by reusing the existing .atb-add buttons/handler in site.js
// unchanged - no basket/ajax logic is duplicated or overridden here.
(function () {
    'use strict';

    function initAccordion() {
        $(document).on('click', '.ppdp-accordion-header', function () {
            var item = $(this).closest('.ppdp-accordion-item');
            var body = item.find('.ppdp-accordion-body').first();

            if (item.hasClass('is-open')) {
                item.removeClass('is-open');
                body.slideUp(200);
            } else {
                item.addClass('is-open');
                body.slideDown(200);
            }
        });
    }

    function initTabs() {
        $(document).on('click', '.ppdp-tab-btn', function () {
            var btn = $(this);
            var tabKey = btn.attr('data-ppdp-tab');
            var container = btn.closest('.ppdp-compatibles');

            container.find('.ppdp-tab-btn').removeClass('is-active');
            btn.addClass('is-active');

            container.find('.ppdp-tab-panel').removeClass('is-active');
            container.find('.ppdp-tab-panel[data-ppdp-tab-panel="' + tabKey + '"]').addClass('is-active');
        });
    }

    function initStickyHeader() {
        var stickyHeader = $('#ppdp-sticky-header');
        var priceBox = $('#price-box');

        if (stickyHeader.length === 0 || priceBox.length === 0) {
            return;
        }

        $(window).on('scroll', function () {
            var triggerPoint = priceBox.offset().top + priceBox.outerHeight();

            if ($(window).scrollTop() > triggerPoint) {
                stickyHeader.addClass('is-visible');
            } else {
                stickyHeader.removeClass('is-visible');
            }
        });
    }

    $(function () {
        initAccordion();
        initTabs();
        initStickyHeader();
    });
})();
