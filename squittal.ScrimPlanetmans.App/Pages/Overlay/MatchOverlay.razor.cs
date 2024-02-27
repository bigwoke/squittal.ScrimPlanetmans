using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;

namespace squittal.ScrimPlanetmans.App.Pages.Overlay
{
    public partial class MatchOverlay
    {
        public const bool DefaultShowReport = true;
        public const bool DefaultShowAnalytics = false;
        public const bool DefaultShowFeed = true;
        public const bool DefaultShowPlayers = true;
        public const bool DefaultShowScoreboard = true;
        public const bool DefaultShowTitle = true;
        public const bool DefaultLegacyUi = false;
        public const bool DefaultShowHsr = true;
        public const bool DefaultShowCurrentRoundOnly = false;
        public const OverlayStatsDisplayType DefaultStatsType = OverlayStatsDisplayType.InfantryScores;

        private Ruleset _activeRuleset;
        private bool? _useCompactLayout = null;
        private bool? _showStatusPanelScores = null;
        private OverlayStatsDisplayType? _activeStatsDisplayType;

        [Parameter]
        [QueryBoolParameter("report", DefaultShowReport)]
        public bool ShowReport { get; set; } = DefaultShowReport;

        [Parameter]
        [QueryBoolParameter("analytic", DefaultShowAnalytics)]
        public bool ShowAnalytics { get; set; } = DefaultShowAnalytics;

        [Parameter]
        [QueryBoolParameter("feed", DefaultShowFeed)]
        public bool ShowFeed { get; set; } = DefaultShowFeed;

        [Parameter]
        [QueryBoolParameter("players", DefaultShowPlayers)]
        public bool ShowPlayers { get; set; } = DefaultShowPlayers;

        [Parameter]
        [QueryBoolParameter("scoreboard", DefaultShowScoreboard)]
        public bool ShowScoreboard { get; set; } = DefaultShowScoreboard;

        [Parameter]
        [QueryBoolParameter("title", DefaultShowTitle)]
        public bool ShowTitle { get; set; } = DefaultShowTitle;

        [Parameter]
        [QueryBoolParameter("legacy", DefaultLegacyUi)]
        public bool LegacyUi { get; set; } = DefaultLegacyUi;

        [Parameter]
        [QueryBoolParameter("reportHsr", DefaultShowHsr)]
        public bool ShowHsr { get; set; } = DefaultShowHsr;

        [Parameter]
        [QueryBoolParameter("currentRound", DefaultShowCurrentRoundOnly)]
        public bool ShowCurrentRoundOnly { get; set; } = DefaultShowCurrentRoundOnly;

        [Parameter]
        [QueryBoolParameter("compact")]
        public bool? CompactLayout { get; set; }
        private bool IsManualCompactLayout => CompactLayout is not null;


        #region Initialization Methods
        protected override async Task OnInitializedAsync()
        {
            MessageService.RaiseActiveRulesetChangeEvent += OnActiveRulesetChanged;
            MessageService.RaiseRulesetOverlayConfigurationChangeEvent += OnRulesetOverlayConfigurationChanged;

            _activeRuleset = await RulesetManager.GetActiveRulesetAsync(false);

            _useCompactLayout = CompactLayout ?? _activeRuleset.RulesetOverlayConfiguration.UseCompactLayout;
            _showStatusPanelScores = _activeRuleset.RulesetOverlayConfiguration.ShowStatusPanelScores;
            _activeStatsDisplayType = _activeRuleset.RulesetOverlayConfiguration.StatsDisplayType;
        }

        protected override void OnParametersSet()
        {
            Uri uri = NavManager.ToAbsoluteUri(NavManager.Uri);

            // Get all properties marked with [QueryParameter]
            IEnumerable<PropertyInfo> props = GetType().GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(QueryBoolParameterAttribute)));

            // Parse query parameter in attribute and set value to property
            foreach (PropertyInfo prop in props)
            {
                QueryBoolParameterAttribute attr = prop.GetCustomAttribute<QueryBoolParameterAttribute>();

                if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(attr.Parameter, out StringValues val))
                {
                    if (bool.TryParse(val, out bool parsed))
                    {
                        prop.SetValue(this, parsed);
                    }
                    else
                    {
                        // If property is nullable, do not assign default
                        if (prop.PropertyType != typeof(Nullable))
                        {
                            prop.SetValue(this, attr.DefaultValue);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            MessageService.RaiseActiveRulesetChangeEvent -= OnActiveRulesetChanged;
            MessageService.RaiseRulesetOverlayConfigurationChangeEvent -= OnRulesetOverlayConfigurationChanged;
        }
        #endregion Initialization Methods

        #region Event Handling
        private void OnActiveRulesetChanged(object sender, ScrimMessageEventArgs<ActiveRulesetChangeMessage> args)
        {
            RulesetOverlayConfiguration configuration = args.Message.ActiveRuleset.RulesetOverlayConfiguration;

            if (TryUpdateFromRuleset(configuration))
            {
                InvokeAsync(StateHasChanged);
            }
        }

        private void OnRulesetOverlayConfigurationChanged(object sender, ScrimMessageEventArgs<RulesetOverlayConfigurationChangeMessage> args)
        {
            if (args.Message.Ruleset.Id != _activeRuleset.Id)
            {
                return;
            }

            // It doesn't really matter which were changed, if they are not in sync with this object they must change
            if (args.Message.ChangedSettings.Count > 0)
            {
                if (TryUpdateFromRuleset(args.Message.OverlayConfiguration))
                {
                    InvokeAsync(StateHasChanged);
                }
            }
        }
        #endregion Event Handling

        private bool TryUpdateFromRuleset(RulesetOverlayConfiguration configuration)
        {
            bool stateChanged = false;

            if (configuration.UseCompactLayout != _useCompactLayout && !IsManualCompactLayout)
            {
                _useCompactLayout = configuration.UseCompactLayout;
                stateChanged = true;
            }

            if (configuration.StatsDisplayType != _activeStatsDisplayType)
            {
                _activeStatsDisplayType = configuration.StatsDisplayType;
                stateChanged = true;
            }

            if (configuration.ShowStatusPanelScores != _showStatusPanelScores)
            {
                _showStatusPanelScores = configuration.ShowStatusPanelScores;
                stateChanged = true;
            }

            return stateChanged;
        }
    }
}