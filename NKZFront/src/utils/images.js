const API_BASE_URL = import.meta.env.VITE_API_URL || "";

export function resolveAssetUrl(url) {
    if (!url) return "";
    if (/^https?:\/\//i.test(url)) return url;
    return `${API_BASE_URL}/${url}`.replace(/([^:]\/)\/+/g, "$1");
}

export function getPlayerImageUrl(player) {
    return resolveAssetUrl(
        player?.profileImageUrl ??
        player?.ProfileImageUrl ??
        player?.profileIconUrl ??
        player?.ProfileIconUrl ??
        ""
    );
}
