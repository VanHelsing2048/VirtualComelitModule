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

        private static ushort ToKey(byte page, byte cell)
            => (ushort)((page << 8) | cell);
    }
}
