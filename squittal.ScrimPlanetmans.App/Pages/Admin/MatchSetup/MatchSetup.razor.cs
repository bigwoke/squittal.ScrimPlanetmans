using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private List<int> _mapZones = new();
        private IEnumerable<MapRegion> _mapRegions;
        private IEnumerable<Zone> _zones;
        private IEnumerable<World> _worlds;
        #endregion

        #region State Control Variables
        private bool _isLoading = false;
        private bool _isChangingRuleset = false;
        private bool _isLoadingRulesets = false;
        private bool _isLoadingActiveRulesetConfig = false;

        private bool _isClearingMatch = false;
        private bool _isEndingRound = false;
        private bool _isResettingRound = false;
        private bool _isRunning = false;
        private bool _isStartingRound = false;

        private bool _isDeleteDataEnabled = false;
        private bool _isStreamServiceEnabled = false;

        private int _currentRound = 0;
        private MatchState _matchState = MatchState.Uninitialized;
        #endregion

        #region Initialization Methods
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isLoading = true;

                var TaskList = new List<Task>();

                var censusStreamStatusTask = GetCensusStreamStatus();
                TaskList.Add(censusStreamStatusTask);

                var zonesTask = ZoneService.GetAllZones();
                TaskList.Add(zonesTask);

                var worldsTask = WorldService.GetAllWorldsAsync();
                TaskList.Add(worldsTask);

                var rulesetsTask = SetUpRulesetsAsync();
                TaskList.Add(rulesetsTask);

                var activeRulesetConfigTask = SetUpActiveRulesetConfigAsync();
                TaskList.Add(activeRulesetConfigTask);

                await Task.WhenAll(TaskList);

                _worlds = worldsTask.Result.OrderBy(worlds => worlds.Name).ToList();

                _zones = zonesTask.Result;

                _isLoading = false;
                InvokeAsyncStateHasChanged();
            }
        }

        protected override void OnInitialized()
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

            _isRunning = ScrimMatchEngine.IsRunning();
            _currentRound = ScrimMatchEngine.GetCurrentRound();
            _matchState = ScrimMatchEngine.GetMatchState();
            _matchId = ScrimMatchEngine.GetMatchId();
        }

        public void Dispose()
        {
            MessageService.RaiseMatchStateUpdateEvent -= ReceiveMatchStateUpdateMessageEvent;
            MessageService.RaiseMatchConfigurationUpdateEvent -= ReceiveMatchConfigurationUpdateMessageEvent;
            MessageService.RaiseRulesetSettingChangeEvent -= ReceiveRulesetSettingChangeEvent;
            MessageService.RaiseMatchControlSignalReceiptMessage -= ReceiveMatchControlSignalReceiptMessage;
        }

        private async Task SetUpRulesetsAsync()
        {
            _isLoadingRulesets = true;
            InvokeAsyncStateHasChanged();

            _rulesets = await RulesetManager.GetRulesetsAsync(CancellationToken.None);

            _isLoadingRulesets = false;
            InvokeAsyncStateHasChanged();
        }

        private async Task SetUpActiveRulesetConfigAsync()
        {
            _isLoadingActiveRulesetConfig = true;

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

                    // TODO: carry over old settings depending on what the Round Win Condition is

                    _matchConfiguration.CopyValues(newMatchConfiguration);
                    MessageService.BroadcastMatchConfigurationUpdateMessage(new MatchConfigurationUpdateMessage(_matchConfiguration));
                }

                if (_activeRuleset.RulesetFacilityRules.Any())
                {
                    var mapRegions = _activeRuleset.RulesetFacilityRules.Select(r => r.MapRegion).ToList();
                    _mapRegions = mapRegions.OrderBy(r => r.FacilityName).ToList();

                    _mapZones = _mapRegions.Select(r => r.ZoneId).Distinct().ToList();
                }
                else
                {
                    var mapRegions = await FacilityService.GetScrimmableMapRegionsAsync();

                    _mapRegions = mapRegions.OrderBy(r => r.FacilityName).ToList();
                    _mapZones = _mapRegions.Select(r => r.ZoneId).Distinct().ToList();
                }
            }

            _isLoadingActiveRulesetConfig = false;
            InvokeAsyncStateHasChanged();
        }
        #endregion Initialization Methods

        #region  Match & Subscription State Buttons
        private async Task GetCensusStreamStatus()
        {
            var status = await WebsocketMonitor.GetStatus();
            _isStreamServiceEnabled = status.IsEnabled;

            if (!_isStreamServiceEnabled)
            {
                SetWebsocketConnectionErrorMessage();
            }
            else
            {
                ClearErrorMessage();
            }
        }

        private void SubscribeToCensus()
        {
            ScrimMatchEngine.SubmitPlayersList();
            LogAdminMessage($"Subscribed all characters to Stream Monitor!");
        }

        private void EndCensusSubscription()
        {
            WebsocketMonitor.RemoveAllCharacterSubscriptions();
            LogAdminMessage($"Removed all characters from Stream Monitor!");
        }

        #region Match Controls
        private async void StartMatch()
        {
            if (!_isStartingRound)
            {
                _isStartingRound = true;
                InvokeAsyncStateHasChanged();

                ScrimMatchEngine.ConfigureMatch(_matchConfiguration);
                await Task.Run(() => ScrimMatchEngine.Start());

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

                await Task.Run(() => ScrimMatchEngine.EndRound());
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
                _matchConfiguration.Title = (_activeRuleset.DefaultMatchTitle == null) ? string.Empty : _activeRuleset.DefaultMatchTitle;

                _isClearingMatch = false;
                InvokeAsyncStateHasChanged();
            }
            else
            {
                _isClearingMatch = false;
                InvokeAsyncStateHasChanged();
            }
        }

        private async void ResetRound()
        {
            if (ScrimMatchEngine.GetMatchState() == MatchState.Stopped && ScrimMatchEngine.GetCurrentRound() > 0 && !_isResettingRound && !_isClearingMatch)
            {
                _isResettingRound = true;
                _isDeleteDataEnabled = false;
                InvokeAsyncStateHasChanged();

                await Task.Run(() => ScrimMatchEngine.ResetRound());
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
                    //Console.WriteLine($"Set Title succeeded: {newTitle}");

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
                    //Console.WriteLine($"Set Round Length succeeded: {newLength}");

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
                    //Console.WriteLine($"Set Target Points succeeded: {newTarget}");

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
                    //Console.WriteLine($"Set Periodic Points succeeded: {newPoints}");

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
                    //Console.WriteLine($"Set Periodic Interval succeeded: {newInterval}");

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
                    //Console.WriteLine($"MatchSetup: Set WorldId succeeded: {newWorldId}");

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
                    //Console.WriteLine($"Set End on Capture succeeded: {newSetting}");

                    ScrimMatchEngine.ConfigureMatch(_matchConfiguration);

                    InvokeAsyncStateHasChanged();
                }
            }
        }
        #endregion Form Handling

        #endregion Match & Subscription State Buttons


        #region  Event Handling
        private void ReceiveMatchStateUpdateMessageEvent(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
        {
            var message = e.Message;

            _isRunning = message.MatchState == MatchState.Running;
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

            //Console.WriteLine("MatchSetup: received MatchConfigurationUpdateMessage");

            var config = message.MatchConfiguration;

            var newWorldId = config.WorldIdString;
            var newWorldIdIsManual = config.IsManualWorldId;

            //Console.WriteLine($"MatchSetup: received MatchConfigurationUpdateMessage with WorldId {newWorldId} & IsManualWorldId {newWorldIdIsManual}");
            //Console.WriteLine($"MatchSetup: current WorldId = {newWorldId} & IsManualWorldId = {newWorldIdIsManual}");


            // Set isRollBack=true to force setting WorldId without chaning IsManualWorldId
            var success = _matchConfiguration.TrySetWorldId(newWorldId, newWorldIdIsManual, true);

            InvokeAsyncStateHasChanged();
        }

        private void ReceiveRulesetSettingChangeEvent(object sender, ScrimMessageEventArgs<RulesetSettingChangeMessage> e)
        {
            var message = e.Message;

            if (!message.ChangedSettings.Contains(RulesetSettingChange.DefaultEndRoundOnFacilityCapture))
            {
                return;
            }

            var success = _matchConfiguration.TrySetEndRoundOnFacilityCapture(message.Ruleset.DefaultEndRoundOnFacilityCapture, false);

            if (success)
            {
                InvokeAsyncStateHasChanged();
            }
        }

        private void ReceiveMatchControlSignalReceiptMessage(object sender, ScrimMessageEventArgs<MatchControlSignalReceiptMessage> e)
        {
            _isDeleteDataEnabled = false;
            InvokeAsyncStateHasChanged();

            if (e.Message.Signal == "Rematch" || e.Message.Signal == "ClearMatch")
            {
                _matchConfiguration.CopyValues(ScrimMatchEngine.MatchConfiguration);
                _matchConfiguration.RoundSecondsTotal = _activeRuleset.DefaultRoundLength;
                _matchConfiguration.Title = (_activeRuleset.DefaultMatchTitle == null) ? string.Empty : _activeRuleset.DefaultMatchTitle;

                InvokeAsyncStateHasChanged();
            }
        }

        #endregion

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

        #region Log Messages
        private void LogAdminMessage(string message)
        {
            Task.Run(() =>
            {
                MessageService.BroadcastSimpleMessage(message);
            }).ConfigureAwait(false);
        }
        #endregion Log Messages

        #region Error Messages
        private void ClearErrorMessage()
        {
            _errorBannerMessage = string.Empty;
        }

        private void SetWebsocketConnectionErrorMessage()
        {
            _errorBannerMessage = "Failed to connect to the Planetside 2 Websocket";
        }
        #endregion Error Messages

        private void InvokeAsyncStateHasChanged()
        {
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
    }
}