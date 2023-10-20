// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


String.format = function () {
    var s = arguments[0];
    for (var i = 0; i < arguments.length - 1; i++) {
        var reg = new RegExp("\\{" + i + "\\}", "gm");
        s = s.replace(reg, arguments[i + 1]);
    }

    return s;
}
$(function () {
    $('#main-panel').slimscroll({
        alwaysVisible: true,
        distance: '4px',
        height: '100%',
        wheelStep: 40,
        railOpacity: 0.4,
        size: '4px',
    });
    $('#main-panel').css('width', '');
});