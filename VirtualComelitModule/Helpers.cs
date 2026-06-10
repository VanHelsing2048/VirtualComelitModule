namespace ComelitVirtualModule
{
    internal static class Helpers
    {
        public const int FrameLength = 5;

        //convert byte [5] to structure
        public static T FromByteArray<T>(byte[] data) where T : struct
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (data.Length != size)
                throw new ArgumentException($"Invalid data length for type {typeof(T).Name}. Expected {size}, got {data.Length}.");
            IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(data, 0, ptr, size);
                return System.Runtime.InteropServices.Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }
        }

        //get module by id from frame
        public static byte GetModuleIdFromFrame(byte[] frame)
        {
            if (frame.Length < 3)
                throw new ArgumentException("Frame is too short to contain a module ID.");
            return frame[2];
        }

        public static byte[] CreateOutputsNormalFrame(byte moduleAddress, byte outputsMask, byte buttonStatus)
            => [0x55, (byte)Models.TechnobusCommand.OutputsNormal, moduleAddress, outputsMask, buttonStatus];

        public static byte[] CreateForceOutputsFrame(byte moduleAddress, byte outputsMask, byte outputsValues)
            => [0x55, (byte)Models.TechnobusCommand.ForceOutputs, moduleAddress, outputsMask, outputsValues];

        public static byte[] CreateReadOutput16BitFrame(byte moduleAddress, byte port, byte subPort)
            => [(byte)Models.TechnobusCommand.ReadOutput16Bit, PackPortAndSubPort(port, subPort), moduleAddress, 0, 0];

        public static byte[] CreateWriteOutput16BitFrame(byte moduleAddress, byte port, byte subPort, short value)
        {
            var bytes = BitConverter.GetBytes(value);
            return [(byte)Models.TechnobusCommand.WriteOutput16Bit, PackPortAndSubPort(port, subPort), moduleAddress, bytes[1], bytes[0]];
        }

        public static bool IsOutputsNormalResponse(byte[] response, byte moduleAddress, byte outputsMask)
            => IsFrame(response)
               && response[0] == 0x55
               && response[1] == 0x00
               && response[2] == moduleAddress
               && response[3] == outputsMask;

        public static bool IsForceOutputsResponse(byte[] response, byte moduleAddress, byte outputsMask)
            => IsFrame(response)
               && response[0] == 0x55
               && response[1] == 0x02
               && response[2] == moduleAddress
               && response[3] == outputsMask;

        public static bool Is16BitResponse(byte[] response, byte moduleAddress, byte port, byte subPort)
            => IsFrame(response)
               && response[0] == (byte)Models.TechnobusCommand.Response
               && response[1] == PackPortAndSubPort(port, subPort)
               && response[2] == moduleAddress;

        public static short Get16BitResponseValue(byte[] response)
        {
            if (!IsFrame(response))
                throw new ArgumentException($"Frame must be exactly {FrameLength} bytes.", nameof(response));

            return BitConverter.ToInt16([response[4], response[3]], 0);
        }

        private static byte PackPortAndSubPort(byte port, byte subPort)
            => (byte)((port << 5) | (subPort & 0x1F));

        private static bool IsFrame(byte[] frame)
            => frame.Length == FrameLength;
    }
}
