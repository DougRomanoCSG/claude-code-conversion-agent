/**
 * Vendor Search JavaScript
 * DataTables initialization and search functionality
 * Reference: boatLocationSearch.js for patterns
 * Location: src/BargeOps.UI/wwwroot/js/vendor-search.js
 */

(function () {
    'use strict';

    let dataTable;
    const config = window.vendorSearchConfig || {};

    /**
     * Initialize DataTables on page load
     */
    $(document).ready(function () {
        initializeDataTable();
        initializeSearchHandlers();
        initializeFilterToggles();
    });

    /**
     * Initialize DataTables with server-side processing
     */
    function initializeDataTable() {
        const columns = [
            {
                data: null,
                orderable: false,
                searchable: false,
                width: '80px',
                render: function (data, type, row) {
                    let html = '';

                    if (config.hasVendorModifyPermission) {
                        html += `<a href="/VendorSearch/Edit/${row.vendorID}" class="btn btn-sm btn-primary" title="Edit">`;
                        html += '<i class="fas fa-edit"></i>';
                        html += '</a>';
                    }

                    return html;
                }
            },
            { data: 'name', name: 'name', className: 'text-start' },
            { data: 'longName', name: 'longName', className: 'text-start' },
            { data: 'accountingCode', name: 'accountingCode', className: 'text-start' },
            {
                data: 'isActive',
                name: 'isActive',
                className: 'text-center',
                render: renderBoolean
            },
            {
                data: 'isFuelSupplier',
                name: 'isFuelSupplier',
                className: 'text-center',
                render: renderBoolean
            },
            {
                data: 'isBoatAssistSupplier',
                name: 'isBoatAssistSupplier',
                className: 'text-center',
                render: renderBoolean
            },
            {
                data: 'isInternalVendor',
                name: 'isInternalVendor',
                className: 'text-center',
                render: renderBoolean
            }
        ];

        // Conditionally add BargeEx column
        if (config.bargeExGlobalSettingEnabled) {
            columns.push({
                data: 'isBargeExEnabled',
                name: 'isBargeExEnabled',
                className: 'text-center',
                render: renderBoolean
            });
        }

        // Conditionally add Portal column
        if (config.portalLicenseActive) {
            columns.push({
                data: 'enablePortal',
                name: 'enablePortal',
                className: 'text-center',
                render: renderBoolean
            });
        }

        // Conditionally add UnitTow columns
        if (config.unitTowLicenseActive) {
            columns.push({
                data: 'isLiquidBroker',
                name: 'isLiquidBroker',
                className: 'text-center',
                render: renderBoolean
            });
            columns.push({
                data: 'isTankerman',
                name: 'isTankerman',
                className: 'text-center',
                render: renderBoolean
            });
        }

        dataTable = $('#vendorTable').DataTable({
            processing: true,
            serverSide: true,
            stateSave: true,
            responsive: true,
            ajax: {
                url: '/VendorSearch/VendorTable',
                type: 'POST',
                data: function (d) {
                    // Add search criteria to DataTables request
                    d.name = $('#Name').val();
                    d.accountingCode = $('#AccountingCode').val();
                    d.isActiveOnly = $('#IsActiveOnly').is(':checked');
                    d.fuelSuppliersOnly = $('#FuelSuppliersOnly').is(':checked');
                    d.internalVendorOnly = $('#InternalVendorOnly').is(':checked');
                    d.isBargeExEnabledOnly = $('#IsBargeExEnabledOnly').is(':checked');
                    d.enablePortalOnly = $('#EnablePortalOnly').is(':checked');
                    d.liquidBrokerOnly = $('#LiquidBrokerOnly').is(':checked');
                    d.tankermanOnly = $('#TankermanOnly').is(':checked');
                },
                error: function (xhr, error, thrown) {
                    console.error('DataTables error:', error, thrown);
                    alert('An error occurred while loading vendor data. Please try again.');
                }
            },
            columns: columns,
            order: [[1, 'asc']], // Default sort by Name
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                 '<"row"<"col-sm-12"tr>>' +
                 '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            language: {
                emptyTable: 'No vendors found',
                zeroRecords: 'No matching vendors found',
                processing: '<i class="fas fa-spinner fa-spin"></i> Loading...'
            }
        });
    }

    /**
     * Initialize search button handlers
     */
    function initializeSearchHandlers() {
        // Search button
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            dataTable.ajax.reload();
        });

        // Reset button
        $('#btnReset').on('click', function (e) {
            e.preventDefault();
            $('#searchForm')[0].reset();

            // Reset to default values
            $('#IsActiveOnly').prop('checked', true);
            $('#FuelSuppliersOnly').prop('checked', false);
            $('#InternalVendorOnly').prop('checked', false);
            $('#IsBargeExEnabledOnly').prop('checked', false);
            $('#EnablePortalOnly').prop('checked', false);
            $('#LiquidBrokerOnly').prop('checked', false);
            $('#TankermanOnly').prop('checked', false);

            dataTable.ajax.reload();
        });

        // Enter key in text inputs triggers search
        $('#Name, #AccountingCode').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                $('#btnSearch').click();
            }
        });

        // Auto-search when checkboxes change
        $('input[type="checkbox"]').on('change', function () {
            dataTable.ajax.reload();
        });
    }

    /**
     * Initialize filter collapse toggles
     */
    function initializeFilterToggles() {
        // Remember collapse state
        const additionalFiltersCollapsed = localStorage.getItem('vendor-search-filters-collapsed') === 'true';

        if (!additionalFiltersCollapsed) {
            $('#additionalFilters').collapse('show');
        }

        $('#additionalFilters').on('hidden.bs.collapse', function () {
            localStorage.setItem('vendor-search-filters-collapsed', 'true');
        });

        $('#additionalFilters').on('shown.bs.collapse', function () {
            localStorage.setItem('vendor-search-filters-collapsed', 'false');
        });
    }

    /**
     * Render boolean value as checkbox
     */
    function renderBoolean(data, type, row) {
        if (type === 'display') {
            return data
                ? '<i class="fas fa-check text-success"></i>'
                : '<i class="fas fa-times text-danger"></i>';
        }
        return data;
    }

})();
