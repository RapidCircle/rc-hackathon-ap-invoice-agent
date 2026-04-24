/**
 * Authentication Service
 * Provides a unified interface for authentication that works with both
 * mock auth (demo) and SWA auth (production) modes.
 * 
 * @module authService
 */

/**
 * @typedef {Object} AuthMode
 * @property {string} mode - The auth mode: 'mock' or 'swa'
 * @property {string} loginUrl - The URL to redirect to for login
 * @property {string} logoutUrl - The URL to redirect to for logout
 */

/**
 * @typedef {Object} UserInfo
 * @property {string} identityProvider - The identity provider
 * @property {string} userId - The user's unique ID
 * @property {string} userDetails - The user's display details
 * @property {string[]} userRoles - The user's roles
 * @property {string} fullName - The user's full name
 * @property {string} email - The user's email
 * @property {boolean} isSystemAdmin - Whether the user is an admin
 * @property {string} personaDescription - Description of the user's persona
 */

/**
 * @typedef {Object} AuthState
 * @property {boolean} isAuthenticated - Whether the user is authenticated
 * @property {string} authMode - The current auth mode
 * @property {UserInfo|null} user - The current user info
 */

const authService = {
    /** @type {AuthMode|null} */
    _authMode: null,

    /** @type {string|null} */
    _sessionToken: null,

    /**
     * Initialize the auth service by detecting the auth mode.
     * Call this before using other methods.
     * @returns {Promise<AuthMode>}
     */
    async initialize() {
        if (this._authMode) {
            return this._authMode;
        }

        try {
            const response = await fetch('/api/auth/mode');
            if (!response.ok) {
                // Fallback to SWA mode if API not available
                this._authMode = {
                    mode: 'swa',
                    loginUrl: '/.auth/login/aad',
                    logoutUrl: '/.auth/logout'
                };
            } else {
                this._authMode = await response.json();
            }
        } catch {
            // Fallback to SWA mode on error
            this._authMode = {
                mode: 'swa',
                loginUrl: '/.auth/login/aad',
                logoutUrl: '/.auth/logout'
            };
        }

        // Load session token from localStorage if in mock mode
        if (this._authMode.mode === 'mock') {
            this._sessionToken = localStorage.getItem('mock_session_token');
        }

        return this._authMode;
    },

    /**
     * Get the current auth mode.
     * @returns {AuthMode|null}
     */
    getAuthMode() {
        return this._authMode;
    },

    /**
     * Check if using mock auth mode.
     * @returns {boolean}
     */
    isMockAuth() {
        return this._authMode?.mode === 'mock';
    },

    /**
     * Get the current authenticated user.
     * @returns {Promise<AuthState>}
     */
    async getCurrentUser() {
        await this.initialize();

        if (this._authMode.mode === 'mock') {
            return this._getMockUser();
        } else {
            return this._getSwaUser();
        }
    },

    /**
     * Get user info from mock auth API.
     * @private
     * @returns {Promise<AuthState>}
     */
    async _getMockUser() {
        try {
            const headers = {};
            if (this._sessionToken) {
                headers['X-Session-Token'] = this._sessionToken;
            }

            const response = await fetch('/api/auth/me', { headers });
            if (!response.ok) {
                return { isAuthenticated: false, authMode: 'mock', user: null };
            }

            const data = await response.json();
            return {
                isAuthenticated: data.isAuthenticated,
                authMode: data.authMode,
                user: data.user
            };
        } catch {
            return { isAuthenticated: false, authMode: 'mock', user: null };
        }
    },

    /**
     * Get user info from SWA /.auth/me endpoint.
     * @private
     * @returns {Promise<AuthState>}
     */
    async _getSwaUser() {
        try {
            const response = await fetch('/.auth/me');
            if (!response.ok) {
                return { isAuthenticated: false, authMode: 'swa', user: null };
            }

            const data = await response.json();
            const clientPrincipal = data.clientPrincipal;

            if (!clientPrincipal) {
                return { isAuthenticated: false, authMode: 'swa', user: null };
            }

            // Convert SWA clientPrincipal to UserInfo format
            return {
                isAuthenticated: true,
                authMode: 'swa',
                user: {
                    identityProvider: clientPrincipal.identityProvider,
                    userId: clientPrincipal.userId,
                    userDetails: clientPrincipal.userDetails,
                    userRoles: clientPrincipal.userRoles,
                    fullName: clientPrincipal.userDetails,
                    email: clientPrincipal.userDetails,
                    isSystemAdmin: false,
                    personaDescription: ''
                }
            };
        } catch {
            return { isAuthenticated: false, authMode: 'swa', user: null };
        }
    },

    /**
     * Initiate login with email (mock auth only).
     * @param {string} email - The user's email
     * @returns {Promise<{success: boolean, requiresMfa?: boolean, sessionToken?: string, error?: string}>}
     */
    async login(email) {
        if (!this.isMockAuth()) {
            // For SWA, redirect to login
            window.location.href = this._authMode.loginUrl;
            return { success: false, error: 'Redirecting to login...' };
        }

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });

            const data = await response.json();

            if (!response.ok) {
                return { success: false, error: data.error || 'Login failed' };
            }

            // Store temp session token for MFA
            this._sessionToken = data.sessionToken;

            return {
                success: true,
                requiresMfa: data.requiresMfa,
                sessionToken: data.sessionToken
            };
        } catch (error) {
            return { success: false, error: 'Network error during login' };
        }
    },

    /**
     * Verify MFA code (mock auth only).
     * @param {string} code - The MFA code (use 123456)
     * @returns {Promise<{success: boolean, user?: UserInfo, error?: string}>}
     */
    async verifyMfa(code) {
        if (!this.isMockAuth()) {
            return { success: false, error: 'MFA not available in SWA mode' };
        }

        try {
            const response = await fetch('/api/auth/mfa', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    sessionToken: this._sessionToken,
                    code
                })
            });

            const data = await response.json();

            if (!response.ok) {
                return { success: false, error: data.error || 'MFA verification failed' };
            }

            // Store the session token
            this._sessionToken = data.sessionToken;
            localStorage.setItem('mock_session_token', data.sessionToken);

            return {
                success: true,
                user: data.user
            };
        } catch (error) {
            return { success: false, error: 'Network error during MFA' };
        }
    },

    /**
     * Log out the current user.
     * @returns {Promise<void>}
     */
    async logout() {
        if (this.isMockAuth()) {
            try {
                const headers = {};
                if (this._sessionToken) {
                    headers['X-Session-Token'] = this._sessionToken;
                }

                await fetch('/api/auth/logout', {
                    method: 'POST',
                    headers
                });
            } catch {
                // Ignore errors during logout
            }

            // Clear local storage
            localStorage.removeItem('mock_session_token');
            this._sessionToken = null;

            // Redirect to login
            window.location.href = '/login.html';
        } else {
            // For SWA, redirect to logout
            window.location.href = this._authMode.logoutUrl;
        }
    },

    /**
     * Get available demo personas (mock auth only).
     * @returns {Promise<Array<{email: string, fullName: string, description: string, isAdmin: boolean}>>}
     */
    async getPersonas() {
        if (!this.isMockAuth()) {
            return [];
        }

        try {
            const response = await fetch('/api/auth/personas');
            if (!response.ok) {
                return [];
            }

            const data = await response.json();
            return data.personas || [];
        } catch {
            return [];
        }
    },

    /**
     * Seed demo data (mock auth only, development only).
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    async seedDemoData() {
        try {
            const response = await fetch('/api/demo/seed', { method: 'POST' });
            const data = await response.json();

            if (!response.ok) {
                return { success: false, error: data.error || 'Seed failed' };
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: 'Network error during seed' };
        }
    },

    /**
     * Reset demo data (mock auth only, development only).
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    async resetDemoData() {
        try {
            const response = await fetch('/api/demo/reset', { method: 'POST' });
            const data = await response.json();

            if (!response.ok) {
                return { success: false, error: data.error || 'Reset failed' };
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: 'Network error during reset' };
        }
    },

    /**
     * Get headers to include in API requests.
     * @returns {Object} Headers object with session token if in mock mode
     */
    getAuthHeaders() {
        const headers = {};
        if (this.isMockAuth() && this._sessionToken) {
            headers['X-Session-Token'] = this._sessionToken;
        }
        return headers;
    },

    /**
     * Make an authenticated fetch request, automatically attaching the session
     * token header in mock auth mode. Use this instead of fetch() for all
     * protected API calls.
     *
     * @param {string} url - The URL to fetch
     * @param {RequestInit} [options={}] - Standard fetch options
     * @returns {Promise<Response>}
     *
     * @example
     * const res = await authService.fetchWithAuth('/api/my-data');
     * const data = await res.json();
     */
    async fetchWithAuth(url, options = {}) {
        await this.initialize();
        const headers = { ...this.getAuthHeaders(), ...(options.headers || {}) };
        return fetch(url, { ...options, headers });
    },

    /**
     * Wait for the app shell to finish authenticating before running page logic.
     * Resolves immediately if the shell is already ready.
     * Use this at the top of every page-level init function that calls an API.
     *
     * @returns {Promise<void>}
     *
     * @example
     * async function init() {
     *     await authService.waitForAppShell();
     *     const res = await authService.fetchWithAuth('/api/my-data');
     * }
     */
    waitForAppShell() {
        if (window.appShellReady) return Promise.resolve();
        return new Promise(resolve =>
            window.addEventListener('app-shell-ready', () => resolve(), { once: true })
        );
    },

    /**
     * Redirect to login if not authenticated.
     * @returns {Promise<AuthState>}
     */
    async requireAuth() {
        const state = await this.getCurrentUser();
        
        if (!state.isAuthenticated) {
            await this.initialize();
            window.location.href = this._authMode.loginUrl;
        }
        
        return state;
    }
};

// Export for ES modules, also make available globally
if (typeof module !== 'undefined' && module.exports) {
    module.exports = authService;
}
