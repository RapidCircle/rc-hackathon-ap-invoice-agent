/**
 * App Shell
 * Checks auth, populates user info in the nav bar, wires logout.
 * Dispatches a 'app-shell-ready' event when done so page scripts can start.
 */
(async function initAppShell() {
    await authService.initialize();
    const state = await authService.getCurrentUser();

    if (!state.isAuthenticated) {
        window.location.href = authService.getAuthMode()?.loginUrl || '/login.html';
        return;
    }

    // Populate user name in the nav
    const userNameEl = document.getElementById('userName');
    if (userNameEl) {
        userNameEl.textContent = state.user.fullName || state.user.userDetails || 'User';
    }

    // Wire logout button
    const logoutBtn = document.getElementById('btnLogout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            await authService.logout();
        });
    }

    // Signal ready
    window.appShellReady = true;
    window.dispatchEvent(new Event('app-shell-ready'));
})();
