/**
 * River Area Search Page
 * DataTables initialization with server-side processing
 * Pattern Reference: boatLocationSearch.js
 */

$(document).ready(function () {
    // Initialize DataTable
    var table = $('#riverAreaTable').DataTable({
        serverSide: true,
        processing: true,
        stateSave: true,
        stateKey: 'riverAreaTable_v1',
        responsive: true,
        autoWidth: false,
        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        order: [[1, 'asc']],  // Sort by Name by default
        ajax: {
            url: '/RiverArea/RiverAreaTable',
            type: 'POST',
            data: function (d) {
                // Add search criteria to request
                d.name = $('#Name').val();
                d.activeOnly = $('#ActiveOnly').is(':checked');
                d.pricingZonesOnly = $('#PricingZonesOnly').is(':checked');
                d.portalAreasOnly = $('#PortalAreasOnly').is(':checked');
                d.customerID = $('#CustomerID').val() || null;
                d.highWaterAreasOnly = $('#HighWaterAreasOnly').is(':checked');
            }
        },
        columns: [
            {
                // Actions column
                data: null,
                orderable: false,
                searchable: false,
                className: 'text-center',
                width: '80px',
                render: function (data, type, row) {
                    return `<a href="/RiverArea/Edit/${row.riverAreaID}" class="btn btn-primary btn-sm" title="Edit">
                                <i class="fas fa-edit"></i>
                            </a>`;
                }
            },
            {
                data: 'name',
                name: 'name',
                title: 'River Area'
            },
            {
                data: 'isActive',
                name: 'isActive',
                title: 'Active',
                className: 'text-center',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            },
            {
                data: 'isPriceZone',
                name: 'isPriceZone',
                title: 'Price Zone',
                className: 'text-center',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            },
            {
                data: 'isPortalArea',
                name: 'isPortalArea',
                title: 'Portal Area',
                className: 'text-center',
                visible: $('#ShowPortalAreas').val() === 'true',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            },
            {
                data: 'isHighWaterArea',
                name: 'isHighWaterArea',
                title: 'High Water Area',
                className: 'text-center',
                visible: $('#ShowHighWaterFilters').val() === 'true',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            },
            {
                data: 'customerName',
                name: 'customerName',
                title: 'High Water Customer',
                visible: $('#ShowHighWaterFilters').val() === 'true'
            },
            {
                data: 'isFuelTaxArea',
                name: 'isFuelTaxArea',
                title: 'Fuel Tax Area',
                className: 'text-center',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            },
            {
                data: 'isLiquidRateArea',
                name: 'isLiquidRateArea',
                title: 'Liquid Rate Area',
                className: 'text-center',
                visible: $('#ShowLiquidRateColumn').val() === 'true',
                render: function (data) {
                    return `<input type="checkbox" disabled ${data ? 'checked' : ''} />`;
                }
            }
        ]
    });

    // Initialize Select2 for Customer dropdown
    $('#CustomerID').select2({
        placeholder: '-- All Customers --',
        allowClear: true,
        width: '100%'
    });

    // Search button click
    $('#btnSearch').on('click', function (e) {
        e.preventDefault();
        table.ajax.reload();
    });

    // Reset button click
    $('#btnReset').on('click', function (e) {
        e.preventDefault();
        // Clear all search fields
        $('#Name').val('');
        $('#ActiveOnly').prop('checked', true);
        $('#PricingZonesOnly').prop('checked', false);
        $('#PortalAreasOnly').prop('checked', false);
        $('#CustomerID').val(null).trigger('change');
        $('#HighWaterAreasOnly').prop('checked', false);
        // Focus first field
        $('#Name').focus();
    });

    // Enter key in search fields
    $('#Name').on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            table.ajax.reload();
        }
    });

    // Checkbox change triggers auto-search
    $('#ActiveOnly, #PricingZonesOnly, #PortalAreasOnly, #HighWaterAreasOnly').on('change', function () {
        table.ajax.reload();
    });

    // Customer dropdown change triggers auto-search
    $('#CustomerID').on('change', function () {
        table.ajax.reload();
    });

    // Focus first field on page load
    $('#Name').focus();
});
