using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComelitVirtualModule.Models
{
    internal sealed class AddonOptions
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        [JsonPropertyName("comelit-ip")]
        public string ComelitIp { get; init; } = "192.168.1.51";

        [JsonPropertyName("comelit-port")]
        public int ComelitPort { get; init; } = 10011;

        [JsonPropertyName("comelit-password")]
        public string ComelitPassword { get; init; } = "";

        [JsonPropertyName("module-addresses")]
        public List<byte> ModuleAddresses { get; init; } = [1];

        [JsonPropertyName("module-name-prefix")]
        public string ModuleNamePrefix { get; init; } = "Virtual I8";

        [JsonPropertyName("initial-state")]
        public byte InitialState { get; init; }

        [JsonPropertyName("modules")]
        public List<VirtualOutputModuleOptions> Modules { get; init; } = [];

        public static AddonOptions Load()
        {
            var path = File.Exists("/data/options.json")
                ? "/data/options.json"
                : Path.Combine(AppContext.BaseDirectory, "data", "options.json");

            if (!File.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), "data", "options.json");

            if (!File.Exists(path))
                return new AddonOptions();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AddonOptions>(json, JsonOptions) ?? new AddonOptions();
        }

        public IReadOnlyList<VirtualOutputModuleOptions> GetModules()
        {
            if (Modules.Count > 0)
                return Modules;

            return ModuleAddresses
                .Distinct()
                .Select(address => new VirtualOutputModuleOptions
                {
                    Address = address,
                    Name = $"{ModuleNamePrefix} {address}",
                    InitialState = InitialState,
                })
                .ToList();
        }
    }

    internal sealed class VirtualOutputModuleOptions
    {
        [JsonPropertyName("address")]
        public byte Address { get; init; } = 1;

        [JsonPropertyName("name")]
        public string Name { get; init; } = "Virtual I8";

        [JsonPropertyName("initial-state")]
        public byte InitialState { get; init; }
    }
}
