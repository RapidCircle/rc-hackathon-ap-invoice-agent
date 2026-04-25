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
    var allInvoices = [];
    var statusChart = null;
    var vendorChart = null;

    try {
        var invRes = await authService.fetchWithAuth('/api/invoices');
        if (invRes.ok) {
            var data = await invRes.json();
            allInvoices = Array.isArray(data) ? data : [];

            // Sort by createdAt desc, take top 10 for recent table
            var invoicesSorted = allInvoices.slice().sort(function(a, b) {
                return new Date(b.createdAt || 0) - new Date(a.createdAt || 0);
            });
            var recent = invoicesSorted.slice(0, 10);

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

            // Initialize charts after invoices are loaded with a delay to ensure DOM is ready
            setTimeout(function() {
                initCharts();
            }, 300);
        }
    } catch (err) {
        console.error('Failed to load invoices:', err);
        var errTbody = document.getElementById('recentInvoicesBody');
        if (errTbody) {
            errTbody.innerHTML = '<tr><td colspan="7" class="px-5 py-8 text-center text-red-500">Failed to load invoices.</td></tr>';
        }
    }

    // ---------- charts initialization ----------
    function initCharts() {
        console.log('Initializing charts...');

        // Wait for Chart.js to be available
        if (typeof Chart === 'undefined') {
            console.warn('Chart.js not loaded yet, retrying...');
            setTimeout(initCharts, 500);
            return;
        }

        if (!allInvoices || allInvoices.length === 0) {
            console.warn('No invoices available for charts');
            return;
        }

        // Calculate status distribution
        var statusCounts = {
            'Received': 0,
            'ReadyForZoho': 0,
            'Exception': 0,
            'InReview': 0,
            'Corrected': 0
        };

        allInvoices.forEach(function(inv) {
            var status = inv.status || 'Received';
            if (statusCounts[status] !== undefined) {
                statusCounts[status]++;
            }
        });

        console.log('Status distribution:', statusCounts);

        // ============ DONUT CHART ============
        try {
            var statusCanvas = document.getElementById('statusChart');
            
            if (!statusCanvas) {
                console.error('Canvas element with id="statusChart" not found!');
                console.log('Available canvas elements:', document.querySelectorAll('canvas'));
                return;
            }

            // Destroy existing chart
            if (statusChart) {
                statusChart.destroy();
                statusChart = null;
            }

            // Ensure canvas is visible and has proper size
            statusCanvas.style.display = 'block';
            statusCanvas.width = statusCanvas.offsetWidth;
            statusCanvas.height = statusCanvas.offsetHeight;

            var ctx = statusCanvas.getContext('2d');

            statusChart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['Received', 'Ready for Zoho', 'Exception', 'In Review', 'Corrected'],
                    datasets: [{
                        label: 'Invoice Count',
                        data: [
                            statusCounts['Received'],
                            statusCounts['ReadyForZoho'],
                            statusCounts['Exception'],
                            statusCounts['InReview'],
                            statusCounts['Corrected']
                        ],
                        backgroundColor: [
                            'rgba(59, 130, 246, 0.8)',      // Blue - Received
                            'rgba(16, 185, 129, 0.8)',      // Green - Ready
                            'rgba(239, 68, 68, 0.8)',       // Red - Exception
                            'rgba(245, 158, 11, 0.8)',      // Orange - In Review
                            'rgba(168, 85, 247, 0.8)'       // Purple - Corrected
                        ],
                        borderColor: [
                            'rgb(59, 130, 246)',
                            'rgb(16, 185, 129)',
                            'rgb(239, 68, 68)',
                            'rgb(245, 158, 11)',
                            'rgb(168, 85, 247)'
                        ],
                        borderWidth: 2,
                        hoverOffset: 10
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: {
                        intersect: false,
                        mode: 'index'
                    },
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                padding: 15,
                                usePointStyle: true,
                                font: {
                                    size: 12,
                                    weight: '500'
                                },
                                color: '#404040',
                                generateLabels: function(chart) {
                                    var data = chart.data;
                                    return data.labels.map(function(label, i) {
                                        var dataset = data.datasets[0];
                                        var value = dataset.data[i];
                                        return {
                                            text: label + ' (' + value + ')',
                                            fillStyle: dataset.backgroundColor[i],
                                            hidden: false,
                                            index: i
                                        };
                                    });
                                }
                            }
                        },
                        tooltip: {
                            enabled: true,
                            backgroundColor: 'rgba(0, 0, 0, 0.9)',
                            padding: 12,
                            titleFont: { size: 14, weight: 'bold' },
                            bodyFont: { size: 13 },
                            borderColor: 'rgba(255, 255, 255, 0.2)',
                            borderWidth: 1,
                            callbacks: {
                                label: function(context) {
                                    return context.label + ': ' + context.parsed + ' invoices';
                                }
                            }
                        }
                    },
                    onClick: function(event, activeElements) {
                        if (activeElements.length > 0) {
                            var index = activeElements[0].index;
                            var statuses = ['Received', 'ReadyForZoho', 'Exception', 'InReview', 'Corrected'];
                            if (statuses[index]) {
                                showVendorStatusDetails(statuses[index]);
                            }
                        }
                    }
                }
            });

            console.log('✓ Donut chart created successfully');
        } catch (e) {
            console.error('✗ Error creating donut chart:', e);
        }

        // ============ VENDOR BAR CHART ============
        try {
            // Calculate vendor invoice counts
            var vendorCounts = {};
            var vendorAmounts = {};

            allInvoices.forEach(function(inv) {
                var vendor = inv.vendorLegalName || 'Unknown';
                vendorCounts[vendor] = (vendorCounts[vendor] || 0) + 1;
                vendorAmounts[vendor] = (vendorAmounts[vendor] || 0) + (inv.totalAmount || 0);
            });

            var sortedVendors = Object.keys(vendorCounts).sort(function(a, b) {
                return vendorCounts[b] - vendorCounts[a];
            });

            var vendorCountsArray = sortedVendors.map(function(v) { return vendorCounts[v]; });

            console.log('Vendor distribution:', sortedVendors, vendorCountsArray);

            var vendorCanvas = document.getElementById('vendorChart');
            
            if (!vendorCanvas) {
                console.error('Canvas element with id="vendorChart" not found!');
                return;
            }

            // Destroy existing chart
            if (vendorChart) {
                vendorChart.destroy();
                vendorChart = null;
            }

            // Ensure canvas is visible and has proper size
            vendorCanvas.style.display = 'block';
            vendorCanvas.width = vendorCanvas.offsetWidth;
            vendorCanvas.height = vendorCanvas.offsetHeight;

            var vendorCtx = vendorCanvas.getContext('2d');

            vendorChart = new Chart(vendorCtx, {
                type: 'bar',
                data: {
                    labels: sortedVendors,
                    datasets: [{
                        label: 'Invoice Count',
                        data: vendorCountsArray,
                        backgroundColor: 'rgba(0, 87, 184, 0.8)',
                        borderColor: 'rgb(0, 87, 184)',
                        borderWidth: 1,
                        borderRadius: 4,
                        hoverBackgroundColor: 'rgba(0, 74, 158, 0.9)'
                    }]
                },
                options: {
                    indexAxis: 'y',
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: {
                        intersect: false,
                        mode: 'index'
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top',
                            labels: {
                                padding: 15,
                                font: {
                                    size: 12,
                                    weight: '500'
                                },
                                color: '#404040'
                            }
                        },
                        tooltip: {
                            enabled: true,
                            backgroundColor: 'rgba(0, 0, 0, 0.9)',
                            padding: 12,
                            titleFont: { size: 14, weight: 'bold' },
                            bodyFont: { size: 13 },
                            borderColor: 'rgba(255, 255, 255, 0.2)',
                            borderWidth: 1
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            ticks: {
                                stepSize: 1
                            },
                            grid: {
                                color: 'rgba(0, 0, 0, 0.05)'
                            }
                        },
                        y: {
                            grid: {
                                display: false
                            }
                        }
                    }
                }
            });

            console.log('✓ Vendor bar chart created successfully');
        } catch (e) {
            console.error('✗ Error creating vendor chart:', e);
        }
    }

    // ---------- show vendor details for selected status ----------
    function showVendorStatusDetails(status) {
        var statusLabels = {
            'Received': 'Received',
            'ReadyForZoho': 'Ready for Zoho',
            'Exception': 'Exception',
            'InReview': 'In Review',
            'Corrected': 'Corrected'
        };

        var filteredInvoices = allInvoices.filter(function(inv) {
            return (inv.status || 'Received') === status;
        });

        // Group by vendor
        var vendorData = {};
        filteredInvoices.forEach(function(inv) {
            var vendor = inv.vendorLegalName || 'Unknown';
            if (!vendorData[vendor]) {
                vendorData[vendor] = { count: 0, amount: 0 };
            }
            vendorData[vendor].count++;
            vendorData[vendor].amount += inv.totalAmount || 0;
        });

        // Sort by count (descending)
        var sortedVendorsInStatus = Object.keys(vendorData).sort(function(a, b) {
            return vendorData[b].count - vendorData[a].count;
        });

        var tbody = document.getElementById('vendorStatusBody');
        var title = document.getElementById('vendorStatusTitle');
        var container = document.getElementById('vendorStatusContainer');

        title.textContent = statusLabels[status] + ' - Vendor Details';

        var html = '';
        for (var i = 0; i < sortedVendorsInStatus.length; i++) {
            var vendor = sortedVendorsInStatus[i];
            var data = vendorData[vendor];
            html += '<tr class="border-b border-neutral-50 hover:bg-neutral-50 transition-colors">'
                + '<td class="px-5 py-3 text-neutral-900 font-medium">' + escapeHtml(vendor) + '</td>'
                + '<td class="px-5 py-3 text-neutral-600">' + data.count + '</td>'
                + '<td class="px-5 py-3 text-neutral-900 font-medium">' + formatCurrency(data.amount, 'INR') + '</td>'
                + '</tr>';
        }

        tbody.innerHTML = html;
        container.classList.remove('hidden');
        
        // Scroll to the details section
        setTimeout(function() {
            container.scrollIntoView({ behavior: 'smooth' });
        }, 100);
    }
})();
