// Facility Search - DataTables initialization and search functionality
(function () {
    'use strict';

    let facilityTable;

    // Initialize on page load
    $(document).ready(function () {
        initializeDataTable();
        bindEventHandlers();
    });

    function initializeDataTable() {
        facilityTable = $('#facilityTable').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: '/Facility/Search',
                type: 'POST',
                contentType: 'application/json',
                data: function (d) {
                    const order = (d.order && d.order.length > 0) ? d.order[0] : null;
                    const orderedColumn = order ? d.columns[order.column] : null;
                    const sortColumn = orderedColumn ? (orderedColumn.data || orderedColumn.name) : null;
                    const sortDirection = order ? order.dir : null;

                    // Add search criteria to DataTables request
                    return JSON.stringify({
                        draw: d.draw,
                        start: d.start,
                        length: d.length,
                        sortColumn: sortColumn,
                        sortDirection: sortDirection,
                        name: $('#Name').val(),
                        shortName: $('#ShortName').val(),
                        river: $('#River').val(),
                        bargeExLocationType: $('#BargeExLocationType').val(),
                        bargeExCode: $('#BargeExCode').val(),
                        isActive: $('#IsActive').is(':checked') ? true : null
                    });
                },
                dataSrc: function (json) {
                    return json.data;
                },
                error: function (xhr, error, code) {
                    console.error('DataTables error:', error, code);
                    alert('An error occurred while loading facility data. Please try again.');
                }
            },
            columns: [
                { data: 'name', name: 'Name' },
                { data: 'shortName', name: 'ShortName', defaultContent: '' },
                { data: 'river', name: 'River', defaultContent: '' },
                {
                    data: 'mile',
                    name: 'Mile',
                    defaultContent: '',
                    render: function (data) {
                        return data ? data.toFixed(1) : '';
                    }
                },
                { data: 'bargeExLocationType', name: 'BargeExLocationType', defaultContent: '' },
                { data: 'bargeExCode', name: 'BargeExCode', defaultContent: '' },
                {
                    data: 'isActive',
                    name: 'IsActive',
                    render: function (data) {
                        return data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-secondary">No</span>';
                    }
                },
                {
                    data: 'locationID',
                    name: 'Actions',
                    orderable: false,
                    searchable: false,
                    render: function (data) {
                        return `<a href="/Facility/Edit/${data}" class="btn btn-sm btn-primary">
                                    <i class="bi bi-pencil"></i> Edit
                                </a>`;
                    }
                }
            ],
            order: [[0, 'asc']],
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            language: {
                processing: '<i class="bi bi-hourglass-split"></i> Loading...',
                emptyTable: 'No facilities found matching your search criteria',
                zeroRecords: 'No facilities found matching your search criteria'
            },
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                 '<"row"<"col-sm-12"tr>>' +
                 '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            initComplete: function () {
                console.log('Facility DataTable initialized');
            }
        });
    }

    function bindEventHandlers() {
        // Search button
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            facilityTable.ajax.reload();
        });

        // Clear button
        $('#btnClear').on('click', function (e) {
            e.preventDefault();
            $('#searchForm')[0].reset();
            $('#IsActive').prop('checked', true);
            facilityTable.ajax.reload();
        });

        // Enter key in search fields
        $('#searchForm input, #searchForm select').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                $('#btnSearch').click();
            }
        });
    }

})();
