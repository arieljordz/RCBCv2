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

function IsStrongPassword(password) {
    // Define criteria for a strong password
    const minLength = 8; // Minimum length
    const minDigitCount = 1; // Minimum number of digits
    const minUpperCount = 1; // Minimum number of uppercase letters
    const minLowerCount = 1; // Minimum number of lowercase letters
    const minSpecialCount = 1; // Minimum number of special characters
    const specialCharacters = "!@#$%^&*()_+[]{}|;:,.<>?";

    // Check if the password meets the criteria
    if (password.length < minLength) return false;
    if ((password.match(/\d/g) || []).length < minDigitCount) return false;
    if ((password.match(/[A-Z]/g) || []).length < minUpperCount) return false;
    if ((password.match(/[a-z]/g) || []).length < minLowerCount) return false;
    if ((password.split('').filter(c => specialCharacters.includes(c)).length) < minSpecialCount) return false;

    // You can add more complex checks as needed, such as checking for common passwords, dictionary words, or patterns.

    return true;
}


function CheckMinimunChars(string) {
    const minLength = 8; // Minimum length
    if (string.length < minLength) return false;

    return true;
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

function DateDataTable(data) {
    var date = new Date(data);

    //Format the date as you want, for example: "MM/DD/YYYY HH:mm:ss"
    var formattedDate = (date.getMonth() + 1) + '/' + date.getDate() + '/' + date.getFullYear() +
        ' ' + date.getHours() + ':' + (date.getMinutes() < 10 ? '0' : '') + date.getMinutes() +
        ':' + (date.getSeconds() < 10 ? '0' : '') + date.getSeconds();

    //var formattedDate = (date.getMonth() + 1) + '/' + date.getDate() + '/' + date.getFullYear();
    return formattedDate;
}

let timeout;

function GetTimeout(callback) {
    $.get('/GetTimeout', function (res) {
        //console.log(res.timeOut);
        callback(res.timeOut);
    });
}

function resetIdleTimeout() {
    clearTimeout(timeout);
    GetTimeout(function (timeoutValue) {
        timeout = setTimeout(sessionTimeout, timeoutValue);
    });
}

function sessionTimeout() {
    $.get('/ResetCookies', function (response) { });
    $("#modal_signout").modal("show");
}

$(document).on('click keypress', resetIdleTimeout);

resetIdleTimeout();


