using System;
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
        private MatchConfiguration _matchConfiguration;
        private string _errorBannerMessage;
        private string _matchId = string.Empty;

        #region Ruleset Select List Variables
        private IEnumerable<Ruleset> _rulesets;
        private string _inputSelectRulesetStringId;

        private Ruleset _activeRuleset;
        private Ruleset _selectedRuleset;
        #endregion

        #region Facility & World Select List Variables
        private const string _noFacilityIdValue = "-1";

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
            MessageService.RaiseRulesetSettingChangeEvent -= ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage -= ReceiveMatchControlSignalReceiptMessage;

            MessageService.RaiseMatchStateUpdateEvent += ReceiveMatchStateUpdateMessageEvent;
            MessageService.RaiseMatchConfigurationUpdateEvent += ReceiveMatchConfigurationUpdateMessageEvent;
            MessageService.RaiseRulesetSettingChangeEvent += ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage += ReceiveMatchControlSignalReceiptMessage;

            if (ScrimMatchEngine.MatchConfiguration != null)
            {
                Console.WriteLine($"MatchSetup: fetching MatchConfiguration from ScrimMatchEngine");
                _matchConfiguration = new MatchConfiguration();
                _matchConfiguration.CopyValues(ScrimMatchEngine.MatchConfiguration);
            }
            else
            {
                Console.WriteLine($"MatchSetup: creating new MatchConfiguration");
                _matchConfiguration = new MatchConfiguration();
            }

            _currentRound = ScrimMatchEngine.GetCurrentRound();
            _matchState = ScrimMatchEngine.GetMatchState();
            _matchId = ScrimMatchEngine.GetMatchId();

            List<Task> taskList = new()
            {
                GetCensusStreamStatusAsync(),
                LoadRulesetsAsync(),
                SetUpActiveRulesetConfigAsync()
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
            MessageService.RaiseRulesetSettingChangeEvent -= ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage -= ReceiveMatchControlSignalReceiptMessage;
        }

        private async Task LoadRulesetsAsync()
        {
            _rulesets = await RulesetManager.GetRulesetsAsync(CancellationToken.None);

            _isLoadingRulesets = false;
            InvokeAsyncStateHasChanged();
        }

        private async Task SetUpActiveRulesetConfigAsync()
        {
            _activeRuleset = await RulesetManager.GetActiveRulesetAsync();

            if (_activeRuleset != null)
            {
                _selectedRuleset = _activeRuleset;
                _inputSelectRulesetStringId = _activeRuleset.Id.ToString();

                if (_matchConfiguration != null)
                {
                    var newMatchConfiguration = new MatchConfiguration(_activeRuleset);

                    if (_matchConfiguration.IsManualTitle)
                    {
                        newMatchConfiguration.TrySetTitle(_matchConfiguration.Title, true);
                    }

                    // Preserve WorldId settings when changing ruleset
                    if (_matchConfiguration.IsWorldIdSet)
                    {
                        newMatchConfiguration.TrySetWorldId(_matchConfiguration.WorldIdString, _matchConfiguration.IsManualWorldId);
                    }

                    if (_matchConfiguration.IsManualRoundSecondsTotal)
                    {
                        newMatchConfiguration.TrySetRoundLength(_matchConfiguration.RoundSecondsTotal, true);
                    }

                    if (_matchConfiguration.IsManualTargetPointValue)
                    {
                        newMatchConfiguration.TrySetTargetPointValue(_matchConfiguration.TargetPointValue, true);
                    }

                    if (_matchConfiguration.IsManualPeriodicFacilityControlPoints)
                    {
                        newMatchConfiguration.TrySetPeriodicFacilityControlPoints(_matchConfiguration.PeriodicFacilityControlPoints, true);
                    }

                    if (_matchConfiguration.IsManualPeriodicFacilityControlInterval)
                    {
                        newMatchConfiguration.TrySetPeriodicFacilityControlInterval(_matchConfiguration.PeriodicFacilityControlInterval, true);
                    }

                    if (_matchConfiguration.IsManualEndRoundOnFacilityCapture)
                    {
                        newMatchConfiguration.TrySetEndRoundOnFacilityCapture(_matchConfiguration.EndRoundOnFacilityCapture, true);
                    }

                    // Preserve facility id on page reload
                    // TODO: separate match configuration logic from the setup page
                    if (_matchConfiguration.FacilityId != -1)
                    {
                        newMatchConfiguration.FacilityIdString = _matchConfiguration.FacilityIdString;
                    }

                    // TODO: carry over old settings depending on what the Round Win Condition is

                    _matchConfiguration.CopyValues(newMatchConfiguration);
                    MessageService.BroadcastMatchConfigurationUpdateMessage(new MatchConfigurationUpdateMessage(_matchConfiguration));
                }

                if (_activeRuleset.RulesetFacilityRules.Any())
                {
                    _mapRegions = _activeRuleset.RulesetFacilityRules.Select(r => r.MapRegion).OrderBy(r => r.FacilityName);
                }
                else
                {
                    _mapRegions = (await FacilityService.GetScrimmableMapRegionsAsync()).OrderBy(r => r.FacilityName);
                }
                }

            _isLoadingActiveRulesetConfig = false;
            InvokeAsyncStateHasChanged();
        }

        #region Census Subscription State
        private async Task GetCensusStreamStatusAsync()
        {
            var status = await WebsocketMonitor.GetStatus();
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

                ScrimMatchEngine.ConfigureMatch(_matchConfiguration);
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

                _matchConfiguration.CopyValues(ScrimMatchEngine.MatchConfiguration);
                _matchConfiguration.RoundSecondsTotal = _activeRuleset.DefaultRoundLength;
                _matchConfiguration.Title = _activeRuleset.DefaultMatchTitle ?? string.Empty;
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
            var oldTitle = _matchConfiguration.Title;

            if (newTitle != oldTitle)
            {
                if (_matchConfiguration.TrySetTitle(newTitle, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangeRoundLength(int newLength)
        {
            var oldLength = _matchConfiguration.RoundSecondsTotal;

            if (newLength != oldLength)
            {
                if (_matchConfiguration.TrySetRoundLength(newLength, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangeTargetPointValue(int newTarget)
        {
            var oldTarget = _matchConfiguration.TargetPointValue;

            if (newTarget != oldTarget)
            {
                if (_matchConfiguration.TrySetTargetPointValue(newTarget, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangePeriodicControlPoints(int newPoints)
        {
            var oldPoints = _matchConfiguration.PeriodicFacilityControlPoints;

            if (newPoints != oldPoints)
            {
                if (_matchConfiguration.TrySetPeriodicFacilityControlPoints(newPoints, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangePeriodicControlPointsInterval(int newInterval)
        {
            var oldInterval = _matchConfiguration.PeriodicFacilityControlInterval;

            if (newInterval != oldInterval)
            {
                if (_matchConfiguration.TrySetPeriodicFacilityControlInterval(newInterval, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangeWorldId(string newWorldId)
        {
            //Console.WriteLine($"MatchSetup: OnChangeWorldId({newWorldId})");

            var oldWorldId = _matchConfiguration.WorldIdString;

            if (newWorldId != oldWorldId)
            {
                if (_matchConfiguration.TrySetWorldId(newWorldId, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }

        private void OnChangeEndRoundOnFacilityCapture(bool newSetting)
        {
            var oldSetting = _matchConfiguration.EndRoundOnFacilityCapture;

            if (newSetting != oldSetting)
            {
                if (_matchConfiguration.TrySetEndRoundOnFacilityCapture(newSetting, true))
                {
                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }
        #endregion Form Handling

        #region Event Handling
        private void ReceiveMatchStateUpdateMessageEvent(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
        {
            var message = e.Message;

            _currentRound = message.CurrentRound;
            _matchState = message.MatchState;
            _matchId = message.MatchId;
            _matchConfiguration.Title = message.MatchTitle;

            InvokeAsyncStateHasChanged();
        }

        // ScrimMatchEngine sends out this message after updating the WorldId from players/outfits
        private void ReceiveMatchConfigurationUpdateMessageEvent(object sender, ScrimMessageEventArgs<MatchConfigurationUpdateMessage> e)
        {
            var message = e.Message;

            var config = message.MatchConfiguration;

            var newWorldId = config.WorldIdString;
            var newWorldIdIsManual = config.IsManualWorldId;


            // Set isRollBack=true to force setting WorldId without changing IsManualWorldId
            _matchConfiguration.TrySetWorldId(newWorldId, newWorldIdIsManual, true);

            InvokeAsyncStateHasChanged();
        }

        private void ReceiveRulesetSettingChangeEvent(object sender, ScrimMessageEventArgs<RulesetSettingChangeMessage> e)
        {
            var message = e.Message;

            if (message.ChangedSettings.Contains(RulesetSettingChange.DefaultEndRoundOnFacilityCapture))
            {
                bool newSetting = message.Ruleset.DefaultEndRoundOnFacilityCapture;

                if (_matchConfiguration.TrySetEndRoundOnFacilityCapture(newSetting, false))
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
                _matchConfiguration.CopyValues(ScrimMatchEngine.MatchConfiguration);
                _matchConfiguration.RoundSecondsTotal = _activeRuleset.DefaultRoundLength;
                _matchConfiguration.Title = _activeRuleset.DefaultMatchTitle ?? string.Empty;

                InvokeAsyncStateHasChanged();
            }
        }
        #endregion Event Handling

        #region Ruleset Form Controls
        private async void OnChangeRulesetSelection(string rulesetStringId)
        {
            _isChangingRuleset = true;
            InvokeAsyncStateHasChanged();

            if (!int.TryParse(rulesetStringId, out var rulesetId))
            {
                _isChangingRuleset = false;
                InvokeAsyncStateHasChanged();
                return;
            }

            if (rulesetId == _selectedRuleset.Id || rulesetId == _activeRuleset.Id)
            {
                _isChangingRuleset = false;
                InvokeAsyncStateHasChanged();
                return;
            }

            var newActiveRuleset = await RulesetManager.ActivateRulesetAsync(rulesetId);

            if (newActiveRuleset == null || newActiveRuleset.Id == _activeRuleset.Id)
            {
                _isChangingRuleset = false;
                InvokeAsyncStateHasChanged();
                return;
            }

            _activeRuleset = newActiveRuleset;
            _selectedRuleset = newActiveRuleset;
            _inputSelectRulesetStringId = newActiveRuleset.Id.ToString();

            await SetUpActiveRulesetConfigAsync();

            _isChangingRuleset = false;
            InvokeAsyncStateHasChanged();
        }
        #endregion Ruleset Form Controls

        private void InvokeAsyncStateHasChanged() => InvokeAsync(StateHasChanged);
    }
}