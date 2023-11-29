using System;

namespace squittal.ScrimPlanetmans.ScrimMatch.Messages
{
    public record MatchControlSignalReceiptMessage(string Signal, DateTime Timestamp)
    {
        public string Info => $"Received signal \"{Signal}\" at {Timestamp}";
    }
}
