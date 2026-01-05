/**
 * Barge Position History Search Page JavaScript
 * Target: C:\Dev\BargeOps.Admin.Mono\src\BargeOps.UI\wwwroot\js\bargePositionHistory-search.js
 *
 * Handles DataTables initialization, search/reset functionality, and toolbar button states.
 */

var BargePositionHistorySearch = (function() {
    'use strict';

    let dataTable;
    let config = {};

    /**
     * Initialize the search page.
     * @param {Object} options - Configuration options (fleetId, canModify, canDelete)
     */
    function init(options) {
        config = options || {};
        initializeSelect2();
        initializeDataTable();
        initializeSearchHandlers();
        initializeToolbarHandlers();
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
     * Initialize DataTables with server-side processing.
     */
    function initializeDataTable() {
        dataTable = $('#bargePositionHistoryTable').DataTable({
            processing: true,
            serverSide: true,
            stateSave: true,
            stateKey: 'bargePositionHistoryTable_v1_' + config.fleetId,
            responsive: true,
            ajax: {
                url: '/BargePositionHistory/SearchTable',
                type: 'POST',
                contentType: 'application/json',
                data: function(d) {
                    return JSON.stringify({
                        fleetID: config.fleetId,
                        positionStartDate: $('#SearchDate').val(),
                        tierGroupID: $('#TierGroupID').val() || null,
                        bargeNum: $('#BargeNum').val(),
                        includeBlankTierPos: $('#IncludeBlankTierPos').is(':checked'),
                        draw: d.draw,
                        start: d.start,
                        length: d.length,
                        orderColumn: d.columns[d.order[0].column].data,
                        orderDirection: d.order[0].dir.toUpperCase()
                    });
                }
            },
            columns: [
                {
                    data: 'positionStartDateTime',
                    title: 'Date/Time',
                    orderable: true,
                    render: function(data) {
                        return data || '';
                    }
                },
                {
                    data: 'bargeNum',
                    title: 'Barge',
                    orderable: true,
                    render: function(data) {
                        return data || '';
                    }
                },
                {
                    data: 'leftFleet',
                    title: 'Left Fleet',
                    orderable: true,
                    className: 'text-center',
                    render: function(data) {
                        return data
                            ? '<i class="fas fa-check-square text-success"></i>'
                            : '<i class="far fa-square text-muted"></i>';
                    }
                },
                {
                    data: 'tierName',
                    title: 'Tier',
                    orderable: true,
                    render: function(data) {
                        return data || '';
                    }
                },
                {
                    data: 'tierPos',
                    title: 'Tier Pos',
                    orderable: true,
                    render: function(data) {
                        return data || '';
                    }
                }
            ],
            order: [[0, 'asc']], // Sort by PositionStartDateTime ascending
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            select: {
                style: 'single',
                selector: 'tr'
            },
            language: {
                emptyTable: 'No barge position history records found. Click Find to search.',
                zeroRecords: 'No matching records found.'
            }
        });

        // Update toolbar button states on selection change
        dataTable.on('select.dt deselect.dt', function() {
            updateToolbarState();
        });
    }

    /**
     * Initialize search and reset button handlers.
     */
    function initializeSearchHandlers() {
        $('#btnSearch').on('click', function() {
            if (validateSearchCriteria()) {
                dataTable.ajax.reload();
            }
        });

        $('#btnReset').on('click', function() {
            $('#searchForm')[0].reset();
            $('#TierGroupID').val('').trigger('change');
            $('#SearchDate').val(new Date().toISOString().split('T')[0]);
            dataTable.ajax.reload();
        });

        // Allow Enter key to trigger search
        $('#searchForm input, #searchForm select').on('keypress', function(e) {
            if (e.which === 13) {
                e.preventDefault();
                $('#btnSearch').click();
            }
        });
    }

    /**
     * Validate search criteria before search.
     * @returns {boolean} True if valid, false otherwise
     */
    function validateSearchCriteria() {
        let isValid = true;
        let errorMessage = '';

        if (!$('#SearchDate').val()) {
            isValid = false;
            errorMessage += 'Date is required.\n';
        }

        if (!$('#TierGroupID').val()) {
            isValid = false;
            errorMessage += 'Tier Group is required.\n';
        }

        if (!isValid) {
            alert('Please complete the following required fields:\n\n' + errorMessage);
        }

        return isValid;
    }

    /**
     * Initialize toolbar button handlers.
     */
    function initializeToolbarHandlers() {
        // Add button
        $('#btnAdd').on('click', function() {
            const tierGroupId = $('#TierGroupID').val();
            window.location.href = `/BargePositionHistory/Create?fleetId=${config.fleetId}&tierGroupId=${tierGroupId}`;
        });

        // Modify button
        $('#btnModify').on('click', function() {
            const selectedRow = dataTable.rows({ selected: true }).data()[0];
            if (selectedRow) {
                const tierGroupId = $('#TierGroupID').val();
                window.location.href = `/BargePositionHistory/Edit/${selectedRow.fleetPositionHistoryID}?tierGroupId=${tierGroupId}`;
            }
        });

        // Remove button
        $('#btnRemove').on('click', function() {
            const selectedRow = dataTable.rows({ selected: true }).data()[0];
            if (selectedRow && confirm('Are you sure you want to delete this barge position history record?')) {
                deleteRecord(selectedRow.fleetPositionHistoryID);
            }
        });

        // Export button
        $('#btnExport').on('click', function() {
            // Use DataTables export extension or implement custom export
            alert('Export functionality to be implemented.');
        });

        // Double-click to edit
        $('#bargePositionHistoryTable tbody').on('dblclick', 'tr', function() {
            if (config.canModify) {
                const data = dataTable.row(this).data();
                if (data) {
                    const tierGroupId = $('#TierGroupID').val();
                    window.location.href = `/BargePositionHistory/Edit/${data.fleetPositionHistoryID}?tierGroupId=${tierGroupId}`;
                }
            }
        });
    }

    /**
     * Update toolbar button enabled/disabled state based on selection.
     */
    function updateToolbarState() {
        const hasSelection = dataTable.rows({ selected: true }).count() > 0;
        $('#btnModify, #btnRemove').prop('disabled', !hasSelection);
    }

    /**
     * Delete a barge position history record.
     * @param {number} id - FleetPositionHistoryID
     */
    function deleteRecord(id) {
        $.ajax({
            url: `/BargePositionHistory/Delete/${id}`,
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    dataTable.ajax.reload();
                    showSuccessMessage(response.message);
                } else {
                    showErrorMessage(response.message);
                }
            },
            error: function() {
                showErrorMessage('An error occurred while deleting the record.');
            }
        });
    }

    /**
     * Show success message.
     * @param {string} message - Message text
     */
    function showSuccessMessage(message) {
        alert(message); // Replace with toast notification
    }

    /**
     * Show error message.
     * @param {string} message - Message text
     */
    function showErrorMessage(message) {
        alert(message); // Replace with toast notification
    }

    // Public API
    return {
        init: init
    };
})();
