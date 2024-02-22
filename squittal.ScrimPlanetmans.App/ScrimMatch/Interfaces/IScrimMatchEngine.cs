using squittal.ScrimPlanetmans.Models.ScrimEngine;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.ScrimMatch.Timers;
using System.Threading.Tasks;

namespace squittal.ScrimPlanetmans.ScrimMatch
{
    public interface IScrimMatchEngine
    {
        Ruleset MatchRuleset { get; }

        bool ConfigEndRoundOnFacilityCapture { get; }
        int ConfigFacilityId { get; }
        int? ConfigInitialPoints { get; }
        int? ConfigPeriodicFacilityControlInterval { get; }
        int? ConfigPeriodicFacilityControlPoints { get; }
        int ConfigRoundSecondsTotal { get; }
        int? ConfigTargetPointValue { get; }
        string ConfigTitle { get; }
        int ConfigWorldId { get; }
        bool ConfigEnablePeriodicFacilityControlRewards { get; }
        bool ConfigEnableRoundTimeLimit { get; }
        bool ConfigEndRoundOnPointValueReached { get; }
        bool ConfigIsManualTitle { get; }
        bool ConfigIsManualPeriodicFacilityControlInterval { get; }
        bool ConfigIsManualPeriodicFacilityControlPoints { get; }
        bool ConfigIsManualRoundSecondsTotal { get; }
        bool ConfigIsManualTargetPointValue { get; }
        bool ConfigIsWorldIdSet { get; }
        bool ConfigIsManualWorldId { get; }
        bool ConfigIsManualEndRoundOnFacilityCapture { get; }

        Task Start();
        Task InitializeNewMatch();
        void ConfigureMatch(MatchConfiguration configuration);
        Task InitializeNewRound();
        void StartRound();
        void PauseRound();
        void ResumeRound();
        Task EndRound();
        Task ResetRound();
        Task ClearMatch(bool isRematch);
        MatchTimerTickMessage GetLatestTimerTickMessage();
        bool IsRunning();
        int GetCurrentRound();
        MatchState GetMatchState();

        void SubmitPlayersList();
        string GetMatchId();
        PeriodicPointsTimerStateMessage GetLatestPeriodicPointsTimerTickMessage();
        ScrimFacilityControlActionEventMessage GetLatestFacilityControlMessage();
        int? GetFacilityControlTeamOrdinal();

        void ResetConfigWorldId();
        bool TrySetConfigEndRoundOnFacilityCapture(bool endOnCapture, bool isManualValue);
        bool TrySetConfigInitialPoints(int? initialPoints, bool isManualValue);
        bool TrySetConfigPeriodicFacilityControlInterval(int? interval, bool isManualValue);
        bool TrySetConfigPeriodicFacilityControlPoints(int? points, bool isManualValue);
        bool TrySetConfigRoundLength(int seconds, bool isManualValue);
        bool TrySetConfigTargetPointValue(int? targetPointValue, bool isManualValue);
        bool TrySetConfigTitle(string title, bool isManualValue);
        bool TrySetConfigWorldId(int worldId, bool isManualValue = false, bool isRollBack = false);
        bool TrySetConfigWorldId(string worldId, bool isManualValue = false, bool isRollBack = false);
        bool TrySetConfigFacilityId(string facilityId);
    }
}
