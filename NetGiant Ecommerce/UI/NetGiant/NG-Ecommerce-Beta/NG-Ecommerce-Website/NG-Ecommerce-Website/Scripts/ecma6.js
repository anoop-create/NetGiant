const checkElement = async selector => {
    while (document.querySelector(selector) === null) {
        await new Promise(resolve => requestAnimationFrame(resolve));
    }
    return document.querySelector(selector);
};

function execEcma6() {
    if ($('#SearchApplication').val() === '1') {
        // It's been found that binding the following function to the 'document' doesn't seem to work. The following code waits for the 
        // 'sli_autocomplete' element to be created and binds the function to that
        checkElement('#sli_autocomplete').then((selector) => {
            $('#sli_autocomplete')
                .on("click", ".sli_ac_sugg", function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    if ($(this).attr('data-suggested-term')) {
                        $('#searchform #keyword').val($(this).attr('data-suggested-term'));
                    }
                    if ($(this).attr('data-suggested-facet')) {
                        $('#searchform #cat').val($(this).attr('data-suggested-facet'));
                    }
                    $('#searchform').submit();
                })
                .on("click", ".view_more", function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    // if the main search box is blank assume the wizard search is in use
                    if ($('#searchform #keyword').val() === '') {
                        $('#searchform #keyword').val($('#wizardSearch').val());
                    }
                    $('#searchform').submit();
                });
        });
    }
}