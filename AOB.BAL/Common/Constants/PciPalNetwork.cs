using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Constants
{
    /// <summary>One PCI Pal region and the IP addresses a customer may need to allow-list for it.</summary>
    public sealed record RegionNetwork(string Name, IReadOnlyList<string> IpAddresses);

    /// <summary>
    /// The IP addresses to allow-list for each PCI Pal region (staging and production). The admin region is
    /// always needed; the others depend on the regions the customer picked on the Technical Environment step.
    /// A region that is not listed here has no addresses published yet.
    /// </summary>
    public static class PciPalNetwork
    {
        public static readonly RegionNetwork StagingAdmin = new(
            "Admin Region – Staging",
            new[] { "52.213.121.42", "52.213.124.241" });

        public static readonly RegionNetwork ProductionAdmin = new(
            "Admin Region – Production",
            new[] { "35.176.49.129", "35.176.147.106" });

        public static readonly IReadOnlyDictionary<PciPalRegion, RegionNetwork> Staging =
            new Dictionary<PciPalRegion, RegionNetwork>
            {
                [PciPalRegion.EuWest1] = new("EU West – Staging",
                    new[] { "52.19.189.43", "52.31.240.210", "34.241.184.158", "52.208.201.124" }),
                [PciPalRegion.CaCentral1] = new("CA Central – Staging",
                    new[] { "52.60.138.63", "52.60.242.238", "99.79.11.178", "99.79.21.41" }),
                [PciPalRegion.ApSoutheast2] = new("AU Southeast – Staging",
                    new[] { "13.211.70.40", "13.237.83.249", "13.239.68.82", "54.153.208.17" })
            };

        public static readonly IReadOnlyDictionary<PciPalRegion, RegionNetwork> Production =
            new Dictionary<PciPalRegion, RegionNetwork>
            {
                [PciPalRegion.EuWest2] = new("EU West – Production",
                    new[] { "52.56.102.207", "35.176.207.76", "35.177.54.89", "35.177.185.226" }),
                [PciPalRegion.UsEast1] = new("US East – Production",
                    new[] { "52.71.245.207", "35.169.184.101", "52.200.16.254", "35.171.29.158" }),
                [PciPalRegion.CaCentral1] = new("CA Central – Production",
                    new[] { "35.182.76.17", "35.182.18.191", "52.60.36.152", "35.182.34.107" }),
                [PciPalRegion.ApSoutheast2] = new("AU Southeast – Production",
                    new[] { "13.238.16.116", "13.54.226.218", "13.55.205.172", "54.79.23.22" }),
                [PciPalRegion.EuCentral1] = new("EU Frankfurt – Production",
                    new[] { "52.28.219.24", "52.59.109.11", "18.194.113.217", "35.157.13.246" })
            };
    }
}
