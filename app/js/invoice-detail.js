/**
 * Invoice Detail Page Logic
 * Loads a single invoice by ID and renders extracted fields, validation, and actions.
 */
(async function initInvoiceDetail() {
    await authService.waitForAppShell();

    // ---------- helpers ----------
    function escapeHtml(str) {
        if (str == null) return '';
        var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(str).replace(/[&<>"']/g, function(m) { return map[m]; });
    }

    function formatCurrency(amount, currency) {
        if (amount == null || isNaN(amount)) return '--';
        var cur = currency || 'INR';
        try {
            return new Intl.NumberFormat('en-IN', { style: 'currency', currency: cur }).format(amount);
        } catch (e) {
            return cur + ' ' + Number(amount).toLocaleString('en-IN', { minimumFractionDigits: 2 });
        }
    }

    function formatDate(dateStr) {
        if (!dateStr) return '--';
        try {
            var d = new Date(dateStr);
            return d.toLocaleDateString('en-IN', { year: 'numeric', month: 'short', day: 'numeric' });
        } catch (e) {
            return dateStr;
        }
    }

    function statusPillClass(status) {
        var map = {
            Received:     'bg-blue-50 text-blue-700',
            ReadyForZoho: 'bg-green-50 text-green-700',
            Exception:    'bg-red-50 text-red-700',
            InReview:     'bg-amber-50 text-amber-700',
            Corrected:    'bg-purple-50 text-purple-700'
        };
        return map[status] || 'bg-neutral-100 text-neutral-600';
    }

    function showToast(message, type) {
        type = type || 'info';
        var container = document.getElementById('toastContainer');
        if (!container) return;
        var colors = {
            success: 'bg-green-50 text-green-800 border-green-200',
            error:   'bg-red-50 text-red-800 border-red-200',
            info:    'bg-blue-50 text-blue-800 border-blue-200'
        };
        var toast = document.createElement('div');
        toast.className = 'px-4 py-3 rounded-lg border shadow-sm text-sm font-medium ' + (colors[type] || colors.info);
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(function() { toast.style.opacity = '0'; setTimeout(function() { toast.remove(); }, 300); }, 4000);
    }

    // ---------- get invoice ID from URL ----------
    var params = new URLSearchParams(window.location.search);
    var invoiceId = params.get('id');

    if (!invoiceId) {
        document.getElementById('headerVendor').textContent = 'No invoice ID provided';
        return;
    }

    // ---------- load invoice ----------
    var invoice = null;

    try {
        var res = await authService.fetchWithAuth('/api/invoices/' + encodeURIComponent(invoiceId));
        if (!res.ok) {
            document.getElementById('headerVendor').textContent = 'Invoice not found';
            return;
        }
        invoice = await res.json();
    } catch (err) {
        console.error('Failed to load invoice:', err);
        document.getElementById('headerVendor').textContent = 'Error loading invoice';
        return;
    }

    // ---------- render header ----------
    // Backend returns InvoiceEntity with: vendorLegalName, invoiceNumber, invoiceCurrency, totalAmount, status, etc.
    document.getElementById('headerVendor').textContent = invoice.vendorLegalName || 'Unknown Vendor';
    document.getElementById('headerInvoiceNum').textContent = 'Invoice #' + escapeHtml(invoice.invoiceNumber || '--');

    var statusEl = document.getElementById('headerStatus');
    statusEl.className = 'inline-block px-3 py-1 rounded-full text-xs font-semibold ' + statusPillClass(invoice.status);
    statusEl.textContent = invoice.status || 'Unknown';

    document.getElementById('headerAmount').textContent = formatCurrency(invoice.totalAmount, invoice.invoiceCurrency);

    // ---------- render extracted fields (8 mandatory) ----------
    var mandatoryFields = [
        { label: 'Vendor Legal Name',       value: invoice.vendorLegalName },
        { label: 'Invoice Number',           value: invoice.invoiceNumber },
        { label: 'Invoice Date',             value: formatDate(invoice.invoiceDate) },
        { label: 'Invoice Currency',         value: invoice.invoiceCurrency },
        { label: 'Total Amount (incl. tax)', value: formatCurrency(invoice.totalAmount, invoice.invoiceCurrency) },
        { label: 'Tax Amount',               value: formatCurrency(invoice.taxAmount, invoice.invoiceCurrency) },
        { label: 'PO Number',                value: invoice.poNumber },
        { label: 'Line Items',               value: invoice.lineItemsSummary || '--' }
    ];

    var fieldsContainer = document.getElementById('extractedFields');
    var fieldsHtml = '<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">';
    for (var i = 0; i < mandatoryFields.length; i++) {
        var f = mandatoryFields[i];
        var hasValue = f.value && f.value !== '--' && f.value !== '';
        var borderColor = hasValue ? 'border-neutral-100' : 'border-red-200 bg-red-50/30';
        fieldsHtml += '<div class="border ' + borderColor + ' rounded-lg p-3">'
            + '<p class="text-xs font-medium text-neutral-500 mb-1">' + escapeHtml(f.label) + '</p>'
            + '<p class="text-sm font-medium text-neutral-900">' + escapeHtml(f.value || '--') + '</p>'
            + '</div>';
    }
    fieldsHtml += '</div>';
    fieldsContainer.innerHTML = fieldsHtml;

    // ---------- render validation results ----------
    // Run validation via API and display results
    var validationContainer = document.getElementById('validationResults');

    function renderValidationResults(results) {
        if (!results || !results.length) {
            validationContainer.innerHTML = '<p class="text-neutral-400 text-sm italic">Click "Validate" to run the 8-field validation check.</p>';
            return;
        }
        var html = '<div class="space-y-2">';
        for (var j = 0; j < results.length; j++) {
            var v = results[j];
            var passed = v.passed;
            var icon = passed
                ? '<svg class="w-4 h-4 text-green-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>'
                : '<svg class="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>';
            var bg = passed ? 'bg-green-50 border-green-100' : 'bg-red-50 border-red-100';
            html += '<div class="flex items-start gap-3 p-3 rounded-lg border ' + bg + '">'
                + icon
                + '<div>'
                + '<p class="text-sm font-medium text-neutral-800">' + escapeHtml(v.field) + '</p>'
                + (v.message ? '<p class="text-xs text-neutral-500 mt-0.5">' + escapeHtml(v.message) + '</p>' : '')
                + '</div></div>';
        }
        html += '</div>';
        validationContainer.innerHTML = html;
    }

    // Build client-side validation results from the invoice data
    function buildValidationChecks(inv) {
        var checks = [];
        // 1. Vendor Legal Name
        var hasVendor = inv.vendorLegalName && inv.vendorLegalName.trim() !== '';
        checks.push({ field: 'Vendor Legal Name', passed: hasVendor, message: hasVendor ? 'Present' : 'Missing — vendor name is required' });
        // 2. Invoice Number
        var hasInvNum = inv.invoiceNumber && inv.invoiceNumber.trim() !== '';
        checks.push({ field: 'Invoice Number', passed: hasInvNum, message: hasInvNum ? inv.invoiceNumber : 'Missing — invoice number is required' });
        // 3. Invoice Date
        var hasDate = inv.invoiceDate != null;
        checks.push({ field: 'Invoice Date', passed: hasDate, message: hasDate ? formatDate(inv.invoiceDate) : 'Missing — invoice date is required' });
        // 4. Invoice Currency
        var hasCurrency = inv.invoiceCurrency && inv.invoiceCurrency.trim() !== '';
        var validCurrencies = ['INR', 'USD', 'EUR', 'GBP', 'AUD', 'CAD', 'SGD', 'AED', 'JPY', 'CHF'];
        var currencyValid = hasCurrency && validCurrencies.indexOf(inv.invoiceCurrency.toUpperCase()) >= 0;
        checks.push({ field: 'Invoice Currency', passed: currencyValid, message: currencyValid ? inv.invoiceCurrency : (hasCurrency ? 'Invalid ISO 4217 currency: ' + inv.invoiceCurrency : 'Missing — currency is required') });
        // 5. Total Amount > 0
        var amountValid = inv.totalAmount != null && inv.totalAmount > 0;
        checks.push({ field: 'Total Amount', passed: amountValid, message: amountValid ? formatCurrency(inv.totalAmount, inv.invoiceCurrency) : 'Must be greater than zero' });
        // 6. Tax Amount >= 0
        var taxValid = inv.taxAmount != null && inv.taxAmount >= 0;
        checks.push({ field: 'Tax Amount', passed: taxValid, message: taxValid ? formatCurrency(inv.taxAmount, inv.invoiceCurrency) : 'Cannot be negative' });
        // 7. PO Number
        var hasPO = inv.poNumber && inv.poNumber.trim() !== '';
        checks.push({ field: 'PO Number', passed: hasPO, message: hasPO ? inv.poNumber : 'Missing — purchase order reference required' });
        // 8. Duplicate Check
        var isDuplicate = inv.exceptionReason && inv.exceptionReason.toLowerCase().indexOf('duplicate') >= 0;
        checks.push({ field: 'Duplicate Check', passed: !isDuplicate, message: isDuplicate ? 'Duplicate detected: same vendor + invoice number' : 'No duplicate found' });
        return checks;
    }

    // ---------- render validation stepper pipeline ----------
    function renderStepper(checks) {
        var stepper = document.getElementById('validationStepper');
        var placeholder = document.getElementById('stepperPlaceholder');
        var summary = document.getElementById('stepperSummary');
        if (!checks || !checks.length) return;

        if (placeholder) placeholder.style.display = 'none';

        var passed = 0;
        for (var s = 0; s < checks.length; s++) {
            if (checks[s].passed) passed++;
        }
        if (summary) {
            summary.textContent = passed + '/' + checks.length + ' passed';
            summary.className = 'text-xs font-semibold ' + (passed === checks.length ? 'text-green-600' : 'text-amber-600');
        }

        var html = '<div class="flex items-center w-full min-w-[600px]">';
        for (var k = 0; k < checks.length; k++) {
            var c = checks[k];
            var isLast = k === checks.length - 1;
            var iconBg = c.passed ? 'bg-green-100 text-green-600 border-green-300' : 'bg-red-100 text-red-500 border-red-300';
            var checkIcon = c.passed
                ? '<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>'
                : '<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>';
            var lineColor = c.passed ? 'bg-green-300' : 'bg-red-200';

            html += '<div class="flex flex-col items-center" style="min-width:70px">'
                + '<div class="w-8 h-8 rounded-full border-2 flex items-center justify-center ' + iconBg + '">'
                + checkIcon
                + '</div>'
                + '<p class="text-[10px] font-medium text-neutral-600 mt-1 text-center leading-tight" style="max-width:72px">' + escapeHtml(c.field) + '</p>'
                + '</div>';
            if (!isLast) {
                html += '<div class="flex-1 h-0.5 ' + lineColor + ' mx-1 mt-[-16px]"></div>';
            }
        }
        html += '</div>';
        stepper.innerHTML = html;
    }

    // Auto-show validation for non-Received invoices
    if (invoice.status !== 'Received') {
        var checks = buildValidationChecks(invoice);
        renderValidationResults(checks);
        renderStepper(checks);
    } else {
        validationContainer.innerHTML = '<p class="text-neutral-400 text-sm italic">Click "Validate" to run the 8-field validation check.</p>';
    }

    // ---------- render exception info ----------
    if (invoice.status === 'Exception' && invoice.exceptionReason) {
        var exDiv = document.createElement('div');
        exDiv.className = 'mt-4 p-4 bg-red-50 border border-red-200 rounded-lg';
        exDiv.innerHTML = '<p class="text-sm font-semibold text-red-700 mb-1">Exception Reason</p>'
            + '<p class="text-sm text-red-600">' + escapeHtml(invoice.exceptionReason) + '</p>'
            + (invoice.exceptionNotes ? '<p class="text-xs text-red-500 mt-2">Notes: ' + escapeHtml(invoice.exceptionNotes) + '</p>' : '');
        validationContainer.parentElement.appendChild(exDiv);
    }

    // ---------- render linked documents ----------
    var docsContainer = document.getElementById('linkedDocs');
    if (invoice.attachmentName) {
        docsContainer.innerHTML = '<div class="flex items-center gap-3 p-3 border border-neutral-100 rounded-lg">'
            + '<svg class="w-5 h-5 text-neutral-400 flex-shrink-0" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/></svg>'
            + '<div>'
            + '<p class="text-sm font-medium text-neutral-800">' + escapeHtml(invoice.attachmentName) + '</p>'
            + '<p class="text-xs text-neutral-500">Source: ' + escapeHtml(invoice.emailSender || 'Email attachment') + '</p>'
            + '</div></div>';
    }

    // ---------- action buttons ----------
    // Validate button — runs API validation and updates stepper + detail list
    var btnValidate = document.getElementById('btnReExtract');
    if (btnValidate) {
        btnValidate.textContent = 'Validate';
        btnValidate.onclick = async function() {
            btnValidate.disabled = true;
            btnValidate.textContent = 'Validating...';

            // Show client-side stepper immediately for visual feedback
            var clientChecks = buildValidationChecks(invoice);
            renderStepper(clientChecks);
            renderValidationResults(clientChecks);

            try {
                var vRes = await authService.fetchWithAuth('/api/invoices/' + encodeURIComponent(invoiceId) + '/validate', { method: 'POST' });
                if (vRes.ok) {
                    var result = await vRes.json();
                    showToast(result.isValid ? 'All 8 checks passed! Status: ReadyForZoho' : 'Validation failed: ' + (result.errors || []).join(', '), result.isValid ? 'success' : 'error');
                    setTimeout(function() { window.location.reload(); }, 1200);
                } else {
                    showToast('Validation failed.', 'error');
                }
            } catch (e) {
                showToast('Network error during validation.', 'error');
            }
            btnValidate.disabled = false;
            btnValidate.textContent = 'Validate';
        };
    }

    // Mark Ready button
    var btnReady = document.getElementById('btnMarkReady');
    if (btnReady) {
        btnReady.onclick = async function() {
            try {
                var r = await authService.fetchWithAuth('/api/invoices/' + encodeURIComponent(invoiceId), {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: 'ReadyForZoho' })
                });
                if (r.ok) {
                    showToast('Invoice marked as Ready for Zoho.', 'success');
                    setTimeout(function() { window.location.reload(); }, 800);
                } else { showToast('Failed to update status.', 'error'); }
            } catch (e) { showToast('Network error.', 'error'); }
        };
    }

    // Flag Exception button
    var btnException = document.getElementById('btnFlagException');
    if (btnException) {
        btnException.onclick = async function() {
            var reason = prompt('Enter exception reason:');
            if (reason === null) return;
            try {
                var r = await authService.fetchWithAuth('/api/invoices/' + encodeURIComponent(invoiceId), {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: 'Exception', exceptionReason: reason })
                });
                if (r.ok) {
                    showToast('Invoice flagged as Exception.', 'success');
                    setTimeout(function() { window.location.reload(); }, 800);
                } else { showToast('Failed to flag exception.', 'error'); }
            } catch (e) { showToast('Network error.', 'error'); }
        };
    }

    // Edit button
    var btnEdit = document.getElementById('btnEdit');
    if (btnEdit) {
        btnEdit.onclick = function() { showToast('Edit mode — use AI coding tools to build this!', 'info'); };
    }

    // ---------- file upload & preview ----------
    var uploadArea = document.getElementById('uploadArea');
    var fileInput = document.getElementById('fileInput');
    var uploadProgress = document.getElementById('uploadProgress');
    var uploadBar = document.getElementById('uploadBar');
    var uploadPercent = document.getElementById('uploadPercent');
    var uploadFileName = document.getElementById('uploadFileName');
    var docPreview = document.getElementById('docPreview');
    var previewFileName = document.getElementById('previewFileName');
    var previewContent = document.getElementById('previewContent');
    var btnRemoveDoc = document.getElementById('btnRemoveDoc');

    if (uploadArea && fileInput) {
        // Click to browse
        uploadArea.addEventListener('click', function() { fileInput.click(); });

        // Drag and drop
        uploadArea.addEventListener('dragover', function(e) {
            e.preventDefault();
            uploadArea.classList.add('border-primary-500', 'bg-primary-50');
        });
        uploadArea.addEventListener('dragleave', function() {
            uploadArea.classList.remove('border-primary-500', 'bg-primary-50');
        });
        uploadArea.addEventListener('drop', function(e) {
            e.preventDefault();
            uploadArea.classList.remove('border-primary-500', 'bg-primary-50');
            if (e.dataTransfer.files.length > 0) {
                handleFileSelected(e.dataTransfer.files[0]);
            }
        });

        fileInput.addEventListener('change', function() {
            if (fileInput.files.length > 0) {
                handleFileSelected(fileInput.files[0]);
            }
        });
    }

    function handleFileSelected(file) {
        var maxSize = 10 * 1024 * 1024; // 10MB
        var allowed = ['application/pdf', 'image/png', 'image/jpeg'];
        if (allowed.indexOf(file.type) < 0) {
            showToast('Only PDF, PNG, and JPG files are allowed.', 'error');
            return;
        }
        if (file.size > maxSize) {
            showToast('File too large. Maximum 10MB.', 'error');
            return;
        }

        // Show simulated upload progress (no real blob endpoint yet — hackathon MVP)
        uploadProgress.classList.remove('hidden');
        uploadFileName.textContent = file.name + ' (' + (file.size / 1024).toFixed(0) + ' KB)';
        var progress = 0;
        var interval = setInterval(function() {
            progress += Math.random() * 25 + 10;
            if (progress >= 100) {
                progress = 100;
                clearInterval(interval);
                uploadBar.style.width = '100%';
                uploadPercent.textContent = '100%';
                setTimeout(function() {
                    uploadProgress.classList.add('hidden');
                    showPreview(file);
                    showToast('File uploaded: ' + file.name, 'success');
                }, 400);
            } else {
                uploadBar.style.width = Math.round(progress) + '%';
                uploadPercent.textContent = Math.round(progress) + '%';
            }
        }, 200);
    }

    function showPreview(file) {
        uploadArea.classList.add('hidden');
        docPreview.classList.remove('hidden');
        previewFileName.textContent = file.name;

        if (file.type === 'application/pdf') {
            var objectUrl = URL.createObjectURL(file);
            previewContent.innerHTML = '<iframe src="' + objectUrl + '" class="w-full" style="height:400px" frameborder="0"></iframe>';
        } else {
            var reader = new FileReader();
            reader.onload = function(e) {
                previewContent.innerHTML = '<img src="' + e.target.result + '" alt="Invoice preview" class="max-w-full max-h-[400px] object-contain rounded">';
            };
            reader.readAsDataURL(file);
        }
    }

    if (btnRemoveDoc) {
        btnRemoveDoc.addEventListener('click', function() {
            docPreview.classList.add('hidden');
            uploadArea.classList.remove('hidden');
            previewContent.innerHTML = '';
            fileInput.value = '';
            showToast('Document removed.', 'info');
        });
    }

    // If invoice already has an attachment, show it in the existing docs area
    if (invoice.attachmentName) {
        uploadArea.classList.add('hidden');
        docPreview.classList.remove('hidden');
        previewFileName.textContent = invoice.attachmentName;
        previewContent.innerHTML = '<div class="flex flex-col items-center gap-2 py-6">'
            + '<svg class="w-12 h-12 text-neutral-300" fill="none" stroke="currentColor" stroke-width="1" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/></svg>'
            + '<p class="text-sm text-neutral-500">' + escapeHtml(invoice.attachmentName) + '</p>'
            + '<p class="text-xs text-neutral-400">Stored in Azure Blob Storage</p>'
            + '</div>';
    }
})();
