using System;

namespace squittal.ScrimPlanetmans.App.Pages.Overlay
{
    [AttributeUsage(AttributeTargets.Property)]
    public class QueryBoolParameterAttribute : Attribute
    {
        public QueryBoolParameterAttribute(string parameter, bool defaultValue = default)
        {
            DefaultValue = defaultValue;
            Parameter = parameter;
        }

        public bool DefaultValue { get; }
        public string Parameter { get; }
    }
}
