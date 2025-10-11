using System.ComponentModel.DataAnnotations;

namespace libray2.Models
{
    public class AdminSettingsViewModel
    {
        [Required]
        [Range(0, 1000)] // Example range, adjust as needed
        [Display(Name = "Private Room Rate Per Hour")]
        public decimal PrivateRoomRatePerHour { get; set; }

        [Required]
        [Range(0, 1000)] // Example range, adjust as needed
        [Display(Name = "Shared Room Rate Per Hour")]
        public decimal SharedRoomRatePerHour { get; set; }
    }
} 