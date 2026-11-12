using System.Collections.Concurrent;
using System.Net;
using AOB.BAL.Entities;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.WEB.Services
{
    /// <summary>
    /// Remembers which IP addresses use the site and which are blocked. The blocked set is kept in memory so the
    /// check on every request costs nothing; visits are counted in memory and written to the database at most once
    /// a minute per address, so a busy visitor does not produce a write per request.
    /// </summary>
    public sealed class IpAccessTracker(IServiceScopeFactory scopes, IIpGeolocator geolocator, ILogger<IpAccessTracker> logger)
    {
        private const long FlushEveryMs = 60_000;
        private static readonly TimeSpan GeoRetryAfter = TimeSpan.FromDays(1);

        private readonly ConcurrentDictionary<string, Pending> _pending = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile HashSet<string>? _blocked;

        private sealed class Pending
        {
            public long Hits;
            public long LastFlushTicks = long.MinValue / 2;
            public string? LastPath;
            public string? LastUser;
        }

        // Bounds on what anonymous traffic can make the tracker hold or do.
        private const int MaxTrackedAddresses = 20_000;
        private const int MaxGeoLookupsPerMinute = 30;
        private static readonly long PruneIdleAfterMs = (long)TimeSpan.FromHours(1).TotalMilliseconds;

        private long _lastPruneTicks;
        private long _geoWindowStartTicks;
        private int _geoLookupsInWindow;

        /// <summary>
        /// The address as stored: an IPv4-mapped IPv6 address becomes plain IPv4, and an IPv6 address is reduced to its
        /// /64 network, because one subscriber normally owns a whole /64 and could otherwise use endless "new" addresses
        /// to dodge a block or fill the table. Blocking an IPv6 address therefore blocks its /64.
        /// </summary>
        public static string Normalize(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return address.MapToIPv4().ToString();
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !address.IsIPv6LinkLocal)
            {
                var bytes = address.GetAddressBytes();
                Array.Clear(bytes, 8, 8);
                return new IPAddress(bytes).ToString();
            }

            return address.ToString();
        }

        public async ValueTask<bool> IsBlockedAsync(string ip)
        {
            var set = _blocked ?? await LoadBlockedAsync();
            return set.Contains(ip);
        }

        /// <summary>Counts one request. Cheap: the database is touched at most once a minute per address.</summary>
        public void Track(string ip, string path, string? userName)
        {
            if (!_pending.TryGetValue(ip, out var pending))
            {
                if (_pending.Count >= MaxTrackedAddresses && !TryPrune())
                {
                    return; // under a flood of new addresses, existing ones keep being counted but nothing new is remembered
                }

                pending = _pending.GetOrAdd(ip, _ => new Pending());
            }

            Interlocked.Increment(ref pending.Hits);
            pending.LastPath = path;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                pending.LastUser = userName;
            }

            var now = Environment.TickCount64;
            var last = Interlocked.Read(ref pending.LastFlushTicks);
            if (now - last < FlushEveryMs || Interlocked.CompareExchange(ref pending.LastFlushTicks, now, last) != last)
            {
                return;
            }

            _ = Task.Run(() => FlushAsync(ip, pending));
        }

        /// <summary>Drops addresses that were last flushed over an hour ago (at most once a minute). True when room was made.</summary>
        private bool TryPrune()
        {
            var now = Environment.TickCount64;
            var last = Interlocked.Read(ref _lastPruneTicks);
            if (now - last < 60_000 || Interlocked.CompareExchange(ref _lastPruneTicks, now, last) != last)
            {
                return _pending.Count < MaxTrackedAddresses;
            }

            foreach (var (key, value) in _pending)
            {
                if (now - Interlocked.Read(ref value.LastFlushTicks) > PruneIdleAfterMs)
                {
                    _pending.TryRemove(key, out _);
                }
            }

            return _pending.Count < MaxTrackedAddresses;
        }

        /// <summary>At most 30 third-party location lookups a minute, so anonymous traffic cannot drive outbound calls.</summary>
        private bool TryTakeGeoLookup()
        {
            var now = Environment.TickCount64;
            lock (_pending)
            {
                if (now - _geoWindowStartTicks >= 60_000)
                {
                    _geoWindowStartTicks = now;
                    _geoLookupsInWindow = 0;
                }

                return _geoLookupsInWindow++ < MaxGeoLookupsPerMinute;
            }
        }

        /// <summary>Blocks an address (creating its row when it has never visited). Returns false when already blocked.</summary>
        public async Task<bool> BlockAsync(string ip, string? reason, string? blockedBy)
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();

            var row = await db.VisitorIps.FirstOrDefaultAsync(v => v.IpAddress == ip);
            if (row is null)
            {
                row = new VisitorIp { IpAddress = ip };
                db.VisitorIps.Add(row);
                await ApplyLocationAsync(row);
            }
            else if (row.IsBlocked)
            {
                return false;
            }

            row.IsBlocked = true;
            row.BlockedReason = Trim(reason, 300);
            row.BlockedAt = DateTimeOffset.UtcNow;
            row.BlockedByName = Trim(blockedBy, 200);
            await db.SaveChangesAsync();

            _blocked = null;
            return true;
        }

        public async Task UnblockAsync(Guid id)
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();

            var row = await db.VisitorIps.FirstOrDefaultAsync(v => v.Id == id);
            if (row is null || !row.IsBlocked)
            {
                return;
            }

            row.IsBlocked = false;
            row.BlockedReason = null;
            row.BlockedAt = null;
            row.BlockedByName = null;
            await db.SaveChangesAsync();

            _blocked = null;
        }

        private async Task<HashSet<string>> LoadBlockedAsync()
        {
            await _loadLock.WaitAsync();
            try
            {
                if (_blocked is { } loaded)
                {
                    return loaded;
                }

                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();
                var ips = await db.VisitorIps.Where(v => v.IsBlocked).Select(v => v.IpAddress).ToListAsync();
                return _blocked = new HashSet<string>(ips, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // Never take the whole site down because the block list could not be read.
                logger.LogError(ex, "Could not load the blocked IP list.");
                return [];
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private async Task FlushAsync(string ip, Pending pending)
        {
            try
            {
                var hits = Interlocked.Exchange(ref pending.Hits, 0);
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();

                var now = DateTimeOffset.UtcNow;
                var row = await db.VisitorIps.FirstOrDefaultAsync(v => v.IpAddress == ip);
                if (row is null)
                {
                    row = new VisitorIp { IpAddress = ip, FirstSeenAt = now };
                    db.VisitorIps.Add(row);
                }

                row.HitCount += hits;
                row.LastSeenAt = now;
                row.LastPath = Trim(pending.LastPath, 300);
                row.LastUserName = Trim(pending.LastUser, 200) ?? row.LastUserName;

                var needsLocation = row.GeoLookedUpAt is null
                    || (row.Country is null && now - row.GeoLookedUpAt > GeoRetryAfter);
                if (needsLocation)
                {
                    await ApplyLocationAsync(row);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not record a visit from {Ip}.", ip);
            }
        }

        private async Task ApplyLocationAsync(VisitorIp row)
        {
            if (!TryTakeGeoLookup())
            {
                return; // GeoLookedUpAt stays empty, so a later visit tries again
            }

            var geo = await geolocator.LookupAsync(row.IpAddress);
            row.GeoLookedUpAt = DateTimeOffset.UtcNow;
            if (geo is null)
            {
                return;
            }

            row.Country = Trim(geo.Country, 100);
            row.CountryCode = Trim(geo.CountryCode, 10);
            row.Region = Trim(geo.Region, 100);
            row.City = Trim(geo.City, 100);
            row.Isp = Trim(geo.Isp, 200);
        }

        private static string? Trim(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            return value.Length <= max ? value : value[..max];
        }
    }
}
