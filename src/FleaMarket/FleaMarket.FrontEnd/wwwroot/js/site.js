// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Handle reservation button clicks on the listings page.
$(document).on('click', '.reserve-button', function () {
    const itemName = $(this).data('item-name');
    const sellerEmail = $(this).data('seller-email');
    const confirmed = confirm('Reserve "' + itemName + '"?\n\nYou will need to contact the seller directly to complete the purchase.');
    if (confirmed) {
        alert('Reservation noted! Please email ' + sellerEmail + ' to arrange pickup or payment.');
    }
});
