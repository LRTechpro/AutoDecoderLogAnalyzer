using System;
using System.ComponentModel;

namespace AutoDecoder.Models
{
    public sealed class LogSession
    {
        public string Name { get; set; } = $"Session {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        public BindingList<LogLine> AllLines { get; } = new();
        public BindingList<LogLine> FilteredLines { get; } = new();
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    }
}