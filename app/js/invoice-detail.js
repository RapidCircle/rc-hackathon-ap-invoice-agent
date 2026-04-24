/**
 * Invoice Detail Page Logic
 * Loads a single invoice by ID and renders extracted fields, validation, and actions.
 */
(async function initInvoiceDetail() {
    await authService.waitForAppShell();

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
        return cls;
    }

    function formatCurrency(amount, currency) {
        if (amount == null) return '--';
        try {
            return new Intl.NumberFormat('en-US', { style: 'currency', currency: currency || 'USD' }).format(amount);
        } catch {
            return `${currency || ''} ${Number(amount).toFixed(2)}`;
        }
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

    // ---------- get invoice ID from URL ----------
    const params = new URLSearchParams(window.location.search);
    const invoiceId = params.get('id');

    if (!invoiceId) {
        document.getElementById('headerVendor').textContent = 'No invoice ID provided';
        return;
    }

    // ---------- load invoice ----------
    let invoice = null;

    try {
        const res = await authService.fetchWithAuth(`/api/invoices/${encodeURIComponent(invoiceId)}`);
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
    document.getElementById('headerVendor').textContent = invoice.vendorName || invoice.vendor || 'Unknown Vendor';
    document.getElementById('headerInvoiceNum').textContent = `Invoice #${escapeHtml(invoice.invoiceNumber || invoice.invoiceNo || '--')}`;

    const statusEl = document.getElementById('headerStatus');
    statusEl.className = `inline-block px-3 py-1 rounded-full text-xs font-semibold ${statusPill(invoice.status)}`;
    statusEl.textContent = invoice.status || 'Unknown';

    document.getElementById('headerAmount').textContent = formatCurrency(invoice.totalAmount || invoice.amount, invoice.currency);

    // ---------- render extracted fields ----------
    const mandatoryFields = [
        { key: 'vendorLegalName', label: 'Vendor Legal Name',     value: invoice.vendorName || invoice.vendor },
        { key: 'invoiceNumber',   label: 'Invoice Number',        value: invoice.invoiceNumber || invoice.invoiceNo },
        { key: 'invoiceDate',     label: 'Invoice Date',          value: invoice.invoiceDate },
        { key: 'invoiceCurrency', label: 'Invoice Currency',      value: invoice.currency },
        { key: 'totalAmount',     label: 'Total Amount (incl. tax)', value: formatCurrency(invoice.totalAmount || invoice.amount, invoice.currency) },
        { key: 'taxAmount',       label: 'Tax Amount',            value: invoice.taxAmount != null ? formatCurrency(invoice.taxAmount, invoice.currency) : '--' },
        { key: 'poNumber',        label: 'PO Number',             value: invoice.poNumber || invoice.purchaseOrder },
        { key: 'lineItems',       label: 'Line Items',            value: invoice.lineItems ? `${Array.isArray(invoice.lineItems) ? invoice.lineItems.length : 0} item(s)` : '--' }
    ];

    const fieldsContainer = document.getElementById('extractedFields');
    fieldsContainer.innerHTML = `
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            ${mandatoryFields.map(f => `
                <div class="border border-neutral-100 rounded-lg p-3">
                    <p class="text-xs font-medium text-neutral-500 mb-1">${escapeHtml(f.label)}</p>
                    <p class="text-sm font-medium text-neutral-900">${escapeHtml(f.value ?? '--')}</p>
                </div>
            `).join('')}
        </div>
    `;

    // ---------- render validation results ----------
    const validationContainer = document.getElementById('validationResults');
    const validations = invoice.validationResults || invoice.validations || [];

    if (Array.isArray(validations) && validations.length > 0) {
        validationContainer.innerHTML = `
            <div class="space-y-2">
                ${validations.map(v => {
                    const passed = v.passed || v.valid || v.status === 'pass';
                    const icon = passed
                        ? '<svg class="w-4 h-4 text-green-500 flex-shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>'
                        : '<svg class="w-4 h-4 text-red-500 flex-shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>';
                    const bg = passed ? 'bg-green-50' : 'bg-red-50';
                    return `
                        <div class="flex items-start gap-2 p-2 rounded-lg ${bg}">
                            ${icon}
                            <div>
                                <p class="text-sm font-medium text-neutral-800">${escapeHtml(v.field || v.name || v.rule || 'Check')}</p>
                                ${v.message ? `<p class="text-xs text-neutral-500">${escapeHtml(v.message)}</p>` : ''}
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        `;
    } else {
        validationContainer.innerHTML = '<p class="text-neutral-400 text-sm">No validation results available.</p>';
    }

    // ---------- render linked documents ----------
    const linkedDocs = invoice.linkedDocuments || invoice.documents || [];
    const docsContainer = document.getElementById('linkedDocs');

    if (Array.isArray(linkedDocs) && linkedDocs.length > 0) {
        docsContainer.innerHTML = `
            <div class="space-y-2">
                ${linkedDocs.map(doc => `
                    <div class="flex items-center gap-3 p-3 border border-neutral-100 rounded-lg">
                        <svg class="w-5 h-5 text-neutral-400 flex-shrink-0" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z"/></svg>
                        <div>
                            <p class="text-sm font-medium text-neutral-800">${escapeHtml(doc.name || doc.fileName || 'Document')}</p>
                            <p class="text-xs text-neutral-500">${escapeHtml(doc.type || doc.contentType || '')}</p>
                        </div>
                        ${doc.url ? `<a href="${escapeHtml(doc.url)}" target="_blank" class="ml-auto text-sm text-primary-500 hover:text-primary-600">Download</a>` : ''}
                    </div>
                `).join('')}
            </div>
        `;
    }

    // ---------- action buttons ----------
    async function invoiceAction(action, method = 'POST') {
        try {
            const res = await authService.fetchWithAuth(`/api/invoices/${encodeURIComponent(invoiceId)}/${action}`, { method });
            if (res.ok) {
                showToast(`Action "${action}" completed.`, 'success');
                // Reload the page to reflect changes
                setTimeout(() => window.location.reload(), 800);
            } else {
                const err = await res.json().catch(() => ({}));
                showToast(err.error || `Action "${action}" failed.`, 'error');
            }
        } catch (err) {
            console.error(`Action ${action} error:`, err);
            showToast(`Network error during "${action}".`, 'error');
        }
    }

    document.getElementById('btnReExtract').addEventListener('click', () => {
        if (confirm('Re-extract data from this invoice? This will overwrite current extracted fields.')) {
            invoiceAction('re-extract');
        }
    });

    document.getElementById('btnMarkReady').addEventListener('click', () => {
        invoiceAction('mark-ready');
    });

    document.getElementById('btnFlagException').addEventListener('click', () => {
        const reason = prompt('Enter exception reason (optional):');
        if (reason !== null) {
            authService.fetchWithAuth(`/api/invoices/${encodeURIComponent(invoiceId)}/flag-exception`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ reason })
            }).then(res => {
                if (res.ok) {
                    showToast('Invoice flagged as exception.', 'success');
                    setTimeout(() => window.location.reload(), 800);
                } else {
                    showToast('Failed to flag exception.', 'error');
                }
            }).catch(() => showToast('Network error.', 'error'));
        }
    });

    document.getElementById('btnEdit').addEventListener('click', () => {
        showToast('Edit mode coming soon.', 'info');
    });
})();
