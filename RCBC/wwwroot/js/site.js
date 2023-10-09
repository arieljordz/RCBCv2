// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function ActiveMenu(id) {
    var targetObj = $(id);
    $('.nav-sidebar').find('.menu-open').removeClass('menu-open');
    $('.nav-sidebar').find('.nav-link').removeClass('active');
    $('.nav-item').find('.menu-open').removeClass('menu-open');
    $('.nav-item').find('.nav-link').removeClass('active');
    setTimeout(function () {
        var currObj = targetObj;
        if (currObj.hasClass('nav-link')) {
            currObj.addClass('active');
        }
        while (!currObj.hasClass('nav-sidebar')) {
            if (currObj.hasClass('nav')) {
                currObj.show();
            }
            if (currObj.hasClass('nav-item')) {

                currObj.addClass('menu-open');
            }
            currObj = currObj.parent();
        }
    }, 100);
}

function SelectedValue(DataTableID, rowData) {
    var row = $("#" + DataTableID).find(".dtactive");
    if (row.length > 0) {
        return $("#" + DataTableID).DataTable().row(row[0]).data()[rowData];
    }
    else {
        return 0;
    }
}

function TextMoneyFormat(value) {
    return parseFloat(value).toLocaleString("en", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function DateToText(jsonDate) {
    if (jsonDate != null) {

        var date = new Date(jsonDate);
        var mm = (date.getMonth() + 1).toString();
        var dd = date.getDate() < 10 ? "0" + date.getDate() : date.getDate();

        if ((date.getMonth() + 1).toString() < 10) {
            mm = "0" + (date.getMonth() + 1).toString();
        }
        var res = date.getFullYear().toString() + "-" + mm + "-" + dd;
        return res;
    }
    return null;
}
