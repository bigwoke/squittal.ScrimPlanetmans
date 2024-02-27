using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using squittal.ScrimPlanetmans.Models;
using squittal.ScrimPlanetmans.Models.ScrimMatchReports;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;

namespace squittal.ScrimPlanetmans.App.Pages.Overlay.MatchAnalyticReports
{
    public partial class MatchAnalyticReportContainer
    {
        private const int RefreshTimerMs = 5000;

        private readonly Regex _idRegex = new("[0-9]{19}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _nameRegex = new("[A-Za-z0-9]{1,32}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool _renderedCurrentRoundOnly;
        private OverlayStatsDisplayType? _renderedStatsType;

        private bool _useObjectiveStats = false;

        private string _scrimMatchId;
        private string _renderedScrimMatchId;
        private ScrimMatchInfo _matchInfo;

        private bool _isLoadingScrimPlayers = false;
        private bool _isChangingScrimMatch = true;
        private bool _isChangingMatchRound = false;
        private MatchState _matchState;
        private int _currentRound;

        private Timer _refreshTimer;
        private int _refreshTimerCount = 0;
        private int _matchStateUpdateCount = 0;

        private IEnumerable<ScrimMatchReportInfantryPlayerStats> _playerStats;
        private IEnumerable<ScrimMatchReportInfantryTeamStats> _teamStats;
        private IEnumerable<ScrimMatchReportInfantryPlayerRoundStats> _playerRoundStats;
        private IEnumerable<ScrimMatchReportInfantryTeamRoundStats> _teamRoundStats;

        private int _maxPlayerPoints = 1;
        private bool _anyRevives = false;

        private CancellationTokenSource cts;

        [Parameter]
        public bool CurrentRoundOnly { get; set; } = MatchOverlay.DefaultShowCurrentRoundOnly;

        [Parameter]
        public bool ShowHsr { get; set; } = MatchOverlay.DefaultShowHsr;

        [Parameter]
        public OverlayStatsDisplayType StatsType { get; set; } = MatchOverlay.DefaultStatsType;

        [Parameter]
        public bool Debug { get; set; } = false;


        #region Initialization Methods
        protected override async Task OnInitializedAsync()
        {
            MessageService.RaiseMatchStateUpdateEvent += ReceiveMatchStateUpdateEvent;

            _scrimMatchId = ScrimMatchEngine.GetMatchId();
            _matchState = ScrimMatchEngine.GetMatchState();
            _currentRound = ScrimMatchEngine.GetCurrentRound();

            _refreshTimer = new Timer(HandleTimerTick, null, 0, RefreshTimerMs);

            await RefreshAllStats();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_renderedStatsType != StatsType)
            {
                if (StatsType == OverlayStatsDisplayType.InfantryObjective)
                {
                    _renderedStatsType = StatsType;
                    _useObjectiveStats = true;
                    InvokeAsyncStateHasChanged();
                }
                else
                {
                    if (_renderedStatsType != MatchOverlay.DefaultStatsType)
                    {
                        _renderedStatsType = MatchOverlay.DefaultStatsType;
                        _useObjectiveStats = false;
                        InvokeAsyncStateHasChanged();
                    }
                }
            }

            if (_renderedCurrentRoundOnly != CurrentRoundOnly)
            {
                _renderedCurrentRoundOnly = CurrentRoundOnly;
                _currentRound = ScrimMatchEngine.GetCurrentRound();

                _isChangingMatchRound = true;
                InvokeAsyncStateHasChanged();

                await RefreshAllStats();

                _isChangingMatchRound = false;
                InvokeAsyncStateHasChanged();
            }
        }

        public void Dispose()
        {
            MessageService.RaiseMatchStateUpdateEvent -= ReceiveMatchStateUpdateEvent;

            _refreshTimer.Dispose();

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        private async Task RefreshAllStats()
        {
            _isLoadingScrimPlayers = true;

            if (!string.IsNullOrWhiteSpace(_scrimMatchId))
            {
                // If a process is already underway, cancel it
                cts?.Cancel();

                // Set cts to cancel the current process if another table refresh is requested
                CancellationTokenSource newCTS = new();
                cts = newCTS;

                try
                {
                    _renderedScrimMatchId = _scrimMatchId;

                    List<Task> taskList = new()
                    {
                        LoadMatchInfo(cts.Token)
                    };

                    if (!_renderedCurrentRoundOnly)
                    {
                        taskList.Add(LoadInfantryPlayerStats(cts.Token));
                        taskList.Add(LoadInfantryTeamStats(cts.Token));
                    }
                    else
                    {
                        taskList.Add(LoadInfantryPlayerRoundStats(cts.Token));
                        taskList.Add(LoadInfantryTeamRoundStats(cts.Token));
                    }

                    await Task.WhenAll(taskList);

                    cts.Token.ThrowIfCancellationRequested();
                }
                catch (Exception ex)
                {
                    // Ignore
                    Console.WriteLine($"Failed to refresh match stats with error: {ex.Message}");
                }

                // When the process is complete, signal that another process can proceed
                if (cts == newCTS)
                {
                    cts = null;
                }
            }

            if (_renderedCurrentRoundOnly && _teamRoundStats != null && _teamRoundStats.Any())
            {
                _anyRevives = _teamRoundStats.Where(r => r.ScrimMatchRound == _currentRound).Max(r => r.Revives) > 0;
            }
            else if (!_renderedCurrentRoundOnly && _teamStats != null && _teamStats.Any())
            {
                _anyRevives = _teamStats.Max(r => r.Revives) > 0;
            }

            _isLoadingScrimPlayers = false;
            _isChangingScrimMatch = false;
            InvokeAsyncStateHasChanged();
        }

        #region Individual Data Fetchers
        private async Task LoadInfantryPlayerStats(CancellationToken cancellationToken)
        {
            _playerStats = await ReportDataService.GetHistoricalScrimMatchInfantryPlayerStatsAsync(_scrimMatchId, cancellationToken);

            if (_playerStats != null && _playerStats.Any())
            {
                _maxPlayerPoints = _playerStats.Max(p => p.Points);
            }
        }

        private async Task LoadInfantryPlayerRoundStats(CancellationToken cancellationToken)
        {
            _playerRoundStats = await ReportDataService.GetHistoricalScrimMatchInfantryPlayerRoundStatsAsync(_scrimMatchId, _currentRound, cancellationToken);

            if (_playerRoundStats != null && _playerRoundStats.Any())
            {
                _maxPlayerPoints = _playerRoundStats.Max(p => p.Points);
            }
        }

        private async Task LoadInfantryTeamStats(CancellationToken cancellationToken)
        {
            _currentRound = ScrimMatchEngine.GetCurrentRound();
            _teamStats = await ReportDataService.GetHistoricalScrimMatchInfantryTeamStatsAsync(_scrimMatchId, cancellationToken);
        }

        private async Task LoadInfantryTeamRoundStats(CancellationToken cancellationToken)
        {
            _currentRound = ScrimMatchEngine.GetCurrentRound();
            _teamRoundStats = await ReportDataService.GetHistoricalScrimMatchInfantryTeamRoundStatsAsync(_scrimMatchId, _currentRound, cancellationToken);
        }

        private async Task LoadMatchInfo(CancellationToken cancellationToken)
        {
            _matchInfo = await ReportDataService.GetHistoricalScrimMatchInfoAsync(_scrimMatchId, cancellationToken);
        }
        #endregion Individual Data Fetchers
        #endregion Initialization Methods

        #region Event Handling
        private async void HandleTimerTick(object stateInfo)
        {
            if (_isChangingScrimMatch || _isLoadingScrimPlayers || _isChangingMatchRound)
            {
                return;
            }

            _refreshTimerCount += 1;
            await RefreshAllStats();
        }

        private async void ReceiveMatchStateUpdateEvent(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
        {
            if (_isChangingScrimMatch || _isLoadingScrimPlayers)
            {
                return;
            }

            string updateMatchId = e.Message.MatchId;
            MatchState matchState = e.Message.MatchState;
            int currentRound = e.Message.CurrentRound;

            if (matchState == MatchState.Running && _matchState != MatchState.Running)
            {
                _refreshTimer.Change(RefreshTimerMs, RefreshTimerMs);
            }
            else if (matchState != MatchState.Running)
            {
                _refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            _matchState = matchState;

            if (string.IsNullOrWhiteSpace(_scrimMatchId) || (!string.IsNullOrWhiteSpace(updateMatchId) && (_scrimMatchId != updateMatchId)))
            {
                _scrimMatchId = updateMatchId;
                _isChangingScrimMatch = true;
                InvokeAsyncStateHasChanged();
            }
            else if (_currentRound != currentRound)
            {
                _currentRound = currentRound;
                _isChangingMatchRound = true;
                InvokeAsyncStateHasChanged();
            }

            _matchStateUpdateCount += 1;

            await RefreshAllStats();

            _isChangingScrimMatch = false;
            _isChangingMatchRound = false;
        }
        #endregion Event Handling

        #region Utilities
        private string GetCharacterNameFullFromParameter(string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString) || _playerStats == null || !_playerStats.Any())
            {
                return string.Empty;
            }

            bool isId = _idRegex.Match(inputString).Success;

            if (isId)
            {
                return _playerStats.Where(p => p.CharacterId == inputString).Select(p => p.NameFull).FirstOrDefault();
            }

            bool isName = _nameRegex.Match(inputString).Success;

            if (isName)
            {
                string nameFullMatch = _playerStats.Where(p => p.NameFull == inputString).Select(p => p.NameFull).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(nameFullMatch))
                {
                    return nameFullMatch;
                }

                string nameDisplayMatch = _playerStats.Where(p => p.NameDisplay == inputString).Select(p => p.NameFull).FirstOrDefault();
                return nameDisplayMatch;
            }

            return string.Empty;
        }

        private string GetCharacterIdFromParameter(string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString) || _playerStats == null || !_playerStats.Any())
            {
                return string.Empty;
            }

            bool isId = _idRegex.Match(inputString).Success;

            if (isId)
            {
                return _playerStats.Where(p => p.CharacterId == inputString).Select(p => p.CharacterId).FirstOrDefault();
            }

            bool isName = _nameRegex.Match(inputString).Success;

            if (isName)
            {
                string nameFullMatch = _playerStats.Where(p => p.NameFull == inputString).Select(p => p.CharacterId).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(nameFullMatch))
                {
                    return nameFullMatch;
                }

                string nameDisplayMatch = _playerStats.Where(p => p.NameDisplay == inputString).Select(p => p.CharacterId).FirstOrDefault();
                return nameDisplayMatch;
            }

            return string.Empty;
        }

        private decimal GetPointGraphWidth(int points)
        {
            int maxPoints = _maxPlayerPoints;
            int playerPoints = points;

            return (playerPoints >= 1 && maxPoints > 0) ? Math.Ceiling(100 * ((decimal)playerPoints / (decimal)maxPoints)) : 4;
        }
        private void InvokeAsyncStateHasChanged() => InvokeAsync(StateHasChanged);

        private static double GetStatOpacity(int value)
        {
            return (value != 0)
                ? 1.0
                : 0.25;
        }

        private static string GetNetScoreLabelCssClass(int netScore)
        {
            if (netScore == 0)
            {
                return "neutral";
            }
            else if (netScore < 0)
            {
                return "negative";
            }
            else
            {
                return string.Empty;
            }
        }

        private static string GetNetScoreLabelText(int netScore)
        {
            if (netScore == 0)
            {
                return "•";
            }
            else
            {
                return "Δ";
            }
        }

        private static string GetLoadoutIconFromLoadoutId(PlanetsideClass planetsideClass)
        {
            if (planetsideClass == PlanetsideClass.Infiltrator)
            {
                return "infil";
            }
            else if (planetsideClass == PlanetsideClass.LightAssault)
            {
                return "la";
            }
            else if (planetsideClass == PlanetsideClass.Medic)
            {
                return "medic";
            }
            else if (planetsideClass == PlanetsideClass.Engineer)
            {
                return "engy";
            }
            else if (planetsideClass == PlanetsideClass.HeavyAssault)
            {
                return "heavy";
            }
            else if (planetsideClass == PlanetsideClass.MAX)
            {
                return "max";
            }
            else
            {
                return "unknown";
            }
        }

        private static string GetLoadoutIconFilterStyle(int factionId)
        {
            if (factionId == 1) // VS
            {
                //return "brightness(0) saturate(100%) invert(39%) sepia(41%) saturate(1794%) hue-rotate(224deg) brightness(98%) contrast(91%);";
                return "brightness(0) saturate(100%) invert(78%) sepia(7%) saturate(3557%) hue-rotate(203deg) brightness(101%) contrast(104%)";
            }
            if (factionId == 2) // NC
            {
                //return "brightness(0) saturate(100%) invert(49%) sepia(57%) saturate(1428%) hue-rotate(184deg) brightness(101%) contrast(98%);";
                return "brightness(0) saturate(100%) invert(79%) sepia(13%) saturate(853%) hue-rotate(177deg) brightness(105%) contrast(98%);";
            }
            if (factionId == 3) // TR
            {
                //return "brightness(0) saturate(100%) invert(50%) sepia(34%) saturate(1466%) hue-rotate(307deg) brightness(98%) contrast(88%);";
                return "brightness(0) saturate(100%) invert(84%) sepia(7%) saturate(946%) hue-rotate(309deg) brightness(101%) contrast(101%);";
            }

            return string.Empty;
        }

        private static string GetRoundRowBorderClass(int round, int maxRounds)
        {
            return round == maxRounds ? "last-of-group" : string.Empty;
        }

        private static MarkupString GetAbbreviatedDamageDealt(int damageDealt, bool isTeamValue)
        {
            //if (damageDealt < 10000)
            string color = isTeamValue ? "var(sq-ov-ps2-primary)" : "var(--sq-ov-ps2-primary-dark-alpha-70)";

            if (damageDealt < 1000)
            {
                return (MarkupString)damageDealt.ToString("N0");
            }
            else if (damageDealt < 100000)
            {
                return (MarkupString)$"{Math.Round(damageDealt / 1000.0, 1)}<span style=\"color: {color};\">k</span>";
            }
            else
            {
                return (MarkupString)$"{Math.Round(damageDealt / 1000.0, 0)}<span style=\"color: {color};\">k</span>"; ;
            }
        }
        #endregion Utilities
    }
}