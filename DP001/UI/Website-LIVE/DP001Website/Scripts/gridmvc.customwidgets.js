
/***
* DropDownListWidget - Provides filter user interface for creating drop down list filter. 
*/

function DropDownListWidget(widgetName, dropDownName, url, label) {
    /***
    * This method must return type of registered widget type in 'SetFilterWidgetType' method
    */
    this.getAssociatedTypes = function () {
        return [widgetName];
    };
    /***
    * This method invokes when filter widget was shown on the page
    */
    this.onShow = function () {
        /* Place your on show logic here */
    };

    this.showClearFilterButton = function () {
        return true;
    };
    /***
    * This method will invoke when user was clicked on filter button.
    * container - html element, which must contain widget layout;
    * lang - current language settings;
    * typeName - current column type (if widget assign to multipile types, see: getAssociatedTypes);
    * values - current filter values. Array of objects [{filterValue: '', filterType:'1'}];
    * cb - callback function that must invoked when user want to filter this column. Widget must pass filter type and filter value.
    * data - widget data passed from the server
    */
    this.onRender = function (container, lang, typeName, values, cb, data) {
        //store parameters:
        this.cb = cb;
        this.container = container;
        this.lang = lang;

        //this filterwidget demo supports only 1 filter value for column column
        this.value = values.length > 0 ? values[0] : { filterType: 1, filterValue: "" };

        this.renderWidget(container); //onRender filter widget
        this.loadCustomers(); //load customer's list from the server
        this.registerEvents(); //handle events
    };
    this.renderWidget = function (container) {
        var html = genSortHtml(container) +
                    '<h4>Select ' + label + ' to filter</h4>\
                    <select style="width:250px;" class="grid-filter-type ' + dropDownName + ' form-control">\
                    </select>';
        this.container.append(html);
    };
    /***
    * Method loads all customers from the server via Ajax:
    */
    this.loadCustomers = function () {
        var $this = this;
        $.post(url, function (data) {
            $this.fillCustomers(data.Items);
        });
    };
    /***
    * Method fill customers select list by data
    */
    this.fillCustomers = function (items) {
        var customerList = this.container.find("." + dropDownName);
        if (label == "rule name") {
            customerList.append('<option>Please Select ...</option>');
            customerList.append('<option value="Null">None</option>');
        }
        for (var i = 0; i < items.length; i++) {
            customerList.append('<option ' + (items[i] == this.value.filterValue ? 'selected="selected"' : '') + ' value="' + items[i] + '">' + items[i] + '</option>');
        }
    };
    /***
    * Internal method that register event handlers for 'apply' button.
    */
    this.registerEvents = function () {
        //get list with customers
        var customerList = this.container.find("." + dropDownName);
        //save current context:
        var $context = this;
        //register onclick event handler
        customerList.change(function () {
            //invoke callback with selected filter values:
            var values = [{ filterValue: $(this).val(), filterType: 1 /* Equals */ }];
            $context.cb(values);
        });
    };

}

/***
* TextWidget - Provides filter user interface for creating text filter. 
*/

function TextWidget(widgetName) {
    /***
    * This method must return type of registered widget type in 'SetFilterWidgetType' method
    */
    this.getAssociatedTypes = function () {
        return [widgetName];
    };
    /***
    * This method invokes when filter widget was shown on the page
    */
    this.onShow = function () {
        /* Place your on show logic here */
    };

    this.showClearFilterButton = function () {
        return true;
    };
    /***
    * This method will invoke when user was clicked on filter button.
    * container - html element, which must contain widget layout;
    * lang - current language settings;
    * typeName - current column type (if widget assign to multipile types, see: getAssociatedTypes);
    * values - current filter values. Array of objects [{filterValue: '', filterType:'1'}];
    * cb - callback function that must invoked when user want to filter this column. Widget must pass filter type and filter value.
    * data - widget data passed from the server
    */
    this.onRender = function (container, lang, typeName, values, cb, data) {
        //store parameters:
        this.cb = cb;
        this.container = container;
        this.lang = lang;

        //this filterwidget demo supports only 1 filter value for column column
        this.value = values.length > 0 ? values[0] : { filterType: 1, filterValue: "" };

        this.renderWidget(container); //onRender filter widget
        this.registerEvents(); //handle events
    };
    this.renderWidget = function (container) {
        var dataname = container.closest('.grid-filter').attr('data-name').replace(/\./g, '_')
        var html = genSortHtml(container) +
                    '<h4 class="">Filter</h4>\
                    <div class="input-group" role="group">\
                        <span class="input-group-btn" role="group">\
                            <button id="' + dataname + 'TextSelect" class="btn btn-default dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" onclick="$(\'#' + dataname + 'SelectList\').toggle()">\
                                Select\
                                <i class="fa fa-caret-down"></i>\
                            </button>\
                            <ul id="' + dataname + 'SelectList" class="dropdown-menu" aria-labelledby="' + dataname + 'TextSelect" style="display: none;">\
                                <li onclick="setFilterValueHiddenField(\'1\', \'' + this.lang.filterSelectTypes.Equals + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.Equals + '</li>\
                                <li onclick="setFilterValueHiddenField(\'2\', \'' + this.lang.filterSelectTypes.Contains + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.Contains + '</li>\
                                <li onclick="setFilterValueHiddenField(\'3\', \'' + this.lang.filterSelectTypes.StartsWith + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.StartsWith + '</li>\
                                <li onclick="setFilterValueHiddenField(\'4\', \'' + this.lang.filterSelectTypes.EndsWith + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.EndsWith + '</li>\
                            </ul>\
                        </span>\
                        <input type="text" class="grid-filter-input form-control" value="' + this.value.filterValue + '" />\
                    </div>\
                    <input type="hidden" class="grid-filter-type" value="' + this.value.filterType + '" />\
                    <div class="grid-filter-buttons g-m-t-10">\
                        <button type="button" class="btn btn-success btn-block grid-apply" >' + this.lang.applyFilterButtonText + '</button>\
                    </div>'
        this.container.append(html);
        setFilterValueHiddenField('1', this.lang.filterSelectTypes.Equals + ' ', '#' + dataname + 'SelectList');
    };
    /***
    * Internal method that register event handlers for 'apply' button.
    */
    this.registerEvents = function () {
        //get apply button from:
        var applyBtn = this.container.find(".grid-apply");
        //save current context:
        var $context = this;
        //register onclick event handler
        applyBtn.click(function () {
            $context.container.find('.dropdown-menu').remove();
            //get selected filter type:
            //var type = $context.container.find(".grid-filter-type").val();
            var type = $context.container.find(".grid-filter-type:last").val();
            //get filter value:
            var value = $context.container.find(".grid-filter-input").val();
            //invoke callback with selected filter values:
            var filterValues = [{ filterType: type, filterValue: value }];
            $context.cb(filterValues);
        });
        //register onEnter event for filter text box:
        this.container.find(".grid-filter-input").keyup(function (event) {
            if (event.keyCode == 13) { applyBtn.click(); }
            if (event.keyCode == 27) { GridMvc.closeOpenedPopups(); }
        });
    };

}

/***
* NumberWidget - Provides filter user interface for creating number filter. 
*/
function NumberWidget(widgetName) {
    /***
    * This method must return type of registered widget type in 'SetFilterWidgetType' method
    */
    this.getAssociatedTypes = function () {
        return [widgetName];
    };
    //this.getAssociatedTypes = function () {
    //    return ["System.Int32", "System.Double", "System.Decimal", "System.Byte", "System.Single", "System.Float", "System.Int64", "System.Int16", "System.UInt64", "System.UInt32", "System.UInt16"];
    //};

    /***
    * This method invokes when filter widget was shown on the page
    */
    this.onShow = function () {
        /* Place your on show logic here */
    };

    this.showClearFilterButton = function () {
        return true;
    };
    /***
    * This method will invoke when user was clicked on filter button.
    * container - html element, which must contain widget layout;
    * lang - current language settings;
    * typeName - current column type (if widget assign to multipile types, see: getAssociatedTypes);
    * values - current filter values. Array of objects [{filterValue: '', filterType:'1'}];
    * cb - callback function that must invoked when user want to filter this column. Widget must pass filter type and filter value.
    * data - widget data passed from the server
    */

    this.onRender = function (container, lang, typeName, values, cb, data) {
        //store parameters:
        this.cb = cb;
        this.container = container;
        this.lang = lang;

        //this filterwidget demo supports only 1 filter value for column column
        this.value = values.length > 0 ? values[0] : { filterType: 1, filterValue: "" };

        this.renderWidget(container); //onRender filter widget
        this.registerEvents(); //handle events
    };
    this.renderWidget = function (container) {
        var dataname = container.closest('.grid-filter').attr('data-name').replace(/\./g, '_')
        var html = genSortHtml(container) +
                    '<h4 class="">Filter</h4>\
                    <div class="input-group" role="group">\
                        <span class="input-group-btn" role="group">\
                            <button id="' + dataname + 'TextSelect" class="btn btn-default dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" onclick="$(\'#' + dataname + 'SelectList\').toggle()">\
                                Select\
                                <i class="fa fa-caret-down"></i>\
                            </button>\
                            <ul id="' + dataname + 'SelectList" class="dropdown-menu" aria-labelledby="' + dataname + 'TextSelect" style="display: none;">\
                                <li onclick="setFilterValueHiddenField(\'1\', \'' + this.lang.filterSelectTypes.Equals + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.Equals + '</li>\
                                <li onclick="setFilterValueHiddenField(\'5\', \'' + this.lang.filterSelectTypes.GreaterThan + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.GreaterThan + '</li>\
                                <li onclick="setFilterValueHiddenField(\'6\', \'' + this.lang.filterSelectTypes.LessThan + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.LessThan + '</li>\
                            </ul>\
                        </span>\
                        <input type="text" class="grid-filter-input form-control" value="' + this.value.filterValue + '" />\
                    </div>\
                    <input type="hidden" class="grid-filter-type" value="' + this.value.filterType + '" />\
                    <div class="grid-filter-buttons g-m-t-10">\
                        <button type="button" class="btn btn-success btn-block grid-apply" >' + this.lang.applyFilterButtonText + '</button>\
                    </div>'
        this.container.append(html);
        setFilterValueHiddenField('1', this.lang.filterSelectTypes.Equals + ' ', '#' + dataname + 'SelectList');
    };
    /***
    * Internal method that register event handlers for 'apply' button.
    */
    this.registerEvents = function () {
        var $context = this;
        var applyBtn = this.container.find(".grid-apply");
        applyBtn.click(function () {
            $context.container.find('.dropdown-menu').remove();
            //var type = $context.container.find(".grid-filter-type").val();
            var type = $context.container.find(".grid-filter-type:last").val();
            var value = $context.container.find(".grid-filter-input").val();
            var filters = [{ filterType: type, filterValue: value }];
            $context.cb(filters);
        });
        var txt = this.container.find(".grid-filter-input");
        txt.keyup(function (event) {
            if (event.keyCode == 13) { applyBtn.click(); }
            if (event.keyCode == 27) { GridMvc.closeOpenedPopups(); }
        })
            .keypress(function (event) { return $context.validateInput.call($context, event); });
        if (this.typeName == "System.Byte")
            txt.attr("maxlength", "3");
    };

    this.validateInput = function (evt) {
        var $event = evt || window.event;
        var key = $event.keyCode || $event.which;
        key = String.fromCharCode(key);
        var regex;
        switch (this.typeName) {
            case "System.Byte":
            case "System.Int32":
            case "System.Int64":
            case "System.UInt32":
            case "System.UInt64":
                regex = /[0-9]/;
                break;
            default:
                regex = /[0-9]|\.|\,/;
        }
        if (!regex.test(key)) {
            $event.returnValue = false;
            if ($event.preventDefault) $event.preventDefault();
        }
    };
}

/***
* DateTimeWidget - Provides filter user interface for creating date time filter. 
*/
function DateTimeWidget(widgetName) {
    /***
    * This method must return type of registered widget type in 'SetFilterWidgetType' method
    */
    this.getAssociatedTypes = function () {
        return [widgetName];
    };
    /***
    * This method invokes when filter widget was shown on the page
    */
    this.onShow = function () {
        /* Place your on show logic here */
    };

    this.showClearFilterButton = function () {
        return true;
    };
    /***
    * This method will invoke when user was clicked on filter button.
    * container - html element, which must contain widget layout;
    * lang - current language settings;
    * typeName - current column type (if widget assign to multipile types, see: getAssociatedTypes);
    * values - current filter values. Array of objects [{filterValue: '', filterType:'1'}];
    * cb - callback function that must invoked when user want to filter this column. Widget must pass filter type and filter value.
    * data - widget data passed from the server
    */
    this.onRender = function (container, lang, typeName, values, cb, data) {
        //store parameters:
        this.cb = cb;
        this.container = container;
        this.lang = lang;

        //this filterwidget demo supports only 1 filter value for column column
        this.value = values.length > 0 ? values[0] : { filterType: 1, filterValue: "" };

        this.renderWidget(container); //onRender filter widget
        this.registerEvents(); //handle events
    };
    this.renderWidget = function (container) {
        var dataname = container.closest('.grid-filter').attr('data-name').replace(/\./g, '_')
        var html = '<div class="form-group">\
                        <label>' + this.lang.filterTypeLabel + '</label>\
                        <select class="grid-filter-type form-control">\
                            <option value="1" ' + (this.value.filterType == "1" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.Equals + '</option>\
                            <option value="5" ' + (this.value.filterType == "5" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.GreaterThan + '</option>\
                            <option value="6" ' + (this.value.filterType == "6" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.LessThan + '</option>\
                        </select>\
                    </div>' +
                        (this.datePickerIncluded ?
                            '<div class="grid-filter-datepicker"></div>'
                            :
                            '<div class="form-group">\
                                <label>' + this.lang.filterValueLabel + '</label>\
                                <input type="text" class="grid-filter-input form-control" value="' + this.value.filterValue + '" />\
                             </div>\
                             <div class="grid-filter-buttons">\
                                <input type="button" class="btn btn-primary grid-apply" value="' + this.lang.applyFilterButtonText + '" />\
                             </div>');
        var html = genSortHtml(container) +
        //            '<h4 class="">Filter</h4>\
        //            <div class="form-group">\
        //                <label>' + this.lang.filterTypeLabel + '</label>\
        //                <select class="grid-filter-type form-control">\
        //                    <option value="1" ' + (this.value.filterType == "1" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.Equals + '</option>\
        //                    <option value="5" ' + (this.value.filterType == "5" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.GreaterThan + '</option>\
        //                    <option value="6" ' + (this.value.filterType == "6" ? "selected=\"selected\"" : "") + '>' + this.lang.filterSelectTypes.LessThan + '</option>\
        //                </select>\
        //            </div>' +
        //                (this.datePickerIncluded ?
        //                    '<div class="grid-filter-datepicker"></div>'
        //                    :
        //                    '<div class="form-group">\
        //                        <label>' + this.lang.filterValueLabel + '</label>\
        //                        <input type="text" class="grid-filter-input form-control" value="' + this.value.filterValue + '" />\
        //                     </div>\
        //                     <div class="grid-filter-buttons">\
        //                        <input type="button" class="btn btn-primary grid-apply" value="' + this.lang.applyFilterButtonText + '" />\
        //                     </div>');
        //var html = genSortHtml(container) +
        //            '<h4 class="">Filter</h4>\
        //            <div class="input-group" role="group">\
        //                <span class="input-group-btn" role="group">\
        //                    <button id="' + dataname + 'TextSelect" class="btn btn-default dropdown-toggle" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" onclick="$(\'#' + dataname + 'SelectList\').toggle()">\
        //                        Select\
        //                        <i class="fa fa-caret-down"></i>\
        //                    </button>\
        //                    <ul id="' + dataname + 'SelectList" class="dropdown-menu" aria-labelledby="' + dataname + 'TextSelect" style="display: none;">\
        //                        <li onclick="setFilterValueHiddenField(\'1\', \'' + this.lang.filterSelectTypes.Equals + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.Equals + '</li>\
        //                        <li onclick="setFilterValueHiddenField(\'5\', \'' + this.lang.filterSelectTypes.GreaterThan + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.GreaterThan + '</li>\
        //                        <li onclick="setFilterValueHiddenField(\'6\', \'' + this.lang.filterSelectTypes.LessThan + ' \', \'#' + dataname + 'SelectList\')">' + this.lang.filterSelectTypes.LessThan + '</li>\
        //                    </ul>\
        //                </span>\
        //                <input type="text" class="grid-filter-input form-control" value="' + this.value.filterValue + '" />\
        //            </div>\
        //            <input type="hidden" class="grid-filter-type" value="' + this.value.filterType + '" />\
        //            <div class="grid-filter-buttons g-m-t-10">\
        //                <button type="button" class="btn btn-success btn-block grid-apply" >' + this.lang.applyFilterButtonText + '</button>\
        //            </div>'
        this.container.append(html);
    };
    //if window.jQueryUi included:
    if (this.datePickerIncluded) {
        var datePickerOptions = this.data || {};
        datePickerOptions.format = datePickerOptions.format || "yyyy-mm-dd";
        datePickerOptions.language = datePickerOptions.language || this.lang.code;

        var $context = this;
        var dateContainer = this.container.find(".grid-filter-datepicker");
        dateContainer.datepicker(datePickerOptions).on('changeDate', function (ev) {
            var type = $context.container.find(".grid-filter-type").val();
            //if (type == "1") {
            //    var tomorrow = new Date(ev.getTime());
            //    tomorrow.setDate(ev.getDate() + 1);
            //    var filterValues = [{ filterType: type, filterValue: ev.format() }];
            //}
            //else{
            var filterValues = [{ filterType: type, filterValue: ev.format() }];
            //}
            $context.cb(filterValues);
        });
        if (this.value.filterValue)
            dateContainer.datepicker('update', this.value.filterValue);
    }
    /***
    * Internal method that register event handlers for 'apply' button.
    */
    this.registerEvents = function () {
        //get apply button from:
        var applyBtn = this.container.find(".grid-apply");
        //save current context:
        var $context = this;
        //register onclick event handler
        applyBtn.click(function () {
            $context.container.find('.dropdown-menu').remove();
            //get selected filter type:
            //var type = $context.container.find(".grid-filter-type").val();
            var type = $context.container.find(".grid-filter-type:last").val();
            //get filter value:
            var value = $context.container.find(".grid-filter-input").val();
            //invoke callback with selected filter values:
            var filterValues = [{ filterType: type, filterValue: value }];
            $context.cb(filterValues);
        });
        //register onEnter event for filter text box:
        this.container.find(".grid-filter-input").keyup(function (event) {
            if (event.keyCode == 13) { applyBtn.click(); }
            if (event.keyCode == 27) { GridMvc.closeOpenedPopups(); }
        });
    };

}

/***
* BooleanWidget - Provides filter user interface for creating boolean filter. 
*/
function BooleanWidget(widgetName) {
    /***
    * This method must return type of registered widget type in 'SetFilterWidgetType' method
    */
    this.getAssociatedTypes = function () {
        return [widgetName];
    };
    /***
    * This method invokes when filter widget was shown on the page
    */
    this.onShow = function () {
        /* Place your on show logic here */
    };

    this.showClearFilterButton = function () {
        return true;
    };
    /***
    * This method will invoke when user was clicked on filter button.
    * container - html element, which must contain widget layout;
    * lang - current language settings;
    * typeName - current column type (if widget assign to multipile types, see: getAssociatedTypes);
    * values - current filter values. Array of objects [{filterValue: '', filterType:'1'}];
    * cb - callback function that must invoked when user want to filter this column. Widget must pass filter type and filter value.
    * data - widget data passed from the server
    */
    this.onRender = function (container, lang, typeName, values, cb, data) {
        //store parameters:
        this.cb = cb;
        this.container = container;
        this.lang = lang;

        //this filterwidget demo supports only 1 filter value for column column
        this.value = values.length > 0 ? values[0] : { filterType: 1, filterValue: "" };

        this.renderWidget(container); //onRender filter widget
        this.registerEvents(); //handle events
    };
    this.renderWidget = function (container) {
        var dataname = container.closest('.grid-filter').attr('data-name').replace(/\./g, '_')
        var html = genSortHtml(container) +
                    '<h4 class="">Value</h4>\
                    <ul class="menu-list">\
                        <li><a class="grid-filter-choose ' + (this.value.filterValue == "true" ? "choose-selected" : "") + '" data-value="true" href="javascript:void(0);">True</a></li>\
                        <li><a class="grid-filter-choose ' + (this.value.filterValue == "false" ? "choose-selected" : "") + '" data-value="false" href="javascript:void(0);">False</a></li>\
                    </ul>';
        this.container.append(html);
    };
    /***
    * Internal method that register event handlers for 'apply' button.
    */
    this.registerEvents = function () {
        var $context = this;
        var applyBtn = this.container.find(".grid-filter-choose");
        applyBtn.click(function () {
            $context.container.find('.dropdown-menu').remove();
            var filterValues = [{ filterType: "1", filterValue: $(this).attr("data-value") }];
            $context.cb(filterValues);
        });
    };
}

function genSortHtml(container) {
    var sortdir = 2;
    var addSearch = '';
    
    var searchArr = location.search.substring(1).split("&");
    for (i = 0; i < searchArr.length; i++) {
        var entryArr = searchArr[i].split("=");
        if (entryArr[0] == 'grid-filter') {
            addSearch += '&' + searchArr[i];
        }
    }
    if (container.closest('.grid-header').find('.grid-header-title').hasClass('sorted-asc')) {
        sortdir = 0;
    }
    if (container.closest('.grid-header').find('.grid-header-title').hasClass('sorted-desc')) {
        sortdir = 1;
    }
    var html = '<h4 class="">Sort</h4><div class="grid-sort">';
    if (sortdir == 1 || sortdir == 2) {
        html += '<a onclick="javascript: location.href = \'?grid-column=\' + $(this).closest(\'.grid-filter\').attr(\'data-name\') + \'&grid-dir=0' + addSearch +  '\';" class="btn btn-default grid-sort-az g-m-r-10" type="button"><span class="fa fa-sort-alpha-asc"><span></a>';
    }
    if (sortdir == 0 || sortdir == 2) {
        html += '<a onclick="javascript: location.href = \'?grid-column=\' + $(this).closest(\'.grid-filter\').attr(\'data-name\') + \'&grid-dir=1' + addSearch + '\';" class="btn btn-default grid-sort-za" role="button"><span class="fa fa-sort-alpha-desc"><span></a>';
    }
    html += '</div>';

    return html;
}

function setFilterValueHiddenField(value, text, id) {
    $(id).closest('.grid-popup-widget').find('.grid-filter-type').val(value);
    $(id.replace('SelectList', 'TextSelect')).html(text + '<i class="fa fa-caret-down"></i>');
    toggleVisibility(id);
}