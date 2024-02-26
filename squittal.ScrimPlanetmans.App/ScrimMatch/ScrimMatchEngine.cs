using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using squittal.ScrimPlanetmans.CensusStream;
using squittal.ScrimPlanetmans.Data.Models;
using squittal.ScrimPlanetmans.Models.ScrimEngine;
using squittal.ScrimPlanetmans.ScrimMatch;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.ScrimMatch.Timers;
using squittal.ScrimPlanetmans.Services.ScrimMatch;

namespace squittal.ScrimPlanetmans.App.ScrimMatch
{
    public class ScrimMatchEngine : IScrimMatchEngine
    {
        private readonly IScrimTeamsManager _teamsManager;
        private readonly IWebsocketMonitor _wsMonitor;
        private readonly IScrimMessageBroadcastService _messageService;
        private readonly IScrimMatchDataService _matchDataService;
        private readonly IScrimRulesetManager _rulesetManager;
        private readonly IScrimMatchScorer _matchScorer;
        private readonly ILogger<ScrimMatchEngine> _logger;

        private readonly IStatefulTimer _timer;
        private readonly IPeriodicPointsTimer _periodicTimer;
        private readonly IScrimRoundEndCheckerService _roundEndChecker;

        private bool _isRunning = false;
        private int _currentRound = 0;

        private MatchConfiguration _matchConfiguration = new MatchConfiguration();
        private MatchTimerTickMessage _latestTimerTickMessage;
        private PeriodicPointsTimerStateMessage _latestPeriodicPointsTimerTickMessage;
        private ScrimFacilityControlActionEventMessage _latestFacilityControlMessage;

        private MatchState _matchState = MatchState.Uninitialized;

        private DateTime _matchStartTime;

        private readonly AutoResetEvent _captureAutoEvent = new(true);

        public ScrimMatchEngine(
            IScrimTeamsManager teamsManager,
            IWebsocketMonitor wsMonitor,
            IStatefulTimer timer,
            IPeriodicPointsTimer periodicTimer,
            IScrimMatchDataService matchDataService,
            IScrimMessageBroadcastService messageService,
            IScrimRulesetManager rulesetManager,
            IScrimMatchScorer matchScorer,
            IScrimRoundEndCheckerService roundEndChecker,
            ILogger<ScrimMatchEngine> logger)
        {
            _teamsManager = teamsManager;
            _wsMonitor = wsMonitor;
            _timer = timer;
            _periodicTimer = periodicTimer;
            _messageService = messageService;
            _matchDataService = matchDataService;
            _rulesetManager = rulesetManager;
            _matchScorer = matchScorer;
            _roundEndChecker = roundEndChecker;

            _logger = logger;

            _matchConfiguration.PropertyChanged += OnMatchConfigurationPropertyChanged;

            _messageService.RaiseMatchTimerTickEvent += OnMatchTimerTick;
            _messageService.RaisePeriodicPointsTimerTickEvent += async (s, e) => await OnPeriodiocPointsTimerTick(s, e);

            _messageService.RaiseTeamOutfitChangeEvent += OnTeamOutfitChangeEvent;
            _messageService.RaiseTeamPlayerChangeEvent += OnTeamPlayerChangeEvent;

            _messageService.RaiseScrimFacilityControlActionEvent += OnFacilityControlEvent;

            _messageService.RaiseEndRoundCheckerMessage += async (s, e) => await OnEndRoundCheckerMessage(s, e);

            _roundEndChecker.Disable();

            Task.Run(async () => TrySetMatchRuleset(await _rulesetManager.GetActiveRulesetAsync()));
        }

        public MatchConfiguration Config
        {
            get => _matchConfiguration;
            private set
            {
                if (_matchConfiguration != value)
                {
                    _matchConfiguration = value;
                    _matchConfiguration.PropertyChanged += OnMatchConfigurationPropertyChanged;
                    _logger.LogDebug("{SME} {Config} property replaced", nameof(ScrimMatchEngine), nameof(Config));
                }
            }
        }
        public Ruleset MatchRuleset { get; private set; }
        public int CurrentSeriesMatch { get; private set; } = 0;
        private int? FacilityControlTeamOrdinal { get; set; }

        public async Task Start()
        {
            if (_isRunning)
            {
                return;
            }

            if (_currentRound == 0)
            {
                await InitializeNewMatch();
            }

            await InitializeNewRound();

            StartRound();
        }

        public async Task ClearMatch(bool isRematch)
        {
            if (_isRunning)
            {
                await EndRound();
            }

            _wsMonitor.DisableScoring();
            if (!isRematch)
            {
                _wsMonitor.RemoveAllCharacterSubscriptions();
            }
            _messageService.DisableLogging();

            int previousWorldId = Config.WorldId;
            bool previousIsManualWorldId = Config.IsManualWorldId;

            bool previousEndRoundOnFacilityCapture = Config.EndRoundOnFacilityCapture;
            bool previousIsManualEndRoundOnFacilityCapture = Config.EndRoundOnFacilityCapture;

            int? previousTargetPointValue = Config.TargetPointValue;
            bool previousIsManualTargetPointValue = Config.IsManualTargetPointValue;

            int? previousInitialPoints = Config.InitialPoints;
            bool previousIsManualInitialPoints = Config.IsManualInitialPoints;

            int? previousPeriodicFacilityControlPoints = Config.PeriodicFacilityControlPoints;
            bool previousIsManualPeriodicFacilityControlPoints = Config.IsManualPeriodicFacilityControlPoints;

            int? previousPeriodicFacilityControlInterval = Config.PeriodicFacilityControlInterval;
            bool previousIsManualPeriodicFacilityControlInterval = Config.IsManualPeriodicFacilityControlInterval;

            Ruleset activeRuleset = await _rulesetManager.GetActiveRulesetAsync();
            Config = new MatchConfiguration(activeRuleset);

            if (isRematch)
            {
                Config.TrySetWorldId(previousWorldId, previousIsManualWorldId);
                Config.TrySetEndRoundOnFacilityCapture(previousEndRoundOnFacilityCapture, previousIsManualEndRoundOnFacilityCapture);

                Config.TrySetTargetPointValue(previousTargetPointValue, previousIsManualTargetPointValue);
                Config.TrySetInitialPoints(previousInitialPoints, previousIsManualInitialPoints);
                Config.TrySetPeriodicFacilityControlPoints(previousPeriodicFacilityControlPoints, previousIsManualPeriodicFacilityControlPoints);
                Config.TrySetPeriodicFacilityControlInterval(previousPeriodicFacilityControlInterval, previousIsManualPeriodicFacilityControlInterval);
            }
            else
            {
                Config.TrySetEndRoundOnFacilityCapture(activeRuleset.DefaultEndRoundOnFacilityCapture, false);

                Config.TrySetTargetPointValue(activeRuleset.TargetPointValue, false);
                Config.TrySetInitialPoints(activeRuleset.InitialPoints, false);
                Config.TrySetPeriodicFacilityControlPoints(activeRuleset.PeriodicFacilityControlPoints, false);
                Config.TrySetPeriodicFacilityControlInterval(activeRuleset.PeriodicFacilityControlInterval, false);
            }

            _matchState = MatchState.Uninitialized;
            _currentRound = 0;

            _matchDataService.CurrentMatchRound = _currentRound;
            _matchDataService.CurrentMatchId = string.Empty;

            _latestTimerTickMessage = null;

            if (isRematch)
            {
                _teamsManager.UpdateAllTeamsMatchSeriesResults(CurrentSeriesMatch);
                _teamsManager.ResetAllTeamsMatchData();
            }
            else
            {
                CurrentSeriesMatch = 0;

                _teamsManager.ClearAllTeams();
            }

            SendMatchStateUpdateMessage();
            SendMatchConfigurationUpdateMessage();
        }

        private bool TrySetMatchRuleset(Ruleset matchRuleset)
        {
            if (CanChangeRuleset())
            {
                if (MatchRuleset?.Id != matchRuleset.Id)
                {
                    Config.ApplyRuleset(matchRuleset);
                }

                MatchRuleset = matchRuleset;
                _matchDataService.CurrentMatchRulesetId = matchRuleset.Id;

                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task EndRound()
        {
            _logger.LogInformation($"Ending Round {_currentRound}");

            _isRunning = false;
            _matchState = MatchState.Stopped;

            _roundEndChecker.Disable();

            _wsMonitor.DisableScoring();

            // Stop the timer if forcing the round to end (as opposed to timer reaching 0)
            if (GetLatestTimerTickMessage()?.State != TimerState.Stopped)
            {
                _logger.LogInformation($"Trying to Halt Match Timer");
                _timer.Halt();
                _logger.LogInformation($"Halted Match Timer");
            }

            if (Config.EnablePeriodicFacilityControlRewards)
            {
                _logger.LogInformation($"Trying to Halt Periodic Timer");
                _periodicTimer.Halt();
                _logger.LogInformation($"Halted Periodic Timer");
            }

            _logger.LogInformation($"Saving team scores for round {_currentRound}");
            await _teamsManager.SaveRoundEndScores(_currentRound);

            // TODO: Save Match Round results & metadata

            _logger.LogInformation($"Round {_currentRound} ended; scoring disabled");

#pragma warning disable CS4014
            Task.Run(() =>
            {
                _messageService.BroadcastSimpleMessage($"Round {_currentRound} ended; scoring disabled");
            }).ConfigureAwait(false);
#pragma warning restore CS4014

            SendMatchStateUpdateMessage();

            _messageService.DisableLogging();
        }

        public async Task InitializeNewMatch()
        {
            TrySetMatchRuleset(_rulesetManager.ActiveRuleset);

            _matchStartTime = DateTime.UtcNow;
            FacilityControlTeamOrdinal = null;
            _latestFacilityControlMessage = null;
            _latestPeriodicPointsTimerTickMessage = null;

            CurrentSeriesMatch++;

            if (Config.SaveLogFiles == true)
            {
                string matchId = BuildMatchId();

                _messageService.SetLogFileName($"{matchId}.txt");

                Data.Models.ScrimMatch scrimMatch = new()
                {
                    Id = matchId,
                    StartTime = _matchStartTime,
                    Title = Config.Title,
                    RulesetId = MatchRuleset.Id
                };

                await _matchDataService.SaveToCurrentMatch(scrimMatch);
            }
        }

        private string BuildMatchId()
        {
            string matchId = _matchStartTime.ToString("yyyyMMddTHHmmss");

            for (int i = 1; i <= 3; i++)
            {
                string alias = _teamsManager.GetTeamAliasDisplay(i);

                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                matchId = $"{matchId}_{alias}";
            }

            return matchId;
        }

        public async Task InitializeNewRound()
        {
            _currentRound += 1;

            _matchDataService.CurrentMatchRound = _currentRound;

            FacilityControlTeamOrdinal = null;
            _latestFacilityControlMessage = null;
            _latestPeriodicPointsTimerTickMessage = null;

            if (Config.EnableRoundTimeLimit)
            {
                _timer.Configure(TimeSpan.FromSeconds(Config.RoundSecondsTotal));
            }
            else
            {
                _timer.Configure(null);
            }

            if (Config.EnablePeriodicFacilityControlRewards && Config.PeriodicFacilityControlInterval.HasValue)
            {
                _periodicTimer.Configure(TimeSpan.FromSeconds(Config.PeriodicFacilityControlInterval.Value));
            }

            _teamsManager.ClearPlayerLastKilledByMap();

            await _matchDataService.SaveCurrentMatchRoundConfiguration(Config);
        }

        public void StartRound()
        {
            _isRunning = true;
            _matchState = MatchState.Running;

            _roundEndChecker.Enable();

            if (Config.SaveLogFiles)
            {
                _messageService.EnableLogging();
            }

            _timer.Start();
            _wsMonitor.EnableScoring();

            SendMatchStateUpdateMessage();

            Console.WriteLine($"Match Configuration Settings:\n            Title: {Config.Title} (IsManual={Config.IsManualTitle})\n            Round Length: {Config.RoundSecondsTotal} (IsManual={Config.IsManualRoundSecondsTotal})\n            Point Target: {Config.TargetPointValue} (IsManual={Config.IsManualTargetPointValue})\n            Periodic Control Points: {Config.PeriodicFacilityControlPoints} (IsManual={Config.IsManualPeriodicFacilityControlPoints})\n            Periodic Control Interval: {Config.PeriodicFacilityControlInterval} (IsManual={Config.IsManualPeriodicFacilityControlInterval})\n            World ID: {Config.WorldId} (IsManual={Config.IsManualWorldId})\n            Facility ID: {Config.FacilityId}\n            End Round on Capture?: {Config.EndRoundOnFacilityCapture} (IsManual={Config.IsManualEndRoundOnFacilityCapture})");
        }

        public void PauseRound()
        {
            _isRunning = false;
            _matchState = MatchState.Paused;

            _roundEndChecker.Disable();

            _wsMonitor.DisableScoring();

            _timer.Pause();
            if (Config.EnablePeriodicFacilityControlRewards)
            {
                _periodicTimer.Pause();
            }

            SendMatchStateUpdateMessage();

            _messageService.DisableLogging();
        }

        public async Task ResetRound()
        {
            if (_currentRound == 0)
            {
                return;
            }

            _roundEndChecker.Disable();

            _wsMonitor.DisableScoring();

            _timer.Reset();
            if (Config.EnablePeriodicFacilityControlRewards)
            {
                _periodicTimer.Reset();
            }

            await _teamsManager.RollBackAllTeamStats(_currentRound);

            await _matchDataService.RemoveMatchRoundConfiguration(_currentRound);

            _currentRound -= 1;

            if (_currentRound == 0)
            {
                _matchState = MatchState.Uninitialized;
                _latestTimerTickMessage = null;
            }

            FacilityControlTeamOrdinal = null;
            _latestFacilityControlMessage = null;
            _latestPeriodicPointsTimerTickMessage = null;

            _matchDataService.CurrentMatchRound = _currentRound;

            SendMatchStateUpdateMessage();

            _messageService.DisableLogging();
        }

        public void ResumeRound()
        {
            _isRunning = true;
            _matchState = MatchState.Running;

            _roundEndChecker.Enable();

            if (Config.SaveLogFiles)
            {
                _messageService.EnableLogging();
            }

            _timer.Resume();
            if (Config.EnablePeriodicFacilityControlRewards)
            {
                _periodicTimer.Resume();
            }

            _wsMonitor.EnableScoring();

            SendMatchStateUpdateMessage();
        }

        public void SubmitPlayersList()
        {
            _wsMonitor.AddCharacterSubscriptions(_teamsManager.GetAllPlayerIds());
        }

        private void OnMatchTimerTick(object sender, ScrimMessageEventArgs<MatchTimerTickMessage> e)
        {
            _latestTimerTickMessage = e.Message;
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public int GetCurrentRound()
        {
            return _currentRound;
        }

        public MatchState GetMatchState()
        {
            return _matchState;
        }

        public string GetMatchId()
        {
            return _matchDataService.CurrentMatchId;
        }

        public MatchTimerTickMessage GetLatestTimerTickMessage()
        {
            return _latestTimerTickMessage;
        }

        public PeriodicPointsTimerStateMessage GetLatestPeriodicPointsTimerTickMessage()
        {
            return _latestPeriodicPointsTimerTickMessage;
        }

        public ScrimFacilityControlActionEventMessage GetLatestFacilityControlMessage()
        {
            return _latestFacilityControlMessage;
        }

        public int? GetFacilityControlTeamOrdinal()
        {
            return FacilityControlTeamOrdinal;
        }

        private bool CanChangeRuleset()
        {
            return _currentRound == 0 && _matchState == MatchState.Uninitialized && !_isRunning;
        }

        private void OnTeamOutfitChangeEvent(object sender, ScrimMessageEventArgs<TeamOutfitChangeMessage> e)
        {
            if (Config.IsManualWorldId)
            {
                return;
            }

            int? worldId;

            TeamOutfitChangeMessage message = e.Message;
            TeamChangeType changeType = message.ChangeType;
            bool isRollBack = false;

            if (changeType == TeamChangeType.Add)
            {
                worldId = e.Message.Outfit.WorldId;
            }
            else if (changeType == TeamChangeType.Remove)
            {
                worldId = _teamsManager.GetNextWorldId(Config.WorldId);
                isRollBack = true;
            }
            else
            {
                return;
            }

            if (worldId == null)
            {
                //Console.WriteLine($"ScrimMatchEngine: Resetting World ID from Outfit Change ({MatchConfiguration})!");
                Config.ResetWorldId();
                SendMatchConfigurationUpdateMessage();
            }
            else if (Config.TrySetWorldId((int)worldId, false, isRollBack))
            {
                SendMatchConfigurationUpdateMessage();
            }
        }

        private void OnTeamPlayerChangeEvent(object sender, ScrimMessageEventArgs<TeamPlayerChangeMessage> e)
        {
            if (Config.IsManualWorldId)
            {
                return;
            }

            TeamPlayerChangeMessage message = e.Message;
            TeamPlayerChangeType changeType = message.ChangeType;
            Player player = message.Player;

            // Handle outfit additions/removals via Team Outfit Change events
            if (!player.IsOutfitless)
            {
                return;
            }

            int? worldId;
            bool isRollBack = false;

            if (changeType == TeamPlayerChangeType.Add)
            {
                worldId = player.WorldId;
            }
            else
            {
                worldId = _teamsManager.GetNextWorldId(Config.WorldId);
                isRollBack = true;
            }

            if (worldId == null)
            {
                //Console.WriteLine($"ScrimMatchEngine: Resetting World ID from Player Change!");
                Config.ResetWorldId();
                SendMatchConfigurationUpdateMessage();
            }
            else if (Config.TrySetWorldId((int)worldId, false, isRollBack))
            {
                SendMatchConfigurationUpdateMessage();
            }
        }

        private void OnFacilityControlEvent(object sender, ScrimMessageEventArgs<ScrimFacilityControlActionEventMessage> e)
        {
            if (!_isRunning)
            {
                return;
            }

            if (!Config.EnablePeriodicFacilityControlRewards)
            {
                //_logger.LogInformation($"PeriodicFacilityControlRewards not enabled");
                return;
            }

            ScrimFacilityControlActionEvent controlEvent = e.Message.FacilityControl;
            int eventFacilityId = controlEvent.FacilityId;
            int eventWorldId = controlEvent.WorldId;

            if (eventFacilityId == Config.FacilityId && eventWorldId == Config.WorldId)
            {
                _captureAutoEvent.WaitOne();

                _latestFacilityControlMessage = e.Message;

                FacilityControlTeamOrdinal = controlEvent.ControllingTeamOrdinal;
                _logger.LogInformation($"FacilityControlTeamOrdinal: {controlEvent.ControllingTeamOrdinal}");

                if (_periodicTimer.CanStart())
                {
                    _periodicTimer.Start();
                    _logger.LogInformation($"PeriodicTimer started for team {FacilityControlTeamOrdinal}");
                }
                else
                {
                    _periodicTimer.Restart();
                    _logger.LogInformation($"PeriodicTimer restarted for team {FacilityControlTeamOrdinal}");
                }

                _captureAutoEvent.Set();
            }
        }

#pragma warning disable CS1998

        private async Task OnPeriodiocPointsTimerTick(object sender, ScrimMessageEventArgs<PeriodicPointsTimerStateMessage> e)
        {
            _logger.LogInformation($"Received PeriodicPointsTimerStateMessage");

            _latestPeriodicPointsTimerTickMessage = e.Message;

            if (e.Message.PeriodElapsed && Config.EnablePeriodicFacilityControlRewards && FacilityControlTeamOrdinal.HasValue && _isRunning)
            {
#pragma warning disable CS4014
                Task.Run(() =>
                {
                    ProcessPeriodicPointsTick(e.Message);
                }).ConfigureAwait(false);
#pragma warning restore CS4014
            }
        }

#pragma warning restore CS1998

        private async Task ProcessPeriodicPointsTick(PeriodicPointsTimerStateMessage payload)
        {
            _logger.LogInformation($"Processing PeriodicPointsTimer tick");

            if (!_isRunning)
            {
                _logger.LogInformation($"Failed to process PeriodicPointsTimer tick: match is not running");
                return;
            }

            DateTime timestamp = DateTime.Now;

            int controllingTeamOrdinal = FacilityControlTeamOrdinal.Value;

            try
            {
                int? points = _matchScorer.ScorePeriodicFacilityControlTick(controllingTeamOrdinal);

                if (!points.HasValue)
                {
                    _logger.LogInformation($"Failed to score PeriodicPointsTimer tick: ScrimMatchScorer returned no points value");
                    return;
                }

                _logger.LogInformation($"Scored PeriodicPointsTimer tick: {points.Value} points");

                ScrimPeriodicControlTick periodicTickModel = new()
                {
                    ScrimMatchId = GetMatchId(),
                    Timestamp = timestamp,
                    ScrimMatchRound = GetCurrentRound(),
                    TeamOrdinal = controllingTeamOrdinal,
                    Points = points.Value
                };

                await _matchDataService.SaveScrimPeriodicControlTick(periodicTickModel);

                _logger.LogInformation($"ScrimPeriodicControlTick saved to Db: Round {periodicTickModel.ScrimMatchRound}, Team {controllingTeamOrdinal}, {points.Value} points");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return;
            }
        }

        private async Task OnEndRoundCheckerMessage(object sender, ScrimMessageEventArgs<EndRoundCheckerMessage> e)
        {
            _logger.LogInformation($"OnEndRoundCheckerMessage received: {Enum.GetName(typeof(EndRoundReason), e.Message.EndRoundReason)}");

            if (_isRunning)
            {
                await EndRound();
            }
        }

        private void OnMatchConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.WorldId))
            {
                _wsMonitor.SetWorldSubscription(Config.WorldId);
            }

            if (e.PropertyName == nameof(Config.FacilityId))
            {
                _wsMonitor.SetFacilitySubscription(Config.FacilityId);
            }

            _logger.LogDebug("Engine match configuration property changed: {prop}", e.PropertyName);

            SendMatchConfigurationUpdateMessage();
        }

        #region Outbound Messages

        private void SendMatchStateUpdateMessage()
        {
            _messageService.BroadcastMatchStateUpdateMessage(new MatchStateUpdateMessage(_matchState, _currentRound, DateTime.UtcNow, Config.Title, _matchDataService.CurrentMatchId));
        }

        private void SendMatchConfigurationUpdateMessage()
        {
            _messageService.BroadcastMatchConfigurationUpdateMessage(new MatchConfigurationUpdateMessage(Config));
        }

        #endregion Outbound Messages
    }
}