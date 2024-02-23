using System.ComponentModel;
using System.Threading;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.ScrimMatch.Timers;
using squittal.ScrimPlanetmans.Services.Rulesets;

namespace squittal.ScrimPlanetmans.Models.ScrimEngine
{
    public class MatchConfiguration : INotifyPropertyChanged
    {
        public const string DefaultTitle = "PS2 Scrims";
        public const int DefaultRoundSecondsTotal = 900; // 15 Minutes
        public const int DefaultWorldId = 19; // Jaeger
        public const int DefaultNoFacilityId = -1; // No facility
        public const bool DefaultEndRoundOnFacilityCapture = false;
        public const int DefaultTargetPointValue = 200;
        public const int DefaultInitialPoints = 0;
        public const int DefaultPeriodicFacilityControlPoints = 5;
        public const int DefaultPeriodicFacilityControlInterval = 15;

        private readonly AutoResetEvent _autoEvent = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoEventRoundSeconds = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoEventMatchTitle = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoEndRoundOnFacilityCapture = new AutoResetEvent(true);

        private readonly AutoResetEvent _autoTargetPointValue = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoInitialPoints = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoPeriodicFacilityControlPoints = new AutoResetEvent(true);
        private readonly AutoResetEvent _autoPeriodicFacilityControlInterval = new AutoResetEvent(true);

        private string _title = DefaultTitle;
        private bool _isManualTitle = false;

        private int _roundSecondsTotal = DefaultRoundSecondsTotal;
        private bool _isManualRoundSecondsTotal = false;

        private int _worldId = DefaultWorldId;
        private bool _isManualWorldId = false;
        private bool _isWorldIdSet = false;

        private int _facilityId = DefaultNoFacilityId;

        private bool _endRoundOnFacilityCapture = DefaultEndRoundOnFacilityCapture;
        private bool _isManualEndRoundOnFacilityCapture = false;

        private int? _targetPointValue = null;
        private bool _isManualTargetPointValue = false;

        private int? _initialPoints = null;
        private bool _isManualInitialPoints = false;

        private int? _periodicFacilityControlPoints = null;
        private bool _isManualPeriodicFacilityControlPoints = false;

        private int? _periodicFacilityControlInterval = null;
        private bool _isManualPeriodicFacilityControlInterval = false;

        // TODO: get the default values for these from the ruleset manager or smth
        private bool _enableRoundTimeLimit;
        private TimerDirection? _roundTimerDirection = null;
        private bool _endRoundOnPointValueReached;
        private MatchWinCondition _matchWinCondition;
        private RoundWinCondition _roundWinCondition;
        private bool _enablePeriodicFacilityControlRewards = false;
        private PointAttributionType? _periodFacilityControlPointAttributionType = null;

        public MatchConfiguration()
        {
        }

        public MatchConfiguration(Ruleset ruleset)
        {
            Title = ruleset.DefaultMatchTitle;
            EnableRoundTimeLimit = ruleset.EnableRoundTimeLimit;
            RoundSecondsTotal = ruleset.DefaultRoundLength;
            RoundTimerDirection = ruleset.RoundTimerDirection;
            EndRoundOnPointValueReached = ruleset.EndRoundOnPointValueReached;
            MatchWinCondition = ruleset.MatchWinCondition;
            RoundWinCondition = ruleset.RoundWinCondition;
            TargetPointValue = ruleset.TargetPointValue;
            InitialPoints = ruleset.InitialPoints;
            EnablePeriodicFacilityControlRewards = ruleset.EnablePeriodicFacilityControlRewards;
            PeriodFacilityControlPointAttributionType = ruleset.PeriodFacilityControlPointAttributionType;
            PeriodicFacilityControlPoints = ruleset.PeriodicFacilityControlPoints;
            PeriodicFacilityControlInterval = ruleset.PeriodicFacilityControlInterval;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Base Properties
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public bool IsManualTitle
        {
            get => _isManualTitle;
            private set
            {
                if (_isManualTitle != value)
                {
                    _isManualTitle = value;
                    OnPropertyChanged(nameof(IsManualTitle));
                }
            }
        }

        public int RoundSecondsTotal
        {
            get => _roundSecondsTotal;
            set
            {
                if (_roundSecondsTotal != value)
                {
                    _roundSecondsTotal = value;
                    OnPropertyChanged(nameof(RoundSecondsTotal));
                }
            }
        }

        public bool IsManualRoundSecondsTotal
        {
            get => _isManualRoundSecondsTotal;
            private set
            {
                if (_isManualRoundSecondsTotal != value)
                {
                    _isManualRoundSecondsTotal = value;
                    OnPropertyChanged(nameof(IsManualRoundSecondsTotal));
                }
            }
        }

        public int WorldId
        {
            get => _worldId;
            set
            {
                if (_worldId != value)
                {
                    _worldId = value;
                    OnPropertyChanged(nameof(WorldId));
                }
            }
        }

        public bool IsManualWorldId
        {
            get => _isManualWorldId;
            private set
            {
                if (_isManualWorldId != value)
                {
                    _isManualWorldId = value;
                    OnPropertyChanged(nameof(IsManualWorldId));
                }
            }
        }

        public bool IsWorldIdSet
        {
            get => _isWorldIdSet;
            private set
            {
                if (_isWorldIdSet != value)
                {
                    _isWorldIdSet = value;
                    OnPropertyChanged(nameof(IsWorldIdSet));
                }
            }
        }

        public int FacilityId
        {
            get => _facilityId;
            set
            {
                if (_facilityId != value)
                {
                    _facilityId = value;
                    OnPropertyChanged(nameof(FacilityId));
                }
            }
        }

        public bool EndRoundOnFacilityCapture // TODO: move this setting to the Ruleset model
        {
            get => _endRoundOnFacilityCapture;
            set
            {
                if (_endRoundOnFacilityCapture != value)
                {
                    _endRoundOnFacilityCapture = value;
                    OnPropertyChanged(nameof(EndRoundOnFacilityCapture));
                }
            }
        }

        public bool IsManualEndRoundOnFacilityCapture
        {
            get => _isManualEndRoundOnFacilityCapture;
            private set
            {
                if (_isManualEndRoundOnFacilityCapture != value)
                {
                    _isManualEndRoundOnFacilityCapture = value;
                    OnPropertyChanged(nameof(IsManualEndRoundOnFacilityCapture));
                }
            }
        }

        public int? TargetPointValue
        {
            get => _targetPointValue;
            set
            {
                if (_targetPointValue != value)
                {
                    _targetPointValue = value;
                    OnPropertyChanged(nameof(TargetPointValue));
                }
            }
        }

        public bool IsManualTargetPointValue
        {
            get => _isManualTargetPointValue;
            private set
            {
                if (_isManualTargetPointValue != value)
                {
                    _isManualTargetPointValue = value;
                    OnPropertyChanged(nameof(IsManualTargetPointValue));
                }
            }
        }

        public int? InitialPoints
        {
            get => _initialPoints;
            set
            {
                if (_initialPoints != value)
                {
                    _initialPoints = value;
                    OnPropertyChanged(nameof(InitialPoints));
                }
            }
        }

        public bool IsManualInitialPoints
        {
            get => _isManualInitialPoints;
            private set
            {
                if (_isManualInitialPoints != value)
                {
                    _isManualInitialPoints = value;
                    OnPropertyChanged(nameof(IsManualInitialPoints));
                }
            }
        }

        public int? PeriodicFacilityControlPoints
        {
            get => _periodicFacilityControlPoints;
            set
            {
                if (_periodicFacilityControlPoints != value)
                {
                    _periodicFacilityControlPoints = value;
                    OnPropertyChanged(nameof(PeriodicFacilityControlPoints));
                }
            }
        }

        public bool IsManualPeriodicFacilityControlPoints
        {
            get => _isManualPeriodicFacilityControlPoints;
            private set
            {
                if (_isManualPeriodicFacilityControlPoints != value)
                {
                    _isManualPeriodicFacilityControlPoints = value;
                    OnPropertyChanged(nameof(IsManualPeriodicFacilityControlPoints));
                }
            }
        }

        public int? PeriodicFacilityControlInterval
        {
            get => _periodicFacilityControlInterval;
            set
            {
                if (_periodicFacilityControlInterval != value)
                {
                    _periodicFacilityControlInterval = value;
                    OnPropertyChanged(nameof(PeriodicFacilityControlInterval));
                }
            }
        }

        public bool IsManualPeriodicFacilityControlInterval
        {
            get => _isManualPeriodicFacilityControlInterval;
            private set
            {
                if (_isManualPeriodicFacilityControlInterval != value)
                {
                    _isManualPeriodicFacilityControlInterval = value;
                    OnPropertyChanged(nameof(IsManualPeriodicFacilityControlInterval));
                }
            }
        }
        #endregion Base Properties

        #region Ruleset Properties
        public bool EnableRoundTimeLimit
        {
            get => _enableRoundTimeLimit;
            set
            {
                if (_enableRoundTimeLimit != value)
                {
                    _enableRoundTimeLimit = value;
                    OnPropertyChanged(nameof(EnableRoundTimeLimit));
                }
            }
        }

        public TimerDirection? RoundTimerDirection
        {
            get => _roundTimerDirection;
            set
            {
                if (_roundTimerDirection != value)
                {
                    _roundTimerDirection = value;
                    OnPropertyChanged(nameof(RoundTimerDirection));
                }
            }
        }

        public bool EndRoundOnPointValueReached
        {
            get => _endRoundOnPointValueReached;
            set
            {
                if (_endRoundOnPointValueReached != value)
                {
                    _endRoundOnPointValueReached = value;
                    OnPropertyChanged(nameof(EndRoundOnPointValueReached));
                }
            }
        }

        public MatchWinCondition MatchWinCondition
        {
            get => _matchWinCondition;
            set
            {
                if (_matchWinCondition != value)
                {
                    _matchWinCondition = value;
                    OnPropertyChanged(nameof(MatchWinCondition));
                }
            }
        }

        public RoundWinCondition RoundWinCondition
        {
            get => _roundWinCondition;
            set
            {
                if (_roundWinCondition != value)
                {
                    _roundWinCondition = value;
                    OnPropertyChanged(nameof(RoundWinCondition));
                }
            }
        }

        public bool EnablePeriodicFacilityControlRewards
        {
            get => _enablePeriodicFacilityControlRewards;
            set
            {
                if (_enablePeriodicFacilityControlRewards != value)
                {
                    _enablePeriodicFacilityControlRewards = value;
                    OnPropertyChanged(nameof(EnablePeriodicFacilityControlRewards));
                }
            }
        }

        public PointAttributionType? PeriodFacilityControlPointAttributionType
        {
            get => _periodFacilityControlPointAttributionType;
            set
            {
                if (_periodFacilityControlPointAttributionType != value)
                {
                    _periodFacilityControlPointAttributionType = value;
                    OnPropertyChanged(nameof(PeriodFacilityControlPointAttributionType));
                }
            }
        }
        #endregion Ruleset Properties

        public bool SaveLogFiles { get; set; } = true;
        public bool SaveEventsToDatabase { get; set; } = true;

        protected void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
        protected void OnPropertyChanged(string propertyName) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        public void CopyValues(MatchConfiguration sourceConfig)
        {
            Title = sourceConfig.Title;
            IsManualTitle = sourceConfig.IsManualTitle;
            RoundSecondsTotal = sourceConfig.RoundSecondsTotal;
            IsManualRoundSecondsTotal = sourceConfig.IsManualRoundSecondsTotal;
            IsManualWorldId = sourceConfig.IsManualWorldId;
            IsWorldIdSet = sourceConfig.IsWorldIdSet;
            WorldId = sourceConfig.WorldId;
            FacilityId = sourceConfig.FacilityId;
            EndRoundOnFacilityCapture = sourceConfig.EndRoundOnFacilityCapture;
            IsManualEndRoundOnFacilityCapture = sourceConfig.IsManualEndRoundOnFacilityCapture;
            TargetPointValue = sourceConfig.TargetPointValue;
            IsManualTargetPointValue = sourceConfig.IsManualTargetPointValue;
            InitialPoints = sourceConfig.InitialPoints;
            IsManualInitialPoints = sourceConfig.IsManualInitialPoints;
            PeriodicFacilityControlPoints = sourceConfig.PeriodicFacilityControlPoints;
            IsManualPeriodicFacilityControlPoints = sourceConfig.IsManualPeriodicFacilityControlPoints;
            PeriodicFacilityControlInterval = sourceConfig.PeriodicFacilityControlInterval;
            IsManualPeriodicFacilityControlInterval = sourceConfig.IsManualPeriodicFacilityControlInterval;
            EnableRoundTimeLimit = sourceConfig.EnableRoundTimeLimit;
            RoundTimerDirection = sourceConfig.RoundTimerDirection;
            EndRoundOnPointValueReached = sourceConfig.EndRoundOnPointValueReached;
            MatchWinCondition = sourceConfig.MatchWinCondition;
            RoundWinCondition = sourceConfig.RoundWinCondition;
            EnablePeriodicFacilityControlRewards = sourceConfig.EnablePeriodicFacilityControlRewards;
            PeriodFacilityControlPointAttributionType = sourceConfig.PeriodFacilityControlPointAttributionType;
        }

        public bool TrySetTitle(string title, bool isManualValue)
        {
            if (!RulesetDataService.IsValidRulesetDefaultMatchTitle(title))
            {
                return false;
            }

            _autoEventMatchTitle.WaitOne();

            if (isManualValue)
            {
                Title = title;
                IsManualTitle = true;

                _autoEventMatchTitle.Set();

                return true;
            }
            else if (!IsManualTitle)
            {
                Title = title;

                _autoEventMatchTitle.Set();

                return true;
            }
            else
            {
                _autoEventMatchTitle.Set();

                return false;
            }
        }

        public bool TrySetRoundLength(int seconds, bool isManualValue)
        {
            if (seconds <= 0)
            {
                return false;
            }

            _autoEventRoundSeconds.WaitOne();

            if (isManualValue)
            {
                RoundSecondsTotal = seconds;
                IsManualRoundSecondsTotal = true;

                _autoEventRoundSeconds.Set();

                return true;
            }
            else if (!IsManualRoundSecondsTotal)
            {
                RoundSecondsTotal = seconds;

                _autoEventRoundSeconds.Set();

                return true;
            }
            else
            {
                _autoEventRoundSeconds.Set();

                return false;
            }
        }

        public bool TrySetTargetPointValue(int? points, bool isManualValue)
        {
            _autoTargetPointValue.WaitOne();

            if (isManualValue)
            {
                TargetPointValue = points;
                IsManualTargetPointValue = true;

                _autoTargetPointValue.Set();
                return true;
            }
            else if (!IsManualTargetPointValue)
            {
                TargetPointValue = points;
                
                _autoTargetPointValue.Set();
                return true;
            }
            else
            {
                _autoTargetPointValue.Set();
                return false;
            }
        }

        public void ResetTargetPointValue()
        {
            TargetPointValue = DefaultTargetPointValue;
            IsManualTargetPointValue = false;
        }

        public bool TrySetInitialPoints(int? points, bool isManualValue)
        {
            _autoInitialPoints.WaitOne();

            if (isManualValue)
            {
                InitialPoints = points;
                IsManualInitialPoints = true;

                _autoTargetPointValue.Set();
                return true;
            }
            else if (!IsManualInitialPoints)
            {
                InitialPoints = points;

                _autoInitialPoints.Set();
                return true;
            }
            else
            {
                _autoInitialPoints.Set();
                return false;
            }
        }

        public void ResetInitialPoints()
        {
            InitialPoints = DefaultInitialPoints;
            IsManualInitialPoints = false;
        }

        public bool TrySetPeriodicFacilityControlPoints(int? points, bool isManualValue)
        {
            _autoPeriodicFacilityControlPoints.WaitOne();

            if (isManualValue)
            {
                PeriodicFacilityControlPoints = points;
                IsManualPeriodicFacilityControlPoints = true;

                _autoPeriodicFacilityControlPoints.Set();
                return true;
            }
            else if (!IsManualPeriodicFacilityControlPoints)
            {
                PeriodicFacilityControlPoints = points;
                IsManualPeriodicFacilityControlPoints = isManualValue;

                _autoPeriodicFacilityControlPoints.Set();
                return true;
            }
            else
            {
                _autoPeriodicFacilityControlPoints.Set();
                return false;
            }
        }

        public void ResetPeriodicFacilityControlPoints()
        {
            PeriodicFacilityControlPoints = DefaultPeriodicFacilityControlPoints;
            IsManualPeriodicFacilityControlPoints = false;
        }

        public bool TrySetPeriodicFacilityControlInterval(int? interval, bool isManualValue)
        {
            _autoPeriodicFacilityControlInterval.WaitOne();

            if (isManualValue)
            {
                PeriodicFacilityControlInterval = interval;
                IsManualPeriodicFacilityControlInterval = true;

                _autoPeriodicFacilityControlInterval.Set();
                return true;
            }
            else if (!IsManualPeriodicFacilityControlInterval)
            {
                PeriodicFacilityControlInterval = interval;
                IsManualPeriodicFacilityControlInterval = isManualValue;

                _autoPeriodicFacilityControlInterval.Set();
                return true;
            }
            else
            {
                _autoPeriodicFacilityControlInterval.Set();
                return false;
            }
        }

        public void ResetPeriodicFacilityControlInterval()
        {
            PeriodicFacilityControlInterval = DefaultPeriodicFacilityControlInterval;
            IsManualPeriodicFacilityControlInterval = false;
        }

        public bool TrySetEndRoundOnFacilityCapture(bool endOnCapture, bool isManualValue)
        {
            _autoEndRoundOnFacilityCapture.WaitOne();

            if (isManualValue)
            {
                EndRoundOnFacilityCapture = endOnCapture;
                IsManualEndRoundOnFacilityCapture = true;

                _autoEndRoundOnFacilityCapture.Set();

                return true;
            }
            else if (!IsManualEndRoundOnFacilityCapture)
            {
                EndRoundOnFacilityCapture = endOnCapture;
                IsManualEndRoundOnFacilityCapture = isManualValue;

                _autoEndRoundOnFacilityCapture.Set();

                return true;
            }
            else
            {
                _autoEndRoundOnFacilityCapture.Set();

                return false;
            }
        }

        public void ResetEndRoundOnFacilityCapture()
        {
            EndRoundOnFacilityCapture = DefaultEndRoundOnFacilityCapture;
            IsManualEndRoundOnFacilityCapture = false;
        }

        public void ResetWorldId()
        {
            WorldId = DefaultWorldId;
            IsManualWorldId = false;
            IsWorldIdSet = false;
        }
        
        public bool TrySetWorldId(int worldId, bool isManualValue = false, bool isRollBack = false)
        {
            if (worldId <= 0)
            {
                return false;
            }

            _autoEvent.WaitOne();

            if (isManualValue)
            {
                WorldId = worldId;
                IsManualWorldId = true;
                IsWorldIdSet = true;

                _autoEvent.Set();

                return true;
            }
            else if (!IsManualWorldId && (!IsWorldIdSet || isRollBack))
            {
                WorldId = worldId;
                IsWorldIdSet = true;

                _autoEvent.Set();

                return true;
            }
            else
            {
                _autoEvent.Set();

                return false;
            }
        }

        public bool TrySetFacilityId(int facilityId)
        {
            if (facilityId <= 0)
            {
                return false;
            }

            _autoEvent.WaitOne();

            FacilityId = facilityId;

            _autoEvent.Set();
            return true;
        }
    }
}
