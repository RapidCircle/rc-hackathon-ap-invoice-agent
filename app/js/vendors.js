/**
 * Vendors Page Logic
 * Lists vendor master data, handles add/edit/delete.
 */
(async function initVendors() {
    await authService.waitForAppShell();

    let allVendors = [];

    // ---------- helpers ----------
    function escapeHtml(str) {
        if (str == null) return '';
        const div = document.createElement('div');
        div.appendChild(document.createTextNode(String(str)));
        return div.innerHTML;
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
    function renderTable(vendors) {
        const tbody = document.getElementById('vendorsBody');
        if (vendors.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="px-5 py-8 text-center text-neutral-400">No vendors found.</td></tr>';
            return;
        }
        tbody.innerHTML = vendors.map(v => `
            <tr class="border-b border-neutral-50 hover:bg-neutral-50 transition-colors">
                <td class="px-5 py-3 text-neutral-900 font-medium">${escapeHtml(v.legalName || v.name || '--')}</td>
                <td class="px-5 py-3 text-neutral-600">${escapeHtml(v.tradingName || v.displayName || '--')}</td>
                <td class="px-5 py-3 text-neutral-600">${escapeHtml(v.taxId || v.taxNumber || '--')}</td>
                <td class="px-5 py-3">
                    <span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                        (v.status || 'active').toLowerCase() === 'active'
                            ? 'bg-green-50 text-green-700'
                            : 'bg-neutral-100 text-neutral-600'
                    }">${escapeHtml(v.status || 'Active')}</span>
                </td>
                <td class="px-5 py-3">
                    <div class="flex items-center gap-2">
                        <button onclick="window._editVendor('${escapeHtml(v.id)}')"
                                class="text-primary-500 hover:text-primary-600 font-medium text-sm transition-colors">Edit</button>
                        <button onclick="window._deleteVendor('${escapeHtml(v.id)}')"
                                class="text-red-500 hover:text-red-600 font-medium text-sm transition-colors">Delete</button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    // ---------- fetch vendors ----------
    async function loadVendors() {
        try {
            const res = await authService.fetchWithAuth('/api/vendors');
            if (res.ok) {
                const data = await res.json();
                allVendors = Array.isArray(data) ? data : (data.vendors || data.items || []);
                renderTable(allVendors);
            } else {
                document.getElementById('vendorsBody').innerHTML =
                    '<tr><td colspan="5" class="px-5 py-8 text-center text-red-500">Failed to load vendors.</td></tr>';
            }
        } catch (err) {
            console.error('Failed to load vendors:', err);
            document.getElementById('vendorsBody').innerHTML =
                '<tr><td colspan="5" class="px-5 py-8 text-center text-red-500">Failed to load vendors.</td></tr>';
        }
    }

    // ---------- modal ----------
    const vendorModal     = document.getElementById('vendorModal');
    const vendorBackdrop  = document.getElementById('vendorBackdrop');
    const modalTitle      = document.getElementById('vendorModalTitle');
    const btnAddOpen      = document.getElementById('btnAddVendorOpen');
    const btnCancel       = document.getElementById('btnVendorCancel');
    const btnSubmit       = document.getElementById('btnVendorSubmit');
    const editIdField     = document.getElementById('vendorEditId');

    const fields = {
        legalName:   document.getElementById('vendorLegalName'),
        tradingName: document.getElementById('vendorTradingName'),
        taxId:       document.getElementById('vendorTaxId'),
        email:       document.getElementById('vendorEmail'),
        currency:    document.getElementById('vendorCurrency')
    };

    function clearForm() {
        editIdField.value = '';
        Object.values(fields).forEach(el => { if (el.tagName === 'SELECT') el.selectedIndex = 0; else el.value = ''; });
    }

    function openModal(title = 'Add Vendor') {
        modalTitle.textContent = title;
        vendorModal.classList.remove('hidden');
    }

    function closeModal() {
        vendorModal.classList.add('hidden');
        clearForm();
    }

    btnAddOpen.addEventListener('click', () => { clearForm(); openModal('Add Vendor'); });
    btnCancel.addEventListener('click', closeModal);
    vendorBackdrop.addEventListener('click', closeModal);

    // ---------- save vendor (create or update) ----------
    btnSubmit.addEventListener('click', async () => {
        const legalName = fields.legalName.value.trim();
        if (!legalName) { showToast('Legal name is required.', 'error'); return; }

        const payload = {
            legalName,
            tradingName: fields.tradingName.value.trim(),
            taxId:       fields.taxId.value.trim(),
            email:       fields.email.value.trim(),
            currency:    fields.currency.value
        };

        const editId = editIdField.value;
        const isEdit = !!editId;

        btnSubmit.disabled = true;
        btnSubmit.textContent = 'Saving...';

        try {
            const url    = isEdit ? `/api/vendors/${encodeURIComponent(editId)}` : '/api/vendors';
            const method = isEdit ? 'PUT' : 'POST';

            const res = await authService.fetchWithAuth(url, {
                method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                showToast(isEdit ? 'Vendor updated.' : 'Vendor created.', 'success');
                closeModal();
                await loadVendors();
            } else {
                const err = await res.json().catch(() => ({}));
                showToast(err.error || 'Save failed.', 'error');
            }
        } catch (err) {
            console.error('Save vendor error:', err);
            showToast('Network error.', 'error');
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.textContent = 'Save';
        }
    });

    // ---------- edit vendor ----------
    window._editVendor = function(id) {
        const vendor = allVendors.find(v => v.id === id);
        if (!vendor) return;

        editIdField.value            = vendor.id;
        fields.legalName.value       = vendor.legalName || vendor.name || '';
        fields.tradingName.value     = vendor.tradingName || vendor.displayName || '';
        fields.taxId.value           = vendor.taxId || vendor.taxNumber || '';
        fields.email.value           = vendor.email || '';
        fields.currency.value        = vendor.currency || 'USD';

        openModal('Edit Vendor');
    };

    // ---------- delete vendor ----------
    window._deleteVendor = async function(id) {
        if (!confirm('Are you sure you want to delete this vendor?')) return;

        try {
            const res = await authService.fetchWithAuth(`/api/vendors/${encodeURIComponent(id)}`, { method: 'DELETE' });
            if (res.ok) {
                showToast('Vendor deleted.', 'success');
                await loadVendors();
            } else {
                const err = await res.json().catch(() => ({}));
                showToast(err.error || 'Delete failed.', 'error');
            }
        } catch (err) {
            console.error('Delete vendor error:', err);
            showToast('Network error.', 'error');
        }
    };

    // Initial load
    await loadVendors();
})();
