using System.Collections.Generic;

namespace libray2.Models
{
    public class AdminDashboardViewModel
    {
        public int PrivateRoomOccupancy { get; set; }
        public int SharedRoomOccupancy { get; set; }
        public decimal PrivateRoomEarnings { get; set; }
        public decimal SharedRoomEarnings { get; set; }
        public decimal TotalPotentialEarnings { get; set; }
        public List<WorkspaceEntry> ActiveEntries { get; set; }
    }
} 