using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;

namespace squittal.ScrimPlanetmans.App.Pages.Overlay
{
    public partial class MatchOverlay
    {
        [Parameter]
        [SupplyParameterFromQuery(Name = "report")]
        public bool ShowReport { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "analytic")]
        public bool ShowAnalytics { get; set; } = false;

        [Parameter]
        [SupplyParameterFromQuery(Name = "feed")]
        public bool ShowFeed { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "players")]
        public bool ShowPlayers { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "scoreboard")]
        public bool ShowScoreboard { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "title")]
        public bool ShowTitle { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "legacy")]
        public bool LegacyUi { get; set; } = false;

        [Parameter]
        [SupplyParameterFromQuery(Name = "reportHsr")]
        public bool ShowHsr { get; set; } = true;

        [Parameter]
        [SupplyParameterFromQuery(Name = "compact")]
        public bool CompactView { get; set; } = false;
        private bool IsManualCompactView { get; set; } = false;

        [Parameter]
        [SupplyParameterFromQuery(Name = "currentRound")]
        public bool ShowCurrentRoundOnly { get; set; } = false;

        private bool _objectiveStats = false;
        private bool? _showStatusPanelScores = null;

        private Ruleset _activeRuleset;
        private OverlayStatsDisplayType? _activeStatsDisplayType;


        #region Initialization Methods
        protected override void OnInitialized()
        {
            NavManager.LocationChanged += OnLocationChanged;

            MessageService.RaiseActiveRulesetChangeEvent += OnActiveRulesetChanged;
            MessageService.RaiseRulesetOverlayConfigurationChangeEvent += OnRulesetOverlayConfigurationChanged;
        }

        public void Dispose()
        {
            NavManager.LocationChanged -= OnLocationChanged;

            MessageService.RaiseActiveRulesetChangeEvent -= OnActiveRulesetChanged;
            MessageService.RaiseRulesetOverlayConfigurationChangeEvent -= OnRulesetOverlayConfigurationChanged;
        }

        protected override void OnParametersSet()
        {
            UpdateUriParameters();
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _activeRuleset = await RulesetManager.GetActiveRulesetAsync(false);

                if (!IsManualCompactView)
                {
                    CompactView = _activeRuleset.RulesetOverlayConfiguration.UseCompactLayout;
                }

                _activeStatsDisplayType = _activeRuleset.RulesetOverlayConfiguration.StatsDisplayType;

                _objectiveStats = _activeStatsDisplayType == OverlayStatsDisplayType.InfantryObjective;

                _showStatusPanelScores = _activeRuleset.RulesetOverlayConfiguration.ShowStatusPanelScores;
            }
            catch
            {
                // Ignore
            }
        }

        #endregion Initialization Methods

        #region Event Handling
        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            if (UpdateUriParameters())
            {
                StateHasChanged();
            }
        }

        private void OnActiveRulesetChanged(object sender, ScrimMessageEventArgs<ActiveRulesetChangeMessage> args)
        {
            var ruleset = args.Message.ActiveRuleset;

            var newRulesetCompact = ruleset.RulesetOverlayConfiguration.UseCompactLayout;
            var newRulesetOverlayStatsDisplayType = ruleset.RulesetOverlayConfiguration.StatsDisplayType;
            var newRulesetShowStatusPanelScores = ruleset.RulesetOverlayConfiguration.ShowStatusPanelScores;

            var stateChanged = false;

            if (newRulesetCompact != CompactView && !IsManualCompactView)
            {
                CompactView = newRulesetCompact;
                stateChanged = true;
            }

            if (newRulesetOverlayStatsDisplayType != _activeStatsDisplayType)
            {
                _activeStatsDisplayType = newRulesetOverlayStatsDisplayType;

                _objectiveStats = _activeStatsDisplayType == OverlayStatsDisplayType.InfantryObjective;

                stateChanged = true;
            }

            if (newRulesetShowStatusPanelScores != _showStatusPanelScores)
            {
                _showStatusPanelScores = newRulesetShowStatusPanelScores;
                stateChanged = true;
            }

            if (stateChanged)
            {
                InvokeAsyncStateHasChanged();
            }
        }

        private void OnRulesetOverlayConfigurationChanged(object sender, ScrimMessageEventArgs<RulesetOverlayConfigurationChangeMessage> args)
        {
            var changes = args.Message.ChangedSettings;
            var ruleset = args.Message.Ruleset;
            var configuration = args.Message.OverlayConfiguration;

            if (ruleset.Id != _activeRuleset.Id)
            {
                return;
            }

            var stateChanged = false;

            if (changes.Contains(RulesetOverlayConfigurationChange.UseCompactLayout))
            {
                if (configuration.UseCompactLayout != CompactView && !IsManualCompactView)
                {
                    CompactView = configuration.UseCompactLayout;
                    stateChanged = true;
                }
            }

            if (changes.Contains(RulesetOverlayConfigurationChange.StatsDisplayType))
            {
                if (configuration.StatsDisplayType != _activeStatsDisplayType)
                {
                    _activeStatsDisplayType = configuration.StatsDisplayType;

                    _objectiveStats = _activeStatsDisplayType == OverlayStatsDisplayType.InfantryObjective;

                    stateChanged = true;
                }
            }

            if (changes.Contains(RulesetOverlayConfigurationChange.ShowStatusPanelScores))
            {
                if (configuration.ShowStatusPanelScores != _showStatusPanelScores)
                {
                    _showStatusPanelScores = configuration.ShowStatusPanelScores;

                    stateChanged = true;
                }
            }

            if (stateChanged)
            {
                InvokeAsyncStateHasChanged();
            }
        }

        #endregion Event Handling

        private bool UpdateUriParameters()
        {
            var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
            var stateChanged = false;

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("report", out var qReport))
            {
                if (bool.TryParse(qReport, out var report))
                {
                    if (report != ShowReport)
                    {
                        ShowReport = report;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowReport = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("analytic", out var qAnalytic))
            {
                if (bool.TryParse(qAnalytic, out var analytic))
                {
                    if (analytic != ShowAnalytics)
                    {
                        ShowAnalytics = analytic;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowAnalytics = false;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("feed", out var qFeed))
            {
                if (bool.TryParse(qFeed, out var feed))
                {
                    if (feed != ShowFeed)
                    {
                        ShowFeed = feed;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowFeed = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("players", out var qPlayers))
            {
                if (bool.TryParse(qPlayers, out var players))
                {
                    if (players != ShowPlayers)
                    {
                        ShowPlayers = players;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowPlayers = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("scoreboard", out var qScoreboard))
            {
                if (bool.TryParse(qScoreboard, out var scoreboard))
                {
                    if (scoreboard != ShowScoreboard)
                    {
                        ShowScoreboard = scoreboard;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowScoreboard = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("title", out var qTitle))
            {
                if (bool.TryParse(qTitle, out var title))
                {
                    if (title != ShowTitle)
                    {
                        ShowTitle = title;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowTitle = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("legacy", out var qLegacy))
            {
                if (bool.TryParse(qLegacy, out var legacy))
                {
                    if (legacy != LegacyUi)
                    {
                        LegacyUi = legacy;
                        stateChanged = true;
                    }
                }
                else
                {
                    LegacyUi = false;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("reportHsr", out var qShowHsr))
            {
                if (bool.TryParse(qShowHsr, out var showHsr))
                {
                    if (showHsr != ShowHsr)
                    {
                        ShowHsr = showHsr;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowHsr = true;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("compact", out var qCompact))
            {
                if (bool.TryParse(qCompact, out var compact))
                {
                    if (compact != CompactView)
                    {
                        CompactView = compact;
                        stateChanged = true;
                    }

                    IsManualCompactView = true;
                }
                else if (_activeRuleset != null)
                {
                    CompactView = _activeRuleset.RulesetOverlayConfiguration.UseCompactLayout;
                    IsManualCompactView = false;
                    stateChanged = true;
                }
            }

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("currentRound", out var qCurrentRoundOnly))
            {
                if (bool.TryParse(qCurrentRoundOnly, out var currentRoundOnly))
                {
                    if (currentRoundOnly != ShowCurrentRoundOnly)
                    {
                        ShowCurrentRoundOnly = currentRoundOnly;
                        stateChanged = true;
                    }
                }
                else
                {
                    ShowCurrentRoundOnly = false;
                    stateChanged = true;
                }
            }

            if (ShowAnalytics == true && ShowPlayers == true)
            {
                ShowPlayers = false;
                stateChanged = true;
            }

            return stateChanged;
        }

        private void InvokeAsyncStateHasChanged()
        {
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
    }
}