const TOKEN_KEY = "token";
const USER_ID_KEYS = ["userId", "UserId", "id", "Id"];

function decodeJwtPayload(token) {
    try {
        const [, payload] = token.split(".");
        if (!payload) return null;

        const normalizedPayload = payload
            .replace(/-/g, "+")
            .replace(/_/g, "/")
            .padEnd(Math.ceil(payload.length / 4) * 4, "=");
        const decodedPayload = atob(normalizedPayload);
        return JSON.parse(decodedPayload);
    } catch {
        return null;
    }
}

function isTokenExpired(payload) {
    if (!payload?.exp) return false;
    return payload.exp * 1000 <= Date.now();
}

export function getAuthToken() {
    return localStorage.getItem(TOKEN_KEY) || localStorage.getItem("authToken");
}

export function getAuthHeaders() {
    const token = getAuthToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
}

export function getCurrentUser() {
    const token = getAuthToken();
    if (!token) return null;

    const payload = decodeJwtPayload(token);
    if (!payload) return { token };
    if (isTokenExpired(payload)) {
        clearSession();
        return null;
    }

    const userId =
        payload.nameid ||
        payload.sub ||
        payload.Id ||
        payload.id ||
        payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];

    return {
        token,
        userId,
        email: payload.Email || payload.email,
        role: payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
    };
}

export function saveSession(token) {
    localStorage.setItem(TOKEN_KEY, token);

    const user = getCurrentUser();
    if (user?.userId) {
        localStorage.setItem("userId", user.userId);
    }

    return user;
}

export function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem("authToken");
    USER_ID_KEYS.forEach((key) => localStorage.removeItem(key));
}

export function isAuthenticated() {
    const token = getAuthToken();
    if (!token) return false;

    const payload = decodeJwtPayload(token);
    if (payload && isTokenExpired(payload)) {
        clearSession();
        return false;
    }

    return true;
}
