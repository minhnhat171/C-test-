(function () {
    const payloadCachePrefix = "vinhkhanh.qr.payload.v1:";
    const assetCacheName = "vinhkhanh.qr.assets.v1";

    function normalizeTargetType(value) {
        const normalized = String(value || "").trim().toLowerCase();
        return normalized === "tour" ? "tour" : "poi";
    }

    function buildQrPayloadCacheKey(context) {
        const targetType = normalizeTargetType(context && context.targetType);
        const targetId = String(context && context.targetId || "").trim();
        return `${payloadCachePrefix}${targetType}:${targetId}`;
    }

    function resolveQrDeviceProfile() {
        const params = new URLSearchParams(window.location.search);
        const forced = String(params.get("deviceProfile") || "").trim().toLowerCase();

        if (forced === "strong") {
            return { value: 0, label: "Mạnh", mode: "full-offline", forced: true };
        }

        if (forced === "weak") {
            return { value: 1, label: "Yếu", mode: "minimal", forced: true };
        }

        const profile = Math.random() < 0.5 ? 0 : 1;

        if (profile === 0) {
            return { value: 0, label: "Mạnh", mode: "full-offline", forced: false };
        }

        return { value: 1, label: "Yếu", mode: "minimal", forced: false };
    }

    function readQrPayloadFromCache(key) {
        if (!key) {
            return null;
        }

        try {
            const raw = window.localStorage.getItem(key);
            return raw ? JSON.parse(raw) : null;
        } catch (error) {
            console.warn("[QR Device Profile] cannot read offline payload cache", error);
            return null;
        }
    }

    function writeQrPayloadToCache(key, data) {
        if (!key) {
            return false;
        }

        try {
            window.localStorage.setItem(key, JSON.stringify(data));
            return true;
        } catch (error) {
            console.warn("[QR Device Profile] cannot write offline payload cache", error);
            return false;
        }
    }

    function toAbsoluteUrl(value) {
        const trimmed = String(value || "").trim();
        if (!trimmed) {
            return "";
        }

        try {
            return new URL(trimmed, window.location.href).toString();
        } catch (error) {
            return "";
        }
    }

    function collectOfflineAssetUrls(context) {
        const urls = new Set();
        const addUrl = (value) => {
            const absoluteUrl = toAbsoluteUrl(value);
            if (absoluteUrl) {
                urls.add(absoluteUrl);
            }
        };

        addUrl(context && context.audioAssetPath);

        const relatedPois = Array.isArray(context && context.relatedPois)
            ? context.relatedPois
            : [];

        relatedPois.forEach((poi) => {
            addUrl(poi && poi.audioAssetPath);
        });

        return Array.from(urls);
    }

    async function cacheOfflineAssets(urls) {
        if (!("caches" in window) || !Array.isArray(urls) || urls.length === 0) {
            return { total: Array.isArray(urls) ? urls.length : 0, cached: 0 };
        }

        let cached = 0;
        const cache = await window.caches.open(assetCacheName);

        for (const url of urls) {
            try {
                const request = new Request(url, { mode: "no-cors" });
                const response = await fetch(request);
                if (!response.ok && response.type !== "opaque") {
                    throw new Error(`Asset fetch returned ${response.status}`);
                }

                await cache.put(request, response.clone());
                cached += 1;
            } catch (error) {
                console.warn("[QR Device Profile] cannot pre-cache asset", url, error);
            }
        }

        return { total: urls.length, cached };
    }

    async function cacheQrPayloadForOffline(context) {
        const safeContext = context || {};
        const key = buildQrPayloadCacheKey(safeContext);
        const relatedPois = Array.isArray(safeContext.relatedPois)
            ? safeContext.relatedPois
            : [];
        const payload = {
            version: 1,
            cachedAtUtc: new Date().toISOString(),
            targetType: normalizeTargetType(safeContext.targetType),
            targetId: String(safeContext.targetId || ""),
            targetName: String(safeContext.targetName || ""),
            deviceProfile: safeContext.deviceProfile || null,
            data: safeContext
        };
        const payloadCached = writeQrPayloadToCache(key, payload);
        const assetUrls = collectOfflineAssetUrls(safeContext);
        const assetResult = await cacheOfflineAssets(assetUrls);

        return {
            key,
            payloadCached,
            relatedPoiCount: relatedPois.length,
            assetTotal: assetResult.total,
            assetCached: assetResult.cached
        };
    }

    function setText(id, text) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = text;
        }
    }

    function setProfileState(profile, message) {
        const root = document.getElementById("qrDeviceProfilePanel");
        if (root) {
            root.dataset.profileValue = String(profile.value);
            root.dataset.profileMode = profile.mode;
        }

        const title = profile.value === 0
            ? "Cấu hình thiết bị: Mạnh - chế độ offline đầy đủ"
            : "Cấu hình thiết bị: Yếu - chế độ tải tối thiểu";

        setText("qrDeviceProfileLabel", title);
        setText("qrDeviceProfileMode", profile.forced
            ? `Đang test bằng query deviceProfile=${profile.value === 0 ? "strong" : "weak"}.`
            : "Đang chạy mô phỏng random 0/1 theo yêu cầu.");
        setText("qrDeviceProfileStatus", message || "");
    }

    function applyNetworkNotice(context) {
        const cachedPayload = readQrPayloadFromCache(buildQrPayloadCacheKey(context || {}));
        const message = navigator.onLine
            ? "Mạng đang khả dụng, trang tiếp tục dùng dữ liệu hiện tại."
            : cachedPayload
                ? "Thiết bị đang offline. Đã tìm thấy payload QR trong cache cục bộ."
                : "Thiết bị đang offline. Trang chỉ dùng dữ liệu đang hiển thị, không gọi thêm dữ liệu nền.";

        setText("qrDeviceProfileNetwork", message);
    }

    async function applyStrongDeviceMode(context) {
        const profile = (context && context.deviceProfile) ||
            { value: 0, label: "Mạnh", mode: "full-offline", forced: false };

        setProfileState(profile, "Đang cache dữ liệu QR để ưu tiên dùng offline khi mạng yếu.");

        try {
            const result = await cacheQrPayloadForOffline(context || {});
            const poiLabel = result.relatedPoiCount > 0
                ? `${result.relatedPoiCount} POI liên quan`
                : "payload QR hiện tại";
            const assetLabel = result.assetTotal > 0
                ? `, ${result.assetCached}/${result.assetTotal} asset audio đã cache`
                : ", không có asset audio cần cache";

            setProfileState(
                profile,
                `Đã cache ${poiLabel}${assetLabel}. Khi mạng yếu, trang có thể đọc lại payload từ cache.`);
            applyNetworkNotice(context);

            return result;
        } catch (error) {
            console.warn("[QR Device Profile] strong mode cache failed", error);
            setProfileState(
                profile,
                "Không cache được đầy đủ, nhưng trang vẫn giữ nội dung hiện tại và không bị dừng.");
            applyNetworkNotice(context);
            return { error };
        }
    }

    function applyWeakDeviceMode(context) {
        const profile = (context && context.deviceProfile) ||
            { value: 1, label: "Yếu", mode: "minimal", forced: false };
        const cachedPayload = readQrPayloadFromCache(buildQrPayloadCacheKey(context || {}));
        const offlineSuffix = !navigator.onLine
            ? (cachedPayload
                ? " Đang offline, có thể đối chiếu payload đã cache trước đó."
                : " Đang offline, chỉ hiển thị dữ liệu hiện có trên trang.")
            : "";

        setProfileState(
            profile,
            `Không prefetch toàn bộ POI/audio. Chỉ dùng POI/Tour đang mở và fallback về text/TTS nếu thiếu audio.${offlineSuffix}`);
        applyNetworkNotice(context);

        return {
            cacheHit: Boolean(cachedPayload)
        };
    }

    function buildProfileUrl(profileName) {
        const url = new URL(window.location.href);

        if (profileName) {
            url.searchParams.set("deviceProfile", profileName);
        } else {
            url.searchParams.delete("deviceProfile");
        }

        return url.toString();
    }

    function wireProfileLinks() {
        document.querySelectorAll("[data-device-profile-link]").forEach((link) => {
            const profileName = link.getAttribute("data-device-profile-link");
            link.setAttribute("href", buildProfileUrl(profileName === "random" ? "random" : profileName));
        });
    }

    function normalizeContext(context) {
        const safeContext = context || {};
        return {
            targetType: normalizeTargetType(safeContext.targetType),
            targetId: String(safeContext.targetId || ""),
            targetCode: String(safeContext.targetCode || ""),
            targetName: String(safeContext.targetName || ""),
            targetKindLabel: String(safeContext.targetKindLabel || ""),
            description: String(safeContext.description || ""),
            narrationText: String(safeContext.narrationText || ""),
            audioAssetPath: String(safeContext.audioAssetPath || ""),
            narrationByLanguage: safeContext.narrationByLanguage || {},
            mapLink: String(safeContext.mapLink || ""),
            specialDish: String(safeContext.specialDish || ""),
            publicUrl: String(safeContext.publicUrl || ""),
            appLaunchUrl: String(safeContext.appLaunchUrl || ""),
            estimatedDurationLabel: String(safeContext.estimatedDurationLabel || ""),
            tourStops: Array.isArray(safeContext.tourStops) ? safeContext.tourStops : [],
            relatedPois: Array.isArray(safeContext.relatedPois) ? safeContext.relatedPois : [],
            generatedAtUtc: safeContext.generatedAtUtc || new Date().toISOString()
        };
    }

    function initialize(context) {
        wireProfileLinks();

        const normalizedContext = normalizeContext(context);
        const profile = resolveQrDeviceProfile();
        normalizedContext.deviceProfile = profile;

        if (profile.value === 0) {
            console.log("[QR Device Profile] profile=0 strong device");
            void applyStrongDeviceMode(normalizedContext);
        } else {
            console.log("[QR Device Profile] profile=1 weak device");
            applyWeakDeviceMode(normalizedContext);
        }

        window.addEventListener("online", () => applyNetworkNotice(normalizedContext));
        window.addEventListener("offline", () => applyNetworkNotice(normalizedContext));
        applyNetworkNotice(normalizedContext);

        return {
            profile,
            cacheKey: buildQrPayloadCacheKey(normalizedContext)
        };
    }

    window.VinhKhanhQrDeviceProfile = {
        resolveQrDeviceProfile,
        applyStrongDeviceMode,
        applyWeakDeviceMode,
        cacheQrPayloadForOffline,
        readQrPayloadFromCache,
        writeQrPayloadToCache,
        initialize
    };
})();
