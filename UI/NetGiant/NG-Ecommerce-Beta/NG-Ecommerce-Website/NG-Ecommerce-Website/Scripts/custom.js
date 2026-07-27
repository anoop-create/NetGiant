// START Of Menu Code
$(document).ready(function (e) {
    $(".nav-level1").hover(function () {
        $('select').blur();
    });
    $('.nav-label1').hoverIntent(
        function () {
            //$('> .nav-level2', this).css({filter: "alpha(opacity = 0)", opacity: "0"}).fadeTo("fast", 1);
            $('> .nav-level2', this).show();
            $('> .nav-level2Full', this).show();
        },
        function () {
            //$('> .nav-level2', this).fadeOut("fast", function(){});
            $('> .nav-level2', this).hide();
            $('> .nav-level2Full', this).hide();
        }
    );
    $('.nav-label2').hover(
        function () {
            var par = $(this).parent();
            //$('> .nav-level3', this).css({filter: "alpha(opacity = 0)", opacity: "0"}).fadeTo("fast", 1);
            $('> .nav-level3', this).show();
            $('> .nav-level3', this).css("min-height", function () { return par.height() + 4; });
            $(' .nav-label2Chevron1', this).removeClass("g-fc-s").addClass("g-fc-st");
            $(' .nav-label2Chevron2', this).removeClass("g-fc-s").addClass("g-fc-st");
        },
        function () {
            //$('> .nav-level3', this).fadeOut("fast", function(){});
            $('> .nav-level3', this).hide();
            $(' .nav-label2Chevron1', this).removeClass("g-fc-st").addClass("g-fc-s");
            $(' .nav-label2Chevron2', this).removeClass("g-fc-st").addClass("g-fc-s");
        }
    );
    $('.nav-level1').hoverIntent(
        function () {
            //if (!stopOverlay) {
            $("body").append('<div class="nav-overlay"></div>');
            $('.nav-overlay').css({ filter: "alpha(opacity = 0)", opacity: "0", height: ($(document).height() - 171) }).fadeTo("fast", 0.8);
            //}
        },
        function () {
            $('.nav-overlay').fadeOut("fast", function () { $(this).remove(); });
        }
    );
});
// END of Menu Code