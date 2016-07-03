﻿using System;
using System.Collections.Generic;
using System.Linq;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.Helpers.Api;
using AcManager.Tools.Managers.Addons;
using AcManager.Tools.Starters;
using AcTools.DataFile;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;

// ReSharper disable RedundantArgumentDefaultValue

namespace AcManager.Tools.Helpers {
    public class SettingsHolder {
        public class PeriodEntry {
            public string DisplayName { get; internal set; }

            public TimeSpan TimeSpan { get; internal set; }
        }

        public class SearchEngineEntry {
            public string DisplayName { get; internal set; }

            public string Value { get; internal set; }

            public string GetUri(string s) {
                if (Content.SearchWithWikipedia) {
                    s = "site:wikipedia.org " + s;
                }

                return string.Format(Value, Uri.EscapeDataString(s).Replace("%20", "+"));
            }
        }

        public class OnlineServerEntry {
            private string _displayName;

            public string DisplayName => _displayName ?? (_displayName = (Id + 1).GetOrdinalReadable());

            public int Id { get; internal set; }
        }

        public class OnlineSettings : NotifyPropertyChanged {
            internal OnlineSettings() { }

            private OnlineServerEntry[] _onlineServers;

            public OnlineServerEntry[] OnlineServers => _onlineServers ??
                    (_onlineServers = Enumerable.Range(0, KunosApiProvider.ServersNumber).Select(x => new OnlineServerEntry { Id = x }).ToArray());

            public OnlineServerEntry OnlineServer {
                get {
                    var id = OnlineServerId;
                    return OnlineServers.FirstOrDefault(x => x.Id == id) ??
                            OnlineServers.FirstOrDefault();
                }
                set { OnlineServerId = value.Id; }
            }

            private int? _onlineServerId;

            public int OnlineServerId {
                get { return _onlineServerId ?? (_onlineServerId = ValuesStorage.GetInt("Settings.OnlineSettings.OnlineServerId", 1)).Value; }
                set {
                    if (Equals(value, _onlineServerId)) return;
                    _onlineServerId = value;
                    ValuesStorage.Set("Settings.OnlineSettings.OnlineServerId", value);
                    OnPropertyChanged();
                }
            }

            private bool? _rememberPasswords;

            public bool RememberPasswords {
                get { return _rememberPasswords ?? (_rememberPasswords = ValuesStorage.GetBool("Settings.OnlineSettings.RememberPasswords", true)).Value; }
                set {
                    if (Equals(value, _rememberPasswords)) return;
                    _rememberPasswords = value;
                    ValuesStorage.Set("Settings.OnlineSettings.RememberPasswords", value);
                    OnPropertyChanged();
                }
            }

            private bool? _useFastServer;

            public bool UseFastServer {
                get {
                    return false;
                    /*return _useFastServer ??
                            (_useFastServer = AppKeyHolder.IsAllRight && ValuesStorage.GetBool("Settings.OnlineSettings.UseFastServer", false)).Value;*/
                }
                set {
                    if (Equals(value, _useFastServer)) return;
                    _useFastServer = value;
                    ValuesStorage.Set("Settings.OnlineSettings.UseFastServer", value);
                    OnPropertyChanged();
                }
            }

            private string _portsEnumeration;

            public string PortsEnumeration {
                get { return _portsEnumeration ?? (_portsEnumeration = ValuesStorage.GetString("Settings.OnlineSettings.PortsEnumeration", "9000-10000")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _portsEnumeration)) return;
                    _portsEnumeration = value;
                    ValuesStorage.Set("Settings.OnlineSettings.PortsEnumeration", value);
                    OnPropertyChanged();
                }
            }

            private string _lanPortsEnumeration;

            public string LanPortsEnumeration {
                get { return _lanPortsEnumeration ?? (_lanPortsEnumeration = ValuesStorage.GetString("Settings.OnlineSettings.LanPortsEnumeration", "9456-9458,9556,9600-9612,9700")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _lanPortsEnumeration)) return;
                    _lanPortsEnumeration = value;
                    ValuesStorage.Set("Settings.OnlineSettings.LanPortsEnumeration", value);
                    OnPropertyChanged();
                }
            }

            private List<string> _ignoredInterfaces;

            public IEnumerable<string> IgnoredInterfaces {
                get { return _ignoredInterfaces ?? (_ignoredInterfaces = ValuesStorage.GetStringList("Settings.OnlineSettings.IgnoredInterfaces").ToList()); }
                set {
                    if (Equals(value, _ignoredInterfaces)) return;
                    _ignoredInterfaces = value.ToList();
                    ValuesStorage.Set("Settings.OnlineSettings.IgnoredInterfaces", value);
                    OnPropertyChanged();
                }
            }
        }

        private static OnlineSettings _online;

        public static OnlineSettings Online => _online ?? (_online = new OnlineSettings());

        public class CommonSettings : NotifyPropertyChanged {
            internal CommonSettings() { }

            private PeriodEntry[] _periodEntries;

            public PeriodEntry[] Periods => _periodEntries ?? (_periodEntries = new[] {
                new PeriodEntry { DisplayName = "Disabled", TimeSpan = TimeSpan.Zero },
                new PeriodEntry { DisplayName = "On startup only", TimeSpan = TimeSpan.MaxValue },
                new PeriodEntry { DisplayName = "Every 30 minutes", TimeSpan = TimeSpan.FromMinutes(30) },
                new PeriodEntry { DisplayName = "Every three hours", TimeSpan = TimeSpan.FromHours(3) },
                new PeriodEntry { DisplayName = "Every ten hours", TimeSpan = TimeSpan.FromHours(6) },
                new PeriodEntry { DisplayName = "Every day", TimeSpan = TimeSpan.FromDays(1) }
            });

            private PeriodEntry _updatePeriod;

            [NotNull]
            public PeriodEntry UpdatePeriod {
                get {
                    return _updatePeriod ?? (_updatePeriod = Periods.FirstOrDefault(x =>
                            x.TimeSpan == ValuesStorage.GetTimeSpan("Settings.CommonSettings.UpdatePeriod", Periods.ElementAt(2).TimeSpan)) ??
                            Periods.First());
                }
                set {
                    if (Equals(value, _updatePeriod)) return;
                    _updatePeriod = value;
                    ValuesStorage.Set("Settings.CommonSettings.UpdatePeriod", value.TimeSpan);
                    OnPropertyChanged();
                }
            }

            private bool? _updateToNontestedVersions;

            public bool UpdateToNontestedVersions {
                get {
                    return _updateToNontestedVersions ??
                            (_updateToNontestedVersions = ValuesStorage.GetBool("Settings.CommonSettings.UpdateToNontestedVersions", false)).Value;
                }
                set {
                    if (Equals(value, _updateToNontestedVersions)) return;
                    _updateToNontestedVersions = value;
                    ValuesStorage.Set("Settings.CommonSettings.UpdateToNontestedVersions", value);
                    OnPropertyChanged();
                }
            }

            private bool? _createStartMenuShortcutIfMissing;

            public bool CreateStartMenuShortcutIfMissing {
                get { return _createStartMenuShortcutIfMissing ?? (_createStartMenuShortcutIfMissing = ValuesStorage.GetBool("Settings.CommonSettings.CreateStartMenuShortcutIfMissing", false)).Value; }
                set {
                    if (Equals(value, _createStartMenuShortcutIfMissing)) return;
                    _createStartMenuShortcutIfMissing = value;
                    ValuesStorage.Set("Settings.CommonSettings.CreateStartMenuShortcutIfMissing", value);
                    OnPropertyChanged();
                }
            }

            private bool? _developerMode;

            public bool DeveloperMode {
                get { return _developerMode ?? (_developerMode = ValuesStorage.GetBool("Settings.CommonSettings.DeveloperMode", false)).Value; }
                set {
                    if (Equals(value, _developerMode)) return;
                    _developerMode = value;
                    ValuesStorage.Set("Settings.CommonSettings.DeveloperMode", value);
                    OnPropertyChanged();
                }
            }

            private bool? _fixResolutionAutomatically;

            public bool FixResolutionAutomatically {
                get { return _fixResolutionAutomatically ?? (_fixResolutionAutomatically = ValuesStorage.GetBool("Settings.CommonSettings.FixResolutionAutomatically", true)).Value; }
                set {
                    if (Equals(value, _fixResolutionAutomatically)) return;
                    _fixResolutionAutomatically = value;
                    ValuesStorage.Set("Settings.CommonSettings.FixResolutionAutomatically", value);
                    OnPropertyChanged();
                }
            }
        }

        private static CommonSettings _common;

        public static CommonSettings Common => _common ?? (_common = new CommonSettings());

        public class DriveSettings : NotifyPropertyChanged {
            internal DriveSettings() {
                if (PlayerName == null) {
                    PlayerName = new IniFile(FileUtils.GetRaceIniFilename())["CAR_0"].Get("DRIVER_NAME") ?? "Player";
                    PlayerNameOnline = PlayerName;
                }

                if (PlayerNationality == null) {
                    PlayerNationality = new IniFile(FileUtils.GetRaceIniFilename())["CAR_0"].Get("NATIONALITY");
                }
            }

            public class StarterType : NotifyPropertyChanged, IWithId {
                internal readonly string RequiredAddonId;

                public string Id { get; }

                public string DisplayName { get; }

                public bool IsAvailable => RequiredAddonId == null || AppAddonsManager.Instance.IsAddonEnabled(RequiredAddonId);

                internal StarterType(string displayName, string requiredAddonId = null) {
                    Id = displayName;
                    DisplayName = displayName;

                    RequiredAddonId = requiredAddonId;
                }
            }

            public static readonly StarterType TrickyStarterType = new StarterType("Tricky");
            public static readonly StarterType StarterPlusType = new StarterType("Starter+", StarterPlus.AddonId);
            public static readonly StarterType SseStarterType = new StarterType("SSE", SseStarter.AddonId);
            public static readonly StarterType NaiveStarterType = new StarterType("Naive");

            private StarterType _selectedStarterType;

            [NotNull]
            public StarterType SelectedStarterType {
                get {
                    return _selectedStarterType ??
                            (_selectedStarterType = StarterTypes.GetByIdOrDefault(ValuesStorage.GetString("Settings.DriveSettings.SelectedStarterType")) ??
                                    StarterTypes.First());
                }
                set {
                    if (Equals(value, _selectedStarterType)) return;
                    _selectedStarterType = value;
                    ValuesStorage.Set("Settings.DriveSettings.SelectedStarterType", value.Id);
                    OnPropertyChanged();
                }
            }

            private StarterType[] _starterTypes;

            public StarterType[] StarterTypes => _starterTypes ?? (_starterTypes = new[] {
                TrickyStarterType, StarterPlusType, SseStarterType, NaiveStarterType
            });

            private string _preCommand;

            public string PreCommand {
                get { return _preCommand ?? (_preCommand = ValuesStorage.GetString("Settings.DriveSettings.PreCommand", "")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _preCommand)) return;
                    _preCommand = value;
                    ValuesStorage.Set("Settings.DriveSettings.PreCommand", value);
                    OnPropertyChanged();
                }
            }

            private string _postCommand;

            public string PostCommand {
                get { return _postCommand ?? (_postCommand = ValuesStorage.GetString("Settings.DriveSettings.PostCommand", "")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _postCommand)) return;
                    _postCommand = value;
                    ValuesStorage.Set("Settings.DriveSettings.PostCommand", value);
                    OnPropertyChanged();
                }
            }

            private string _preReplayCommand;

            public string PreReplayCommand {
                get { return _preReplayCommand ?? (_preReplayCommand = ValuesStorage.GetString("Settings.DriveSettings.PreReplayCommand", "")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _preReplayCommand)) return;
                    _preReplayCommand = value;
                    ValuesStorage.Set("Settings.DriveSettings.PreReplayCommand", value);
                    OnPropertyChanged();
                }
            }

            private string _postReplayCommand;

            public string PostReplayCommand {
                get { return _postReplayCommand ?? (_postReplayCommand = ValuesStorage.GetString("Settings.DriveSettings.PostReplayCommand", "")); }
                set {
                    value = value.Trim();
                    if (Equals(value, _postReplayCommand)) return;
                    _postReplayCommand = value;
                    ValuesStorage.Set("Settings.DriveSettings.PostReplayCommand", value);
                    OnPropertyChanged();
                }
            }

            private bool? _immediateStart;

            public bool ImmediateStart {
                get { return _immediateStart ?? (_immediateStart = ValuesStorage.GetBool("Settings.DriveSettings.ImmediateStart", false)).Value; }
                set {
                    if (Equals(value, _immediateStart)) return;
                    _immediateStart = value;
                    ValuesStorage.Set("Settings.DriveSettings.ImmediateStart", value);
                    OnPropertyChanged();
                }
            }

            private bool? _skipPracticeResults;

            public bool SkipPracticeResults {
                get { return _skipPracticeResults ?? (_skipPracticeResults = ValuesStorage.GetBool("Settings.DriveSettings.SkipPracticeResults", false)).Value; }
                set {
                    if (Equals(value, _skipPracticeResults)) return;
                    _skipPracticeResults = value;
                    ValuesStorage.Set("Settings.DriveSettings.SkipPracticeResults", value);
                    OnPropertyChanged();
                }
            }

            private bool? _tryToLoadReplays;

            public bool TryToLoadReplays {
                get { return _tryToLoadReplays ?? (_tryToLoadReplays = ValuesStorage.GetBool("Settings.DriveSettings.TryToLoadReplays", true)).Value; }
                set {
                    if (Equals(value, _tryToLoadReplays)) return;
                    _tryToLoadReplays = value;
                    ValuesStorage.Set("Settings.DriveSettings.TryToLoadReplays", value);
                    OnPropertyChanged();
                }
            }

            private bool? _autoSaveReplays;

            public bool AutoSaveReplays {
                get { return _autoSaveReplays ?? (_autoSaveReplays = ValuesStorage.GetBool("Settings.DriveSettings.AutoSaveReplays", false)).Value; }
                set {
                    if (Equals(value, _autoSaveReplays)) return;
                    _autoSaveReplays = value;
                    ValuesStorage.Set("Settings.DriveSettings.AutoSaveReplays", value);
                    OnPropertyChanged();
                }
            }

            private bool? _autoAddReplaysExtension;

            public bool AutoAddReplaysExtension {
                get {
                    return _autoAddReplaysExtension ??
                            (_autoAddReplaysExtension = ValuesStorage.GetBool("Settings.DriveSettings.AutoAddReplaysExtension", true)).Value;
                }
                set {
                    if (Equals(value, _autoAddReplaysExtension)) return;
                    _autoAddReplaysExtension = value;
                    ValuesStorage.Set("Settings.DriveSettings.AutoAddReplaysExtension", value);
                    OnPropertyChanged();
                }
            }

            public string DefaultReplaysNameFormat => "_autosave_{car.id}_{track.id}_{date_ac}.acreplay";

            private string _replaysNameFormat;

            public string ReplaysNameFormat {
                get { return _replaysNameFormat ?? (_replaysNameFormat = ValuesStorage.GetString("Settings.DriveSettings.ReplaysNameFormat", DefaultReplaysNameFormat)); }
                set {
                    value = value?.Trim();
                    if (Equals(value, _replaysNameFormat)) return;
                    _replaysNameFormat = value;
                    ValuesStorage.Set("Settings.DriveSettings.ReplaysNameFormat", value);
                    OnPropertyChanged();
                }
            }

            private bool? _use32BitVersion;

            public bool Use32BitVersion {
                get { return _use32BitVersion ?? (_use32BitVersion = ValuesStorage.GetBool("Settings.DriveSettings.Use32BitVersion", false)).Value; }
                set {
                    if (Equals(value, _use32BitVersion)) return;
                    _use32BitVersion = value;
                    ValuesStorage.Set("Settings.DriveSettings.Use32BitVersion", value);
                    OnPropertyChanged();
                }
            }

            private string _playerName;

            public string PlayerName {
                get { return _playerName ?? (_playerName = ValuesStorage.GetString("Settings.DriveSettings.PlayerName", null)); }
                set {
                    value = value?.Trim();
                    if (Equals(value, _playerName)) return;
                    _playerName = value;
                    ValuesStorage.Set("Settings.DriveSettings.PlayerName", value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayerNameOnline));
                }
            }

            private string _playerNationality;

            public string PlayerNationality {
                get { return _playerNationality ?? (_playerNationality = ValuesStorage.GetString("Settings.DriveSettings.PlayerNationality", null)); }
                set {
                    value = value?.Trim();
                    if (Equals(value, _playerNationality)) return;
                    _playerNationality = value;
                    ValuesStorage.Set("Settings.DriveSettings.PlayerNationality", value);
                    OnPropertyChanged();
                }
            }

            private bool? _differentPlayerNameOnline;

            public bool DifferentPlayerNameOnline {
                get {
                    return _differentPlayerNameOnline ??
                            (_differentPlayerNameOnline = ValuesStorage.GetBool("Settings.DriveSettings.DifferentPlayerNameOnline", false)).Value;
                }
                set {
                    if (Equals(value, _differentPlayerNameOnline)) return;
                    _differentPlayerNameOnline = value;
                    ValuesStorage.Set("Settings.DriveSettings.DifferentPlayerNameOnline", value);
                    OnPropertyChanged();
                }
            }

            private string _playerNameOnline;

            public string PlayerNameOnline {
                get { return _playerNameOnline ?? (_playerNameOnline = ValuesStorage.GetString("Settings.DriveSettings.PlayerNameOnline", PlayerName)); }
                set {
                    value = value.Trim();
                    if (Equals(value, _playerNameOnline)) return;
                    _playerNameOnline = value;
                    ValuesStorage.Set("Settings.DriveSettings.PlayerNameOnline", value);
                    OnPropertyChanged();
                }
            }

            private bool? _quickDriveExpandBounds;

            public bool QuickDriveExpandBounds {
                get { return _quickDriveExpandBounds ?? (_quickDriveExpandBounds = ValuesStorage.GetBool("Settings.DriveSettings.ExpandBounds", false)).Value; }
                set {
                    if (Equals(value, _quickDriveExpandBounds)) return;
                    _quickDriveExpandBounds = value;
                    ValuesStorage.Set("Settings.DriveSettings.ExpandBounds", value);
                    OnPropertyChanged();
                }
            }

            private bool? _kunosCareerUserAiLevel;

            public bool KunosCareerUserAiLevel {
                get { return _kunosCareerUserAiLevel ?? (_kunosCareerUserAiLevel = ValuesStorage.GetBool("Settings.DriveSettings.KunosCareerUserAiLevel", false)).Value; }
                set {
                    if (Equals(value, _kunosCareerUserAiLevel)) return;
                    _kunosCareerUserAiLevel = value;
                    ValuesStorage.Set("Settings.DriveSettings.KunosCareerUserAiLevel", value);
                    OnPropertyChanged();
                }
            }

            private bool? _kunosCareerUserSkin;

            public bool KunosCareerUserSkin {
                get { return _kunosCareerUserSkin ?? (_kunosCareerUserSkin = ValuesStorage.GetBool("Settings.DriveSettings.KunosCareerUserSkin", true)).Value; }
                set {
                    if (Equals(value, _kunosCareerUserSkin)) return;
                    _kunosCareerUserSkin = value;
                    ValuesStorage.Set("Settings.DriveSettings.KunosCareerUserSkin", value);
                    OnPropertyChanged();
                }
            }

            private bool? _quickSwitches;

            public bool QuickSwitches {
                get { return _quickSwitches ?? (_quickSwitches = ValuesStorage.GetBool("Settings.DriveSettings.QuickSwitches", true)).Value; }
                set {
                    if (Equals(value, _quickSwitches)) return;
                    _quickSwitches = value;
                    ValuesStorage.Set("Settings.DriveSettings.QuickSwitches", value);
                    OnPropertyChanged();
                }
            }

            private string[] _quickSwitchesList;

            public string[] QuickSwitchesList {
                get {
                    return _quickSwitchesList ??
                            (_quickSwitchesList = ValuesStorage.GetStringList("Settings.DriveSettings.QuickSwitchesList", new[] {
                                "WidgetExposure",
                                "WidgetUiPresets",
                                "WidgetHideDriveArms",
                                "WidgetHideSteeringWheel"
                            }).ToArray());
                }
                set {
                    if (Equals(value, _quickSwitchesList)) return;
                    ValuesStorage.Set("Settings.DriveSettings.QuickSwitchesList", value);
                    _quickSwitchesList = value;
                    OnPropertyChanged();
                }
            }
        }

        private static DriveSettings _drive;

        public static DriveSettings Drive => _drive ?? (_drive = new DriveSettings());

        public class ContentSettings : NotifyPropertyChanged {
            internal ContentSettings() { }

            private int? _loadingConcurrency;

            public int LoadingConcurrency {
                get {
                    return _loadingConcurrency ??
                            (_loadingConcurrency =
                                    ValuesStorage.GetInt("Settings.ContentSettings.LoadingConcurrency", BaseAcManagerNew.OptionAcObjectsLoadingConcurrency))
                                    .Value;
                }
                set {
                    value = value < 1 ? 1 : value;
                    if (Equals(value, _loadingConcurrency)) return;
                    _loadingConcurrency = value;
                    ValuesStorage.Set("Settings.ContentSettings.LoadingConcurrency", value);
                    OnPropertyChanged();
                }
            }

            private bool? _changeBrandIconAutomatically;

            public bool ChangeBrandIconAutomatically {
                get {
                    return _changeBrandIconAutomatically ??
                            (_changeBrandIconAutomatically = ValuesStorage.GetBool("Settings.ContentSettings.ChangeBrandIconAutomatically", true)).Value;
                }
                set {
                    if (Equals(value, _changeBrandIconAutomatically)) return;
                    _changeBrandIconAutomatically = value;
                    ValuesStorage.Set("Settings.ContentSettings.ChangeBrandIconAutomatically", value);
                    OnPropertyChanged();
                }
            }

            private bool? _downloadShowroomPreviews;

            public bool DownloadShowroomPreviews {
                get {
                    return _downloadShowroomPreviews ??
                            (_downloadShowroomPreviews = ValuesStorage.GetBool("Settings.ContentSettings.DownloadShowroomPreviews", true)).Value;
                }
                set {
                    if (Equals(value, _downloadShowroomPreviews)) return;
                    _downloadShowroomPreviews = value;
                    ValuesStorage.Set("Settings.ContentSettings.DownloadShowroomPreviews", value);
                    OnPropertyChanged();
                }
            }

            private bool? _scrollAutomatically;

            public bool ScrollAutomatically {
                get { return _scrollAutomatically ?? (_scrollAutomatically = ValuesStorage.GetBool("Settings.ContentSettings.ScrollAutomatically", true)).Value; }
                set {
                    if (Equals(value, _scrollAutomatically)) return;
                    _scrollAutomatically = value;
                    ValuesStorage.Set("Settings.ContentSettings.ScrollAutomatically", value);
                    OnPropertyChanged();
                }
            }

            private string _fontIconCharacter;

            public string FontIconCharacter {
                get { return _fontIconCharacter ?? (_fontIconCharacter = ValuesStorage.GetString("Settings.ContentSettings.FontIconCharacter", "A")); }
                set {
                    value = value?.Trim().Substring(0, 1);
                    if (Equals(value, _fontIconCharacter)) return;
                    _fontIconCharacter = value;
                    ValuesStorage.Set("Settings.ContentSettings.FontIconCharacter", value);
                    OnPropertyChanged();
                }
            }

            private PeriodEntry[] _periodEntries;

            public PeriodEntry[] NewContentPeriods => _periodEntries ?? (_periodEntries = new[] {
                new PeriodEntry { DisplayName = "Disabled", TimeSpan = TimeSpan.Zero },
                new PeriodEntry { DisplayName = "One day", TimeSpan = TimeSpan.FromDays(1) },
                new PeriodEntry { DisplayName = "Three days", TimeSpan = TimeSpan.FromDays(3) },
                new PeriodEntry { DisplayName = "Week", TimeSpan = TimeSpan.FromDays(7) },
                new PeriodEntry { DisplayName = "Two weeks", TimeSpan = TimeSpan.FromDays(14) },
                new PeriodEntry { DisplayName = "Month", TimeSpan = TimeSpan.FromDays(30) }
            });

            private PeriodEntry _newContentPeriod;

            public PeriodEntry NewContentPeriod {
                get {
                    return _newContentPeriod ?? (_newContentPeriod = NewContentPeriods.FirstOrDefault(x =>
                            x.TimeSpan == ValuesStorage.GetTimeSpan("Settings.ContentSettings.NewContentPeriod", NewContentPeriods.ElementAt(4).TimeSpan)) ??
                            NewContentPeriods.First());
                }
                set {
                    if (Equals(value, _newContentPeriod)) return;
                    _newContentPeriod = value;
                    ValuesStorage.Set("Settings.ContentSettings.NewContentPeriod", value.TimeSpan);
                    OnPropertyChanged();
                }
            }

            private SearchEngineEntry[] _searchEngines;

            public SearchEngineEntry[] SearchEngines => _searchEngines ?? (_searchEngines = new[] {
                new SearchEngineEntry { DisplayName = "DuckDuckGo", Value = "https://duckduckgo.com/?q={0}&ia=web" },
                new SearchEngineEntry { DisplayName = "Bing", Value = "http://www.bing.com/search?q={0}" },
                new SearchEngineEntry { DisplayName = "Google", Value = "https://www.google.ru/search?q={0}&ie=UTF-8" },
                new SearchEngineEntry { DisplayName = "Yandex", Value = "https://yandex.ru/search/?text={0}" },
                new SearchEngineEntry { DisplayName = "Baidu", Value = "http://www.baidu.com/s?ie=utf-8&wd={0}" }
            });

            private SearchEngineEntry _searchEngine;

            public SearchEngineEntry SearchEngine {
                get {
                    return _searchEngine ?? (_searchEngine = SearchEngines.FirstOrDefault(x =>
                            x.DisplayName == ValuesStorage.GetString("Settings.ContentSettings.SearchEngine")) ??
                            SearchEngines.First());
                }
                set {
                    if (Equals(value, _searchEngine)) return;
                    _searchEngine = value;
                    ValuesStorage.Set("Settings.ContentSettings.SearchEngine", value.DisplayName);
                    OnPropertyChanged();
                }
            }

            private bool? _searchWithWikipedia;

            public bool SearchWithWikipedia {
                get { return _searchWithWikipedia ?? (_searchWithWikipedia = ValuesStorage.GetBool("Settings.ContentSettings.SearchWithWikipedia", true)).Value; }
                set {
                    if (Equals(value, _searchWithWikipedia)) return;
                    _searchWithWikipedia = value;
                    ValuesStorage.Set("Settings.ContentSettings.SearchWithWikipedia", value);
                    OnPropertyChanged();
                }
            }
        }

        private static ContentSettings _content;

        public static ContentSettings Content => _content ?? (_content = new ContentSettings());

        public class CustomShowroomSettings : NotifyPropertyChanged {
            internal CustomShowroomSettings() { }

            public string[] ShowroomTypes { get; } = { "Custom", "Lite" };

            public string ShowroomType {
                get { return LiteByDefault ? ShowroomTypes[1] : ShowroomTypes[0]; }
                set { LiteByDefault = value == ShowroomTypes[1]; }
            }

            private bool? _liteByDefault;

            public bool LiteByDefault {
                get { return _liteByDefault ?? (_liteByDefault = ValuesStorage.GetBool("Settings.CustomShowroomSettings.LiteByDefault", true)).Value; }
                set {
                    if (Equals(value, _liteByDefault)) return;
                    _liteByDefault = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.LiteByDefault", value);
                    OnPropertyChanged();
                }
            }

            private bool? _liteUseFxaa;

            public bool LiteUseFxaa {
                get { return _liteUseFxaa ?? (_liteUseFxaa = ValuesStorage.GetBool("Settings.CustomShowroomSettings.LiteUseFxaa", true)).Value; }
                set {
                    if (Equals(value, _liteUseFxaa)) return;
                    _liteUseFxaa = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.LiteUseFxaa", value);
                    OnPropertyChanged();
                }
            }

            private bool? _liteUseMsaa;

            public bool LiteUseMsaa {
                get { return _liteUseMsaa ?? (_liteUseMsaa = ValuesStorage.GetBool("Settings.CustomShowroomSettings.LiteUseMsaa", false)).Value; }
                set {
                    if (Equals(value, _liteUseMsaa)) return;
                    _liteUseMsaa = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.LiteUseMsaa", value);
                    OnPropertyChanged();
                }
            }

            private bool? _liteUseBloom;

            public bool LiteUseBloom {
                get { return _liteUseBloom ?? (_liteUseBloom = ValuesStorage.GetBool("Settings.CustomShowroomSettings.LiteUseBloom", true)).Value; }
                set {
                    if (Equals(value, _liteUseBloom)) return;
                    _liteUseBloom = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.LiteUseBloom", value);
                    OnPropertyChanged();
                }
            }

            private string _showroomId;

            [CanBeNull]
            public string ShowroomId {
                get { return _showroomId ?? (_showroomId = ValuesStorage.GetString("Settings.CustomShowroomSettings.ShowroomId", "showroom")); }
                set {
                    value = value?.Trim();
                    if (Equals(value, _showroomId)) return;
                    _showroomId = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.ShowroomId", value);
                    OnPropertyChanged();
                }
            }

            private bool? _useFxaa;

            public bool UseFxaa {
                get { return _useFxaa ?? (_useFxaa = ValuesStorage.GetBool("Settings.CustomShowroomSettings.UseFxaa", true)).Value; }
                set {
                    if (Equals(value, _useFxaa)) return;
                    _useFxaa = value;
                    ValuesStorage.Set("Settings.CustomShowroomSettings.UseFxaa", value);
                    OnPropertyChanged();
                }
            }
        }

        private static CustomShowroomSettings _customShowroom;

        public static CustomShowroomSettings CustomShowroom => _customShowroom ?? (_customShowroom = new CustomShowroomSettings());

        public class SharingSettings : NotifyPropertyChanged {
            internal SharingSettings() { }

            private bool? _customIds;

            public bool CustomIds {
                get { return _customIds ?? (_customIds = ValuesStorage.GetBool("Settings.SharingSettings.CustomIds", false)).Value; }
                set {
                    if (Equals(value, _customIds)) return;
                    _customIds = value;
                    ValuesStorage.Set("Settings.SharingSettings.CustomIds", value);
                    OnPropertyChanged();
                }
            }

            private bool? _verifyBeforeSharing;

            public bool VerifyBeforeSharing {
                get { return _verifyBeforeSharing ?? (_verifyBeforeSharing = ValuesStorage.GetBool("Settings.SharingSettings.VerifyBeforeSharing", true)).Value; }
                set {
                    if (Equals(value, _verifyBeforeSharing)) return;
                    _verifyBeforeSharing = value;
                    ValuesStorage.Set("Settings.SharingSettings.VerifyBeforeSharing", value);
                    OnPropertyChanged();
                }
            }

            private bool? _copyLinkToClipboard;

            public bool CopyLinkToClipboard {
                get { return _copyLinkToClipboard ?? (_copyLinkToClipboard = ValuesStorage.GetBool("Settings.SharingSettings.CopyLinkToClipboard", true)).Value; }
                set {
                    if (Equals(value, _copyLinkToClipboard)) return;
                    _copyLinkToClipboard = value;
                    ValuesStorage.Set("Settings.SharingSettings.CopyLinkToClipboard", value);
                    OnPropertyChanged();
                }
            }

            private bool? _shareAnonymously;

            public bool ShareAnonymously {
                get { return _shareAnonymously ?? (_shareAnonymously = ValuesStorage.GetBool("Settings.SharingSettings.ShareAnonymously", false)).Value; }
                set {
                    if (Equals(value, _shareAnonymously)) return;
                    _shareAnonymously = value;
                    ValuesStorage.Set("Settings.SharingSettings.ShareAnonymously", value);
                    OnPropertyChanged();
                }
            }

            private bool? _shareWithoutName;

            public bool ShareWithoutName {
                get { return _shareWithoutName ?? (_shareWithoutName = ValuesStorage.GetBool("Settings.SharingSettings.ShareWithoutName", false)).Value; }
                set {
                    if (Equals(value, _shareWithoutName)) return;
                    _shareWithoutName = value;
                    ValuesStorage.Set("Settings.SharingSettings.ShareWithoutName", value);
                    OnPropertyChanged();
                }
            }

            private string _sharingName;

            [CanBeNull]
            public string SharingName {
                get { return _sharingName ?? (_sharingName = ValuesStorage.GetString("Settings.SharingSettings.SharingName", null) ?? Drive.PlayerNameOnline); }
                set {
                    value = value?.Trim();

                    if (value?.Length > 60) {
                        value = value.Substring(0, 60);
                    }

                    if (Equals(value, _sharingName)) return;
                    _sharingName = value;
                    ValuesStorage.Set("Settings.SharingSettings.SharingName", value);
                    OnPropertyChanged();
                }
            }
        }

        private static SharingSettings _sharing;

        public static SharingSettings Sharing => _sharing ?? (_sharing = new SharingSettings());


        public class LiveTimingSettings : NotifyPropertyChanged {
            internal LiveTimingSettings() {}

            private bool? _rsrEnabled;

            public bool RsrEnabled {
                get { return _rsrEnabled ?? (_rsrEnabled = ValuesStorage.GetBool("Settings.RsrSettings.RsrEnabled", true)).Value; }
                set {
                    if (Equals(value, _rsrEnabled)) return;
                    _rsrEnabled = value;
                    ValuesStorage.Set("Settings.RsrSettings.RsrEnabled", value);
                    OnPropertyChanged();
                }
            }

            private bool? _rsrCustomStyle;

            public bool RsrCustomStyle {
                get { return _rsrCustomStyle ?? (_rsrCustomStyle = ValuesStorage.GetBool("Settings.RsrSettings.RsrCustomStyle", true)).Value; }
                set {
                    if (Equals(value, _rsrCustomStyle)) return;
                    _rsrCustomStyle = value;
                    ValuesStorage.Set("Settings.RsrSettings.RsrCustomStyle", value);
                    OnPropertyChanged();
                }
            }

            private bool? _rsrDisableAppAutomatically;

            public bool RsrDisableAppAutomatically {
                get {
                    return _rsrDisableAppAutomatically ??
                            (_rsrDisableAppAutomatically = ValuesStorage.GetBool("Settings.LiveTimingSettings.RsrDisableAppAutomatically", false)).Value;
                }
                set {
                    if (Equals(value, _rsrDisableAppAutomatically)) return;
                    _rsrDisableAppAutomatically = value;
                    ValuesStorage.Set("Settings.LiveTimingSettings.RsrDisableAppAutomatically", value);
                    OnPropertyChanged();
                }
            }

            private bool? _rsrDifferentPlayerName;

            public bool RsrDifferentPlayerName {
                get {
                    return _rsrDifferentPlayerName ??
                            (_rsrDifferentPlayerName = ValuesStorage.GetBool("Settings.LiveTimingSettings.RsrDifferentPlayerName", false)).Value;
                }
                set {
                    if (Equals(value, _rsrDifferentPlayerName)) return;
                    _rsrDifferentPlayerName = value;
                    ValuesStorage.Set("Settings.LiveTimingSettings.RsrDifferentPlayerName", value);
                    OnPropertyChanged();
                }
            }

            private string _rsrPlayerName;

            public string RsrPlayerName {
                get { return _rsrPlayerName ?? (_rsrPlayerName = ValuesStorage.GetString("Settings.LiveTimingSettings.RsrPlayerName", Drive.PlayerName)); }
                set {
                    value = value.Trim();
                    if (Equals(value, _rsrPlayerName)) return;
                    _rsrPlayerName = value;
                    ValuesStorage.Set("Settings.LiveTimingSettings.RsrPlayerName", value);
                    OnPropertyChanged();
                }
            }
        }

        private static LiveTimingSettings _liveTiming;

        public static LiveTimingSettings LiveTiming => _liveTiming ?? (_liveTiming = new LiveTimingSettings());
    }
}
