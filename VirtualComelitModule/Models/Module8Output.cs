using ComelitVirtualModule.Interfaces;

namespace ComelitVirtualModule.Models
{
    internal class Module8Output(short moduleId) : IModuleOutputs<byte>
    {
        private byte _outputState;
        public short ModuleId => moduleId;
        public int OutputsCount => 8;

        // Proprietà comode (non richieste dall’interfaccia, ma utili)
        public bool OUT1 { get; private set; }
        public bool OUT2 { get; private set; }
        public bool OUT3 { get; private set; }
        public bool OUT4 { get; private set; }
        public bool OUT5 { get; private set; }
        public bool OUT6 { get; private set; }
        public bool OUT7 { get; private set; }
        public bool OUT8 { get; private set; }

        public ModuleType ModuleType => ModuleType.IOModule;

        public byte GetOutputState() => _outputState;

        public void UpdateOutputState(byte state)
        {
            _outputState = state;
            OUT1 = (_outputState & 0x01) != 0;
            OUT2 = (_outputState & 0x02) != 0;
            OUT3 = (_outputState & 0x04) != 0;
            OUT4 = (_outputState & 0x08) != 0;
            OUT5 = (_outputState & 0x10) != 0;
            OUT6 = (_outputState & 0x20) != 0;
            OUT7 = (_outputState & 0x40) != 0;
            OUT8 = (_outputState & 0x80) != 0;
        }

        public void ApplyOutputMask(byte mask, byte values)
            => UpdateOutputState((byte)((_outputState & ~mask) | (values & mask)));

        public void SetMaskedOutputs(byte mask, bool enabled)
            => ApplyOutputMask(mask, enabled ? mask : (byte)0);

        public bool this[int index]
            => index switch
            {
                0 => OUT1,
                1 => OUT2,
                2 => OUT3,
                3 => OUT4,
                4 => OUT5,
                5 => OUT6,
                6 => OUT7,
                7 => OUT8,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Index 0..7")
            };
    }
}
