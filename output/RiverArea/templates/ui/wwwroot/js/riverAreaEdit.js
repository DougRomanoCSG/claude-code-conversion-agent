/**
 * River Area Edit Page
 * Form validation, conditional logic, and child segment management
 */

$(document).ready(function () {
    // Initialize Select2 dropdowns
    $('#CustomerID').select2({
        placeholder: 'Select Customer',
        allowClear: true,
        width: '100%'
    });

    $('.river-select').select2({
        placeholder: 'Select River',
        width: '100%'
    });

    // Mutually exclusive checkbox logic
    // Only one of IsPriceZone, IsPortalArea, IsHighWaterArea can be checked
    $('#IsPriceZone, #IsPortalArea, #IsHighWaterArea').on('change', function () {
        if (this.checked) {
            $('#IsPriceZone, #IsPortalArea, #IsHighWaterArea').not(this).prop('checked', false);
        }
    });

    // Show/hide CustomerID based on IsHighWaterArea
    function toggleCustomerField() {
        var showCustomer = $('#IsHighWaterArea').is(':checked');
        var $customerGroup = $('#CustomerID').closest('.form-group');

        if (showCustomer) {
            $customerGroup.show();
            // Add required validation
            $('#CustomerID').attr('required', 'required');
        } else {
            $customerGroup.hide();
            // Remove required validation and clear value
            $('#CustomerID').removeAttr('required').val(null).trigger('change');
        }
    }

    // Initialize on page load
    toggleCustomerField();

    // Handle checkbox change
    $('#IsHighWaterArea').on('change', function () {
        toggleCustomerField();
    });

    // Initialize segments grid (client-side DataTable)
    var segmentsTable = $('#segmentsGrid').DataTable({
        paging: false,
        searching: false,
        info: false,
        ordering: true,
        order: [[1, 'asc']],  // Sort by River
        columns: [
            {
                // Actions column
                data: null,
                orderable: false,
                className: 'text-center',
                width: '80px',
                render: function (data, type, row, meta) {
                    return `<button type="button" class="btn btn-danger btn-sm btn-delete-segment" data-index="${meta.row}" title="Delete">
                                <i class="fas fa-trash"></i>
                            </button>`;
                }
            },
            {
                data: 'river',
                title: 'River'
            },
            {
                data: 'startMile',
                title: 'Start Mile',
                className: 'text-end',
                render: function (data) {
                    return data != null ? parseFloat(data).toFixed(2) : '';
                }
            },
            {
                data: 'endMile',
                title: 'End Mile',
                className: 'text-end',
                render: function (data) {
                    return data != null ? parseFloat(data).toFixed(2) : '';
                }
            }
        ]
    });

    // Add segment button
    $('#btnAddSegment').on('click', function (e) {
        e.preventDefault();

        // Get values
        var river = $('#SegmentRiver').val();
        var startMile = $('#SegmentStartMile').val();
        var endMile = $('#SegmentEndMile').val();

        // Validate
        if (!river || !startMile || !endMile) {
            alert('Please enter River, Start Mile, and End Mile');
            return;
        }

        var startMileNum = parseFloat(startMile);
        var endMileNum = parseFloat(endMile);

        if (isNaN(startMileNum) || isNaN(endMileNum)) {
            alert('Start Mile and End Mile must be valid numbers');
            return;
        }

        if (startMileNum >= endMileNum) {
            alert('Start Mile must be less than End Mile');
            return;
        }

        // Add to table
        segmentsTable.row.add({
            riverAreaSegmentID: 0,  // New segment
            riverAreaID: $('#RiverAreaID').val() || 0,
            river: river,
            startMile: startMileNum,
            endMile: endMileNum
        }).draw();

        // Clear form
        $('#SegmentRiver').val(null).trigger('change');
        $('#SegmentStartMile').val('');
        $('#SegmentEndMile').val('');
        $('#SegmentRiver').focus();
    });

    // Delete segment button (delegated event)
    $('#segmentsGrid').on('click', '.btn-delete-segment', function () {
        var index = $(this).data('index');
        segmentsTable.row(index).remove().draw();
    });

    // Form submission - serialize segments
    $('form').on('submit', function (e) {
        // Remove existing segment hidden fields
        $('input[name^="RiverArea.Segments"]').remove();

        // Get segments data
        var segments = segmentsTable.rows().data().toArray();

        // Add hidden fields for each segment
        segments.forEach(function (segment, index) {
            $('<input>').attr({
                type: 'hidden',
                name: `RiverArea.Segments[${index}].RiverAreaSegmentID`,
                value: segment.riverAreaSegmentID || 0
            }).appendTo('form');

            $('<input>').attr({
                type: 'hidden',
                name: `RiverArea.Segments[${index}].RiverAreaID`,
                value: segment.riverAreaID || 0
            }).appendTo('form');

            $('<input>').attr({
                type: 'hidden',
                name: `RiverArea.Segments[${index}].River`,
                value: segment.river
            }).appendTo('form');

            $('<input>').attr({
                type: 'hidden',
                name: `RiverArea.Segments[${index}].StartMile`,
                value: segment.startMile
            }).appendTo('form');

            $('<input>').attr({
                type: 'hidden',
                name: `RiverArea.Segments[${index}].EndMile`,
                value: segment.endMile
            }).appendTo('form');
        });

        // Validate mutually exclusive flags (server-side will also validate)
        var checkedCount = [$('#IsPriceZone'), $('#IsPortalArea'), $('#IsHighWaterArea')]
            .filter(function ($cb) { return $cb.is(':checked'); })
            .length;

        if (checkedCount > 1) {
            e.preventDefault();
            alert('Only one of these may be checked: Pricing Zone, Portal Area, or High Water Area');
            return false;
        }

        // Validate CustomerID if IsHighWaterArea is checked
        if ($('#IsHighWaterArea').is(':checked') && !$('#CustomerID').val()) {
            e.preventDefault();
            alert('High Water Customer is required when High Water Area is checked');
            return false;
        }

        return true;
    });

    // Load existing segments into table (if editing)
    var existingSegments = window.riverAreaSegments || [];
    existingSegments.forEach(function (segment) {
        segmentsTable.row.add(segment).draw();
    });
});
