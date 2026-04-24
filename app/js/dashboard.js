/**
 * Dashboard Page Logic
 * Fetches stats and recent invoices, renders them into the dashboard.
 */
(async function initDashboard() {
    await authService.waitForAppShell();

    // ---------- helpers ----------
    function escapeHtml(str) {
        if (str == null) return '';
        var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(str).replace(/[&<>"']/g, function(m) { return map[m]; });
    }

    function statusPill(status) {
        var map = {
            Received:     'bg-blue-50 text-blue-700',
            ReadyForZoho: 'bg-green-50 text-green-700',
            Exception:    'bg-red-50 text-red-700',
            InReview:     'bg-amber-50 text-amber-700',
            Corrected:    'bg-purple-50 text-purple-700'
        };
        var cls = map[status] || 'bg-neutral-100 text-neutral-600';
        return '<span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold ' + cls + '">' + escapeHtml(status) + '</span>';
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
        toast.className = 'px-4 py-3 rounded-lg border shadow-sm text-sm font-medium ' + (colors[type] || colors.info) + ' transition-opacity duration-300';
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(function() { toast.style.opacity = '0'; setTimeout(function() { toast.remove(); }, 300); }, 3000);
    }

    // ---------- fetch stats ----------
    // Backend returns: receivedCount, readyForZohoCount, exceptionCount, inReviewCount, correctedCount, totalCount, totalAmount
    try {
        var statsRes = await authService.fetchWithAuth('/api/invoice-stats');
        if (statsRes.ok) {
            var stats = await statsRes.json();
            var elReceived = document.getElementById('statReceived');
            var elReady = document.getElementById('statReady');
            var elException = document.getElementById('statException');
            var elInReview = document.getElementById('statInReview');
            if (elReceived) elReceived.textContent = stats.receivedCount || 0;
            if (elReady) elReady.textContent = stats.readyForZohoCount || 0;
            if (elException) elException.textContent = stats.exceptionCount || 0;
            if (elInReview) elInReview.textContent = stats.inReviewCount || 0;
        }
    } catch (err) {
        console.error('Failed to load stats:', err);
    }

    // ---------- fetch recent invoices ----------
    // Backend returns array of: { id, vendorLegalName, invoiceNumber, invoiceDate, invoiceCurrency, totalAmount, status, exceptionReason, poNumber, createdAt }
    try {
        var invRes = await authService.fetchWithAuth('/api/invoices');
        if (invRes.ok) {
            var data = await invRes.json();
            var invoices = Array.isArray(data) ? data : [];

            // Sort by createdAt desc, take top 10
            invoices.sort(function(a, b) {
                return new Date(b.createdAt || 0) - new Date(a.createdAt || 0);
            });
            var recent = invoices.slice(0, 10);

            var tbody = document.getElementById('recentInvoicesBody');
            if (!tbody) return;

            if (recent.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-neutral-400">No invoices found. Seed demo data from the homepage to get started.</td></tr>';
                return;
            }

            var html = '';
            for (var i = 0; i < recent.length; i++) {
                var inv = recent[i];
                html += '<tr class="border-b border-neutral-50 hover:bg-neutral-50 transition-colors">'
                    + '<td class="px-5 py-3 text-neutral-900 font-medium">' + escapeHtml(inv.vendorLegalName || '--') + '</td>'
                    + '<td class="px-5 py-3 text-neutral-600 font-mono text-sm">' + escapeHtml(inv.invoiceNumber || '--') + '</td>'
                    + '<td class="px-5 py-3 text-neutral-600">' + formatDate(inv.invoiceDate) + '</td>'
                    + '<td class="px-5 py-3 text-neutral-900 font-medium">' + formatCurrency(inv.totalAmount, inv.invoiceCurrency) + '</td>'
                    + '<td class="px-5 py-3">' + escapeHtml(inv.invoiceCurrency || '--') + '</td>'
                    + '<td class="px-5 py-3">' + statusPill(inv.status) + '</td>'
                    + '<td class="px-5 py-3">'
                    + '<a href="/app/invoice-detail.html?id=' + encodeURIComponent(inv.id) + '" '
                    + 'class="text-primary-500 hover:text-primary-600 font-medium text-sm">View</a>'
                    + '</td></tr>';
            }
            tbody.innerHTML = html;
        }
    } catch (err) {
        console.error('Failed to load invoices:', err);
        var errTbody = document.getElementById('recentInvoicesBody');
        if (errTbody) {
            errTbody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-red-500">Failed to load invoices.</td></tr>';
        }
    }
})();
