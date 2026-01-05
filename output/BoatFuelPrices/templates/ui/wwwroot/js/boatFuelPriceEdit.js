// boatFuelPriceEdit.js
// Edit modal and form submission logic for Boat Fuel Prices

$(document).ready(function () {
    // Initialize Select2 for vendor dropdown in modal
    $('#FuelVendorBusinessUnitID').select2({
        dropdownParent: $('#editModal'),
        placeholder: '-- Select Vendor --',
        allowClear: true,
        width: '100%'
    });

    // Initialize currency input formatting
    $('#Price').maskMoney({
        prefix: '',
        allowNegative: false,
        thousands: ',',
        decimal: '.',
        precision: 4,
        affixesStay: false
    });

    // Conditional logic: Enable/disable InvoiceNumber based on vendor selection
    $('#FuelVendorBusinessUnitID').on('change', function () {
        var hasVendor = $(this).val() !== '' && $(this).val() !== null;
        $('#InvoiceNumber').prop('disabled', !hasVendor);

        // Clear invoice number if vendor is cleared
        if (!hasVendor) {
            $('#InvoiceNumber').val('');
        }
    });

    // Initialize conditional state on load (for add mode)
    $('#FuelVendorBusinessUnitID').trigger('change');

    // Save button click
    $('#btnSave').on('click', function () {
        if (!validateForm()) {
            return;
        }

        var id = parseInt($('#BoatFuelPriceID').val());
        var isNew = id === 0;

        // Build DTO object
        var dto = {
            boatFuelPriceID: id,
            effectiveDate: $('#EffectiveDate').val(),
            price: parseFloat($('#Price').val().replace(/,/g, '')),
            fuelVendorBusinessUnitID: $('#FuelVendorBusinessUnitID').val() ? parseInt($('#FuelVendorBusinessUnitID').val()) : null,
            invoiceNumber: $('#InvoiceNumber').val() || null
        };

        // Determine URL and method
        var url = isNew ? '/BoatFuelPriceSearch/Create' : '/BoatFuelPriceSearch/Edit';

        // Submit via AJAX
        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            success: function (response) {
                if (response.success) {
                    // Close modal
                    $('#editModal').modal('hide');

                    // Refresh table
                    $('#boatFuelPriceTable').DataTable().ajax.reload(null, false);

                    // Show success message
                    showNotification('success', response.message);
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function (xhr) {
                console.error('Error saving boat fuel price:', xhr);
                var errorMessage = 'Error saving boat fuel price. Please try again.';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }
                
                alert(errorMessage);
            }
        });
    });

    /**
     * Validate form before submission
     */
    function validateForm() {
        var isValid = true;

        // Reset validation
        $('.is-invalid').removeClass('is-invalid');

        // Effective Date validation
        if (!$('#EffectiveDate').val()) {
            $('#EffectiveDate').addClass('is-invalid');
            isValid = false;
        }

        // Price validation
        var price = $('#Price').val().replace(/,/g, '');
        if (!price || parseFloat(price) <= 0) {
            $('#Price').addClass('is-invalid');
            isValid = false;
        }

        // InvoiceNumber conditional validation
        var hasVendor = $('#FuelVendorBusinessUnitID').val() !== '' && $('#FuelVendorBusinessUnitID').val() !== null;
        var hasInvoice = $('#InvoiceNumber').val() !== '';

        if (!hasVendor && hasInvoice) {
            alert('Vendor inv# must be blank when Fuel vendor is blank.');
            $('#InvoiceNumber').addClass('is-invalid');
            isValid = false;
        }

        return isValid;
    }

    /**
     * Show notification (assumes you have a notification system)
     */
    function showNotification(type, message) {
        // TODO: Implement your notification system
        // For now, just use alert
        if (type === 'success') {
            alert(message);
        }
    }

    // Clear validation on input change
    $('#EffectiveDate, #Price, #InvoiceNumber').on('change input', function () {
        $(this).removeClass('is-invalid');
    });
});
