namespace ComelitVirtualModule.Models
{
    internal sealed class VirtualModuleMemory
    {
        private readonly Dictionary<ushort, byte> _values = new();

        public VirtualModuleMemory(byte moduleType = (byte)ModuleType.IOModule)
        {
            Write(0, 42, moduleType);
            Write(0, 43, 0x60);
        }

        public byte Read(byte page, byte cell)
            => _values.TryGetValue(ToKey(page, cell), out var value) ? value : (byte)0;

        public void Write(byte page, byte cell, byte value)
            => _values[ToKey(page, cell)] = value;

        public Dictionary<string, byte> Export()
            => _values.ToDictionary(pair => $"{pair.Key >> 8}:{pair.Key & 0xFF}", pair => pair.Value);

        public void Import(IReadOnlyDictionary<string, byte>? values)
        {
            if (values is null)
                return;

            foreach (var (key, value) in values)
            {
                var parts = key.Split(':', 2);
                if (parts.Length != 2)
                    continue;

                if (!byte.TryParse(parts[0], out var page) || !byte.TryParse(parts[1], out var cell))
                    continue;

                Write(page, cell, value);
            }
        }

        private static ushort ToKey(byte page, byte cell)
            => (ushort)((page << 8) | cell);
    }
}
