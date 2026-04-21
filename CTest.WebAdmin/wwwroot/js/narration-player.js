(function () {
    const localeMap = {
        vi: ["vi-VN"],
        en: ["en-US", "en-GB", "en-AU", "en-CA"],
        zh: ["zh-CN", "zh-TW", "zh-HK"],
        ko: ["ko-KR"],
        fr: ["fr-FR", "fr-CA"]
    };

    const femaleHints = ["female", "woman", "zira", "aria", "samantha", "hazel", "susan", "linda", "anna", "nu"];
    const maleHints = ["male", "man", "david", "guy", "ryan", "tom", "alex", "john", "nam"];

    function normalizeLanguageCode(languageCode) {
        const normalized = (languageCode || "").trim().toLowerCase();

        if (normalized.startsWith("en")) {
            return "en";
        }

        if (normalized.startsWith("zh")) {
            return "zh";
        }

        if (normalized.startsWith("ko")) {
            return "ko";
        }

        if (normalized.startsWith("fr")) {
            return "fr";
        }

        return "vi";
    }

    function normalizePlaybackMode(playbackMode) {
        return String(playbackMode || "").trim().toLowerCase() === "audio"
            ? "audio"
            : "tts";
    }

    function normalizeVoiceType(voiceType) {
        const normalized = (voiceType || "").trim().toLowerCase();

        if (normalized === "male") {
            return "male";
        }

        if (normalized === "neutral") {
            return "neutral";
        }

        return "female";
    }

    function getPreferredLocaleCodes(languageCode) {
        return localeMap[normalizeLanguageCode(languageCode)] || localeMap.vi;
    }

    function waitForVoices(timeoutMs) {
        if (!("speechSynthesis" in window)) {
            return Promise.resolve([]);
        }

        const existingVoices = window.speechSynthesis.getVoices();
        if (existingVoices.length > 0) {
            return Promise.resolve(existingVoices);
        }

        return new Promise((resolve) => {
            let finished = false;

            const finish = () => {
                if (finished) {
                    return;
                }

                finished = true;
                window.speechSynthesis.removeEventListener("voiceschanged", onVoicesChanged);
                window.clearTimeout(timer);
                resolve(window.speechSynthesis.getVoices());
            };

            const onVoicesChanged = () => finish();
            const timer = window.setTimeout(finish, timeoutMs || 800);

            window.speechSynthesis.addEventListener("voiceschanged", onVoicesChanged);
        });
    }

    function matchesVoiceHint(voiceName, voiceType) {
        const normalizedName = String(voiceName || "").trim().toLowerCase();
        if (!normalizedName || voiceType === "neutral") {
            return true;
        }

        const hints = voiceType === "male" ? maleHints : femaleHints;
        return hints.some((hint) => normalizedName.includes(hint));
    }

    function voiceMatchesLocale(voice, preferredLocaleCodes) {
        const voiceLanguage = String(voice && voice.lang || "").trim().toLowerCase();
        if (!voiceLanguage) {
            return false;
        }

        return preferredLocaleCodes.some((localeCode) => {
            const normalizedLocale = localeCode.toLowerCase();
            return voiceLanguage === normalizedLocale ||
                voiceLanguage.startsWith(normalizedLocale + "-") ||
                normalizedLocale.startsWith(voiceLanguage + "-") ||
                voiceLanguage.startsWith(normalizedLocale.slice(0, 2));
        });
    }

    function pickVoice(candidates, voiceType) {
        if (!Array.isArray(candidates) || candidates.length === 0) {
            return null;
        }

        const normalizedVoiceType = normalizeVoiceType(voiceType);
        const hintedVoice = candidates.find((voice) => matchesVoiceHint(voice.name, normalizedVoiceType));
        return hintedVoice || candidates[0];
    }

    function selectVoice(voices, languageCode, voiceType) {
        const preferredLocaleCodes = getPreferredLocaleCodes(languageCode);
        const localeCandidates = (voices || []).filter((voice) => voiceMatchesLocale(voice, preferredLocaleCodes));
        const voiceMatch = pickVoice(localeCandidates, voiceType);

        if (voiceMatch) {
            return voiceMatch;
        }

        return pickVoice(voices || [], voiceType);
    }

    function resolveAudioUrl(audioAssetPath, baseUrl) {
        const trimmed = String(audioAssetPath || "").trim();
        if (!trimmed) {
            return "";
        }

        if (/^[a-zA-Z]:\\/.test(trimmed) || trimmed.startsWith("\\\\")) {
            return "";
        }

        try {
            return new URL(trimmed, baseUrl || window.location.href).toString();
        } catch (error) {
            return "";
        }
    }

    function resolvePlaybackRequest(preferredPlaybackMode, audioAssetPath, allowAudioFallback) {
        const normalizedPlaybackMode = normalizePlaybackMode(preferredPlaybackMode);
        if (normalizedPlaybackMode !== "audio") {
            return {
                mode: "tts",
                audioAssetPath: "",
                allowAudioFallback: allowAudioFallback === true,
                fallbackMessage: ""
            };
        }

        const trimmedAudioPath = String(audioAssetPath || "").trim();
        if (trimmedAudioPath) {
            return {
                mode: "audio",
                audioAssetPath: trimmedAudioPath,
                allowAudioFallback: allowAudioFallback === true,
                fallbackMessage: ""
            };
        }

        return allowAudioFallback === true
            ? {
                mode: "tts",
                audioAssetPath: "",
                allowAudioFallback: true,
                fallbackMessage: "Quán này chưa có bản thu sẵn, hệ thống sẽ chuyển sang TTS."
            }
            : {
                mode: "audio",
                audioAssetPath: "",
                allowAudioFallback: false,
                fallbackMessage: "Quán này chưa có bản thu sẵn để phát trên web."
            };
    }

    function createPlayer(options) {
        const settings = options || {};
        const onStatus = typeof settings.onStatus === "function"
            ? settings.onStatus
            : function () { };

        let activeAudio = null;
        let playbackToken = 0;

        function stop(message) {
            playbackToken += 1;

            if (activeAudio) {
                try {
                    activeAudio.pause();
                    activeAudio.removeAttribute("src");
                    activeAudio.load();
                } catch (error) {
                }

                activeAudio = null;
            }

            if ("speechSynthesis" in window) {
                window.speechSynthesis.cancel();
            }

            if (message) {
                onStatus(message, "stopped");
            }
        }

        async function play(request) {
            const playbackRequest = resolvePlaybackRequest(
                request.playbackMode,
                request.audioAssetPath,
                request.allowAudioFallback === true);

            const narrationText = String(request.narrationText || "").trim();
            const languageCode = normalizeLanguageCode(request.languageCode);
            const preferredLocale = getPreferredLocaleCodes(languageCode)[0];
            const messages = request.messages || {};
            const currentToken = playbackToken + 1;

            stop();
            playbackToken = currentToken;

            async function speakTts() {
                if (!narrationText) {
            onStatus(messages.empty || "Chưa có nội dung để phát.", "empty");
                    return { mode: "none", usedFallback: false };
                }

                if (!("speechSynthesis" in window)) {
                    onStatus(messages.unsupportedTts || "Trình duyệt này không hỗ trợ TTS.", "error");
                    return { mode: "none", usedFallback: false };
                }

                const voices = await waitForVoices(900);
                if (currentToken !== playbackToken) {
                    return { mode: "tts", cancelled: true, usedFallback: false };
                }

                const utterance = new SpeechSynthesisUtterance(narrationText);
                utterance.lang = preferredLocale;

                const matchedVoice = selectVoice(voices, languageCode, request.voiceType);
                if (matchedVoice) {
                    utterance.voice = matchedVoice;
                    if (matchedVoice.lang) {
                        utterance.lang = matchedVoice.lang;
                    }
                }

                utterance.rate = typeof request.rate === "number" ? request.rate : 1;
                utterance.pitch = 1;
                utterance.volume = 1;
                utterance.onstart = function () {
                    if (currentToken === playbackToken) {
                        onStatus(messages.playingTts || "Đang đọc thuyết minh...", "playing");
                    }
                };
                utterance.onend = function () {
                    if (currentToken === playbackToken) {
                        onStatus(messages.ttsEnded || "Đã phát xong.", "ended");
                    }
                };
                utterance.onerror = function () {
                    if (currentToken === playbackToken) {
                        onStatus(messages.ttsFailed || "Không thể phát TTS. Thử bấm lại một lần nữa.", "error");
                    }
                };

                window.speechSynthesis.cancel();
                window.speechSynthesis.speak(utterance);

                return { mode: "tts", usedFallback: playbackRequest.mode === "audio" };
            }

            if (playbackRequest.mode === "audio") {
                const audioUrl = resolveAudioUrl(playbackRequest.audioAssetPath, request.baseUrl);
                if (!audioUrl) {
                    if (playbackRequest.allowAudioFallback) {
                        onStatus(messages.audioFallback || playbackRequest.fallbackMessage, "fallback");
                        return speakTts();
                    }

                    onStatus(messages.audioUnavailable || playbackRequest.fallbackMessage, "error");
                    return { mode: "audio", usedFallback: false };
                }

                const audio = new Audio(audioUrl);
                activeAudio = audio;
                audio.preload = "auto";
                audio.onplaying = function () {
                    if (currentToken === playbackToken) {
                        onStatus(messages.playingAudio || "Đang phát bản thu sẵn...", "playing");
                    }
                };
                audio.onended = function () {
                    if (currentToken === playbackToken) {
                        activeAudio = null;
                        onStatus(messages.audioEnded || "Đã phát xong.", "ended");
                    }
                };
                audio.onerror = function () {
                    if (currentToken !== playbackToken) {
                        return;
                    }

                    activeAudio = null;

                    if (playbackRequest.allowAudioFallback) {
                        onStatus(messages.audioFallback || "Không mở được file audio. Đang chuyển sang TTS.", "fallback");
                        speakTts();
                        return;
                    }

                    onStatus(messages.audioFailed || "Không phát được file audio này trên web.", "error");
                };

                try {
                    await audio.play();
                    return { mode: "audio", usedFallback: false };
                } catch (error) {
                    activeAudio = null;

                    if (playbackRequest.allowAudioFallback) {
                        onStatus(messages.audioFallback || "Không mở được file audio. Đang chuyển sang TTS.", "fallback");
                        return speakTts();
                    }

                    onStatus(messages.audioFailed || "Không phát được file audio này trên web.", "error");
                    return { mode: "audio", usedFallback: false, error: error };
                }
            }

            if (playbackRequest.fallbackMessage) {
                onStatus(messages.audioFallback || playbackRequest.fallbackMessage, "fallback");
            }

            return speakTts();
        }

        return {
            play: play,
            stop: stop
        };
    }

    window.VinhKhanhNarrationPlayer = {
        createPlayer: createPlayer,
        normalizeLanguageCode: normalizeLanguageCode,
        normalizePlaybackMode: normalizePlaybackMode,
        normalizeVoiceType: normalizeVoiceType,
        getPreferredLocaleCodes: getPreferredLocaleCodes,
        resolveAudioUrl: resolveAudioUrl
    };
})();
