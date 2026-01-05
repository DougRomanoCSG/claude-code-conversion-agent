// Facility Detail - Edit form logic with tab management and child collections
(function () {
    'use strict';

    let statusTable;
    let berthTable;
    let currentStatusRow = null;
    let currentBerthRow = null;
    let tabsLoaded = {
        details: true,
        status: false,
        berths: false,
        ndc: false
    };

    // Initialize on page load
    $(document).ready(function () {
        initializeConditionalFields();
        initializeTabs();
        bindEventHandlers();
        initializeFormValidation();

        // Load child collections if editing existing facility
        if (facilityId > 0) {
            // Status and Berths will lazy load on tab shown
        }
    });

    // Conditional field enabling for Lock/Gauge fields
    function initializeConditionalFields() {
        toggleLockGaugeFields();

        $('#facilityType').on('change', function () {
            toggleLockGaugeFields();
        });
    }

    function toggleLockGaugeFields() {
        const facilityType = $('#facilityType').val();
        const isLockOrGauge = facilityType === 'Lock' || facilityType === 'Gauge Location';

        if (isLockOrGauge) {
            $('#lockGaugeGroup').show();
            $('.lock-field').prop('disabled', false);
        } else {
            $('#lockGaugeGroup').hide();
            $('.lock-field').prop('disabled', true).val('');
        }
    }

    // Tab management with lazy loading
    function initializeTabs() {
        $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
            const targetId = $(e.target).data('bs-target');

            switch (targetId) {
                case '#status':
                    if (!tabsLoaded.status && facilityId > 0) {
                        loadStatusData();
                        tabsLoaded.status = true;
                    }
                    break;
                case '#berths':
                    if (!tabsLoaded.berths && facilityId > 0) {
                        loadBerthData();
                        tabsLoaded.berths = true;
                    }
                    break;
            }
        });
    }

    // Status Tab Management
    function loadStatusData() {
        if (!statusTable) {
            statusTable = $('#statusTable').DataTable({
                ajax: {
                    url: `/Facility/GetStatuses?facilityId=${facilityId}`,
                    dataSrc: ''
                },
                columns: [
                    {
                        data: 'startDateTime',
                        render: function (data) {
                            return data ? new Date(data).toLocaleString() : '';
                        }
                    },
                    {
                        data: 'endDateTime',
                        render: function (data) {
                            return data ? new Date(data).toLocaleString() : '';
                        }
                    },
                    { data: 'status' },
                    { data: 'note', defaultContent: '' }
                ],
                order: [[0, 'desc']],
                select: {
                    style: 'single'
                },
                pageLength: 10
            });

            // Selection handling
            statusTable.on('select', function () {
                $('#btnEditStatus, #btnDeleteStatus').prop('disabled', false);
            });

            statusTable.on('deselect', function () {
                $('#btnEditStatus, #btnDeleteStatus').prop('disabled', true);
            });
        } else {
            statusTable.ajax.reload();
        }
    }

    function loadBerthData() {
        if (!berthTable) {
            berthTable = $('#berthTable').DataTable({
                ajax: {
                    url: `/Facility/GetBerths?facilityId=${facilityId}`,
                    dataSrc: ''
                },
                columns: [
                    { data: 'name' },
                    { data: 'shipName', defaultContent: '<em>None</em>' }
                ],
                order: [[0, 'asc']],
                select: {
                    style: 'single'
                },
                paging: false,
                searching: false,
                info: false
            });

            // Selection handling
            berthTable.on('select', function () {
                $('#btnEditBerth, #btnDeleteBerth').prop('disabled', false);
            });

            berthTable.on('deselect', function () {
                $('#btnEditBerth, #btnDeleteBerth').prop('disabled', true);
            });
        } else {
            berthTable.ajax.reload();
        }
    }

    // Event Handlers
    function bindEventHandlers() {
        // Status buttons
        $('#btnAddStatus').on('click', addStatus);
        $('#btnEditStatus').on('click', editStatus);
        $('#btnDeleteStatus').on('click', deleteStatus);
        $('#btnSaveStatus').on('click', saveStatus);
        $('#btnCancelStatus').on('click', cancelStatusEdit);
        $('#btnExportStatus').on('click', exportStatus);

        // Berth buttons
        $('#btnAddBerth').on('click', addBerth);
        $('#btnEditBerth').on('click', editBerth);
        $('#btnDeleteBerth').on('click', deleteBerth);
        $('#btnSaveBerth').on('click', saveBerth);
        $('#btnCancelBerth').on('click', cancelBerthEdit);

        // Delete facility button
        $('#btnDelete').on('click', deleteFacility);

        // Form dirty tracking
        let formChanged = false;
        $('#facilityForm :input').on('change', function () {
            formChanged = true;
        });

        $(window).on('beforeunload', function () {
            if (formChanged) {
                return 'You have unsaved changes. Are you sure you want to leave?';
            }
        });

        $('#facilityForm').on('submit', function () {
            formChanged = false;
        });
    }

    // Status CRUD Operations
    function addStatus() {
        clearStatusForm();
        $('#statusEditForm').show();
        disableMainForm();
    }

    function editStatus() {
        const selectedData = statusTable.rows({ selected: true }).data();
        if (selectedData.length === 0) return;

        const status = selectedData[0];
        currentStatusRow = statusTable.rows({ selected: true });

        $('#statusId').val(status.facilityStatusID);

        // Split DateTime into date and time
        const startDate = new Date(status.startDateTime);
        $('#statusStartDate').val(startDate.toISOString().split('T')[0]);
        $('#statusStartTime').val(startDate.toTimeString().substring(0, 5));

        if (status.endDateTime) {
            const endDate = new Date(status.endDateTime);
            $('#statusEndDate').val(endDate.toISOString().split('T')[0]);
            $('#statusEndTime').val(endDate.toTimeString().substring(0, 5));
        }

        $('#statusStatus').val(status.status);
        $('#statusNote').val(status.note);

        $('#statusEditForm').show();
        disableMainForm();
    }

    function saveStatus() {
        if (!validateStatusForm()) return;

        // Combine date and time
        const startDateTime = combineDateAndTime($('#statusStartDate').val(), $('#statusStartTime').val());
        const endDateTime = $('#statusEndDate').val()
            ? combineDateAndTime($('#statusEndDate').val(), $('#statusEndTime').val())
            : null;

        const statusData = {
            facilityStatusID: parseInt($('#statusId').val()) || 0,
            locationID: facilityId,
            startDateTime: startDateTime,
            endDateTime: endDateTime,
            status: $('#statusStatus').val(),
            note: $('#statusNote').val()
        };

        $.ajax({
            url: '/Facility/SaveStatus',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(statusData),
            success: function (response) {
                if (response.success) {
                    statusTable.ajax.reload();
                    cancelStatusEdit();
                    showSuccess('Status saved successfully');
                } else {
                    showError(response.error || 'Failed to save status');
                }
            },
            error: function () {
                showError('An error occurred while saving the status');
            }
        });
    }

    function deleteStatus() {
        if (!confirm('Are you sure you want to remove this status?')) return;

        const selectedData = statusTable.rows({ selected: true }).data();
        if (selectedData.length === 0) return;

        const statusId = selectedData[0].facilityStatusID;

        $.ajax({
            url: '/Facility/DeleteStatus',
            type: 'POST',
            data: { id: statusId },
            success: function (response) {
                if (response.success) {
                    statusTable.ajax.reload();
                    showSuccess('Status deleted successfully');
                } else {
                    showError('Failed to delete status');
                }
            },
            error: function () {
                showError('An error occurred while deleting the status');
            }
        });
    }

    function cancelStatusEdit() {
        clearStatusForm();
        $('#statusEditForm').hide();
        enableMainForm();
    }

    function clearStatusForm() {
        $('#statusId').val('');
        $('#statusStartDate').val('');
        $('#statusStartTime').val('');
        $('#statusEndDate').val('');
        $('#statusEndTime').val('');
        $('#statusStatus').val('');
        $('#statusNote').val('');
        currentStatusRow = null;
    }

    function validateStatusForm() {
        if (!$('#statusStartDate').val()) {
            alert('Start date is required');
            return false;
        }
        if (!$('#statusStatus').val()) {
            alert('Status is required');
            return false;
        }

        // Validate end date >= start date
        if ($('#statusEndDate').val()) {
            const startDateTime = new Date(combineDateAndTime($('#statusStartDate').val(), $('#statusStartTime').val()));
            const endDateTime = new Date(combineDateAndTime($('#statusEndDate').val(), $('#statusEndTime').val()));

            if (endDateTime < startDateTime) {
                alert('End date/time must be later than or equal to start date/time');
                return false;
            }
        }

        return true;
    }

    // Berth CRUD Operations
    function addBerth() {
        clearBerthForm();
        $('#berthEditForm').show();
        disableMainForm();
    }

    function editBerth() {
        const selectedData = berthTable.rows({ selected: true }).data();
        if (selectedData.length === 0) return;

        const berth = selectedData[0];
        currentBerthRow = berthTable.rows({ selected: true });

        $('#berthId').val(berth.facilityBerthID);
        $('#berthName').val(berth.name);
        $('#berthShipName').val(berth.shipName || '');

        $('#berthEditForm').show();
        disableMainForm();
    }

    function saveBerth() {
        if (!validateBerthForm()) return;

        const berthData = {
            facilityBerthID: parseInt($('#berthId').val()) || 0,
            locationID: facilityId,
            name: $('#berthName').val()
        };

        $.ajax({
            url: '/Facility/SaveBerth',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(berthData),
            success: function (response) {
                if (response.success) {
                    berthTable.ajax.reload();
                    cancelBerthEdit();
                    showSuccess('Berth saved successfully');
                } else {
                    showError(response.error || 'Failed to save berth');
                }
            },
            error: function () {
                showError('An error occurred while saving the berth');
            }
        });
    }

    function deleteBerth() {
        if (!confirm('Are you sure you want to remove this berth?')) return;

        const selectedData = berthTable.rows({ selected: true }).data();
        if (selectedData.length === 0) return;

        const berthId = selectedData[0].facilityBerthID;

        $.ajax({
            url: '/Facility/DeleteBerth',
            type: 'POST',
            data: { id: berthId },
            success: function (response) {
                if (response.success) {
                    berthTable.ajax.reload();
                    showSuccess('Berth deleted successfully');
                } else {
                    showError('Failed to delete berth');
                }
            },
            error: function () {
                showError('An error occurred while deleting the berth');
            }
        });
    }

    function cancelBerthEdit() {
        clearBerthForm();
        $('#berthEditForm').hide();
        enableMainForm();
    }

    function clearBerthForm() {
        $('#berthId').val('');
        $('#berthName').val('');
        $('#berthShipName').val('');
        currentBerthRow = null;
    }

    function validateBerthForm() {
        if (!$('#berthName').val().trim()) {
            alert('Berth name is required');
            return false;
        }
        return true;
    }

    // Export Status
    function exportStatus() {
        window.location.href = `/Facility/ExportStatus?facilityId=${facilityId}`;
    }

    // Delete Facility
    function deleteFacility() {
        if (!confirm('Are you sure you want to delete this facility? This action cannot be undone.')) return;

        $('#deleteForm').submit();
    }

    // Helper Functions
    function disableMainForm() {
        $('#facilityForm :input').not('#statusEditForm :input, #berthEditForm :input').prop('disabled', true);
        $('button[data-bs-toggle="tab"]').prop('disabled', true);
    }

    function enableMainForm() {
        $('#facilityForm :input').prop('disabled', false);
        $('button[data-bs-toggle="tab"]').prop('disabled', false);
        toggleLockGaugeFields();
    }

    function combineDateAndTime(date, time) {
        if (!date) return null;
        const timeValue = time || '00:00';
        return `${date}T${timeValue}:00`;
    }

    function showSuccess(message) {
        alert(message);
    }

    function showError(message) {
        alert(message);
    }

    function initializeFormValidation() {
        // jQuery validation will be handled by asp-validation tags
    }

})();
