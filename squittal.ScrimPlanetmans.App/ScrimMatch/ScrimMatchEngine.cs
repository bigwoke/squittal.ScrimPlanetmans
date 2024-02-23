using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using squittal.ScrimPlanetmans.CensusStream;
using squittal.ScrimPlanetmans.Data.Models;
using squittal.ScrimPlanetmans.Models.ScrimEngine;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.ScrimMatch.Timers;
using squittal.ScrimPlanetmans.Services.ScrimMatch;

namespace squittal.ScrimPlanetmans.ScrimMatch
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

        private MatchConfiguration _matchConfiguration = new();

        public MatchConfiguration Config => _matchConfiguration;
        public Ruleset MatchRuleset { get; private set; }
        public int CurrentSeriesMatch { get; private set; } = 0;
        private int? FacilityControlTeamOrdinal { get; set; }

        private bool _isRunning = false;
        private int _currentRound = 0;

        private MatchTimerTickMessage _latestTimerTickMessage;
        private PeriodicPointsTimerStateMessage _latestPeriodicPointsTimerTickMessage;
        private ScrimFacilityControlActionEventMessage _latestFacilityControlMessage;

        private MatchState _matchState = MatchState.Uninitialized;

        private DateTime _matchStartTime;

        private readonly AutoResetEvent _captureAutoEvent = new AutoResetEvent(true);

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
        }

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

            var previousWorldId = _matchConfiguration.WorldIdString;
            var previousIsManualWorldId = _matchConfiguration.IsManualWorldId;

            var previousEndRoundOnFacilityCapture = _matchConfiguration.EndRoundOnFacilityCapture;
            var previousIsManualEndRoundOnFacilityCapture = _matchConfiguration.EndRoundOnFacilityCapture;

            var previousTargetPointValue = _matchConfiguration.TargetPointValue;
            var previousIsManualTargetPointValue = _matchConfiguration.IsManualTargetPointValue;

            var previousInitialPoints = _matchConfiguration.InitialPoints;
            var previousIsManualInitialPoints = _matchConfiguration.IsManualInitialPoints;

            var previousPeriodicFacilityControlPoints = _matchConfiguration.PeriodicFacilityControlPoints;
            var previousIsManualPeriodicFacilityControlPoints = _matchConfiguration.IsManualPeriodicFacilityControlPoints;

            var previousPeriodicFacilityControlInterval = _matchConfiguration.PeriodicFacilityControlInterval;
            var previousIsManualPeriodicFacilityControlInterval = _matchConfiguration.IsManualPeriodicFacilityControlInterval;

            var activeRuleset = await _rulesetManager.GetActiveRulesetAsync();
            _matchConfiguration = new MatchConfiguration(activeRuleset);

            if (isRematch)
            {
                _matchConfiguration.TrySetWorldId(previousWorldId, previousIsManualWorldId);
                _matchConfiguration.TrySetEndRoundOnFacilityCapture(previousEndRoundOnFacilityCapture, previousIsManualEndRoundOnFacilityCapture);
                
                _matchConfiguration.TrySetTargetPointValue(previousTargetPointValue, previousIsManualTargetPointValue);
                _matchConfiguration.TrySetInitialPoints(previousInitialPoints, previousIsManualInitialPoints);
                _matchConfiguration.TrySetPeriodicFacilityControlPoints(previousPeriodicFacilityControlPoints, previousIsManualPeriodicFacilityControlPoints);
                _matchConfiguration.TrySetPeriodicFacilityControlInterval(previousPeriodicFacilityControlInterval, previousIsManualPeriodicFacilityControlInterval);
            }
            else
            {
                _matchConfiguration.TrySetEndRoundOnFacilityCapture(activeRuleset.DefaultEndRoundOnFacilityCapture, false);

                _matchConfiguration.TrySetTargetPointValue(activeRuleset.TargetPointValue, false);
                _matchConfiguration.TrySetInitialPoints(activeRuleset.InitialPoints, false);
                _matchConfiguration.TrySetPeriodicFacilityControlPoints(activeRuleset.PeriodicFacilityControlPoints, false);
                _matchConfiguration.TrySetPeriodicFacilityControlInterval(activeRuleset.PeriodicFacilityControlInterval, false);
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

        public void ConfigureMatch(MatchConfiguration configuration)
        {
            _matchConfiguration.CopyValues(configuration);

            _wsMonitor.SetFacilitySubscription(_matchConfiguration.FacilityId);
            _wsMonitor.SetWorldSubscription(_matchConfiguration.WorldId);

            SendMatchConfigurationUpdateMessage();
        }

        private bool TrySetMatchRuleset(Ruleset matchRuleset)
        {
            if (CanChangeRuleset())
            {
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
            if (GetLatestTimerTickMessage().State != TimerState.Stopped)
            {
                _logger.LogInformation($"Trying to Halt Match Timer");
                _timer.Halt();
                _logger.LogInformation($"Halted Match Timer");
            }

            if (_matchConfiguration.EnablePeriodicFacilityControlRewards)
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

            if (_matchConfiguration.SaveLogFiles == true)
            {
                var matchId = BuildMatchId();

                _messageService.SetLogFileName($"{matchId}.txt");

                var scrimMatch = new Data.Models.ScrimMatch
                {
                    Id = matchId,
                    StartTime = _matchStartTime,
                    Title = _matchConfiguration.Title,
                    RulesetId = MatchRuleset.Id
                };

                await _matchDataService.SaveToCurrentMatch(scrimMatch);
            }
        }

        private string BuildMatchId()
        {
            var matchId = _matchStartTime.ToString("yyyyMMddTHHmmss");

            for (var i = 1; i <= 3; i++)
            {
                var alias = _teamsManager.GetTeamAliasDisplay(i);

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

            if (_matchConfiguration.EnableRoundTimeLimit)
            {
                _timer.Configure(TimeSpan.FromSeconds(_matchConfiguration.RoundSecondsTotal));
            }
            else
            {
                _timer.Configure(null);
            }
            
            if (_matchConfiguration.EnablePeriodicFacilityControlRewards && _matchConfiguration.PeriodicFacilityControlInterval.HasValue)
            {
                _periodicTimer.Configure(TimeSpan.FromSeconds(_matchConfiguration.PeriodicFacilityControlInterval.Value));
            }

            _teamsManager.ClearPlayerLastKilledByMap();

            await _matchDataService.SaveCurrentMatchRoundConfiguration(_matchConfiguration);
        }

        public void StartRound()
        {
            _isRunning = true;
            _matchState = MatchState.Running;

            _roundEndChecker.Enable();

            if (_matchConfiguration.SaveLogFiles)
            {
                _messageService.EnableLogging();
            }

            _timer.Start();
            _wsMonitor.EnableScoring();

            SendMatchStateUpdateMessage();

            Console.WriteLine($"Match Configuration Settings:\n            Title: {_matchConfiguration.Title} (IsManual={_matchConfiguration.IsManualTitle})\n            Round Length: {_matchConfiguration.RoundSecondsTotal} (IsManual={_matchConfiguration.IsManualRoundSecondsTotal})\n            Point Target: {_matchConfiguration.TargetPointValue} (IsManual={_matchConfiguration.IsManualTargetPointValue})\n            Periodic Control Points: {_matchConfiguration.PeriodicFacilityControlPoints} (IsManual={_matchConfiguration.IsManualPeriodicFacilityControlPoints})\n            Periodic Control Interval: {_matchConfiguration.PeriodicFacilityControlInterval} (IsManual={_matchConfiguration.IsManualPeriodicFacilityControlInterval})\n            World ID: {_matchConfiguration.WorldIdString} (IsManual={_matchConfiguration.IsManualWorldId})\n            Facility ID: {_matchConfiguration.FacilityIdString}\n            End Round on Capture?: {_matchConfiguration.EndRoundOnFacilityCapture} (IsManual={_matchConfiguration.IsManualEndRoundOnFacilityCapture})");
        }

        public void PauseRound()
        {
            _isRunning = false;
            _matchState = MatchState.Paused;

            _roundEndChecker.Disable();

            _wsMonitor.DisableScoring();

            _timer.Pause();
            if (_matchConfiguration.EnablePeriodicFacilityControlRewards)
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
            if (_matchConfiguration.EnablePeriodicFacilityControlRewards)
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

            if (_matchConfiguration.SaveLogFiles)
            {
                _messageService.EnableLogging();
            }

            _timer.Resume();
            if (_matchConfiguration.EnablePeriodicFacilityControlRewards)
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
            return (_currentRound == 0 && _matchState == MatchState.Uninitialized && !_isRunning);
        }


        private void OnTeamOutfitChangeEvent(object sender, ScrimMessageEventArgs<TeamOutfitChangeMessage> e)
        {
            if (_matchConfiguration.IsManualWorldId)
            {
                return;
            }

            int? worldId;

            var message = e.Message;
            var changeType = message.ChangeType;
            bool isRollBack = false;

            if (changeType == TeamChangeType.Add)
            {
                worldId = e.Message.Outfit.WorldId;
            }
            else if (changeType == TeamChangeType.Remove)
            {
                worldId = _teamsManager.GetNextWorldId(_matchConfiguration.WorldId);
                isRollBack = true;
            }
            else
            {
                return;
            }

            if (worldId == null)
            {
                //Console.WriteLine($"ScrimMatchEngine: Resetting World ID from Outfit Change ({MatchConfiguration})!");
                _matchConfiguration.ResetWorldId();
                SendMatchConfigurationUpdateMessage();
            }
            else if (_matchConfiguration.TrySetWorldId((int)worldId, false, isRollBack))
            {
                SendMatchConfigurationUpdateMessage();
            }
        }

        private void OnTeamPlayerChangeEvent(object sender, ScrimMessageEventArgs<TeamPlayerChangeMessage> e)
        {           
            if (_matchConfiguration.IsManualWorldId)
            {
                return;
            }

            var message = e.Message;
            var changeType = message.ChangeType;
            var player = message.Player;


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
                worldId = _teamsManager.GetNextWorldId(_matchConfiguration.WorldId);
                isRollBack = true;
            }

            if (worldId == null)
            {
                //Console.WriteLine($"ScrimMatchEngine: Resetting World ID from Player Change!");
                _matchConfiguration.ResetWorldId();
                SendMatchConfigurationUpdateMessage();
            }
            else if (_matchConfiguration.TrySetWorldId((int)worldId, false, isRollBack))
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
            
            if (!_matchConfiguration.EnablePeriodicFacilityControlRewards)
            {
                //_logger.LogInformation($"PeriodicFacilityControlRewards not enabled");
                return;
            }

            var controlEvent = e.Message.FacilityControl;
            var eventFacilityId = controlEvent.FacilityId;
            var eventWorldId = controlEvent.WorldId;

            if (eventFacilityId == _matchConfiguration.FacilityId && eventWorldId == _matchConfiguration.WorldId)
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


            if (e.Message.PeriodElapsed && _matchConfiguration.EnablePeriodicFacilityControlRewards && FacilityControlTeamOrdinal.HasValue && _isRunning)
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

            var timestamp = DateTime.Now;

            var controllingTeamOrdinal = FacilityControlTeamOrdinal.Value;

            try
            {
                var points = _matchScorer.ScorePeriodicFacilityControlTick(controllingTeamOrdinal);

                if (!points.HasValue)
                {
                    _logger.LogInformation($"Failed to score PeriodicPointsTimer tick: ScrimMatchScorer returned no points value");
                    return;
                }

                _logger.LogInformation($"Scored PeriodicPointsTimer tick: {points.Value} points");


                var periodicTickModel = new ScrimPeriodicControlTick()
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
            SendMatchConfigurationUpdateMessage();
        }

        #region Outbound Messages
        private void SendMatchStateUpdateMessage()
        {
            _messageService.BroadcastMatchStateUpdateMessage(new MatchStateUpdateMessage(_matchState, _currentRound, DateTime.UtcNow, _matchConfiguration.Title, _matchDataService.CurrentMatchId));
        }

        private void SendMatchConfigurationUpdateMessage()
        {
            _messageService.BroadcastMatchConfigurationUpdateMessage(new MatchConfigurationUpdateMessage(_matchConfiguration));
        }
        #endregion Outbound Messages
    }
}
