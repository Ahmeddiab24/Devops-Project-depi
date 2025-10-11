using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using libray2.Models;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace libray2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            ViewBag.Settings = settings ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEntry(WorkspaceEntry entry)
        {
            if (ModelState.IsValid)
            {
                entry.EntryTime = DateTime.Now;
                entry.ExitTime = null; // Mark as active
                _context.WorkspaceEntries.Add(entry);
                await _context.SaveChangesAsync();
                // Redirect to Entries page, which will now be publicly accessible
                return RedirectToAction(nameof(Entries));
            }
            var settings = await _context.Settings.FirstOrDefaultAsync();
            ViewBag.Settings = settings ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };
            return View("Index", entry);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
             var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };

            var activeEntries = await _context.WorkspaceEntries
                .Where(e => e.ExitTime == null)
                .ToListAsync();

            var privateEntries = activeEntries.Where(e => e.RoomType == RoomType.Private).ToList();
            var sharedEntries = activeEntries.Where(e => e.RoomType == RoomType.Shared).ToList();

            // Calculate potential earnings for active entries based on current time and rates from settings
            decimal privateRoomPotentialEarnings = privateEntries.Sum(e => (decimal)(DateTime.Now - e.EntryTime).TotalHours * settings.PrivateRoomRatePerHour);
            decimal sharedRoomPotentialEarnings = sharedEntries.Sum(e => (decimal)(DateTime.Now - e.EntryTime).TotalHours * settings.SharedRoomRatePerHour);
            decimal totalPotentialEarnings = privateRoomPotentialEarnings + sharedRoomPotentialEarnings;

            var dashboardModel = new AdminDashboardViewModel
            {
                PrivateRoomOccupancy = privateEntries.Count,
                SharedRoomOccupancy = sharedEntries.Count,
                PrivateRoomEarnings = privateRoomPotentialEarnings,
                SharedRoomEarnings = sharedRoomPotentialEarnings,
                TotalPotentialEarnings = totalPotentialEarnings,
                ActiveEntries = activeEntries
            };

            return View(dashboardModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetEarnings()//edit to initial mode 
        {
            var activeEntries = await _context.WorkspaceEntries
                .Where(e => e.ExitTime == null)
                .ToListAsync();

            foreach (var entry in activeEntries)
            {
                entry.ExitTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AdminDashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageSettings()//edit price
        {
            var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings();
            var viewModel = new AdminSettingsViewModel
            {
                PrivateRoomRatePerHour = settings.PrivateRoomRatePerHour,
                SharedRoomRatePerHour = settings.SharedRoomRatePerHour
            };
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(AdminSettingsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new Settings();
                    _context.Settings.Add(settings);
                }
                settings.PrivateRoomRatePerHour = viewModel.PrivateRoomRatePerHour;
                settings.SharedRoomRatePerHour = viewModel.SharedRoomRatePerHour;

                await _context.SaveChangesAsync();
                ViewBag.Message = "Settings saved successfully!";
                return View("ManageSettings", viewModel);
            }
            return View("ManageSettings", viewModel);
        }

        // Removed [Authorize(Roles = "Admin")] to make this page publicly accessible
        public async Task<IActionResult> Entries(EntryFilterModel filter)
        {
            var query = _context.WorkspaceEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(e => e.Name.Contains(filter.Name));
            }

            if (filter.RoomType.HasValue)
            {
                query = query.Where(e => e.RoomType == filter.RoomType.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.EntryTime >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(e => e.EntryTime <= filter.EndDate.Value.AddDays(1));
            }

            var entries = await query
                .OrderByDescending(e => e.EntryTime)
                .ToListAsync();

            // Calculate earnings for completed entries
             var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };
            foreach (var entry in entries)
            {
                 if (entry.ExitTime.HasValue)
                 {
                     var duration = entry.ExitTime.Value - entry.EntryTime;
                     if (entry.RoomType == RoomType.Private)
                     {
                         entry.CalculatedEarnings = (decimal)duration.TotalHours * settings.PrivateRoomRatePerHour;
                     }
                     else if (entry.RoomType == RoomType.Shared)
                     {
                         entry.CalculatedEarnings = (decimal)duration.TotalHours * settings.SharedRoomRatePerHour;
                     }
                 }
            }

            ViewBag.Filter = filter;
            return View(entries);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportCsv(EntryFilterModel filter)
        {
            var query = _context.WorkspaceEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(e => e.Name.Contains(filter.Name));
            }

            if (filter.RoomType.HasValue)
            {
                query = query.Where(e => e.RoomType == filter.RoomType.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.EntryTime >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(e => e.EntryTime <= filter.EndDate.Value.AddDays(1));
            }

            var entries = await query
                .OrderByDescending(e => e.EntryTime)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Room Type,Entry Time,Exit Time,Duration (Hours),Calculated Earnings");

             var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };
            foreach (var entry in entries)
            {
                 var duration = (entry.ExitTime ?? DateTime.Now) - entry.EntryTime;
                 decimal earnings = 0;
                 if (entry.ExitTime.HasValue)
                 {
                     if (entry.RoomType == RoomType.Private)
                     {
                         earnings = (decimal)duration.TotalHours * settings.PrivateRoomRatePerHour;
                     }
                     else if (entry.RoomType == RoomType.Shared)
                     {
                         earnings = (decimal)duration.TotalHours * settings.SharedRoomRatePerHour;
                     }
                 }
                csv.AppendLine($"{entry.Name},{entry.RoomType},{entry.EntryTime:g},{entry.ExitTime?.ToString("g") ?? ""},{duration.TotalHours:F2},{earnings:C}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "workspace_entries.csv");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Statistics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var statistics = new WorkspaceStatistics
            {
                TotalEntries = await _context.WorkspaceEntries.CountAsync(),
                PrivateRoomEntries = await _context.WorkspaceEntries.CountAsync(e => e.RoomType == RoomType.Private && e.ExitTime != null),
                SharedRoomEntries = await _context.WorkspaceEntries.CountAsync(e => e.RoomType == RoomType.Shared && e.ExitTime != null),
                TodayEntries = await _context.WorkspaceEntries.CountAsync(e => e.EntryTime >= today && e.EntryTime < tomorrow),
                LastEntryTime = await _context.WorkspaceEntries.MaxAsync(e => e.EntryTime)
            };

            return View(statistics);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CompleteEntry(int id)
        {
            var entry = await _context.WorkspaceEntries.FindAsync(id);

            if (entry == null)
            {
                return NotFound();
            }

            if (!entry.ExitTime.HasValue)
            {
                entry.ExitTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Redirect to show the invoice for the completed entry
            return RedirectToAction(nameof(ShowInvoice), new { id = entry.Id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ShowInvoice(int id)
        {
            var entry = await _context.WorkspaceEntries.FindAsync(id);

            if (entry == null || !entry.ExitTime.HasValue)
            {
                return NotFound(); // Only show invoice for completed entries
            }

            var settings = await _context.Settings.FirstOrDefaultAsync() ?? new Settings { PrivateRoomRatePerHour = 0, SharedRoomRatePerHour = 0 };
            var duration = entry.ExitTime.Value - entry.EntryTime;

            if (entry.RoomType == RoomType.Private)
            {
                entry.CalculatedEarnings = (decimal)duration.TotalHours * settings.PrivateRoomRatePerHour;
            }
            else if (entry.RoomType == RoomType.Shared)
            {
                entry.CalculatedEarnings = (decimal)duration.TotalHours * settings.SharedRoomRatePerHour;
            }

            return View(entry);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}