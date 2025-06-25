// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


// Preview selected images when listing an item.
$(document).on('change', '#images', function () {
    const preview = $('#imagePreview');
    preview.empty();
    const files = this.files;
    if (!files) return;
    Array.from(files).forEach(file => {
        const reader = new FileReader();
        reader.onload = e => {
            const col = $('<div class="col-6 col-md-3 mb-2"></div>');
            $('<img class="img-fluid" />').attr('src', e.target.result).appendTo(col);
            col.appendTo(preview);
        };
        reader.readAsDataURL(file);
    });
});
