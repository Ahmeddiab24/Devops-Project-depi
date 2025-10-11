using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace libray2.Models
{
    public class WorkspaceEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Room type is required")]
        public RoomType RoomType { get; set; }

        public DateTime EntryTime { get; set; }

        public DateTime? ExitTime { get; set; }

        [NotMapped] // This property is calculated and not stored in the database
        public decimal CalculatedEarnings { get; set; }
    }

    public enum RoomType
    {
        Private,
        Shared
    }
} 