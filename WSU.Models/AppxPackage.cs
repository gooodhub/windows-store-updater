namespace WSU.Models
{
    public class AppxPackage
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Architecture { get; set; }
        public string ResourceId { get; set; }
        public string Version { get; set; }
        public string PackageFullName { get; set; }
        public string InstallLocation { get; set; }
        public bool IsFramework { get; set; }
        public string PackageFamilyName { get; set; }
        public string PublisherId { get; set; }
        public bool IsResourcePackage { get; set; }
        public bool IsBundle { get; set; }
        public bool IsDevelopmentMode { get; set; }
        public string Dependencies { get; set; }
    }
}
