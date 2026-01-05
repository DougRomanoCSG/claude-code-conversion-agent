// boatStatus.js
// ⭐ Boat Status (BoatMaintenanceLog) management JavaScript
// ⭐ Handles DataTables, conditional field display, split/combine DateTime, cascading dropdowns

$(document).ready(function () {
    // ⭐ Initialize DataTables (server-side processing)
    var locationId = $('#LocationID').val();
    var dataTable = $('#boatStatusTable').DataTable({
        processing: true,
        serverSide: true,
        stateSave: true,
        stateKey: 'boatStatusTable_v1',
        responsive: true,
        autoWidth: false,
        ajax: {
            url: '/BoatStatus/GetMaintenanceLogTable',
            type: 'POST',
            contentType: 'application/json',
            data: function (d) {
                d.locationID = parseInt(locationId);
                return JSON.stringify(d);
            }
        },
        columns: [
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-center',
                width: '80px',
                render: function (data, type, row) {
                    return '<div class="btn-group btn-group-sm" role="group">' +
                        '<button class="btn btn-sm btn-primary btn-edit" data-id="' + row.boatMaintenanceLogID + '" title="Edit">' +
                        '<i class="fas fa-edit"></i></button>' +
                        '<button class="btn btn-sm btn-danger btn-delete" data-id="' + row.boatMaintenanceLogID + '" title="Delete">' +
                        '<i class="fas fa-trash"></i></button>' +
                        '</div>';
                }
            },
            {
                data: 'startDateTime',
                name: 'startDateTime',
                render: function (data) {
                    // ⭐ Format DateTime as MM/dd/yyyy HH:mm (24-hour format)
                    if (data) {
                        var date = new Date(data);
                        return date.toLocaleString('en-US', {
                            year: 'numeric',
                            month: '2-digit',
                            day: '2-digit',
                            hour: '2-digit',
                            minute: '2-digit',
                            hour12: false
                        });
                    }
                    return '';
                }
            },
            { data: 'maintenanceType', name: 'maintenanceType', defaultContent: '' },
            { data: 'status', name: 'status', defaultContent: '' },
            { data: 'division', name: 'division', defaultContent: '' },
            { data: 'portFacility', name: 'portFacility', defaultContent: '' },
            { data: 'boatRole', name: 'boatRole', defaultContent: '' },
            { data: 'note', name: 'note', defaultContent: '' }
        ],
        order: [[1, 'desc']], // Default sort by StartDateTime descending
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        language: {
            processing: '<i class="fas fa-spinner fa-spin"></i> Loading...'
        },
        drawCallback: function (settings) {
            // Initialize tooltips
            $('[data-bs-toggle="tooltip"]').tooltip();

            // Attach event handlers for Edit and Delete buttons
            attachGridEventHandlers();
        }
    });

    // ⭐ Initialize Select2 for all dropdowns
    $('[data-select2="true"]').select2({
        allowClear: true,
        width: '100%',
        placeholder: function () {
            return $(this).data('placeholder') || '-- Select --';
        }
    });

    // ⭐ Add button handler
    $('#btnAdd').on('click', function () {
        clearForm();
        $('#detailSection').show();
        $('#IsNew').val('true');
        $('input[name="MaintenanceLog.MaintenanceType"]').prop('disabled', false);
        enableDisableFields();
        $('#dtStartDate').focus();
    });

    // ⭐ Cancel button handler
    $('#btnCancel').on('click', function () {
        if (confirm('Discard changes?')) {
            $('#detailSection').hide();
            clearForm();
        }
    });

    // ⭐ Export button handler
    $('#btnExport').on('click', function () {
        // DataTables export to Excel
        dataTable.button('.buttons-excel').trigger();
    });

    // ⭐ Maintenance Type change handler (conditional field display)
    $('input[name="MaintenanceLog.MaintenanceType"]').on('change', function () {
        enableDisableFields();
    });

    // ⭐ Division change handler (cascading dropdown)
    $('#cboDivision').on('change', function () {
        var division = $(this).val();
        var $portFacility = $('#cboPortFacility');

        if (division) {
            // Load port facilities for selected division
            $.get('/BoatStatus/GetPortFacilitiesByDivision', {
                division: division,
                locationId: locationId
            }, function (data) {
                $portFacility.empty().append('<option value="">-- Select Port Facility --</option>');
                $.each(data, function (i, item) {
                    $portFacility.append($('<option>', {
                        value: item.value,
                        text: item.text
                    }));
                });
                $portFacility.trigger('change.select2');
            });
        } else {
            $portFacility.empty().append('<option value="">-- Select Port Facility --</option>').trigger('change.select2');
        }
    });

    // ⭐ Form submit handler
    $('#detailForm').on('submit', function (e) {
        e.preventDefault();

        // ⭐ CRITICAL: Combine date and time before submit
        var combinedDateTime = combineDateTime('dtStartDate', 'dtStartTime');
        if (!combinedDateTime) {
            alert('Start date and time are required');
            return false;
        }

        // Set combined datetime as hidden field
        var $startDateTimeInput = $('<input>').attr({
            type: 'hidden',
            name: 'MaintenanceLog.StartDateTime',
            value: combinedDateTime
        });
        $(this).append($startDateTimeInput);

        // ⭐ CRITICAL: Clear unused fields based on MaintenanceType
        clearUnusedFields();

        // Submit form via AJAX
        var formData = $(this).serialize();
        $.post($(this).attr('action'), formData)
            .done(function (response) {
                if (response.success) {
                    $('#detailSection').hide();
                    dataTable.ajax.reload();
                    clearForm();
                } else {
                    alert('Error saving: ' + (response.error || 'Unknown error'));
                }
            })
            .fail(function (xhr) {
                alert('Error saving maintenance log: ' + xhr.statusText);
            })
            .always(function () {
                // Remove the temporary hidden field
                $startDateTimeInput.remove();
            });

        return false;
    });

    // Initialize conditional fields on load
    enableDisableFields();
});

// ⭐ CRITICAL: Split DateTime into separate date and time inputs
function splitDateTime(dateTimeValue, dateFieldId, timeFieldId) {
    if (dateTimeValue) {
        var date = new Date(dateTimeValue);
        if (!isNaN(date.getTime())) {
            // Set date field (YYYY-MM-DD format for type="date")
            $('#' + dateFieldId).val(date.toISOString().split('T')[0]);

            // Set time field (HH:mm format for type="time" - 24-hour)
            var hours = ('0' + date.getHours()).slice(-2);
            var minutes = ('0' + date.getMinutes()).slice(-2);
            $('#' + timeFieldId).val(hours + ':' + minutes);
        }
    }
}

// ⭐ CRITICAL: Combine date and time inputs into single DateTime
function combineDateTime(dateFieldId, timeFieldId) {
    var date = $('#' + dateFieldId).val();
    var time = $('#' + timeFieldId).val();

    if (date && time) {
        return date + 'T' + time + ':00';
    } else if (date) {
        return date + 'T00:00:00';
    } else {
        return '';
    }
}

// ⭐ CRITICAL: Enable/disable fields based on MaintenanceType selection
function enableDisableFields() {
    var selectedType = $('input[name="MaintenanceLog.MaintenanceType"]:checked').val();

    // Hide all conditional sections
    $('#statusSection, #divisionSection, #facilitySection, #boatRoleSection').hide();

    // Disable all conditional fields
    $('#cboStatus, #cboDivision, #cboPortFacility, #cboBoatRole').prop('disabled', true);

    // Show and enable fields based on selected type
    if (selectedType === 'Boat Status') {
        $('#statusSection').show();
        $('#cboStatus').prop('disabled', false);
    } else if (selectedType === 'Change Division/Facility') {
        $('#divisionSection, #facilitySection').show();
        $('#cboDivision, #cboPortFacility').prop('disabled', false);
    } else if (selectedType === 'Change Boat Role') {
        $('#boatRoleSection').show();
        $('#cboBoatRole').prop('disabled', false);
    }

    // ⭐ CRITICAL: Disable MaintenanceType radio buttons when editing existing record
    var isNew = $('#IsNew').val() === 'true';
    if (!isNew) {
        $('input[name="MaintenanceLog.MaintenanceType"]').prop('disabled', true);
    }
}

// ⭐ CRITICAL: Clear fields not applicable to selected MaintenanceType before save
function clearUnusedFields() {
    var selectedType = $('input[name="MaintenanceLog.MaintenanceType"]:checked').val();

    if (selectedType !== 'Boat Status') {
        $('#cboStatus').val('').trigger('change.select2');
    }
    if (selectedType !== 'Change Division/Facility') {
        $('#cboDivision, #cboPortFacility').val('').trigger('change.select2');
    }
    if (selectedType !== 'Change Boat Role') {
        $('#cboBoatRole').val('').trigger('change.select2');
    }
}

// Load form data for editing
function loadForm(data) {
    $('#BoatMaintenanceLogID').val(data.boatMaintenanceLogID);
    $('#ModifyDateTime').val(data.modifyDateTime);
    $('#IsNew').val('false');

    // ⭐ CRITICAL: Split DateTime into date and time inputs
    splitDateTime(data.startDateTime, 'dtStartDate', 'dtStartTime');

    // Set MaintenanceType (radio buttons)
    $('input[name="MaintenanceLog.MaintenanceType"][value="' + data.maintenanceType + '"]').prop('checked', true);

    // Set conditional field values
    $('#cboStatus').val(data.status).trigger('change.select2');
    $('#cboDivision').val(data.division).trigger('change');
    $('#cboPortFacility').val(data.portFacilityID).trigger('change.select2');
    $('#cboBoatRole').val(data.boatRoleID).trigger('change.select2');

    // Set note
    $('#txtNote').val(data.note);

    // Enable/disable fields based on MaintenanceType
    enableDisableFields();
}

// Clear form (reset to default state)
function clearForm() {
    $('#detailForm')[0].reset();
    $('#BoatMaintenanceLogID').val('0');
    $('#ModifyDateTime').val('');
    $('#IsNew').val('true');

    // Clear date/time fields
    $('#dtStartDate, #dtStartTime').val('');

    // Reset MaintenanceType to default (Boat Status)
    $('input[name="MaintenanceLog.MaintenanceType"][value="Boat Status"]').prop('checked', true);

    // Clear all dropdowns
    $('#cboStatus, #cboDivision, #cboPortFacility, #cboBoatRole').val('').trigger('change.select2');

    // Clear note
    $('#txtNote').val('');

    // Enable MaintenanceType radio buttons (since it's a new record)
    $('input[name="MaintenanceLog.MaintenanceType"]').prop('disabled', false);

    // Reset conditional field display
    enableDisableFields();
}

// Attach event handlers for grid buttons
function attachGridEventHandlers() {
    // Edit button handler
    $('.btn-edit').off('click').on('click', function (e) {
        e.preventDefault();
        var id = $(this).data('id');

        $.get('/BoatStatus/GetById/' + id, function (data) {
            if (data) {
                loadForm(data);
                $('#detailSection').show();
            }
        }).fail(function () {
            alert('Error loading maintenance log data');
        });
    });

    // Delete button handler
    $('.btn-delete').off('click').on('click', function (e) {
        e.preventDefault();
        var id = $(this).data('id');

        if (confirm('Are you sure you want to delete this boat status entry?')) {
            var token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/BoatStatus/Delete/' + id,
                type: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                success: function (response) {
                    if (response.success) {
                        dataTable.ajax.reload();
                    } else {
                        alert('Error deleting: ' + (response.error || 'Unknown error'));
                    }
                },
                error: function (xhr) {
                    alert('Error deleting maintenance log: ' + xhr.statusText);
                }
            });
        }
    });
}
