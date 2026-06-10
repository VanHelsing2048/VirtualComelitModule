namespace ComelitVirtualModule.Models
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    internal struct Buffer
    {
        public ModuleType ModuleType;
        public DataType Command;
        public byte ModuleId;
        public byte Target;
        public byte Value;
    }

    internal enum ModuleType : byte
    {
        ThermoHygrometricSensor = 0xBB,
        ConsumptionSensor = 0xB6,
        IOModule = 0x55,
        EnergyCounter = 0xC0,
    }

    internal enum DataType : byte
    {
        Temperature = 0x01,
        OutputState = 0x02,
        Humidity = 0x03,
        DewPoint = 0x05,
        ToggleOutput = 0x10,
        Interrogate = 0x21,
        OutputForce = 0x22,
    }

    internal enum TechnobusCommand : byte
    {
        OutputsNormal = 0x10,
        ForceOutputs = 0x22,
        ReadOutput16Bit = 0xF7,
        WriteOutput16Bit = 0xF9,
        Response = 0xFD,
    }

    internal enum HomeServerLoginResponse
    {
        Correct = 0,
        NewPasswordSet = 1,
        PasswordAlreadySet = 2,
        SocketError = 12,
        Error = 14,
    }
}
