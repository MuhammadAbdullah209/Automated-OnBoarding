using System.Net;
using AOB.BAL.Common.Constants;
using AOB.DAL.OnboardingContext;
using AOB.WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AOB.WEB.Controllers
{
    /// <summary>Where the site is used from, and the block list. Super admin only.</summary>
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public class IpAddressesController(OnboardingDbContext db, IpAccessTracker tracker) : Controller
    {
        private const int MaxRows = 300;

        public async Task<IActionResult> Index(string? q, bool blockedOnly = false)
        {
            var query = db.VisitorIps.AsNoTracking().AsQueryable();
            if (blockedOnly)
            {
                query = query.Where(v => v.IsBlocked);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var text = q.Trim();
                query = query.Where(v => v.IpAddress.Contains(text)
                    || (v.Country != null && v.Country.Contains(text))
                    || (v.City != null && v.City.Contains(text))
                    || (v.LastUserName != null && v.LastUserName.Contains(text)));
            }

            ViewData["Query"] = q;
            ViewData["BlockedOnly"] = blockedOnly;
            ViewData["Total"] = await query.CountAsync();
            ViewData["MyIp"] = CallerIp();
            return View(await query.OrderByDescending(v => v.LastSeenAt).Take(MaxRows).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Block(string ip, string? reason)
        {
            if (!IPAddress.TryParse(ip?.Trim(), out var address))
            {
                TempData["Error"] = "Enter a valid IPv4 or IPv6 address.";
                return RedirectToAction(nameof(Index));
            }

            var normalized = IpAccessTracker.Normalize(address);
            if (normalized == CallerIp())
            {
                TempData["Error"] = "You cannot block the address you are using right now.";
                return RedirectToAction(nameof(Index));
            }

            var added = await tracker.BlockAsync(normalized, reason, User.Identity?.Name);
            if (added)
            {
                TempData["Success"] = $"{normalized} is now blocked.";
            }
            else
            {
                TempData["Error"] = $"{normalized} is already blocked.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Unblock(Guid id)
        {
            await tracker.UnblockAsync(id);
            TempData["Success"] = "The address is no longer blocked.";
            return RedirectToAction(nameof(Index));
        }

        private string CallerIp() =>
            HttpContext.Connection.RemoteIpAddress is { } ip ? IpAccessTracker.Normalize(ip) : string.Empty;
    }
}
