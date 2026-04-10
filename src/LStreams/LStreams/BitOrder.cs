namespace LStreams
{
    /// <summary>
    /// This enum contains the available endians used when writing or reading using bitstreams.
    /// </summary>
    public enum BitOrder
    {
        /// <summary>Used for Big-endian bit order.</summary>
        MsbFirst,

        /// <summary>Used for Little-endian bit order.</summary>
        LsbFirst
    }
}