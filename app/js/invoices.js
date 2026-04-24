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

    // ---------- upload modal ----------
    const uploadModal     = document.getElementById('uploadModal');
    const uploadBackdrop  = document.getElementById('uploadBackdrop');
    const btnUploadOpen   = document.getElementById('btnUploadOpen');
    const btnUploadCancel = document.getElementById('btnUploadCancel');
    const btnUploadSubmit = document.getElementById('btnUploadSubmit');
    const uploadFile      = document.getElementById('uploadFile');
    const uploadFileName  = document.getElementById('uploadFileName');
    const uploadVendor    = document.getElementById('uploadVendor');

    function openUploadModal()  { uploadModal.classList.remove('hidden'); }
    function closeUploadModal() { uploadModal.classList.add('hidden'); uploadFile.value = ''; uploadFileName.textContent = 'Click to select or drag a file'; }

    btnUploadOpen.addEventListener('click', openUploadModal);
    btnUploadCancel.addEventListener('click', closeUploadModal);
    uploadBackdrop.addEventListener('click', closeUploadModal);

    uploadFile.addEventListener('change', () => {
        if (uploadFile.files.length > 0) {
            uploadFileName.textContent = uploadFile.files[0].name;
        }
    });

    // Load vendors for the dropdown
    try {
        const res = await authService.fetchWithAuth('/api/vendors');
        if (res.ok) {
            const data = await res.json();
            const vendors = Array.isArray(data) ? data : (data.vendors || data.items || []);
            vendors.forEach(v => {
                const opt = document.createElement('option');
                opt.value = v.id;
                opt.textContent = v.legalName || v.name || v.vendorName || '--';
                uploadVendor.appendChild(opt);
            });
        }
    } catch (err) {
        console.error('Failed to load vendors for dropdown:', err);
    }

    // Submit upload
    btnUploadSubmit.addEventListener('click', async () => {
        const file = uploadFile.files[0];
        if (!file) { showToast('Please select a file to upload.', 'error'); return; }

        const formData = new FormData();
        formData.append('file', file);
        if (uploadVendor.value) {
            formData.append('vendorId', uploadVendor.value);
        }

        btnUploadSubmit.disabled = true;
        btnUploadSubmit.textContent = 'Uploading...';

        try {
            const res = await authService.fetchWithAuth('/api/invoices', {
                method: 'POST',
                body: formData
            });

            if (res.ok) {
                showToast('Invoice uploaded successfully!', 'success');
                closeUploadModal();
                await loadInvoices();
            } else {
                const err = await res.json().catch(() => ({}));
                showToast(err.error || 'Upload failed.', 'error');
            }
        } catch (err) {
            console.error('Upload error:', err);
            showToast('Network error during upload.', 'error');
        } finally {
            btnUploadSubmit.disabled = false;
            btnUploadSubmit.textContent = 'Upload';
        }
    });

    // Initial load
    await loadInvoices();
})();
