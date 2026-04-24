/**
 * Dashboard Page Logic
 * Fetches stats and recent invoices, renders them into the dashboard.
 */
(async function initDashboard() {
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
        return `<span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold ${cls}">${escapeHtml(status)}</span>`;
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

    // ---------- fetch stats ----------
    try {
        const res = await authService.fetchWithAuth('/api/invoices/stats');
        if (res.ok) {
            const stats = await res.json();
            document.getElementById('statReceived').textContent  = stats.received  ?? 0;
            document.getElementById('statReady').textContent     = stats.readyForZoho ?? stats.ready ?? 0;
            document.getElementById('statException').textContent = stats.exception  ?? 0;
            document.getElementById('statInReview').textContent  = stats.inReview   ?? 0;
        }
    } catch (err) {
        console.error('Failed to load stats:', err);
    }

    // ---------- fetch recent invoices ----------
    try {
        const res = await authService.fetchWithAuth('/api/invoices?top=10');
        if (res.ok) {
            const data = await res.json();
            const invoices = Array.isArray(data) ? data : (data.invoices || data.items || []);
            const tbody = document.getElementById('recentInvoicesBody');

            if (invoices.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="px-5 py-8 text-center text-neutral-400">No invoices found. Upload one to get started.</td></tr>';
                return;
            }

            tbody.innerHTML = invoices.map(inv => `
                <tr class="border-b border-neutral-50 hover:bg-neutral-50 transition-colors">
                    <td class="px-5 py-3 text-neutral-900 font-medium">${escapeHtml(inv.vendorName || inv.vendor || '--')}</td>
                    <td class="px-5 py-3 text-neutral-600">${escapeHtml(inv.invoiceNumber || inv.invoiceNo || '--')}</td>
                    <td class="px-5 py-3 text-neutral-600">${escapeHtml(inv.invoiceDate || '--')}</td>
                    <td class="px-5 py-3 text-neutral-900 font-medium">${formatCurrency(inv.totalAmount || inv.amount, inv.currency)}</td>
                    <td class="px-5 py-3">${statusPill(inv.status)}</td>
                    <td class="px-5 py-3">
                        <a href="/app/invoice-detail.html?id=${encodeURIComponent(inv.id)}"
                           class="text-primary-500 hover:text-primary-600 font-medium text-sm transition-colors">View</a>
                    </td>
                </tr>
            `).join('');
        }
    } catch (err) {
        console.error('Failed to load invoices:', err);
        document.getElementById('recentInvoicesBody').innerHTML =
            '<tr><td colspan="6" class="px-5 py-8 text-center text-red-500">Failed to load invoices.</td></tr>';
    }
})();
