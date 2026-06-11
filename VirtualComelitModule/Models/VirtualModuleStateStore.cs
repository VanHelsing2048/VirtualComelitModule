using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComelitVirtualModule.Models
{
    internal sealed class VirtualModuleStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        private readonly string _path;
        private readonly object _lock = new();
        private PersistedState _state;

        public VirtualModuleStateStore(string? path = null)
        {
            _path = path ?? GetDefaultPath();
            _state = Load();
        }

        public PersistedModuleState? GetModule(byte address, string type)
        {
            lock (_lock)
            {
                return _state.Modules.TryGetValue(GetModuleKey(address, type), out var module)
                    ? module
                    : null;
            }
        }

        public void SaveModule(byte address, string type, byte outputState, VirtualModuleMemory memory)
        {
            lock (_lock)
            {
                _state.Modules[GetModuleKey(address, type)] = new PersistedModuleState
                {
                    Address = address,
                    Type = type,
                    OutputState = outputState,
                    Memory = memory.Export(),
                };

                Save();
            }
        }

        private PersistedState Load()
        {
            if (!File.Exists(_path))
                return new PersistedState();

            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<PersistedState>(json, JsonOptions) ?? new PersistedState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to load persisted state from {_path}: {ex.Message}");
                return new PersistedState();
            }
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(_path, JsonSerializer.Serialize(_state, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to save persisted state to {_path}: {ex.Message}");
            }
        }

        private static string GetDefaultPath()
        {
            if (Directory.Exists("/data"))
                return "/data/state.json";

            return Path.Combine(AppContext.BaseDirectory, "data", "state.json");
        }

        private static string GetModuleKey(byte address, string type)
            => $"{type.ToLowerInvariant()}:{address}";
    }

    internal sealed class PersistedState
    {
        [JsonPropertyName("modules")]
        public Dictionary<string, PersistedModuleState> Modules { get; init; } = [];
    }

    internal sealed class PersistedModuleState
    {
        [JsonPropertyName("address")]
        public byte Address { get; init; }

        [JsonPropertyName("type")]
        public string Type { get; init; } = VirtualModuleTypes.I8;

        [JsonPropertyName("output-state")]
        public byte OutputState { get; init; }

        [JsonPropertyName("memory")]
        public Dictionary<string, byte> Memory { get; init; } = [];
    }
}
