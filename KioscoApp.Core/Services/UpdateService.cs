using System.Text.Json;

namespace KioscoApp.Core.Services
{
    public class UpdateInfo { public string Version { get; set; } public string Url { get; set; } public string Notes { get; set; } }

    public class UpdateService
    {
        private readonly string _versionUrl;
        public UpdateService(string versionUrl) => _versionUrl = versionUrl;
        public async Task<UpdateInfo> CheckForUpdatesAsync(string currentVersion)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var txt = await http.GetStringAsync(_versionUrl);
                var info = JsonSerializer.Deserialize<UpdateInfo>(txt);
                if (Version.TryParse(info?.Version, out var remote) && Version.TryParse(currentVersion, out var local))
                {
                    return remote > local ? info : null;
                }
            }
            catch { /* sin internet -> silencioso */ }
            return null;
        }
    }
}
