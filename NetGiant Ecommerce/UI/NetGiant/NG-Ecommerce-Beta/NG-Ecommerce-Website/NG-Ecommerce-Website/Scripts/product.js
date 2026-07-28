// Structure of this document is as follows:
//      1. Functions
//      2. Immediate Code

// For use on the following pages
//      product
//      products
//      model
//      search-results
//      printer-finder

$(function () {

    // Product Pages
    if (isCurrentPage('/product/')) {

        $('.mini-product-container').jScrollPane({ showArrows: true });
        $(function () {
            $(".product-pdfs").mouseenter(function () {
                $(this).find('.content').removeClass('hide');
            }).mouseleave(function () {
                $(this).find('.content').addClass('hide');
            });
        });

        if (typeof flixJsCallbacks === "object") {
            flixJsCallbacks.setLoadCallback(function () {
                try {
                    $('.flix-data, .flix-container').remove();
                } catch (e) {
                    // empty
                }
            },
                'noshow');
        }

        $('#imageModal').on('show.bs.modal',
            function (event) {
                var clickedElem = $(event.relatedTarget);
                var clickedImage = clickedElem.attr('src');

                if (clickedImage.indexOf("mediapool.getthespec.com") !== -1) {
                    clickedImage = clickedImage + "&V=HR";
                }

                $('.image-modal .image-slide-container').removeClass('active');
                $('.image-modal .large-image').attr('src', clickedImage);
                $('.image-modal .image-slides img').each(function (index, elem) {
                    if ($(this).attr('src') === clickedImage) {
                        $(this).parent().addClass('active');
                    }
                });
            });

        $(document).on('click',
            '.image-modal .image-slides .image-slide-container',
            function () {
                var imageUrl = $('img', this).attr('src');
                $('.image-modal .large-image').attr('src', imageUrl);
                $('.image-modal .image-slide-container').removeClass('active');
                $(this).addClass('active');
            });

        $(document).on('click',
            '.image-modal #previous-image',
            function () {
                var previousImage = $('.image-slide-container.active').prev('div');
                if (previousImage.length > 0) {
                    $('.image-modal .image-slide-container').removeClass('active');
                    previousImage.addClass('active');
                    $('.image-modal .large-image').attr('src', previousImage.find('img').attr('src'));
                }
            });

        $(document).on('click',
            '.image-modal #next-image',
            function () {
                var nextImage = $('.image-slide-container.active').next('div');
                if (nextImage.length > 0) {
                    $('.image-modal .image-slide-container').removeClass('active');
                    nextImage.addClass('active');
                    $('.image-modal .large-image').attr('src', nextImage.find('img').attr('src'));
                }
            });

        $(document).ready(adjustModal(110));
        $(window).resize(adjustModal(110));
    }

    // Products Pages
    // Grid Pages and Printer Finder Page
    if (isCurrentPage('/products/') || isCurrentPage('/printer-finder')) {
        $('.pg-entry').hover(
            function () {
                $(this).find('.pg-compare').removeClass('g-v-h');
            },
            function () {
                if ($(this).find('.fa').hasClass('fa-square-o')) {
                    $(this).find('.pg-compare').addClass('g-v-h');
                }
            }
        );

        $(document).on('click',
            '.pg-compare-select',
            function () {
                if ($(this).find('i').hasClass('fa-square-o')) {
                    if ($('.pg-compare-count:first').html() === '4') {
                        alert('4 only');
                        return false;
                    }
                    $(this).find('i').removeClass('fa-square-o').addClass('fa-check-square-o');
                } else {
                    $(this).find('i').removeClass('fa-check-square-o').addClass('fa-square-o');
                }
                $('.pg-compare-count').html($('.pg-products .fa-check-square-o').length);
                checkCompareCount();
            });
        $(document).ready(adjustModal(178));
        $(window).resize(adjustModal(178));
    }

    // Grid Pages
    if (isCurrentPage('/products/')) {
        checkCompareCount();

        $(document).on('mouseenter',
            '.compare-product',
            function () {
                $(this).find('.delete-compare').removeClass('g-d-n');
            });

        $(document).on('mouseleave',
            '.compare-product',
            function () {
                $(this).find('.delete-compare').addClass('g-d-n');
            });

        $(document).on('click',
            '.delete-compare',
            function () {
                var id = $(this).attr('data-productid');
                $('td[data-productid=' + id + ']').addClass('g-d-n');
            });
    }

    // Sorting
    if (isCurrentPage('/products/') || isCurrentPage('/search-results') || isCurrentPage('/printer-finder')) {
        var entries;
        var container;

        if (isCurrentPage('/search-results')) {

            entries = $(".pl-entry");
            container = $('.pl-products > .clearfix').next();

        } else if (isCurrentPage('/products/')) {

            entries = $(".pg-entry");
            container = $('.pg-products');

        } else if (isCurrentPage('/printer-finder')) {

            entries = $(".pg-entry");
            container = $('.pg-products');
        }

        $(document).on('changed.bs.select',
            '.sortResults',
            function () {
                var $divs = entries;
                var sortMethod = $(this).val();
                var alphabeticallyOrderedDivs;

                if (sortMethod === 1 || sortMethod === 2) {
                    alphabeticallyOrderedDivs = $divs.sort(function (a, b) {
                        if (sortMethod === 1) {
                            return $(a).find(".productName").text().toUpperCase().
                                localeCompare($(b).find(".productName").text().toUpperCase());
                        } else {
                            return $(b).find(".productName").text().toUpperCase().
                                localeCompare($(a).find(".productName").text().toUpperCase());
                        }
                    });
                    $(container).html(alphabeticallyOrderedDivs);
                } else if (sortMethod === 3 || sortMethod === 4) {
                    var numericallyOrderedDivs = $divs.sort(function (a, b) {
                        var aA = parseFloat($(a).find(".price").text());
                        var bB = parseFloat($(b).find(".price").text());
                        if (aA > bB)
                            return sortMethod === 3 ? 1 : -1;
                        if (aA < bB)
                            return sortMethod === 3 ? -1 : 1;
                        return 0;
                    });
                    $(container).html(numericallyOrderedDivs);
                } else {

                    location.reload();
                }

                $("img.lazy").lazyload();
                $('.pg-entry').hover(
                    function () {
                        $(this).find('.pg-compare').removeClass('g-v-h');
                    },
                    function () {
                        if ($(this).find('.fa').hasClass('fa-square-o')) {
                            $(this).find('.pg-compare').addClass('g-v-h');
                        }
                    }
                );

                return false;
            });
    }

    // Printer Finder Page
    if (isCurrentPage('/printer-finder')) {

        $('#pl-product-count').text($('.pg-entry').not('.g-d-n').length);

        $(document).on('click',
            '.wizardNext',
            function () {
                var monoOrColour = $("input:radio[name='checkboxColourOrMono']:checked").val();
                var paperSize = $("input:radio[name='checkboxPaperSize']:checked").val();
                var functionType = $("input:radio[name='checkboxFunctions']:checked").val();
                var twoSided = $("input:radio[name='checkboxTwoSided']:checked").val();
                var connectivity = $("input:radio[name='checkboxConnectivity']:checked").val();

                var filteredJson = jsonData.Printers.filter(function (row) {
                    if (monoOrColour.includes(row.Colour) &&
                        paperSize.includes(row.Pagesize) &&
                        functionType.includes(row.Function) &&
                        twoSided.includes(row.Duplex)) {

                        switch (connectivity) {
                            case 'WIFI':
                                if (row.Wifi === 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            case 'MOBILE':
                                if (row.Mobile === 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            case 'NETWORK':
                                if (row.Network === 'Y') {
                                    return true;
                                } else {
                                    return false;
                                }
                            default:
                                return true;
                        }
                    } else {
                        return false;
                    }
                });

                var productIds = [];
                $.each(filteredJson,
                    function (i, item) {
                        productIds.push(parseInt(item.StockRef));
                    });

                $('.pg-entry').each(function () {
                    $(this).removeClass('g-d-n');
                    var prodId = parseInt($(this).find('.atb-add').data('productid'));

                    if (productIds.indexOf(prodId) === -1) {
                        $(this).addClass('g-d-n');
                    }
                });

                $('#pl-product-count').text($('.pg-entry').not('.g-d-n').length);

                return false;
            });

        $(document).on('click',
            '.showPrinters',
            function () {
                $('html, body').animate({
                    scrollTop: $('#pl-product-count').offset().top - 20
                },
                    1000);
            });
    }

    // Grid / Model / Search Pages
    if (isCurrentPage('/products/') || isCurrentPage('/model/') || isCurrentPage('/search-results')) {
        triggerFilter();
    }

});