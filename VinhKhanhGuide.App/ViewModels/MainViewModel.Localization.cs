using System.Collections.ObjectModel;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public partial class MainViewModel
{
    private string _selectedFeaturedDishCategoryKey = "bo";

    public static LocationDto EntranceLocation => new()
    {
        Latitude = EntranceLatitude,
        Longitude = EntranceLongitude
    };

    public string GreetingText => LocalizeUi(
        $"Xin chào, {CurrentUserDisplayName}",
        $"Hello, {CurrentUserDisplayName}",
        $"您好，{CurrentUserDisplayName}",
        $"{CurrentUserDisplayName}님, 안녕하세요",
        $"Bonjour, {CurrentUserDisplayName}");

    public string SearchSectionTitle => LocalizeUi("Tìm kiếm", "Search", "搜索", "검색", "Recherche");
    public string SearchPlaceholderText => LocalizeUi(
        "Tìm quán hoặc tour",
        "Search restaurants or tours",
        "搜索餐厅或路线",
        "식당 또는 투어 검색",
        "Rechercher un restaurant ou un parcours");
    public string MapSectionTitle => LocalizeUi("Bản đồ khám phá", "Explore Map", "探索地图", "탐색 지도", "Carte d'exploration");
    public string MapOpenLargeText => LocalizeUi("Mở lớn", "Expand", "放大查看", "크게 보기", "Agrandir");
    public string StartGpsButtonText => LocalizeUi("Bật GPS", "Turn On GPS", "开启 GPS", "GPS 켜기", "Activer le GPS");
    public string StopGpsButtonText => LocalizeUi("Tắt GPS", "Turn Off GPS", "关闭 GPS", "GPS 끄기", "Couper le GPS");
    public string GoToEntranceButtonText => LocalizeUi("40 Vĩnh Khánh", "40 Vinh Khanh", "40 永庆街", "40 Vinh Khanh", "40 Vinh Khanh");
    public string TourSectionTitle => LocalizeUi("Tour", "Tours", "路线导览", "투어", "Parcours");
    public string ActiveTourBadgeText => LocalizeUi("Tour đang chạy", "Tour Active", "路线进行中", "진행 중인 투어", "Parcours en cours");
    public string CurrentStopTitle => LocalizeUi("Chặng hiện tại", "Current Stop", "当前站点", "현재 경유지", "Étape actuelle");
    public string NextStopTitle => LocalizeUi("Chặng kế tiếp", "Next Stop", "下一站", "다음 경유지", "Étape suivante");
    public string ViewRouteButtonText => LocalizeUi("Xem lộ trình", "View Route", "查看路线", "경로 보기", "Voir l'itinéraire");
    public string StopTourButtonText => LocalizeUi("Dừng tour", "Stop Tour", "结束路线", "투어 중지", "Arrêter le parcours");
    public string SelectedPoiSectionTitle => LocalizeUi("Điểm đang xem", "Selected Place", "当前查看地点", "현재 보는 장소", "Lieu consulté");
    public string SelectedPoiDetailButtonText => LocalizeUi("Chi tiết", "Details", "详情", "상세 보기", "Détails");
    public string PoiListTitle => LocalizeUi("Danh sách quán", "Places", "店铺列表", "매장 목록", "Lieux");
    public string FeaturedSectionTitle => LocalizeUi("Món ăn nổi bật", "Featured Dishes", "招牌菜", "추천 메뉴", "Plats phares");
    public string FeaturedSectionSummary => LocalizeUi(
        "Giữ bố cục 4 nhóm món quen thuộc, đổi tông màu để dễ nhìn hơn và chạm vào để xem chi tiết như flow cũ.",
        "The classic four dish groups stay in place with a cleaner color treatment. Tap a group to open its details.",
        "保留原来的四个招牌菜分组，只调整色彩，并可点击查看旧流程中的详情。",
        "기존의 4개 대표 메뉴 그룹은 유지하고 색감만 정리했습니다. 탭하면 이전 흐름처럼 상세를 볼 수 있습니다.",
        "Les quatre familles de plats restent en place avec une nouvelle palette. Touchez une famille pour voir ses détails comme avant.");
    public string ListeningHistorySectionTitle => LocalizeUi("Lịch sử nghe", "Listening History", "收听记录", "청취 기록", "Historique d'écoute");
    public string ListeningHistorySectionSummary => LocalizeUi(
        "Lưu lại những nội dung vừa nghe để khách quay lại nhanh mà không bị rối.",
        "Keeps the latest listened items easy to revisit without feeling crowded.",
        "用更简洁的方式保留最近收听内容，方便回看而不打扰用户。",
        "최근 들은 내용을 부담 없이 다시 열 수 있도록 간결하게 정리했습니다.",
        "Conserve les écoutes récentes dans une vue compacte et facile à reprendre.");
    public string ListeningHistorySeeAllText => LocalizeUi("Xem đầy đủ", "View All", "查看全部", "전체 보기", "Tout afficher");
    public string ListeningHistoryEmptyStateText => LocalizeUi(
        "Chưa có lượt nghe nào. Khi khách bắt đầu nghe, lịch sử sẽ xuất hiện gọn gàng ở đây.",
        "No listening history yet. Recent plays will appear here in a compact list.",
        "暂时没有收听记录。开始收听后，记录会简洁地显示在这里。",
        "아직 청취 기록이 없습니다. 재생이 시작되면 여기에 간결하게 표시됩니다.",
        "Aucun historique pour le moment. Les écoutes récentes apparaîtront ici dans une vue compacte.");
    public string HomeNavigationLabel => LocalizeUi("Trang chủ", "Home", "首页", "홈", "Accueil");
    public string EntranceNavigationLabel => LocalizeUi("40 Vĩnh Khánh", "40 Vinh Khanh", "40 永庆街", "40 Vinh Khanh", "40 Vinh Khanh");
    public string HistoryNavigationLabel => LocalizeUi("Lịch sử", "History", "历史", "기록", "Historique");
    public string AccountNavigationLabel => LocalizeUi("Tài khoản", "Account", "账户", "계정", "Compte");
    public string FullScreenMapTitle => LocalizeUi("Bản đồ lộ trình", "Route Map", "路线地图", "경로 지도", "Carte de l'itinéraire");
    public string FullScreenMapSubtitle => LocalizeUi(
        "Chạm vào ghim để xem chi tiết quán",
        "Tap a pin to open place details",
        "点击图钉查看地点详情",
        "핀을 눌러 매장 상세를 확인하세요",
        "Touchez une épingle pour ouvrir les détails du lieu");
    public string StopListeningButtonText => LocalizeUi("Dừng nghe", "Stop Audio", "停止收听", "재생 중지", "Arrêter l'écoute");

    public string AccountPageTitle => LocalizeUi("Tài khoản", "Account", "账户", "계정", "Compte");
    public string AccountStatusTitle => LocalizeUi("Trạng thái", "Status", "状态", "상태", "Statut");
    public string AccountStatusValue => LocalizeUi("Đang hoạt động", "Active", "运行中", "활성 상태", "Actif");
    public string PersonalInfoSectionTitle => LocalizeUi("Thông tin cá nhân", "Personal Information", "个人信息", "개인 정보", "Informations personnelles");
    public string FullNameLabel => LocalizeUi("Họ và tên", "Full Name", "姓名", "이름", "Nom complet");
    public string FullNamePlaceholder => LocalizeUi("Nhập họ và tên", "Enter full name", "输入姓名", "이름 입력", "Saisir le nom complet");
    public string EmailLabel => LocalizeUi("Email", "Email", "电子邮箱", "이메일", "E-mail");
    public string EmailPlaceholder => LocalizeUi("Nhập email", "Enter email", "输入邮箱", "이메일 입력", "Saisir l'e-mail");
    public string PhoneLabel => LocalizeUi("Số điện thoại", "Phone Number", "电话号码", "전화번호", "Numéro de téléphone");
    public string PhonePlaceholder => LocalizeUi("Nhập số điện thoại", "Enter phone number", "输入电话号码", "전화번호 입력", "Saisir le numéro de téléphone");
    public string SaveProfileButtonText => LocalizeUi("Lưu thông tin", "Save Profile", "保存资料", "정보 저장", "Enregistrer");
    public string ResetButtonText => LocalizeUi("Đặt lại", "Reset", "重置", "초기화", "Réinitialiser");
    public string LanguageSectionTitle => LocalizeUi("Ngôn ngữ và TTS", "Language & TTS", "语言与 TTS", "언어 및 TTS", "Langue et TTS");
    public string LanguageSectionSummary => LocalizeUi(
        "Khi đổi ngôn ngữ, giao diện và phần thuyết minh sẽ đổi theo cùng lúc.",
        "Changing the language updates both the interface and narration together.",
        "切换语言时，界面和语音讲解会一起更新。",
        "언어를 바꾸면 화면과 음성 안내가 함께 바뀝니다.",
        "Quand vous changez de langue, l'interface et la narration changent ensemble.");
    public string LanguagePickerTitle => LocalizeUi("Chọn ngôn ngữ", "Choose language", "选择语言", "언어 선택", "Choisir la langue");
    public string SaveLanguageButtonText => LocalizeUi("Lưu ngôn ngữ", "Save Language", "保存语言", "언어 저장", "Enregistrer la langue");
    public string RestoreButtonText => LocalizeUi("Khôi phục", "Restore", "恢复", "복원", "Restaurer");
    public string RefreshButtonText => LocalizeUi("Làm mới", "Refresh", "刷新", "새로 고침", "Actualiser");

    public string PoiDetailPageTitle => LocalizeUi("Chi tiết địa điểm", "Place Details", "地点详情", "장소 상세", "Détails du lieu");
    public string PoiDescriptionSectionTitle => LocalizeUi("Mô tả quán", "About This Place", "地点介绍", "장소 소개", "À propos du lieu");
    public string PoiNarrationSectionTitle => LocalizeUi("Thuyết minh", "Narration", "语音讲解", "오디오 안내", "Narration");
    public string PoiDirectionButtonText => LocalizeUi("Chỉ đường", "Directions", "导航", "길찾기", "Itinéraire");

    public string FeaturedDishPageTitle => LocalizeUi("Món nổi bật", "Featured Dishes", "招牌菜", "추천 메뉴", "Plats phares");
    public string FeaturedDishGroupPrefix => LocalizeUi("Nhóm món", "Category", "分组", "카테고리", "Catégorie");
    public string FeaturedDishPriceTitle => LocalizeUi("Giá tham khảo", "Starting Price", "参考价格", "시작 가격", "Prix de départ");
    public string SelectedFeaturedDishCategoryHeaderText => $"{FeaturedDishGroupPrefix}: {SelectedFeaturedDishCategoryName}";

    public IReadOnlyList<ListeningHistoryDisplayItem> ListeningHistoryPreviewItems =>
        ListeningHistory.Take(4).ToList();

    public bool HasListeningHistoryPreview => ListeningHistoryPreviewItems.Count > 0;

    public double TourPackagesHeight => HasTours ? 440 : 0;

    public double ActiveTourStopsHeight => HasActiveTourStops
        ? Math.Min(420, Math.Max(156, ActiveTourStops.Count * 82 + 18))
        : 0;

    private string LocalizeUi(string vi, string en, string zh, string ko, string fr)
    {
        return LocalizeUiFor(SelectedLanguage, vi, en, zh, ko, fr);
    }

    private static string LocalizeUiFor(
        string? languageCode,
        string vi,
        string en,
        string zh,
        string ko,
        string fr)
    {
        return NormalizeUiLanguage(languageCode) switch
        {
            "en" => en,
            "zh" => zh,
            "ko" => ko,
            "fr" => fr,
            _ => vi
        };
    }

    private static string NormalizeUiLanguage(string? languageCode)
    {
        return languageCode?.Trim().ToLowerInvariant() switch
        {
            "en" => "en",
            "zh" => "zh",
            "ko" => "ko",
            "fr" => "fr",
            _ => "vi"
        };
    }

    private string GetRoleLabel(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => LocalizeUi("Quản trị viên", "Administrator", "管理员", "관리자", "Administrateur"),
            "poi_owner" => LocalizeUi("Chủ quán", "Venue Owner", "店主", "매장 관리자", "Propriétaire"),
            "guest" => LocalizeUi("Khách tham quan", "Guest Visitor", "访客", "방문객", "Visiteur"),
            _ => LocalizeUi("Khách khám phá", "Explorer", "探索用户", "탐험 사용자", "Explorateur")
        };
    }

    private string GetDefaultEntranceLocationText()
    {
        return LocalizeUi(
            $"Bạn đang xem {EntranceAddress}",
            $"Viewing {EntranceAddress}",
            $"当前查看 {EntranceAddress}",
            $"{EntranceAddress}를 보고 있습니다",
            $"Affichage de {EntranceAddress}");
    }

    private string GetDefaultPoiPromptText()
    {
        return LocalizeUi(
            "Chọn quán hoặc chạm bản đồ để nghe thuyết minh",
            "Select a place or tap the map to start narration",
            "选择店铺或点击地图开始收听讲解",
            "매장을 선택하거나 지도를 눌러 오디오 안내를 들으세요",
            "Choisissez un lieu ou touchez la carte pour lancer la narration");
    }

    private string GetSearchEmptyStateMessage(string keyword)
    {
        return LocalizeUi(
            $"Không tìm thấy quán phù hợp với từ khóa \"{keyword}\".",
            $"No place matched \"{keyword}\".",
            $"没有找到与“{keyword}”匹配的地点。",
            $"\"{keyword}\"와(과) 일치하는 매장을 찾지 못했습니다.",
            $"Aucun lieu ne correspond à \"{keyword}\".");
    }

    private string GetLocalizedTourMetaLabel(int stopCount, int estimatedMinutes)
    {
        return LocalizeUi(
            $"{stopCount} điểm dừng • {estimatedMinutes} phút",
            $"{stopCount} stops • {estimatedMinutes} min",
            $"{stopCount} 个站点 • {estimatedMinutes} 分钟",
            $"{stopCount}개 경유지 • {estimatedMinutes}분",
            $"{stopCount} étapes • {estimatedMinutes} min");
    }

    private string GetLocalizedTourPackageStatusLabel(bool isSelected, bool isCompleted)
    {
        if (isSelected)
        {
            return isCompleted
                ? LocalizeUi("Đã hoàn tất", "Completed", "已完成", "완료됨", "Terminé")
                : LocalizeUi("Đang chạy", "In Progress", "进行中", "진행 중", "En cours");
        }

        return LocalizeUi("Sẵn sàng", "Ready", "就绪", "준비됨", "Prêt");
    }

    private string GetLocalizedTourPackageActionLabel(bool isSelected)
    {
        return isSelected
            ? LocalizeUi("Đang chọn", "Selected", "已选择", "선택됨", "Sélectionné")
            : LocalizeUi("Bắt đầu", "Start", "开始", "시작", "Démarrer");
    }

    private string GetLocalizedTourStopOrderLabel(int order)
    {
        return LocalizeUi(
            $"Chặng {order}",
            $"Stop {order}",
            $"第 {order} 站",
            $"{order}번 경유지",
            $"Étape {order}");
    }

    private string GetLocalizedTourStopStatusLabel(bool isCompleted, bool isCurrent)
    {
        if (isCompleted)
        {
            return LocalizeUi("Đã phát", "Played", "已播放", "재생 완료", "Déjà lu");
        }

        return isCurrent
            ? LocalizeUi("Đang theo dõi", "Tracking", "跟踪中", "추적 중", "En suivi")
            : LocalizeUi("Chờ tới lượt", "Upcoming", "等待中", "대기 중", "À venir");
    }

    private string GetLocalizedPoiCodeLabel(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return LocalizeUi("Chưa có mã", "No code yet", "暂无编号", "코드 없음", "Code indisponible");
        }

        return LocalizeUi($"Mã: {code}", $"Code: {code}", $"编号：{code}", $"코드: {code}", $"Code : {code}");
    }

    private string GetLocalizedPoiStatusLabel(bool isInsideRadius)
    {
        return isInsideRadius
            ? LocalizeUi("Trong vùng geofence", "Inside geofence", "位于触发范围内", "지오펜스 범위 안", "Dans la zone de déclenchement")
            : LocalizeUi("Ngoài vùng", "Outside area", "不在范围内", "범위 밖", "Hors zone");
    }

    private string GetLocalizedPoiNearestLabel(bool isNearest)
    {
        return isNearest
            ? LocalizeUi("POI gần nhất", "Nearest POI", "最近的 POI", "가장 가까운 POI", "POI le plus proche")
            : string.Empty;
    }

    private string GetLocalizedPoiInRadiusBadge(bool isInsideRadius)
    {
        return isInsideRadius
            ? LocalizeUi("Đang ở gần", "Nearby", "就在附近", "근처에 있음", "À proximité")
            : string.Empty;
    }

    private string GetLocalizedSpecialDishLabel(string specialDish)
    {
        if (string.IsNullOrWhiteSpace(specialDish))
        {
            return string.Empty;
        }

        return LocalizeUi(
            $"Món nổi bật: {specialDish}",
            $"Featured dish: {specialDish}",
            $"招牌菜：{specialDish}",
            $"대표 메뉴: {specialDish}",
            $"Plat phare : {specialDish}");
    }

    private string GetLocalizedPoiNarrationActionText(bool isNarrationActive)
    {
        return isNarrationActive
            ? LocalizeUi("Dừng thuyết minh", "Stop Narration", "停止讲解", "안내 중지", "Arrêter la narration")
            : LocalizeUi("Nghe thuyết minh", "Listen", "收听讲解", "안내 듣기", "Écouter");
    }

    private string GetLocalizedPoiNarrationStateText(bool isNarrationActive)
    {
        return isNarrationActive
            ? LocalizeUi("Đang phát thuyết minh", "Narration playing", "讲解播放中", "안내 재생 중", "Narration en lecture")
            : LocalizeUi("Sẵn sàng nghe", "Ready to play", "可立即播放", "바로 재생 가능", "Prêt à lire");
    }

    private string GetLocalizedPoiNarrationGuideText(bool isNarrationActive)
    {
        return isNarrationActive
            ? LocalizeUi(
                "Phần Talk to Speech của quán này đang phát.",
                "This place is currently playing narration.",
                "该地点的语音讲解正在播放。",
                "이 매장의 오디오 안내가 재생 중입니다.",
                "La narration de ce lieu est en cours.")
            : LocalizeUi(
                "Bấm để nghe phần Talk to Speech ngay tại trang chủ.",
                "Tap to hear the narration right from the home screen.",
                "点击即可在首页直接收听讲解。",
                "홈 화면에서 바로 오디오 안내를 들을 수 있습니다.",
                "Touchez pour écouter la narration directement depuis l'accueil.");
    }

    private string GetLocalizedTourBadgeText(bool hasTourBadge, bool isCompletedTourStop, int? order)
    {
        if (!hasTourBadge)
        {
            return string.Empty;
        }

        var safeOrder = order ?? 0;
        return isCompletedTourStop
            ? LocalizeUi(
                $"Chặng {safeOrder} đã xong",
                $"Stop {safeOrder} done",
                $"第 {safeOrder} 站已完成",
                $"{safeOrder}번 경유지 완료",
                $"Étape {safeOrder} terminée")
            : LocalizeUi(
                $"Chặng {safeOrder} đang dẫn",
                $"Guiding stop {safeOrder}",
                $"当前引导第 {safeOrder} 站",
                $"현재 {safeOrder}번 경유지 안내 중",
                $"Étape {safeOrder} en cours");
    }

    private string GetLocalizedPoiDistanceLabel(double distanceMeters, double triggerRadiusMeters)
    {
        return double.IsNaN(distanceMeters)
            ? LocalizeUi(
                $"Bán kính {triggerRadiusMeters:F0}m",
                $"Radius {triggerRadiusMeters:F0}m",
                $"半径 {triggerRadiusMeters:F0} 米",
                $"반경 {triggerRadiusMeters:F0}m",
                $"Rayon {triggerRadiusMeters:F0} m")
            : LocalizeUi(
                $"{distanceMeters:F0}m • Bán kính {triggerRadiusMeters:F0}m",
                $"{distanceMeters:F0}m • Radius {triggerRadiusMeters:F0}m",
                $"{distanceMeters:F0} 米 • 半径 {triggerRadiusMeters:F0} 米",
                $"{distanceMeters:F0}m • 반경 {triggerRadiusMeters:F0}m",
                $"{distanceMeters:F0} m • Rayon {triggerRadiusMeters:F0} m");
    }

    private string GetLocalizedHistoryTriggerLabel(bool isAutoTriggered)
    {
        return isAutoTriggered
            ? LocalizeUi("Tự động", "Automatic", "自动", "자동", "Automatique")
            : LocalizeUi("Thủ công", "Manual", "手动", "수동", "Manuel");
    }

    private string GetLocalizedHistoryDurationLabel(int listenSeconds)
    {
        return listenSeconds > 0
            ? LocalizeUi(
                $"{listenSeconds} giây",
                $"{listenSeconds} sec",
                $"{listenSeconds} 秒",
                $"{listenSeconds}초",
                $"{listenSeconds} s")
            : LocalizeUi("Đang ghi nhận", "Recording", "记录中", "기록 중", "En cours");
    }

    private string GetLocalizedHistoryStatusLabel(bool completed, bool hasError)
    {
        if (completed)
        {
            return LocalizeUi("Hoàn tất", "Completed", "已完成", "완료", "Terminé");
        }

        return hasError
            ? LocalizeUi("Dừng vì lỗi", "Stopped on error", "因错误停止", "오류로 중지", "Arrêt sur erreur")
            : LocalizeUi("Dừng sớm", "Stopped early", "提前结束", "중간 종료", "Arrêt anticipé");
    }

    private string GetLocalizedHistoryDescriptionFallback()
    {
        return LocalizeUi(
            "Bản ghi này chưa có mô tả ngắn từ quán.",
            "This record does not have a short place description yet.",
            "此记录暂时没有地点简介。",
            "이 기록에는 아직 장소 설명이 없습니다.",
            "Cet enregistrement ne contient pas encore de courte description.");
    }

    private string GetLocalizedListeningHistorySummaryText(int syncedCount, int localCount)
    {
        if (syncedCount > 0)
        {
            return LocalizeUi(
                $"{syncedCount} lượt nghe gần nhất",
                $"{syncedCount} recent listens",
                $"{syncedCount} 条最近收听记录",
                $"최근 청취 {syncedCount}건",
                $"{syncedCount} écoutes récentes");
        }

        if (localCount > 0)
        {
            return LocalizeUi(
                $"{localCount} lượt nghe vừa ghi nhận",
                $"{localCount} recently captured plays",
                $"{localCount} 条刚记录的播放",
                $"방금 기록된 청취 {localCount}건",
                $"{localCount} écoutes tout juste enregistrées");
        }

        return LocalizeUi("Chưa có bản ghi nghe nào", "No listening history yet", "暂无收听记录", "청취 기록 없음", "Aucun historique d'écoute");
    }

    private string GetLocalizedListeningHistoryFallbackSummaryText(int localCount)
    {
        return localCount > 0
            ? LocalizeUi(
                $"{localCount} lượt nghe đã được lưu cục bộ trên thiết bị này.",
                $"{localCount} listens are stored locally on this device.",
                $"此设备上已本地保存 {localCount} 条收听记录。",
                $"이 기기에 {localCount}개의 청취 기록이 저장되어 있습니다.",
                $"{localCount} écoutes sont stockées localement sur cet appareil.")
            : LocalizeUi(
                "Các lượt nghe mới sẽ được lưu cục bộ trên thiết bị này.",
                "New listens will also be saved locally on this device.",
                "新的收听记录也会保存在这台设备上。",
                "새 청취 기록은 이 기기에 함께 저장됩니다.",
                "Les nouvelles écoutes seront aussi enregistrées localement sur cet appareil.");
    }

    private string GetLocalizedListeningHistorySyncStatus(DateTimeOffset? syncedAt)
    {
        return syncedAt.HasValue
            ? LocalizeUi(
                $"Đồng bộ lúc {syncedAt.Value.ToLocalTime():HH:mm:ss}",
                $"Synced at {syncedAt.Value.ToLocalTime():HH:mm:ss}",
                $"同步时间 {syncedAt.Value.ToLocalTime():HH:mm:ss}",
                $"{syncedAt.Value.ToLocalTime():HH:mm:ss} 동기화",
                $"Synchronisé à {syncedAt.Value.ToLocalTime():HH:mm:ss}")
            : LocalizeUi("Chưa đồng bộ lịch sử nghe", "History not synced yet", "历史尚未同步", "기록이 아직 동기화되지 않음", "Historique non synchronisé");
    }

    private string GetLocalizedAccountAccessMessage(bool canManage)
    {
        return canManage
            ? LocalizeUi(
                "Bạn có thể cập nhật hồ sơ cá nhân trực tiếp trên thiết bị này.",
                "You can update the visitor profile directly on this device.",
                "您可以直接在此设备上更新访客资料。",
                "이 기기에서 바로 방문자 정보를 수정할 수 있습니다.",
                "Vous pouvez mettre à jour le profil directement sur cet appareil.")
            : LocalizeUi(
                "Chưa có hồ sơ người dùng để cập nhật.",
                "There is no user profile to update yet.",
                "暂时没有可更新的用户资料。",
                "아직 수정할 사용자 정보가 없습니다.",
                "Aucun profil utilisateur à mettre à jour pour le moment.");
    }

    private string GetLocalizedLanguageSavedMessage()
    {
        return LocalizeUi(
            "Đã lưu tùy chọn ngôn ngữ cho ứng dụng.",
            "Language preference saved for the app.",
            "应用语言偏好已保存。",
            "앱 언어 설정이 저장되었습니다.",
            "La langue de l'application a été enregistrée.");
    }

    private string GetPreferredLanguageLocaleCode(string? languageCode)
    {
        return NormalizeUiLanguage(languageCode) switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            "ko" => "ko-KR",
            "fr" => "fr-FR",
            _ => "vi-VN"
        };
    }

    private void RaiseLocalizedUiChanged()
    {
        OnPropertyChanged(nameof(GreetingText));
        OnPropertyChanged(nameof(SearchSectionTitle));
        OnPropertyChanged(nameof(SearchPlaceholderText));
        OnPropertyChanged(nameof(MapSectionTitle));
        OnPropertyChanged(nameof(MapOpenLargeText));
        OnPropertyChanged(nameof(StartGpsButtonText));
        OnPropertyChanged(nameof(StopGpsButtonText));
        OnPropertyChanged(nameof(GoToEntranceButtonText));
        OnPropertyChanged(nameof(TourSectionTitle));
        OnPropertyChanged(nameof(ActiveTourBadgeText));
        OnPropertyChanged(nameof(CurrentStopTitle));
        OnPropertyChanged(nameof(NextStopTitle));
        OnPropertyChanged(nameof(ViewRouteButtonText));
        OnPropertyChanged(nameof(StopTourButtonText));
        OnPropertyChanged(nameof(SelectedPoiSectionTitle));
        OnPropertyChanged(nameof(SelectedPoiDetailButtonText));
        OnPropertyChanged(nameof(PoiListTitle));
        OnPropertyChanged(nameof(FeaturedSectionTitle));
        OnPropertyChanged(nameof(FeaturedSectionSummary));
        OnPropertyChanged(nameof(ListeningHistorySectionTitle));
        OnPropertyChanged(nameof(ListeningHistorySectionSummary));
        OnPropertyChanged(nameof(ListeningHistorySeeAllText));
        OnPropertyChanged(nameof(ListeningHistoryEmptyStateText));
        OnPropertyChanged(nameof(HomeNavigationLabel));
        OnPropertyChanged(nameof(EntranceNavigationLabel));
        OnPropertyChanged(nameof(HistoryNavigationLabel));
        OnPropertyChanged(nameof(AccountNavigationLabel));
        OnPropertyChanged(nameof(FullScreenMapTitle));
        OnPropertyChanged(nameof(FullScreenMapSubtitle));
        OnPropertyChanged(nameof(StopListeningButtonText));
        OnPropertyChanged(nameof(AccountPageTitle));
        OnPropertyChanged(nameof(AccountStatusTitle));
        OnPropertyChanged(nameof(AccountStatusValue));
        OnPropertyChanged(nameof(PersonalInfoSectionTitle));
        OnPropertyChanged(nameof(FullNameLabel));
        OnPropertyChanged(nameof(FullNamePlaceholder));
        OnPropertyChanged(nameof(EmailLabel));
        OnPropertyChanged(nameof(EmailPlaceholder));
        OnPropertyChanged(nameof(PhoneLabel));
        OnPropertyChanged(nameof(PhonePlaceholder));
        OnPropertyChanged(nameof(SaveProfileButtonText));
        OnPropertyChanged(nameof(ResetButtonText));
        OnPropertyChanged(nameof(LanguageSectionTitle));
        OnPropertyChanged(nameof(LanguageSectionSummary));
        OnPropertyChanged(nameof(LanguagePickerTitle));
        OnPropertyChanged(nameof(SaveLanguageButtonText));
        OnPropertyChanged(nameof(RestoreButtonText));
        OnPropertyChanged(nameof(RefreshButtonText));
        OnPropertyChanged(nameof(PoiDetailPageTitle));
        OnPropertyChanged(nameof(PoiDescriptionSectionTitle));
        OnPropertyChanged(nameof(PoiNarrationSectionTitle));
        OnPropertyChanged(nameof(PoiDirectionButtonText));
        OnPropertyChanged(nameof(FeaturedDishPageTitle));
        OnPropertyChanged(nameof(FeaturedDishGroupPrefix));
        OnPropertyChanged(nameof(FeaturedDishPriceTitle));
        OnPropertyChanged(nameof(SelectedFeaturedDishCategoryHeaderText));
        OnPropertyChanged(nameof(TourPackagesHeight));
        OnPropertyChanged(nameof(ActiveTourStopsHeight));
        OnPropertyChanged(nameof(ListeningHistoryPreviewItems));
        OnPropertyChanged(nameof(HasListeningHistoryPreview));
    }

    private void RebuildFeaturedDishCategories()
    {
        ReplaceCollection(FeaturedDishes, BuildFeaturedDishCategories());
    }

    private List<FoodCategoryItem> BuildFeaturedDishCategories()
    {
        return
        [
            new()
            {
                Key = "bo",
                Icon = "🥩",
                Name = LocalizeUi("Bò", "Beef", "牛肉", "소고기", "Boeuf"),
                Description = LocalizeUi(
                    "Món bò nướng đậm vị, dễ chọn cho khách mới.",
                    "Rich grilled beef picks that are easy for first-time visitors.",
                    "味道浓郁的烤牛肉，适合第一次来的客人。",
                    "처음 방문한 손님도 고르기 쉬운 진한 풍미의 소고기 메뉴.",
                    "Des plats de boeuf grillé faciles à choisir pour une première visite."),
                DishCount = CountFeaturedDishes("bo"),
                CountLabel = LocalizeUi(
                    $"{CountFeaturedDishes("bo")} món",
                    $"{CountFeaturedDishes("bo")} dishes",
                    $"{CountFeaturedDishes("bo")} 道菜",
                    $"{CountFeaturedDishes("bo")}개 메뉴",
                    $"{CountFeaturedDishes("bo")} plats"),
                BackgroundColor = "#FFF2E8",
                AccentColor = "#C6672F"
            },
            new()
            {
                Key = "lau",
                Icon = "🍲",
                Name = LocalizeUi("Lẩu", "Hotpot", "火锅", "전골", "Fondue"),
                Description = LocalizeUi(
                    "Các món lẩu nóng hổi cho nhóm bạn hoặc gia đình.",
                    "Hotpot sets for friends and family groups.",
                    "适合朋友和家庭的热腾腾火锅。",
                    "친구나 가족이 함께 즐기기 좋은 전골 메뉴.",
                    "Des plats à partager en groupe ou en famille."),
                DishCount = CountFeaturedDishes("lau"),
                CountLabel = LocalizeUi(
                    $"{CountFeaturedDishes("lau")} món",
                    $"{CountFeaturedDishes("lau")} dishes",
                    $"{CountFeaturedDishes("lau")} 道菜",
                    $"{CountFeaturedDishes("lau")}개 메뉴",
                    $"{CountFeaturedDishes("lau")} plats"),
                BackgroundColor = "#EEF7FF",
                AccentColor = "#2563EB"
            },
            new()
            {
                Key = "oc",
                Icon = "🐚",
                Name = LocalizeUi("Ốc", "Shellfish", "贝类", "조개류", "Coquillages"),
                Description = LocalizeUi(
                    "Món ốc nổi bật với vị sốt và nướng quen thuộc.",
                    "Popular shellfish dishes with signature sauces and grills.",
                    "招牌贝类料理，酱香与烧烤风味突出。",
                    "소스와 구이 풍미가 돋보이는 대표 조개 메뉴.",
                    "Des coquillages remarquables, entre sauces maison et grillades."),
                DishCount = CountFeaturedDishes("oc"),
                CountLabel = LocalizeUi(
                    $"{CountFeaturedDishes("oc")} món",
                    $"{CountFeaturedDishes("oc")} dishes",
                    $"{CountFeaturedDishes("oc")} 道菜",
                    $"{CountFeaturedDishes("oc")}개 메뉴",
                    $"{CountFeaturedDishes("oc")} plats"),
                BackgroundColor = "#EEFDF5",
                AccentColor = "#15803D"
            },
            new()
            {
                Key = "cua",
                Icon = "🦀",
                Name = LocalizeUi("Cua", "Crab", "螃蟹", "게", "Crabe"),
                Description = LocalizeUi(
                    "Các món cua đáng chú ý với mức giá mở đầu rõ ràng.",
                    "Crab dishes with a clear starting price and strong appeal.",
                    "价格起点清晰、辨识度很高的螃蟹料理。",
                    "가격대가 명확하고 존재감 있는 게 요리.",
                    "Des plats de crabe attractifs avec un prix de départ lisible."),
                DishCount = CountFeaturedDishes("cua"),
                CountLabel = LocalizeUi(
                    $"{CountFeaturedDishes("cua")} món",
                    $"{CountFeaturedDishes("cua")} dishes",
                    $"{CountFeaturedDishes("cua")} 道菜",
                    $"{CountFeaturedDishes("cua")}개 메뉴",
                    $"{CountFeaturedDishes("cua")} plats"),
                BackgroundColor = "#FFF7ED",
                AccentColor = "#C2410C"
            }
        ];
    }

    private void RefreshFeaturedDishLocalization()
    {
        var currentKey = _selectedFeaturedDishCategoryKey;
        RebuildFeaturedDishCategories();
        ShowFeaturedDishCategory(currentKey);
        OnPropertyChanged(nameof(SelectedFeaturedDishResultsText));
        OnPropertyChanged(nameof(SelectedFeaturedDishCategoryHeaderText));
    }

    private async Task SyncCurrentUserProfileToAdminAsync()
    {
        try
        {
            await _userProfileSyncService.SyncCurrentUserAsync(GetPreferredLanguageLocaleCode(SelectedLanguage));
        }
        catch
        {
        }
    }

    private FeaturedDishItem LocalizeFeaturedDishItem(FeaturedDishItem seed)
    {
        var name = seed.Name switch
        {
            "Sườn bò nướng" => LocalizeUi("Sườn bò nướng", "Grilled beef ribs", "烤牛肋排", "소갈비 구이", "Côtes de boeuf grillées"),
            "Bò nướng miếng" => LocalizeUi("Bò nướng miếng", "Grilled beef slices", "烤牛肉片", "소고기 구이", "Pièces de boeuf grillées"),
            "Bò nướng lá lốt" => LocalizeUi("Bò nướng lá lốt", "Beef wrapped in betel leaf", "蒌叶烤牛肉", "라롯잎 소고기 구이", "Boeuf grillé à la feuille de lolot"),
            "Lẩu Thái" => LocalizeUi("Lẩu Thái", "Thai hotpot", "泰式火锅", "태국식 전골", "Fondue thaïlandaise"),
            "Lẩu Hàn Quốc" => LocalizeUi("Lẩu Hàn Quốc", "Korean hotpot", "韩式火锅", "한식 전골", "Fondue coréenne"),
            "Ốc hương sốt trứng muối" => LocalizeUi("Ốc hương sốt trứng muối", "Sea snails with salted egg sauce", "咸蛋黄酱海螺", "소금달걀 소스 소라", "Bulots sauce oeuf salé"),
            "Ốc nướng mỡ hành" => LocalizeUi("Ốc nướng mỡ hành", "Grilled snails with scallion oil", "葱油烤螺", "쪽파 오일 구이 소라", "Escargots grillés à l'huile de ciboule"),
            "Cua Hoàng đế" => LocalizeUi("Cua Hoàng đế", "King crab", "帝王蟹", "킹크랩", "Crabe royal"),
            "Cua Cà Mau" => LocalizeUi("Cua Cà Mau", "Ca Mau crab", "金瓯蟹", "까마우 게", "Crabe de Ca Mau"),
            _ => seed.Name
        };

        var categoryName = seed.CategoryName switch
        {
            "Bò" => LocalizeUi("Bò", "Beef", "牛肉", "소고기", "Boeuf"),
            "Lẩu" => LocalizeUi("Lẩu", "Hotpot", "火锅", "전골", "Fondue"),
            "Ốc" => LocalizeUi("Ốc", "Shellfish", "贝类", "조개류", "Coquillages"),
            "Cua" => LocalizeUi("Cua", "Crab", "螃蟹", "게", "Crabe"),
            _ => seed.CategoryName
        };

        var description = seed.Name switch
        {
            "Sườn bò nướng" => LocalizeUi(
                "Phần sườn bò nướng đậm vị, phù hợp cho khách thích món nướng.",
                "Bold grilled beef ribs for guests who love charcoal flavors.",
                "风味浓郁的烤牛肋排，适合喜欢烧烤的客人。",
                "진한 구이 맛을 좋아하는 손님에게 어울리는 소갈비 메뉴.",
                "Des côtes de boeuf grillées au goût intense."),
            "Bò nướng miếng" => LocalizeUi(
                "Bò nướng cắt miếng dễ dùng, hợp để gọi chia sẻ theo nhóm.",
                "Easy-to-share grilled beef slices for groups.",
                "切片烤牛肉，适合多人分享。",
                "함께 나눠 먹기 좋은 소고기 구이 메뉴.",
                "Des morceaux de boeuf grillé faciles à partager."),
            "Bò nướng lá lốt" => LocalizeUi(
                "Món bò cuốn lá lốt thơm mùi đặc trưng, phù hợp khách thích vị truyền thống.",
                "A fragrant traditional beef dish wrapped in betel leaf.",
                "带有传统香气的蒌叶烤牛肉。",
                "전통적인 향이 살아있는 라롯잎 소고기 요리.",
                "Un plat traditionnel de boeuf parfumé à la feuille de lolot."),
            "Lẩu Thái" => LocalizeUi(
                "Nước lẩu chua cay kiểu Thái, hợp nhóm khách thích vị đậm và nóng.",
                "A spicy Thai-style broth ideal for groups.",
                "酸辣泰式锅底，适合喜欢重口味的客人。",
                "매콤한 태국식 국물로 여러 명이 함께 먹기 좋습니다.",
                "Un bouillon thaï épicé, parfait pour les groupes."),
            "Lẩu Hàn Quốc" => LocalizeUi(
                "Lẩu cay phong cách Hàn Quốc với topping phong phú và dễ gọi theo nhóm.",
                "A Korean-style hotpot with generous toppings for groups.",
                "韩式辣火锅，配料丰富，适合多人点用。",
                "푸짐한 토핑의 한식 전골로 단체 손님에게 좋습니다.",
                "Une fondue coréenne généreuse et facile à partager."),
            "Ốc hương sốt trứng muối" => LocalizeUi(
                "Ốc hương phủ sốt trứng muối béo mặn, là món nổi bật dễ thu hút khách mới.",
                "Sea snails coated in salted egg sauce, a strong signature dish.",
                "咸蛋黄酱海螺，辨识度很高，适合新客尝试。",
                "짭조름한 소금달걀 소스가 특징인 대표 메뉴입니다.",
                "Des bulots nappés de sauce oeuf salé, très signature."),
            "Ốc nướng mỡ hành" => LocalizeUi(
                "Ốc nướng mỡ hành quen vị, dễ ăn và có mức giá khởi điểm nhẹ hơn.",
                "A familiar grilled shellfish dish with a gentler starting price.",
                "经典葱油烤螺，容易入口，起步价也更轻松。",
                "익숙하고 부담 없는 가격의 조개 구이 메뉴입니다.",
                "Un coquillage grillé classique, facile d'accès et plus abordable."),
            "Cua Hoàng đế" => LocalizeUi(
                "Món cua cao cấp nổi bật, phù hợp nhóm khách muốn trải nghiệm đặc biệt.",
                "A premium crab experience for special occasions.",
                "高端帝王蟹料理，适合想要特别体验的客人。",
                "특별한 경험을 원하는 손님에게 어울리는 프리미엄 게 요리.",
                "Un plat premium pour une expérience plus marquante."),
            "Cua Cà Mau" => LocalizeUi(
                "Cua Cà Mau là lựa chọn dễ tiếp cận hơn với mức giá mở đầu rõ ràng.",
                "A more approachable crab option with a clear entry price.",
                "更容易入门的金瓯蟹选择，价格起点清晰。",
                "비교적 부담 없이 고를 수 있는 까마우 게 메뉴입니다.",
                "Une option plus accessible avec un prix de départ lisible."),
            _ => seed.ShortDescription
        };

        return new FeaturedDishItem
        {
            CategoryKey = seed.CategoryKey,
            CategoryName = categoryName,
            Name = name,
            StartingPrice = seed.StartingPrice,
            ImageSource = seed.ImageSource,
            ShortDescription = description,
            StartingPriceLabel = LocalizeUi(
                $"Chỉ từ {seed.StartingPrice}",
                $"From {seed.StartingPrice}",
                $"{seed.StartingPrice} 起",
                $"{seed.StartingPrice}부터",
                $"À partir de {seed.StartingPrice}")
        };
    }
}
