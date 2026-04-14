using System.Runtime.InteropServices;

namespace LStreams
{
    /// <summary>
    /// Helper structure for the <see cref="LegacyBitConverter"/>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct SingleUnion
    {
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public int IntValue;
    }

    /// <summary>
    /// The bit converter (as from System namespace) but with helper functions.
    /// </summary>
    public static class LegacyBitConverter
    {
        public static int SingleToInt32Bits(float value)
        {
            var union = new SingleUnion { FloatValue = value };
            return union.IntValue;
        }

        public static float Int32BitsToSingle(int value)
        {
            var union = new SingleUnion { IntValue = value };
            return union.FloatValue;
        }
    }
}
