using System;

namespace libray2.Models
{
    public class WorkspaceStatistics
    {
        public int TotalEntries { get; set; }
        public int PrivateRoomEntries { get; set; }
        public int SharedRoomEntries { get; set; }
        public int TodayEntries { get; set; }
        public DateTime LastEntryTime { get; set; }
    }
} 