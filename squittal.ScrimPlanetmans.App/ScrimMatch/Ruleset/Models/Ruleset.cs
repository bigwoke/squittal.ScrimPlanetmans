using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using squittal.ScrimPlanetmans.ScrimMatch.Timers;

namespace squittal.ScrimPlanetmans.ScrimMatch.Models
{
    public class Ruleset
    {
        public const string DefaultMatchTitleValue = "PS2 Scrims";
        public const bool DefaultEnableRoundTimeLimit = true;
        public const int DefaultRoundLengthValue = 900;
        public const TimerDirection DefaultRoundTimerDirection = TimerDirection.Down;
        public const bool DefaultEndRoundOnFacilityCaptureValue = false;
        public const bool DefaultEndRoundOnPointValueReached = false;
        public const int DefaultTargetPointValue = 100;
        public const int DefaultInitialPoints = 0;
        public const bool DefaultEnablePeriodicFacilityControlRewards = false;
        public const int DefaultPeriodicFacilityControlPoints = 1;
        public const int DefaultPeriodicFacilityControlInterval = 10;
        public const PointAttributionType DefaultPeriodicFacilityControlPointAttributionType = PointAttributionType.Standard;
        public const MatchWinCondition DefaultMatchWinCondition = MatchWinCondition.MostPoints;
        public const RoundWinCondition DefaultRoundWinCondition = RoundWinCondition.MostPoints;

        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }

        public bool IsDefault { get; set; }
        public bool IsCustomDefault { get; set; }
        public string SourceFile { get; set; }

        public string DefaultMatchTitle { get; set; } = DefaultMatchTitleValue;

        public bool EnableRoundTimeLimit { get; set; } = DefaultEnableRoundTimeLimit;
        public int DefaultRoundLength { get; set; } = DefaultRoundLengthValue;
        public TimerDirection? RoundTimerDirection { get; set; } = DefaultRoundTimerDirection;

        public bool DefaultEndRoundOnFacilityCapture { get; set; } = DefaultEndRoundOnFacilityCaptureValue;
        public bool EndRoundOnPointValueReached { get; set; } = DefaultEndRoundOnPointValueReached;

        public int? TargetPointValue { get; set; } = DefaultTargetPointValue;
        public int? InitialPoints { get; set; } = DefaultInitialPoints;

        public MatchWinCondition MatchWinCondition { get; set; } = DefaultMatchWinCondition;
        public RoundWinCondition RoundWinCondition { get; set; } = DefaultRoundWinCondition;

        public bool EnablePeriodicFacilityControlRewards { get; set; } = DefaultEnablePeriodicFacilityControlRewards;
        public int? PeriodicFacilityControlPoints { get; set; } = DefaultPeriodicFacilityControlPoints;
        public int? PeriodicFacilityControlInterval { get; set; } = DefaultPeriodicFacilityControlInterval;
        public PointAttributionType? PeriodFacilityControlPointAttributionType { get; set; } = DefaultPeriodicFacilityControlPointAttributionType;


        public ICollection<RulesetActionRule> RulesetActionRules { get; set; }
        public ICollection<RulesetItemCategoryRule> RulesetItemCategoryRules { get; set; }
        public ICollection<RulesetItemRule> RulesetItemRules { get; set; }
        public ICollection<RulesetFacilityRule> RulesetFacilityRules { get; set; }

        public RulesetOverlayConfiguration RulesetOverlayConfiguration { get; set; }

    }
}
