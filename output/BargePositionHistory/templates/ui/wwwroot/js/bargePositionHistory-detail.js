/**
 * Barge Position History Edit/Detail Page JavaScript
 * Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\bargePositionHistory-detail.js
 *
 * CRITICAL: Handles DateTime split/combine pattern and LeftFleet conditional logic.
 */

var BargePositionHistoryDetail = (function() {
    'use strict';

    let config = {};

    /**
     * Initialize the detail/edit page.
     * @param {Object} options - Configuration options (positionStartDateTime, leftFleet)
     */
    function init(options) {
        config = options || {};
        initializeSelect2();
        initializeDateTimeSplit();
        initializeLeftFleetLogic();
        initializeValidation();
        initializeFormSubmit();
    }

    /**
     * Initialize Select2 for dropdowns.
     */
    function initializeSelect2() {
        $('[data-select2="true"]').select2({
            placeholder: 'Select...',
            allowClear: true,
            width: '100%'
        });
    }

    /**
     * CRITICAL: Initialize DateTime split pattern.
     * Splits PositionStartDateTime into separate date and time inputs on load.
     */
    function initializeDateTimeSplit() {
        if (config.positionStartDateTime) {
            const dateTime = new Date(config.positionStartDateTime);
            if (!isNaN(dateTime.getTime())) {
                // Set date input (YYYY-MM-DD format for HTML5 date input)
                const dateStr = dateTime.toISOString().split('T')[0];
                $('#dtPositionDate').val(dateStr);

                // Set time input (HH:mm format for HTML5 time input, 24-hour)
                const hours = ('0' + dateTime.getHours()).slice(-2);
                const minutes = ('0' + dateTime.getMinutes()).slice(-2);
                const timeStr = hours + ':' + minutes;
                $('#dtPositionTime').val(timeStr);
            }
        } else {
            // Default to current date/time
            const now = new Date();
            $('#dtPositionDate').val(now.toISOString().split('T')[0]);
            const hours = ('0' + now.getHours()).slice(-2);
            const minutes = ('0' + now.getMinutes()).slice(-2);
            $('#dtPositionTime').val(hours + ':' + minutes);
        }
    }

    /**
     * CRITICAL: Initialize LeftFleet checkbox logic.
     * When checked, disables and clears tier-related fields.
     */
    function initializeLeftFleetLogic() {
        const leftFleetCheckbox = $('#LeftFleet');

        // Set initial state
        updateTierFieldsState(leftFleetCheckbox.is(':checked'));

        // Handle checkbox change
        leftFleetCheckbox.on('change', function() {
            const isChecked = $(this).is(':checked');
            updateTierFieldsState(isChecked);
        });
    }

    /**
     * Update tier fields enabled/disabled state based on LeftFleet.
     * @param {boolean} leftFleet - True if barge left fleet
     */
    function updateTierFieldsState(leftFleet) {
        const tierFields = $('#TierID, #TierX, #TierY');

        if (leftFleet) {
            // Disable and clear tier fields
            tierFields.prop('disabled', true);
            $('#TierID').val('').trigger('change');
            $('#TierX, #TierY').val('');
        } else {
            // Enable tier fields
            tierFields.prop('disabled', false);
        }
    }

    /**
     * Initialize client-side validation.
     */
    function initializeValidation() {
        // Add remote validation for BargeNum
        $('#BargeNum').on('blur', function() {
            const bargeNum = $(this).val();
            if (bargeNum) {
                validateBargeNum(bargeNum);
            }
        });
    }

    /**
     * Validate barge number via remote API call.
     * @param {string} bargeNum - Barge number to validate
     */
    function validateBargeNum(bargeNum) {
        $.ajax({
            url: '/BargePositionHistory/ValidateBarge',
            type: 'GET',
            data: { bargeNum: bargeNum },
            success: function(isValid) {
                const errorSpan = $('span[data-valmsg-for="BargeNum"]');
                if (!isValid) {
                    errorSpan.text('Barge number must match an existing barge record.');
                    errorSpan.removeClass('field-validation-valid').addClass('field-validation-error');
                } else {
                    errorSpan.text('');
                    errorSpan.removeClass('field-validation-error').addClass('field-validation-valid');
                }
            }
        });
    }

    /**
     * CRITICAL: Initialize form submit handler to combine date and time.
     */
    function initializeFormSubmit() {
        $('#editForm').on('submit', function(e) {
            const dateValue = $('#dtPositionDate').val();
            const timeValue = $('#dtPositionTime').val();

            // Validate date and time are provided
            if (!dateValue || !timeValue) {
                e.preventDefault();
                alert('Position Date and Time are required.');
                return false;
            }

            // Combine date and time into ISO 8601 format
            const combined = combineDateTimeISO(dateValue, timeValue);

            // Create hidden input for PositionStartDateTime
            let hiddenInput = $('#PositionStartDateTime');
            if (hiddenInput.length === 0) {
                hiddenInput = $('<input>')
                    .attr('type', 'hidden')
                    .attr('id', 'PositionStartDateTime')
                    .attr('name', 'PositionStartDateTime');
                $('#editForm').append(hiddenInput);
            }
            hiddenInput.val(combined);

            // Clear tier fields if LeftFleet is checked (server-side validation)
            if ($('#LeftFleet').is(':checked')) {
                $('#TierID').val('');
                $('#TierX, #TierY').val('');
            }

            return true;
        });
    }

    /**
     * Combine date and time into ISO 8601 format.
     * @param {string} date - Date string (YYYY-MM-DD)
     * @param {string} time - Time string (HH:mm)
     * @returns {string} Combined DateTime in ISO 8601 format
     */
    function combineDateTimeISO(date, time) {
        if (!date || !time) {
            return '';
        }
        // Combine as ISO 8601: YYYY-MM-DDTHH:mm:ss
        return date + 'T' + time + ':00';
    }

    /**
     * Split DateTime into date and time components (utility function).
     * @param {string} dateTimeValue - DateTime string in ISO 8601 format
     * @returns {Object} Object with date and time properties
     */
    function splitDateTime(dateTimeValue) {
        if (!dateTimeValue) {
            return { date: '', time: '' };
        }

        const dateTime = new Date(dateTimeValue);
        if (isNaN(dateTime.getTime())) {
            return { date: '', time: '' };
        }

        const date = dateTime.toISOString().split('T')[0];
        const hours = ('0' + dateTime.getHours()).slice(-2);
        const minutes = ('0' + dateTime.getMinutes()).slice(-2);
        const time = hours + ':' + minutes;

        return { date: date, time: time };
    }

    // Public API
    return {
        init: init,
        splitDateTime: splitDateTime,
        combineDateTimeISO: combineDateTimeISO
    };
})();
