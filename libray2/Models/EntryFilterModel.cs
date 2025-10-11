using System;

namespace libray2.Models
{
    public class EntryFilterModel
    {
        public string? Name { get; set; }
        public RoomType? RoomType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
} 