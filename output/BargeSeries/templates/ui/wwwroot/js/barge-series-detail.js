/**
 * BargeSeries Detail/Edit Page JavaScript
 * Handles draft tonnage grid functionality, paste from clipboard, and export
 */
window.bargeSeriesDetail = (function () {
    'use strict';

    let config = {
        isReadOnly: false
    };

    /**
     * Initialize the detail page
     */
    function init(options) {
        config = { ...config, ...options };

        initializeSelect2();
        initializeDraftGrid();
        attachEventHandlers();
    }

    /**
     * Initialize Select2 dropdowns
     */
    function initializeSelect2() {
        $('[data-select2="true"]').select2({
            placeholder: '-- Select --',
            allowClear: false,
            width: '100%'
        });
    }

    /**
     * Initialize draft tonnage grid
     */
    function initializeDraftGrid() {
        if (config.isReadOnly) {
            return;
        }

        // Enable tab navigation between cells
        $('#draftGrid input[type="number"]').on('keydown', function (e) {
            if (e.key === 'Tab') {
                // Default tab behavior works fine
                return true;
            }

            // Arrow key navigation
            if (e.key === 'ArrowRight' || e.key === 'ArrowLeft' ||
                e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                e.preventDefault();
                navigateGrid($(this), e.key);
            }
        });

        // Select all text on focus for easier data entry
        $('#draftGrid input[type="number"]').on('focus', function () {
            $(this).select();
        });

        // Validate input as non-negative integer
        $('#draftGrid input[type="number"]').on('blur', function () {
            const value = $(this).val();
            if (value !== '' && (isNaN(value) || parseInt(value) < 0)) {
                alert('Please enter a non-negative integer value.');
                $(this).val('');
                $(this).focus();
            }
        });
    }

    /**
     * Navigate grid with arrow keys
     */
    function navigateGrid($currentInput, direction) {
        const $currentCell = $currentInput.closest('td');
        const $currentRow = $currentCell.closest('tr');
        let $targetInput = null;

        switch (direction) {
            case 'ArrowRight':
                $targetInput = $currentCell.next('td').find('input[type="number"]');
                break;
            case 'ArrowLeft':
                $targetInput = $currentCell.prev('td').find('input[type="number"]');
                break;
            case 'ArrowUp':
                $targetInput = $currentRow.prev('tr').find(`td:eq(${$currentCell.index()}) input[type="number"]`);
                break;
            case 'ArrowDown':
                $targetInput = $currentRow.next('tr').find(`td:eq(${$currentCell.index()}) input[type="number"]`);
                break;
        }

        if ($targetInput && $targetInput.length > 0) {
            $targetInput.focus();
        }
    }

    /**
     * Attach event handlers
     */
    function attachEventHandlers() {
        // Paste from clipboard button
        $('#btnPaste').on('click', function () {
            pasteFromClipboard();
        });

        // Export button
        $('#btnExport').on('click', function () {
            exportToExcel();
        });
    }

    /**
     * Paste data from clipboard into draft grid
     */
    async function pasteFromClipboard() {
        try {
            // Read clipboard text
            const text = await navigator.clipboard.readText();

            if (!text || text.trim() === '') {
                alert('No data found in clipboard.');
                return;
            }

            // Parse clipboard data
            const rows = text.split('\n').filter(row => row.trim() !== '');
            const parsedData = [];

            for (let i = 0; i < rows.length && i < 14; i++) {
                // Split by tab or comma
                const cells = rows[i].split(/\t|,/).map(cell => cell.trim());
                const rowData = [];

                for (let j = 0; j < cells.length && j < 12; j++) {
                    const value = cells[j];
                    const numValue = parseInt(value);

                    // Validate as non-negative integer
                    if (value !== '' && (!isNaN(numValue) && numValue >= 0)) {
                        rowData.push(numValue);
                    } else {
                        rowData.push(null);
                    }
                }

                parsedData.push(rowData);
            }

            // Update grid with parsed data
            updateGridFromData(parsedData);

            alert(`Successfully pasted ${parsedData.length} rows.`);
        } catch (err) {
            console.error('Clipboard error:', err);
            alert('Failed to read clipboard. Please ensure you have granted clipboard permissions.');
        }
    }

    /**
     * Update grid cells from parsed data
     */
    function updateGridFromData(data) {
        const $rows = $('#draftGrid tbody tr');

        data.forEach((rowData, rowIndex) => {
            if (rowIndex >= $rows.length) return;

            const $row = $rows.eq(rowIndex);
            const $inputs = $row.find('input[type="number"]').not('[readonly]');

            rowData.forEach((value, colIndex) => {
                if (colIndex >= $inputs.length) return;

                const $input = $inputs.eq(colIndex);
                $input.val(value !== null ? value : '');
            });
        });
    }

    /**
     * Export draft grid to CSV
     */
    function exportToExcel() {
        const csvData = [];
        const headers = ['Feet', '0"', '1"', '2"', '3"', '4"', '5"', '6"', '7"', '8"', '9"', '10"', '11"'];
        csvData.push(headers.join(','));

        // Get data from grid
        $('#draftGrid tbody tr').each(function () {
            const row = [];
            $(this).find('input').each(function () {
                const value = $(this).val();
                row.push(value !== '' ? value : '');
            });
            csvData.push(row.join(','));
        });

        // Create CSV file and download
        const csvContent = csvData.join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);

        link.setAttribute('href', url);
        link.setAttribute('download', 'barge_series_drafts.csv');
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    return {
        init: init
    };
})();
