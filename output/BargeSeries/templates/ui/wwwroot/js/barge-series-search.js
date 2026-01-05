/**
 * BargeSeries Search Page JavaScript
 * Handles DataTables initialization, search functionality, and user interactions
 */
window.bargeSeriesSearch = (function () {
    'use strict';

    let dataTable = null;
    let config = {
        canModify: false,
        canDelete: false
    };

    /**
     * Initialize the search page
     */
    function init(options) {
        config = { ...config, ...options };

        initializeSelect2();
        initializeDataTable();
        attachEventHandlers();
    }

    /**
     * Initialize Select2 dropdowns
     */
    function initializeSelect2() {
        $('[data-select2="true"]').select2({
            placeholder: '-- Select --',
            allowClear: true,
            width: '100%'
        });
    }

    /**
     * Initialize DataTables with server-side processing
     */
    function initializeDataTable() {
        dataTable = $('#bargeSeriesTable').DataTable({
            processing: true,
            serverSide: true,
            stateSave: true,
            responsive: true,
            ajax: {
                url: '/BargeSeriesSearch/BargeSeriesTable',
                type: 'POST',
                contentType: 'application/json',
                data: function (d) {
                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,
                        searchValue: $('#SeriesName').val(),
                        customerID: $('#CustomerID').val() || null,
                        hullType: $('#HullType').val() || null,
                        coverType: $('#CoverType').val() || null,
                        activeOnly: $('#ActiveOnly').is(':checked'),
                        sortColumn: getSortColumn(d.order),
                        sortDirection: getSortDirection(d.order)
                    });
                },
                error: function (xhr, error, thrown) {
                    console.error('DataTables error:', error, thrown);
                    alert('Failed to load data. Please try again.');
                }
            },
            columns: [
                {
                    data: null,
                    orderable: false,
                    searchable: false,
                    render: function (data, type, row) {
                        let buttons = '';

                        if (config.canModify) {
                            buttons += `<a href="/BargeSeriesSearch/Edit/${row.bargeSeriesID}" class="btn btn-sm btn-primary me-1" title="Edit">
                                <i class="fas fa-edit"></i>
                            </a>`;
                        }

                        if (config.canDelete && row.isActive) {
                            buttons += `<button type="button" class="btn btn-sm btn-danger btn-delete" data-id="${row.bargeSeriesID}" data-name="${row.name}" title="Deactivate">
                                <i class="fas fa-trash"></i>
                            </button>`;
                        }

                        return buttons || '<span class="text-muted">No actions</span>';
                    }
                },
                { data: 'name', name: 'Name' },
                { data: 'customerName', name: 'CustomerName' },
                { data: 'hullType', name: 'HullType' },
                { data: 'coverType', name: 'CoverType' },
                { data: 'dimensions', name: 'Dimensions' },
                {
                    data: 'draftLightFeet',
                    name: 'DraftLight',
                    render: function (data, type, row) {
                        if (row.draftLightFeet && row.draftLightInches) {
                            return `${row.draftLightFeet}' ${row.draftLightInches}"`;
                        }
                        return '';
                    }
                },
                {
                    data: 'tonsPerInch',
                    name: 'TonsPerInch',
                    render: function (data) {
                        return data ? data.toFixed(2) : '';
                    }
                },
                {
                    data: 'isActive',
                    name: 'IsActive',
                    render: function (data) {
                        return `<input type="checkbox" ${data ? 'checked' : ''} disabled />`;
                    }
                }
            ],
            order: [[1, 'asc']], // Default sort by Series Name
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            language: {
                processing: '<i class="fas fa-spinner fa-spin fa-2x"></i><br>Loading...'
            }
        });
    }

    /**
     * Get sort column name from DataTables order
     */
    function getSortColumn(order) {
        if (!order || order.length === 0) return 'Name';

        const columnIndex = order[0].column;
        const columnMap = {
            1: 'Name',
            2: 'CustomerName',
            3: 'HullType',
            4: 'CoverType',
            5: 'Dimensions',
            6: 'DraftLight',
            7: 'TonsPerInch',
            8: 'IsActive'
        };

        return columnMap[columnIndex] || 'Name';
    }

    /**
     * Get sort direction from DataTables order
     */
    function getSortDirection(order) {
        if (!order || order.length === 0) return 'asc';
        return order[0].dir;
    }

    /**
     * Attach event handlers
     */
    function attachEventHandlers() {
        // Search button click
        $('#btnSearch').on('click', function () {
            dataTable.ajax.reload();
        });

        // Reset button click
        $('#btnReset').on('click', function () {
            $('#SeriesName').val('');
            $('#CustomerID').val('').trigger('change');
            $('#HullType').val('').trigger('change');
            $('#CoverType').val('').trigger('change');
            $('#ActiveOnly').prop('checked', true);
            dataTable.ajax.reload();
        });

        // Enter key in search fields
        $('#SeriesName').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                $('#btnSearch').click();
            }
        });

        // Auto-search when dropdowns change
        $('#CustomerID, #HullType, #CoverType, #ActiveOnly').on('change', function () {
            dataTable.ajax.reload();
        });

        // Delete button click (delegated event for dynamically created buttons)
        $('#bargeSeriesTable').on('click', '.btn-delete', function () {
            const id = $(this).data('id');
            const name = $(this).data('name');

            if (confirm(`Are you sure you want to deactivate barge series "${name}"?`)) {
                deleteBargeSeries(id);
            }
        });
    }

    /**
     * Delete (deactivate) a barge series
     */
    function deleteBargeSeries(id) {
        const form = $('<form>', {
            method: 'POST',
            action: '/BargeSeriesSearch/Delete'
        });

        form.append($('<input>', {
            type: 'hidden',
            name: 'id',
            value: id
        }));

        form.append($('<input>', {
            type: 'hidden',
            name: '__RequestVerificationToken',
            value: $('input[name="__RequestVerificationToken"]').val()
        }));

        $('body').append(form);
        form.submit();
    }

    return {
        init: init
    };
})();
