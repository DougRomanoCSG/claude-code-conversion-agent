// river-detail.js
// Client-side validation and form handling for River edit/create form

$(document).ready(function () {
    // Uppercase transformation for Code field
    $('#River_Code').on('blur', function () {
        var $this = $(this);
        $this.val($this.val().toUpperCase());
    });

    // Custom validation for exact length Code field
    $.validator.addMethod('exactlength', function (value, element, param) {
        return this.optional(element) || value.length === param;
    }, 'Code must be exactly {0} characters');

    // Custom validation for StartMile <= EndMile
    $.validator.addMethod('milesvalid', function (value, element) {
        var startMile = parseFloat($('#River_StartMile').val());
        var endMile = parseFloat($('#River_EndMile').val());

        // If either is empty or NaN, validation passes (let required validation handle it)
        if (isNaN(startMile) || isNaN(endMile)) {
            return true;
        }

        return startMile <= endMile;
    }, 'Start mile must be less than or equal to End mile');

    // Apply custom validation rules
    $('form').validate({
        rules: {
            'River.Code': {
                exactlength: 3
            },
            'River.EndMile': {
                milesvalid: true
            }
        },
        errorPlacement: function (error, element) {
            error.addClass('text-danger');
            element.closest('.mb-3, .col-md-6').find('.text-danger').first().html(error);
        },
        highlight: function (element) {
            $(element).addClass('is-invalid');
        },
        unhighlight: function (element) {
            $(element).removeClass('is-invalid');
        }
    });

    // Format mile fields on blur
    $('#River_StartMile, #River_EndMile').on('blur', function () {
        var $this = $(this);
        var value = parseFloat($this.val());

        if (!isNaN(value)) {
            $this.val(value.toFixed(2));
        }
    });

    // Form submission handling
    $('form').on('submit', function (e) {
        if (!$(this).valid()) {
            e.preventDefault();
            return false;
        }

        // Additional client-side validation can go here
        return true;
    });
});
