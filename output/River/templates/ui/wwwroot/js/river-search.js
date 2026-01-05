// river-search.js
// DataTables initialization for River search/list screen

$(document).ready(function () {
    // Storage keys for state persistence
    const STORAGE_KEYS = {
        CURRENT_STATE: 'riverTable_v1',
        LEGACY_KEYS: []
    };

    // Destroy existing DataTable if present
    if ($.fn.DataTable.isDataTable('#riverTable')) {
        $('#riverTable').DataTable().destroy();
    }

    // Initialize DataTables with server-side processing
    var dataTable = $('#riverTable').DataTable({
        processing: true,
        serverSide: true,
        stateSave: true,
        stateKey: STORAGE_KEYS.CURRENT_STATE,
        responsive: true,
        autoWidth: false,
        ajax: {
            url: '/River/RiverTable',
            type: 'POST',
            contentType: 'application/json',
            data: function (d) {
                // Build search request object
                return JSON.stringify({
                    code: $('#Code').val(),
                    name: $('#Name').val(),
                    activeOnly: $('#ActiveOnly').is(':checked'),
                    start: d.start,
                    length: d.length,
                    draw: d.draw,
                    orderColumn: d.columns[d.order[0].column].data,
                    orderDirection: d.order[0].dir.toUpperCase()
                });
            },
            error: function (xhr, error, code) {
                console.error('DataTables error:', error, code);
            }
        },
        columns: [
            {
                data: 'code',
                name: 'code',
                defaultContent: '',
                width: '80px'
            },
            {
                data: 'name',
                name: 'name',
                defaultContent: '',
                width: '250px'
            },
            {
                data: 'startMile',
                name: 'startMile',
                defaultContent: '',
                width: '100px',
                className: 'text-end',
                render: function (data) {
                    return data != null && data !== '' ? data : '';
                }
            },
            {
                data: 'endMile',
                name: 'endMile',
                defaultContent: '',
                width: '100px',
                className: 'text-end',
                render: function (data) {
                    return data != null && data !== '' ? data : '';
                }
            },
            {
                data: 'isLowToHighDirection',
                name: 'isLowToHighDirection',
                defaultContent: '',
                width: '120px',
                className: 'text-center',
                orderable: true,
                render: function (data) {
                    return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
                }
            },
            {
                data: 'isActive',
                name: 'isActive',
                defaultContent: '',
                width: '80px',
                className: 'text-center',
                orderable: true,
                render: function (data) {
                    return '<input type="checkbox" ' + (data ? 'checked' : '') + ' disabled />';
                }
            },
            {
                data: 'upLabel',
                name: 'upLabel',
                defaultContent: '',
                width: '120px'
            },
            {
                data: 'downLabel',
                name: 'downLabel',
                defaultContent: '',
                width: '120px'
            },
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                defaultContent: '',
                width: '100px',
                className: 'text-center',
                render: function (data, type, row) {
                    return '<div class="btn-group btn-group-sm" role="group">' +
                        '<a href="/River/Edit/' + row.riverId + '" class="btn btn-sm btn-primary" title="Edit">' +
                        '<i class="fas fa-edit"></i></a>' +
                        '</div>';
                }
            }
        ],
        order: [[0, 'asc']], // Default sort by Code ascending
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        language: {
            processing: '<i class="fas fa-spinner fa-spin"></i> Loading...',
            emptyTable: 'No rivers found',
            zeroRecords: 'No matching rivers found'
        }
    });

    // Search button click handler
    $('#btnSearch').click(function () {
        dataTable.ajax.reload();
    });

    // Reset button click handler
    $('#btnReset').click(function () {
        $('#searchForm')[0].reset();
        $('#ActiveOnly').prop('checked', true);
        dataTable.ajax.reload();
    });

    // Enter key handler for text fields
    $('#searchForm input[type="text"]').keypress(function (e) {
        if (e.which == 13) {
            e.preventDefault();
            $('#btnSearch').click();
        }
    });

    // Auto-search on checkbox change
    $('#ActiveOnly').on('change', function () {
        dataTable.ajax.reload();
    });
});
