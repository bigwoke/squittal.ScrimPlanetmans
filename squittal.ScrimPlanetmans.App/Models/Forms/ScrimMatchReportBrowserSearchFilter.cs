using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace squittal.ScrimPlanetmans.Models.Forms
{
    public class ScrimMatchReportBrowserSearchFilter
    {
        private static readonly Regex _teamAliasRegex = new("^[A-Za-z0-9]{1,4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string _searchInput = string.Empty;
        private int _rulesetId = 0; // Any
        private int _worldId = 19; // Jaeger
        private int _facilityId = 0; // Any
        private DateTime _searchStartDate = new DateTime(2012, 11, 20); // PlanetSide 2 release date
        private DateTime _searchEndDate = DateTime.UtcNow.AddDays(1); // Day after object creation
        private int _minRounds = 2; // 2+ rounds
        private bool _isDefaultFilter = true;

        public string SearchInput
        {
            get => _searchInput;
            set
            {
                _searchInput = value;
                _isDefaultFilter = false;
            }
        }

        public int RulesetId
        {
            get => _rulesetId;
            set
            {
                _rulesetId = value;
                _isDefaultFilter = false;
            }
        }

        public int WorldId
        {
            get => _worldId;
            set
            {
                _worldId = value;
                _isDefaultFilter = false;
            }
        }

        public int FacilityId
        {
            get => _facilityId;
            set
            {
                _facilityId = value;
                _isDefaultFilter = false;
            }
        }

        public DateTime SearchStartDate
        {
            get => _searchStartDate;
            set
            {
                _searchStartDate = value;
                _isDefaultFilter = false;
            }
        }

        public DateTime SearchEndDate
        {
            get => _searchEndDate;
            set
            {
                _searchEndDate = value;
                _isDefaultFilter = false;
            }
        }

        public int MinimumRoundCount
        {
            get => _minRounds;
            set
            {
                _minRounds = value;
                _isDefaultFilter = false;
            }
        }

        public bool IsDefaultFilter => _isDefaultFilter;

        public List<string> SearchTermsList { get; private set; } = new List<string>();
        public List<string> AliasSearchTermsList { get; private set; } = new List<string>();

        public void ParseSearchTermsString()
        {
            SearchTermsList = new List<string>();
            AliasSearchTermsList = new List<string>();
            
            if (string.IsNullOrWhiteSpace(_searchInput))
            {
                return;
            }

            foreach (string term in _searchInput.Split(' '))
            {
                string termLower = term.ToLower();

                if (_teamAliasRegex.Match(termLower).Success && !AliasSearchTermsList.Contains(termLower) && termLower != "vs" && termLower != "ps2")
                {
                    AliasSearchTermsList.Add(termLower);
                }
                if (!SearchTermsList.Contains(termLower) && termLower.Length > 1)
                {
                    SearchTermsList.Add(termLower);
                }
            }
        }
    }
}
