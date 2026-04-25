/**
 * Invoices Page Logic
 * Lists all invoices, handles search/filter, and upload modal.
 */
(async function initInvoices() {
    await authService.waitForAppShell();

    let allInvoices = [];

    // ---------- helpers ----------
    function escapeHtml(str) {
        if (str == null) return '';
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(String(str)));
        return div.innerHTML;
    }

    function statusPill(status) {
        const map = {
            Received:     'bg-blue-50 text-blue-700',
            ReadyForZoho: 'bg-green-50 text-green-700',
            Exception:    'bg-red-50 text-red-700',
            InReview:     'bg-amber-50 text-amber-700',
            Corrected:    'bg-purple-50 text-purple-700'
        };
        const cls = map[status] || 'bg-neutral-100 text-neutral-600';
        return `<span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold ${cls}">${escapeHtml(status)}</span>`;
    }

    function formatCurrency(amount, currency) {
        if (amount == null || isNaN(amount)) return '--';
        var cur = currency || 'INR';
        try {
            return new Intl.NumberFormat('en-IN', { style: 'currency', currency: cur }).format(amount);
        } catch (e) {
            return cur + ' ' + Number(amount).toLocaleString('en-IN');
        }
    }

    function formatDate(dateStr) {
        if (!dateStr) return '--';
        try {
            return new Date(dateStr).toLocaleDateString('en-IN', { year: 'numeric', month: 'short', day: 'numeric' });
        } catch (e) { return dateStr; }
    }

    function showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer');
        if (!container) return;
        const colors = {
            success: 'bg-green-50 text-green-800 border-green-200',
            error:   'bg-red-50 text-red-800 border-red-200',
            info:    'bg-blue-50 text-blue-800 border-blue-200'
        };
        const toast = document.createElement('div');
        toast.className = `px-4 py-3 rounded-lg border shadow-sm text-sm font-medium ${colors[type] || colors.info} transition-opacity duration-300`;
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(() => { toast.classList.add('opacity-0'); setTimeout(() => toast.remove(), 300); }, 3000);
    }

    // ---------- render table ----------
    function renderTable(invoices) {
        const tbody = document.getElementById('invoicesBody');
        if (invoices.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-neutral-400">No invoices found.</td></tr>';
            return;
        }
        tbody.innerHTML = invoices.map(inv => `
            <tr class="border-b border-neutral-50 hover:bg-neutral-50 transition-colors">
                <td class="px-5 py-3 text-neutral-900 font-medium">${escapeHtml(inv.vendorLegalName || '--')}</td>
                <td class="px-5 py-3 text-neutral-600">${escapeHtml(inv.invoiceNumber || '--')}</td>
                <td class="px-5 py-3 text-neutral-600">${formatDate(inv.invoiceDate)}</td>
                <td class="px-5 py-3 text-neutral-900 font-medium">${formatCurrency(inv.totalAmount, inv.invoiceCurrency)}</td>
                <td class="px-5 py-3 text-neutral-600">${escapeHtml(inv.invoiceCurrency || 'INR')}</td>
                <td class="px-5 py-3">${statusPill(inv.status)}</td>
                <td class="px-5 py-3">
                    <a href="/app/invoice-detail.html?id=${encodeURIComponent(inv.id)}"
                       class="text-primary-500 hover:text-primary-600 font-medium text-sm transition-colors">View</a>
                </td>
            </tr>
        `).join('');
    }

    // ---------- fetch invoices ----------
    async function loadInvoices() {
        try {
            const res = await authService.fetchWithAuth('/api/invoices');
            if (res.ok) {
                const data = await res.json();
                allInvoices = Array.isArray(data) ? data : (data.invoices || data.items || []);
                renderTable(allInvoices);
            } else {
                document.getElementById('invoicesBody').innerHTML =
                    '<tr><td colspan="7" class="px-5 py-8 text-center text-red-500">Failed to load invoices.</td></tr>';
            }
        } catch (err) {
            console.error('Failed to load invoices:', err);
            document.getElementById('invoicesBody').innerHTML =
                '<tr><td colspan="7" class="px-5 py-8 text-center text-red-500">Failed to load invoices.</td></tr>';
        }
    }

    // ---------- search / filter ----------
    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('input', () => {
        const q = searchInput.value.toLowerCase().trim();
        if (!q) { renderTable(allInvoices); return; }
        const filtered = allInvoices.filter(inv => {
            const vendor  = (inv.vendorLegalName || '').toLowerCase();
            const invNum  = (inv.invoiceNumber || '').toLowerCase();
            const status  = (inv.status || '').toLowerCase();
            return vendor.includes(q) || invNum.includes(q) || status.includes(q);
        });
        renderTable(filtered);
    });

    // ---------- create invoice modal ----------
    var uploadModal     = document.getElementById('uploadModal');
    var uploadBackdrop  = document.getElementById('uploadBackdrop');
    var btnUploadOpen   = document.getElementById('btnUploadOpen');
    var btnUploadCancel = document.getElementById('btnUploadCancel');
    var btnUploadSubmit = document.getElementById('btnUploadSubmit');
    var uploadFile      = document.getElementById('uploadFile');
    var uploadFileName  = document.getElementById('uploadFileName');
    var uploadVendor    = document.getElementById('uploadVendor');
    var uploadDropZone  = document.getElementById('uploadDropZone');

    function openUploadModal()  { uploadModal.classList.remove('hidden'); }
    function closeUploadModal() {
        uploadModal.classList.add('hidden');
        uploadFile.value = '';
        uploadFileName.textContent = 'Click to select or drag a file (PDF, PNG, JPG)';
        // Reset form fields
        var fields = ['fldInvoiceNumber', 'fldInvoiceDate', 'fldTotalAmount', 'fldTaxAmount', 'fldPoNumber', 'fldEmailSender', 'fldLineItems', 'uploadVendorCustom'];
        for (var fi = 0; fi < fields.length; fi++) {
            var el = document.getElementById(fields[fi]);
            if (el) el.value = '';
        }
        document.getElementById('fldCurrency').value = 'INR';
        uploadVendor.value = '';
    }

    btnUploadOpen.addEventListener('click', openUploadModal);
    btnUploadCancel.addEventListener('click', closeUploadModal);
    uploadBackdrop.addEventListener('click', closeUploadModal);

    // File input handling
    if (uploadDropZone) {
        uploadDropZone.addEventListener('click', function() { uploadFile.click(); });
    }
    uploadFile.addEventListener('change', function() {
        if (uploadFile.files.length > 0) {
            uploadFileName.textContent = uploadFile.files[0].name;
        }
    });

    // Load vendors for the dropdown
    try {
        var vRes = await authService.fetchWithAuth('/api/vendors');
        if (vRes.ok) {
            var vData = await vRes.json();
            var vendors = Array.isArray(vData) ? vData : (vData.vendors || vData.items || []);
            for (var vi = 0; vi < vendors.length; vi++) {
                var opt = document.createElement('option');
                opt.value = vendors[vi].legalName || vendors[vi].name || '';
                opt.textContent = vendors[vi].legalName || vendors[vi].name || vendors[vi].vendorName || '--';
                uploadVendor.appendChild(opt);
            }
        }
    } catch (err) {
        console.error('Failed to load vendors for dropdown:', err);
    }

    // Submit create invoice
    btnUploadSubmit.addEventListener('click', async function() {
        // Determine vendor name
        var vendorName = uploadVendor.value || document.getElementById('uploadVendorCustom').value.trim();
        var invoiceNumber = document.getElementById('fldInvoiceNumber').value.trim();
        var invoiceDate = document.getElementById('fldInvoiceDate').value;
        var currency = document.getElementById('fldCurrency').value;
        var totalAmount = parseFloat(document.getElementById('fldTotalAmount').value);
        var taxAmount = parseFloat(document.getElementById('fldTaxAmount').value) || 0;
        var poNumber = document.getElementById('fldPoNumber').value.trim();
        var emailSender = document.getElementById('fldEmailSender').value.trim();
        var lineItems = document.getElementById('fldLineItems').value.trim();
        var file = uploadFile.files[0];

        // Validation
        if (!vendorName) { showToast('Vendor name is required.', 'error'); return; }
        if (!invoiceNumber) { showToast('Invoice number is required.', 'error'); return; }
        if (!invoiceDate) { showToast('Invoice date is required.', 'error'); return; }
        if (isNaN(totalAmount) || totalAmount <= 0) { showToast('Total amount must be greater than zero.', 'error'); return; }

        var payload = {
            vendorLegalName: vendorName,
            invoiceNumber: invoiceNumber,
            invoiceDate: invoiceDate + 'T00:00:00Z',
            currency: currency,
            totalAmount: totalAmount,
            taxAmount: taxAmount,
            poNumber: poNumber,
            emailSender: emailSender,
            lineItemsSummary: lineItems ? '[{"description":"' + lineItems.replace(/"/g, '\\"') + '","amount":' + totalAmount + '}]' : '',
            attachmentName: file ? file.name : ''
        };

        btnUploadSubmit.disabled = true;
        btnUploadSubmit.textContent = 'Creating...';

        try {
            var res = await authService.fetchWithAuth('/api/invoices', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                showToast('Invoice created successfully!', 'success');
                closeUploadModal();
                await loadInvoices();
            } else {
                var errBody = null;
                try { errBody = await res.json(); } catch (e) { errBody = {}; }
                showToast(errBody.error || 'Failed to create invoice.', 'error');
            }
        } catch (err) {
            console.error('Create invoice error:', err);
            showToast('Network error creating invoice.', 'error');
        } finally {
            btnUploadSubmit.disabled = false;
            btnUploadSubmit.textContent = 'Create Invoice';
        }
    });

    // ---------- upload invoice modal ----------
    var uploadInvoiceModal = document.getElementById('uploadInvoiceModal');
    var uploadInvoiceBackdrop = document.getElementById('uploadInvoiceBackdrop');
    var btnUploadInvoiceOpen = document.getElementById('btnUploadInvoiceOpen');
    var btnUploadInvoiceCancel = document.getElementById('btnUploadInvoiceCancel');
    var btnUploadInvoiceSubmit = document.getElementById('btnUploadInvoiceSubmit');
    var uploadInvoiceFile = document.getElementById('uploadInvoiceFile');
    var uploadInvoiceFileName = document.getElementById('uploadInvoiceFileName');
    var uploadInvoiceVendor = document.getElementById('uploadInvoiceVendor');
    var uploadInvoiceDropZone = document.getElementById('uploadInvoiceDropZone');

    function openUploadInvoiceModal() { uploadInvoiceModal.classList.remove('hidden'); }
    function closeUploadInvoiceModal() {
        uploadInvoiceModal.classList.add('hidden');
        uploadInvoiceFile.value = '';
        uploadInvoiceFileName.textContent = 'Click to select or drag file';
        uploadInvoiceVendor.value = '';
    }

    btnUploadInvoiceOpen.addEventListener('click', openUploadInvoiceModal);
    btnUploadInvoiceCancel.addEventListener('click', closeUploadInvoiceModal);
    uploadInvoiceBackdrop.addEventListener('click', closeUploadInvoiceModal);

    // File input handling for upload invoice modal
    if (uploadInvoiceDropZone) {
        uploadInvoiceDropZone.addEventListener('click', function() { uploadInvoiceFile.click(); });

        // Drag and drop support
        uploadInvoiceDropZone.addEventListener('dragover', function(e) {
            e.preventDefault();
            uploadInvoiceDropZone.classList.add('border-primary-400');
        });

        uploadInvoiceDropZone.addEventListener('dragleave', function(e) {
            e.preventDefault();
            uploadInvoiceDropZone.classList.remove('border-primary-400');
        });

        uploadInvoiceDropZone.addEventListener('drop', function(e) {
            e.preventDefault();
            uploadInvoiceDropZone.classList.remove('border-primary-400');
            if (e.dataTransfer.files.length > 0) {
                uploadInvoiceFile.files = e.dataTransfer.files;
                if (uploadInvoiceFile.files.length > 0) {
                    uploadInvoiceFileName.textContent = uploadInvoiceFile.files[0].name;
                }
            }
        });
    }

    uploadInvoiceFile.addEventListener('change', function() {
        if (uploadInvoiceFile.files.length > 0) {
            uploadInvoiceFileName.textContent = uploadInvoiceFile.files[0].name;
        }
    });

    // Load vendors for the upload invoice dropdown
    try {
        var vRes = await authService.fetchWithAuth('/api/vendors');
        if (vRes.ok) {
            var vData = await vRes.json();
            var vendors = Array.isArray(vData) ? vData : (vData.vendors || vData.items || []);
            for (var vi = 0; vi < vendors.length; vi++) {
                var opt = document.createElement('option');
                opt.value = vendors[vi].legalName || vendors[vi].name || '';
                opt.textContent = vendors[vi].legalName || vendors[vi].name || vendors[vi].vendorName || '--';
                uploadInvoiceVendor.appendChild(opt);
            }
        }
    } catch (err) {
        console.error('Failed to load vendors for upload invoice dropdown:', err);
    }

    // Submit upload invoice
    btnUploadInvoiceSubmit.addEventListener('click', async function() {
        var vendorName = uploadInvoiceVendor.value.trim();
        var file = uploadInvoiceFile.files[0];

        // Validation
        if (!vendorName) { showToast('Please select a vendor.', 'error'); return; }
        if (!file) { showToast('Please select a file to upload.', 'error'); return; }

        // Validate file type
        var allowedTypes = ['application/pdf', 'image/png', 'image/jpeg', 'image/jpg'];
        if (!allowedTypes.includes(file.type)) {
            showToast('Only PDF and image files (PNG, JPG, JPEG) are allowed.', 'error');
            return;
        }

        btnUploadInvoiceSubmit.disabled = true;
        btnUploadInvoiceSubmit.textContent = 'Uploading & Extracting...';

        try {
            // Create FormData with file and vendor
            var formData = new FormData();
            formData.append('file', file);
            formData.append('vendorLegalName', vendorName);

            // Call new upload endpoint that does real extraction
            var res = await authService.fetchWithAuth('/api/invoices/upload', {
                method: 'POST',
                body: formData
                // Don't set Content-Type header - browser will set it with boundary
            });

            if (!res.ok) {
                var errBody = null;
                try { errBody = await res.json(); } catch (e) { errBody = {}; }
                showToast(errBody.error || 'Failed to upload invoice.', 'error');
                throw new Error('Upload failed');
            }

            var result = await res.json();
            var invoiceId = result.invoice.rowKey || result.invoice.id;
            
            showToast('Invoice extracted successfully!', 'success');
            
            // Close modal and show extraction results
            closeUploadInvoiceModal();
            
            // Show extraction results modal
            if (result.extraction) {
                showExtractionResults(invoiceId, result.extraction);
            }
            
            // Reload invoices list
            await loadInvoices();
        } catch (err) {
            console.error('Upload invoice error:', err);
            showToast('Network error uploading invoice.', 'error');
        } finally {
            btnUploadInvoiceSubmit.disabled = false;
            btnUploadInvoiceSubmit.textContent = 'Upload';
        }
    });

    // Initial load
    await loadInvoices();

    // ---------- extraction results modal ----------
    var extractionResultsModal = document.getElementById('extractionResultsModal');
    var extractionBackdrop = document.getElementById('extractionBackdrop');
    var btnExtractCancel = document.getElementById('btnExtractCancel');
    var btnExtractViewDetails = document.getElementById('btnExtractViewDetails');
    var currentInvoiceIdForExtraction = null;

    function closeExtractionModal() {
        extractionResultsModal.classList.add('hidden');
    }

    function showExtractionResults(invoiceId, result) {
        currentInvoiceIdForExtraction = invoiceId;
        
        // Helper to format currency
        function formatCurr(amt, curr) {
            if (amt == null || isNaN(amt)) return '--';
            var c = curr || 'INR';
            try {
                return new Intl.NumberFormat('en-IN', { style: 'currency', currency: c }).format(amt);
            } catch (e) {
                return c + ' ' + Number(amt).toLocaleString('en-IN');
            }
        }

        // Helper to format date
        function formatD(dateStr) {
            if (!dateStr) return '--';
            try {
                return new Date(dateStr).toLocaleDateString('en-IN', { year: 'numeric', month: 'short', day: 'numeric' });
            } catch (e) { return dateStr; }
        }

        // Populate fields
        var extracted = result.extractedData || {};
        var fieldConf = result.fieldConfidences || {};

        document.getElementById('resVendor').textContent = escapeHtml(extracted.vendorLegalName || '--');
        document.getElementById('confVendor').textContent = Math.round((fieldConf.VendorLegalName || 0) * 100) + '%';

        document.getElementById('resInvoiceNum').textContent = escapeHtml(extracted.invoiceNumber || '--');
        document.getElementById('confInvoiceNum').textContent = Math.round((fieldConf.InvoiceNumber || 0) * 100) + '%';

        document.getElementById('resInvoiceDate').textContent = formatD(extracted.invoiceDate);
        document.getElementById('confInvoiceDate').textContent = Math.round((fieldConf.InvoiceDate || 0) * 100) + '%';

        document.getElementById('resCurrency').textContent = escapeHtml(extracted.currency || '--');
        document.getElementById('confCurrency').textContent = Math.round((fieldConf.Currency || 0) * 100) + '%';

        document.getElementById('resTotalAmount').textContent = formatCurr(extracted.totalAmount, extracted.currency);
        document.getElementById('confTotalAmount').textContent = Math.round((fieldConf.TotalAmount || 0) * 100) + '%';

        document.getElementById('resPoNumber').textContent = escapeHtml(extracted.poNumber || '--');
        document.getElementById('confPoNumber').textContent = Math.round((fieldConf.PoNumber || 0) * 100) + '%';

        document.getElementById('overallConfidence').textContent = Math.round((result.confidence || 0) * 100) + '%';

        extractionResultsModal.classList.remove('hidden');
    }

    btnExtractCancel.addEventListener('click', closeExtractionModal);
    extractionBackdrop.addEventListener('click', closeExtractionModal);
    btnExtractViewDetails.addEventListener('click', function() {
        closeExtractionModal();
        if (currentInvoiceIdForExtraction) {
            window.location.href = '/app/invoice-detail.html?id=' + encodeURIComponent(currentInvoiceIdForExtraction);
        }
    });
})();
