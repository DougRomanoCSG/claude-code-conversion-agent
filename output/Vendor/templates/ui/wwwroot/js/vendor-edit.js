/**
 * Vendor Edit JavaScript
 * Form validation, conditional fields, and tab management
 * Location: src/BargeOps.UI/wwwroot/js/vendor-edit.js
 */

(function () {
    'use strict';

    const config = window.vendorEditConfig || {};

    /**
     * Initialize on page load
     */
    $(document).ready(function () {
        initializeSelect2();
        initializePhoneMasking();
        initializeFormValidation();
        initializeConditionalFields();
        initializeContactHandlers();
        initializeBusinessUnitHandlers();
        initializeTabPersistence();
    });

    /**
     * Initialize Select2 for enhanced dropdowns
     */
    function initializeSelect2() {
        $('.select2').select2({
            placeholder: '-- Select --',
            allowClear: true,
            width: '100%'
        });
    }

    /**
     * Initialize phone number masking
     */
    function initializePhoneMasking() {
        $('[data-mask]').each(function () {
            const mask = $(this).data('mask');
            if (mask) {
                $(this).mask(mask);
            }
        });
    }

    /**
     * Initialize form validation
     */
    function initializeFormValidation() {
        $('#vendorForm').validate({
            rules: {
                'Vendor.Name': {
                    required: true,
                    maxlength: 20
                },
                'Vendor.LongName': {
                    required: true,
                    maxlength: 50
                },
                'Vendor.AccountingCode': {
                    maxlength: 20
                },
                'Vendor.Address': {
                    maxlength: 80
                },
                'Vendor.City': {
                    maxlength: 30
                },
                'Vendor.State': {
                    maxlength: 2
                },
                'Vendor.Zip': {
                    maxlength: 10
                },
                'Vendor.PhoneNumber': {
                    maxlength: 10
                },
                'Vendor.FaxNumber': {
                    maxlength: 10
                },
                'Vendor.EmailAddress': {
                    email: true,
                    maxlength: 100
                },
                'Vendor.BargeExTradingPartnerNum': {
                    required: function () {
                        return $('#chkIsBargeExEnabled').is(':checked');
                    },
                    maxlength: 8
                },
                'Vendor.BargeExConfigID': {
                    required: function () {
                        return $('#chkIsBargeExEnabled').is(':checked');
                    }
                }
            },
            messages: {
                'Vendor.Name': {
                    required: 'Name is required',
                    maxlength: 'Name cannot exceed 20 characters'
                },
                'Vendor.LongName': {
                    required: 'Long name is required',
                    maxlength: 'Long name cannot exceed 50 characters'
                },
                'Vendor.EmailAddress': {
                    email: 'Please enter a valid email address',
                    maxlength: 'Email address cannot exceed 100 characters'
                },
                'Vendor.BargeExTradingPartnerNum': {
                    required: 'Both Trading partner number & Configuration required for BargeEx enabled.'
                },
                'Vendor.BargeExConfigID': {
                    required: 'Both Trading partner number & Configuration required for BargeEx enabled.'
                }
            },
            errorClass: 'text-danger',
            errorElement: 'span',
            highlight: function (element) {
                $(element).addClass('is-invalid');
            },
            unhighlight: function (element) {
                $(element).removeClass('is-invalid');
            }
        });
    }

    /**
     * Initialize conditional field logic
     */
    function initializeConditionalFields() {
        // BargeEx conditional fields
        if (config.bargeExGlobalSettingEnabled) {
            $('#chkIsBargeExEnabled').on('change', function () {
                const isEnabled = $(this).is(':checked');

                $('#txtBargeExTradingPartnerNum, #cboBargeExConfigID')
                    .prop('disabled', !isEnabled)
                    .prop('required', isEnabled);

                $('#bargeExTradingPartnerGroup, #bargeExConfigGroup')
                    .toggleClass('opacity-50', !isEnabled);

                // Clear values if disabled
                if (!isEnabled) {
                    $('#txtBargeExTradingPartnerNum').val('');
                    $('#cboBargeExConfigID').val('').trigger('change');
                }
            }).trigger('change');
        }

        // Portal conditional section
        if (config.portalLicenseActive) {
            $('#chkEnablePortal').on('change', function () {
                const isEnabled = $(this).is(':checked');
                $('#portalGroupsSection').toggleClass('opacity-50', !isEnabled);
            }).trigger('change');
        }
    }

    /**
     * Initialize contact grid handlers
     */
    function initializeContactHandlers() {
        // Initialize contacts DataTable (client-side)
        if ($('#contactsTable tbody tr').length > 1) {
            $('#contactsTable').DataTable({
                paging: false,
                searching: false,
                ordering: true,
                info: false,
                order: [[1, 'asc']] // Sort by Name
            });
        }

        // Add contact button
        $('#btnAddContact').on('click', function () {
            openContactModal(null);
        });

        // Edit contact buttons
        $(document).on('click', '.btn-edit-contact', function () {
            const contactId = $(this).data('id');
            openContactModal(contactId);
        });

        // Delete contact buttons
        $(document).on('click', '.btn-delete-contact', function () {
            const contactId = $(this).data('id');

            if (confirm('Are you sure you want to delete this contact?')) {
                deleteContact(contactId);
            }
        });
    }

    /**
     * Initialize business unit grid handlers
     */
    function initializeBusinessUnitHandlers() {
        // Initialize business units DataTable (client-side)
        if ($('#businessUnitsTable tbody tr').length > 1) {
            $('#businessUnitsTable').DataTable({
                paging: false,
                searching: false,
                ordering: true,
                info: false,
                order: [[1, 'asc']] // Sort by Name
            });
        }

        // Add business unit button
        $('#btnAddBusinessUnit').on('click', function () {
            openBusinessUnitModal(null);
        });

        // Edit business unit buttons
        $(document).on('click', '.btn-edit-business-unit', function () {
            const businessUnitId = $(this).data('id');
            openBusinessUnitModal(businessUnitId);
        });

        // Delete business unit buttons
        $(document).on('click', '.btn-delete-business-unit', function () {
            const businessUnitId = $(this).data('id');

            if (confirm('Are you sure you want to delete this business unit?')) {
                deleteBusinessUnit(businessUnitId);
            }
        });
    }

    function getVendorId() {
        const value = $('input[name="Vendor.VendorID"]').val();
        const vendorId = parseInt(value, 10);
        return isNaN(vendorId) ? 0 : vendorId;
    }

    function getAntiForgeryToken() {
        // Tag helper should generate this inside the main vendor form.
        return $('#vendorForm input[name="__RequestVerificationToken"]').val();
    }

    function openContactModal(contactId) {
        const vendorId = getVendorId();
        if (!vendorId) {
            alert('Please save the vendor before managing contacts.');
            return;
        }

        $.get('/VendorSearch/ContactModal', { vendorId, contactId: contactId || '' })
            .done(function (html) {
                $('#contactModalContainer').html(html);

                // Masking inside modal
                $('#contactModal [data-mask]').each(function () {
                    const mask = $(this).data('mask');
                    if (mask) {
                        $(this).mask(mask);
                    }
                });

                const modalEl = document.getElementById('contactModal');
                const modal = new bootstrap.Modal(modalEl);
                modal.show();
            })
            .fail(function (xhr) {
                alert(xhr.responseText || 'Failed to load contact form.');
            });
    }

    $(document).on('submit', '#contactModalForm', function (e) {
        e.preventDefault();

        const $form = $(this);
        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize()
        })
            .done(function () {
                window.location.reload();
            })
            .fail(function (xhr) {
                // On validation, controller returns HTML partial with 400.
                if (xhr.status === 400 && xhr.responseText) {
                    $('#contactModalContainer').html(xhr.responseText);

                    // Re-apply masking after re-render
                    $('#contactModal [data-mask]').each(function () {
                        const mask = $(this).data('mask');
                        if (mask) {
                            $(this).mask(mask);
                        }
                    });

                    const modalEl = document.getElementById('contactModal');
                    const modal = new bootstrap.Modal(modalEl);
                    modal.show();
                } else {
                    alert(xhr.responseText || 'Failed to save contact.');
                }
            });
    });

    function deleteContact(contactId) {
        const vendorId = getVendorId();
        const token = getAntiForgeryToken();

        $.ajax({
            url: '/VendorSearch/DeleteContact',
            type: 'POST',
            data: {
                vendorId: vendorId,
                contactId: contactId,
                __RequestVerificationToken: token
            }
        })
            .done(function () {
                window.location.reload();
            })
            .fail(function (xhr) {
                alert(xhr.responseText || 'Failed to delete contact.');
            });
    }

    function openBusinessUnitModal(businessUnitId) {
        const vendorId = getVendorId();
        if (!vendorId) {
            alert('Please save the vendor before managing business units.');
            return;
        }

        $.get('/VendorSearch/BusinessUnitModal', { vendorId, businessUnitId: businessUnitId || '' })
            .done(function (html) {
                $('#businessUnitModalContainer').html(html);

                // Select2 inside modal needs dropdownParent
                const $modal = $('#businessUnitModal');
                $modal.find('.select2').select2({
                    placeholder: '-- Select --',
                    allowClear: true,
                    width: '100%',
                    dropdownParent: $modal
                });

                initializeBusinessUnitModalToggles();

                const modalEl = document.getElementById('businessUnitModal');
                const modal = new bootstrap.Modal(modalEl);
                modal.show();
            })
            .fail(function (xhr) {
                alert(xhr.responseText || 'Failed to load business unit form.');
            });
    }

    function initializeBusinessUnitModalToggles() {
        const $fuelSupplier = $('#chkIsFuelSupplier');
        const $defaultFuelSupplier = $('#chkIsDefaultFuelSupplier');
        const $qty = $('#minDiscountQtyGroup :input');
        const $freq = $('#minDiscountFrequencyGroup :input');

        function toggleFuelFields() {
            const isFuelSupplier = $fuelSupplier.is(':checked');

            $defaultFuelSupplier.prop('disabled', !isFuelSupplier);
            $qty.prop('disabled', !isFuelSupplier);
            $freq.prop('disabled', !isFuelSupplier);

            if (!isFuelSupplier) {
                $defaultFuelSupplier.prop('checked', false);
                $qty.val('');
                $freq.val('');
            }
        }

        $fuelSupplier.on('change', toggleFuelFields);
        toggleFuelFields();
    }

    $(document).on('submit', '#businessUnitModalForm', function (e) {
        e.preventDefault();

        const $form = $(this);
        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize()
        })
            .done(function () {
                window.location.reload();
            })
            .fail(function (xhr) {
                if (xhr.status === 400 && xhr.responseText) {
                    $('#businessUnitModalContainer').html(xhr.responseText);

                    const $modal = $('#businessUnitModal');
                    $modal.find('.select2').select2({
                        placeholder: '-- Select --',
                        allowClear: true,
                        width: '100%',
                        dropdownParent: $modal
                    });

                    initializeBusinessUnitModalToggles();

                    const modalEl = document.getElementById('businessUnitModal');
                    const modal = new bootstrap.Modal(modalEl);
                    modal.show();
                } else {
                    alert(xhr.responseText || 'Failed to save business unit.');
                }
            });
    });

    function deleteBusinessUnit(businessUnitId) {
        const vendorId = getVendorId();
        const token = getAntiForgeryToken();

        $.ajax({
            url: '/VendorSearch/DeleteBusinessUnit',
            type: 'POST',
            data: {
                vendorId: vendorId,
                businessUnitId: businessUnitId,
                __RequestVerificationToken: token
            }
        })
            .done(function () {
                window.location.reload();
            })
            .fail(function (xhr) {
                alert(xhr.responseText || 'Failed to delete business unit.');
            });
    }

    /**
     * Initialize tab persistence (remember last active tab)
     */
    function initializeTabPersistence() {
        // Restore last active tab
        const lastTab = localStorage.getItem('vendor-edit-last-tab');
        if (lastTab) {
            const tabButton = document.querySelector(`button[data-bs-target="${lastTab}"]`);
            if (tabButton) {
                const tab = new bootstrap.Tab(tabButton);
                tab.show();
            }
        }

        // Save active tab on change
        $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
            const target = e.target.getAttribute('data-bs-target');
            localStorage.setItem('vendor-edit-last-tab', target);
        });
    }

})();
