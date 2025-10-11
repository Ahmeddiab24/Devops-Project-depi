using System.ComponentModel.DataAnnotations;

namespace libray2.Models
{
    public class Settings
    {
        // Use a fixed ID since there should only be one settings row
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(0, 1000)] // Example range, adjust as needed
        public decimal PrivateRoomRatePerHour { get; set; }

        [Required]
        [Range(0, 1000)] // Example range, adjust as needed
        public decimal SharedRoomRatePerHour { get; set; }
    }
} 