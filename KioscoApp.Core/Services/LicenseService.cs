using System;
using System.Linq;
using KioscoApp.Core.Data;
using KioscoApp.Core.Models;

namespace KioscoApp.Core.Services
{
    public class LicenseInfo
    {
        public string Type { get; set; }
        public DateTime? Expiry { get; set; }
        public string Key { get; set; }
    }

    public class LicenseService
    {
        public LicenseInfo GetLicense()
        {
            using var ctx = new AppDbContext();
            var lic = ctx.Licenses.OrderByDescending(l => l.Id).FirstOrDefault();
            if (lic == null) return new LicenseInfo { Type = "Demo", Expiry = DateTime.UtcNow.AddDays(-1) };
            return new LicenseInfo { Key = lic.Key, Type = lic.Type, Expiry = lic.ExpiryDate };
        }

        public void ActivateLicense(string key, string type, DateTime? expiry = null)
        {
            using var ctx = new AppDbContext();
            ctx.Licenses.Add(new License
            {
                Key = key,
                Type = type,
                InstallDate = DateTime.UtcNow,
                ExpiryDate = expiry,
                HardwareHash = "activated" // en producción guarda hardware hash apropiado
            });
            ctx.SaveChanges();
        }
    }
}
