using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using squittal.ScrimPlanetmans.App.Hubs;
using squittal.ScrimPlanetmans.Models;
using squittal.ScrimPlanetmans.Models.Planetside;
using squittal.ScrimPlanetmans.Models.ScrimEngine;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;

namespace squittal.ScrimPlanetmans.App.Pages.Admin.MatchSetup
{
    public sealed partial class MatchSetup
    {
        private string _errorBannerMessage;
        private string _matchId = string.Empty;

        #region Ruleset Select List Variables
        private IEnumerable<Ruleset> _rulesets;
        private Ruleset _ruleset;
        #endregion

        #region Facility & World Select List Variables
        private const string NoFacilityIdValue = "0";

        private IOrderedEnumerable<MapRegion> _mapRegions;
        private IOrderedEnumerable<World> _worlds;
        private IEnumerable<Zone> _zones;
        #endregion

        #region State Control Variables
        private bool _isLoading = true;
        private bool _isLoadingRulesets = true;
        private bool _isLoadingActiveRulesetConfig = true;
        private bool _isChangingRuleset = false;

        private bool _isClearingMatch = false;
        private bool _isEndingRound = false;
        private bool _isResettingRound = false;
        private bool _isStartingRound = false;

        private bool _isDeleteDataEnabled = false;
        private bool _isStreamServiceEnabled = false;

        private int _currentRound = 0;
        private MatchState _matchState = MatchState.Uninitialized;
        #endregion

        protected override async Task OnInitializedAsync()
        {
            MessageService.RaiseMatchStateUpdateEvent -= ReceiveMatchStateUpdateMessageEvent;
            MessageService.RaiseMatchConfigurationUpdateEvent -= ReceiveMatchConfigurationUpdateMessageEvent;
            MessageService.RaiseActiveRulesetChangeEvent -= ReceiveActiveRulesetChangeEvent;
            MessageService.RaiseRulesetSettingChangeEvent -= ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage -= ReceiveMatchControlSignalReceiptMessage;

            MessageService.RaiseMatchStateUpdateEvent += ReceiveMatchStateUpdateMessageEvent;
            MessageService.RaiseMatchConfigurationUpdateEvent += ReceiveMatchConfigurationUpdateMessageEvent;
            MessageService.RaiseActiveRulesetChangeEvent += ReceiveActiveRulesetChangeEvent;
            MessageService.RaiseRulesetSettingChangeEvent += ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage += ReceiveMatchControlSignalReceiptMessage;

            _currentRound = ScrimMatchEngine.GetCurrentRound();
            _matchState = ScrimMatchEngine.GetMatchState();
            _matchId = ScrimMatchEngine.GetMatchId();

            List<Task> taskList = new()
            {
                GetCensusStreamStatusAsync(),
                LoadRulesetsAsync(),
                LoadActiveRulesetAsync()
            };
            await Task.WhenAll(taskList);

            _worlds = (await WorldService.GetAllWorldsAsync()).OrderBy(world => world.Name);
            _zones = (await ZoneService.GetAllZonesAsync()).Where(z => _mapRegions.Any(r => r.ZoneId == z.Id));

            _isLoading = false;
        }

        public void Dispose()
        {
            MessageService.RaiseMatchStateUpdateEvent -= ReceiveMatchStateUpdateMessageEvent;
            MessageService.RaiseMatchConfigurationUpdateEvent -= ReceiveMatchConfigurationUpdateMessageEvent;
            MessageService.RaiseActiveRulesetChangeEvent -= ReceiveActiveRulesetChangeEvent;
            MessageService.RaiseRulesetSettingChangeEvent -= ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage -= ReceiveMatchControlSignalReceiptMessage;
        }

        private async Task LoadRulesetsAsync()
        {
            _rulesets = await RulesetManager.GetRulesetsAsync(CancellationToken.None);

            _isLoadingRulesets = false;
            InvokeAsyncStateHasChanged();
        }

        private async Task LoadActiveRulesetAsync()
        {
            _isLoadingActiveRulesetConfig = true;
            InvokeAsyncStateHasChanged();

            _ruleset = await RulesetManager.GetActiveRulesetAsync();

            if (_ruleset != null) // afaik this should never be null
            {
                // TODO: probably move this to SME
                MatchConfiguration newMatchConfiguration = new(_ruleset);

                if (ScrimMatchEngine.Config.IsManualTitle)
                {
                    newMatchConfiguration.TrySetTitle(ScrimMatchEngine.Config.Title, true);
                }

                // Preserve WorldId settings when changing ruleset
                if (ScrimMatchEngine.Config.IsWorldIdSet)
                {
                    newMatchConfiguration.TrySetWorldId(ScrimMatchEngine.Config.WorldId.ToString(), ScrimMatchEngine.Config.IsManualWorldId);
                }

                if (ScrimMatchEngine.Config.IsManualRoundSecondsTotal)
                {
                    newMatchConfiguration.TrySetRoundLength(ScrimMatchEngine.Config.RoundSecondsTotal, true);
                }

                if (ScrimMatchEngine.Config.IsManualTargetPointValue)
                {
                    newMatchConfiguration.TrySetTargetPointValue(ScrimMatchEngine.Config.TargetPointValue, true);
                }

                if (ScrimMatchEngine.Config.IsManualPeriodicFacilityControlPoints)
                {
                    newMatchConfiguration.TrySetPeriodicFacilityControlPoints(ScrimMatchEngine.Config.PeriodicFacilityControlPoints, true);
                }

                if (ScrimMatchEngine.Config.IsManualPeriodicFacilityControlInterval)
                {
                    newMatchConfiguration.TrySetPeriodicFacilityControlInterval(ScrimMatchEngine.Config.PeriodicFacilityControlInterval, true);
                }

                if (ScrimMatchEngine.Config.IsManualEndRoundOnFacilityCapture)
                {
                    newMatchConfiguration.TrySetEndRoundOnFacilityCapture(ScrimMatchEngine.Config.EndRoundOnFacilityCapture, true);
                }

                // Preserve facility id on page reload
                if (ScrimMatchEngine.Config.FacilityId != -1)
                {
                    newMatchConfiguration.FacilityIdString = ScrimMatchEngine.Config.FacilityId.ToString();
                }

                // TODO: carry over old settings depending on what the Round Win Condition is

                ScrimMatchEngine.ConfigureMatch(newMatchConfiguration);
            }

            if (_ruleset.RulesetFacilityRules.Any())
            {
                _mapRegions = _ruleset.RulesetFacilityRules.Select(r => r.MapRegion).OrderBy(r => r.FacilityName);
            }
            else
            {
                _mapRegions = (await FacilityService.GetScrimmableMapRegionsAsync()).OrderBy(r => r.FacilityName);
            }

            _isLoadingActiveRulesetConfig = false;
            InvokeAsyncStateHasChanged();
        }

        #region Census Subscription State
        private async Task GetCensusStreamStatusAsync()
        {
            ServiceState status = await WebsocketMonitor.GetStatus();
            _isStreamServiceEnabled = status.IsEnabled;

            if (!_isStreamServiceEnabled)
            {
                _errorBannerMessage = "Failed to connect to the Planetside 2 Websocket";
            }
            else
            {
                _errorBannerMessage = string.Empty;
            }
        }
        #endregion Census Subscription State

        #region Match Controls
        private async void StartMatch()
        {
            if (!_isStartingRound)
            {
                _isStartingRound = true;
                InvokeAsyncStateHasChanged();

                await Task.Run(ScrimMatchEngine.Start);

                _isDeleteDataEnabled = false;
                _isStartingRound = false;
                InvokeAsyncStateHasChanged();
            }
        }

        private async void EndRound()
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Running && !_isEndingRound)
            {
                _isEndingRound = true;
                InvokeAsyncStateHasChanged();

                await Task.Run(ScrimMatchEngine.EndRound);

                _isDeleteDataEnabled = false;
                _isEndingRound = false;
                InvokeAsyncStateHasChanged();
            }
        }

        private void PauseRound()
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Running)
            {
                ScrimMatchEngine.PauseRound();

                _isDeleteDataEnabled = false;
                InvokeAsyncStateHasChanged();
            }
        }

        private void ResumeRound()
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Paused)
            {
                ScrimMatchEngine.ResumeRound();

                _isDeleteDataEnabled = false;
                InvokeAsyncStateHasChanged();
            }
        }

        private async void ClearMatch(bool isRematch)
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Stopped || ScrimMatchEngine.GetMatchState() == MatchState.Uninitialized && !_isResettingRound && !_isClearingMatch)
            {
                _isClearingMatch = true;
                _isDeleteDataEnabled = false;
                InvokeAsyncStateHasChanged();

                await Task.Run(() => ScrimMatchEngine.ClearMatch(isRematch));

                ScrimMatchEngine.Config.TrySetRoundLength(_ruleset.DefaultRoundLength, false);
                ScrimMatchEngine.Config.TrySetTitle(_ruleset.DefaultMatchTitle ?? string.Empty, false);
            }

            _isClearingMatch = false;
            InvokeAsyncStateHasChanged();
        }

        private async void ResetRound()
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Stopped && ScrimMatchEngine.GetCurrentRound() > 0 && !_isResettingRound && !_isClearingMatch)
            {
                _isResettingRound = true;
                _isDeleteDataEnabled = false;
                InvokeAsyncStateHasChanged();

                await Task.Run(ScrimMatchEngine.ResetRound);

                _isResettingRound = false;
                InvokeAsyncStateHasChanged();
            }
        }
        #endregion Match Controls

        #region Form Handling
        private void OnChangeMatchTitle(string newTitle)
        {
            string oldTitle = ScrimMatchEngine.Config.Title;

            if (newTitle != oldTitle)
            {
                ScrimMatchEngine.Config.TrySetTitle(newTitle, true);
            }
        }

        private void OnChangeRoundLength(int newLength)
        {
            int oldLength = ScrimMatchEngine.Config.RoundSecondsTotal;

            if (newLength != oldLength)
            {
                ScrimMatchEngine.Config.TrySetRoundLength(newLength, true);
            }
        }

        private void OnChangeTargetPointValue(int? newTarget)
        {
            int? oldTarget = ScrimMatchEngine.Config.TargetPointValue;

            if (newTarget != oldTarget)
            {
                ScrimMatchEngine.Config.TrySetTargetPointValue(newTarget, true);
            }
        }

        private void OnChangePeriodicControlPoints(int? newPoints)
        {
            int? oldPoints = ScrimMatchEngine.Config.PeriodicFacilityControlPoints;

            if (newPoints != oldPoints)
            {
                ScrimMatchEngine.Config.TrySetPeriodicFacilityControlPoints(newPoints, true);
            }
        }

        private void OnChangePeriodicControlPointsInterval(int? newInterval)
        {
            int? oldInterval = ScrimMatchEngine.Config.PeriodicFacilityControlInterval;

            if (newInterval != oldInterval)
            {
                ScrimMatchEngine.Config.TrySetPeriodicFacilityControlInterval(newInterval, true);
            }
        }

        private void OnChangeWorldId(int newWorldId)
        {
            int oldWorldId = ScrimMatchEngine.Config.WorldId;

            if (newWorldId != oldWorldId)
            {
                ScrimMatchEngine.Config.TrySetWorldId(newWorldId, true);
            }
        }

        private void OnChangeFacilityId(int newFacilityId)
        {
            int oldFacilityId = ScrimMatchEngine.Config.FacilityId;

            if (newFacilityId != oldFacilityId)
            {
                ScrimMatchEngine.Config.TrySetFacilityId(newFacilityId.ToString());
            }
        }

        private void OnChangeEndRoundOnFacilityCapture(bool newSetting)
        {
            bool oldSetting = ScrimMatchEngine.Config.EndRoundOnFacilityCapture;

            if (newSetting != oldSetting)
            {
                ScrimMatchEngine.Config.TrySetEndRoundOnFacilityCapture(newSetting, true);
            }
        }
        #endregion Form Handling

        #region Event Handling
        private void ReceiveMatchStateUpdateMessageEvent(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
        {
            MatchStateUpdateMessage message = e.Message;

            _currentRound = message.CurrentRound;
            _matchState = message.MatchState;
            _matchId = message.MatchId;

            InvokeAsyncStateHasChanged();
        }

        // ScrimMatchEngine sends out this message after updating the WorldId from players/outfits
        private void ReceiveMatchConfigurationUpdateMessageEvent(object sender, ScrimMessageEventArgs<MatchConfigurationUpdateMessage> e)
        {
            InvokeAsyncStateHasChanged();
        }

        private async void ReceiveActiveRulesetChangeEvent(object sender, ScrimMessageEventArgs<ActiveRulesetChangeMessage> e)
        {
            await LoadActiveRulesetAsync();
        }

        private void ReceiveRulesetSettingChangeEvent(object sender, ScrimMessageEventArgs<RulesetSettingChangeMessage> e)
        {
            RulesetSettingChangeMessage message = e.Message;

            if (message.ChangedSettings.Contains(RulesetSettingChange.DefaultEndRoundOnFacilityCapture))
            {
                bool newSetting = message.Ruleset.DefaultEndRoundOnFacilityCapture;

                if (ScrimMatchEngine.Config.TrySetEndRoundOnFacilityCapture(newSetting, false))
                {
                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void ReceiveMatchControlSignalReceiptMessage(object sender, ScrimMessageEventArgs<MatchControlSignalReceiptMessage> e)
        {
            _isDeleteDataEnabled = false;
            InvokeAsyncStateHasChanged();

            string signal = e.Message.Signal;

            if (signal == nameof(MatchControlHub.Rematch) || signal == nameof(MatchControlHub.ClearMatch))
            {                
                ScrimMatchEngine.Config.TrySetRoundLength(_ruleset.DefaultRoundLength, false);
                ScrimMatchEngine.Config.TrySetTitle(_ruleset.DefaultMatchTitle ?? string.Empty, false);
                InvokeAsyncStateHasChanged();
            }
        }
        #endregion Event Handling

        #region Ruleset Form Controls
        private async Task OnChangeRulesetSelection(int rulesetId)
        {
            _isChangingRuleset = true;
            InvokeAsyncStateHasChanged();

            if (rulesetId == _ruleset.Id)
            {
                _isChangingRuleset = false;
                InvokeAsyncStateHasChanged();
                return;
            }

            Ruleset newActiveRuleset = await RulesetManager.ActivateRulesetAsync(rulesetId);

            if (newActiveRuleset == null)
            {
                _isChangingRuleset = false;
                InvokeAsyncStateHasChanged();
                return;
            }

            _ruleset = newActiveRuleset;

            // LoadActiveRuleset is called by the event raised by ActivateRulesetAsync

            _isChangingRuleset = false;
            InvokeAsyncStateHasChanged();
        }
        #endregion Ruleset Form Controls

        private void InvokeAsyncStateHasChanged() => InvokeAsync(StateHasChanged);
    }
}