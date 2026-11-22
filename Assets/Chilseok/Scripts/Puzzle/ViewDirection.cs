namespace Chilseok.Puzzle
{
    public enum ViewDirection
    {
        Degrees0 = 0,
        Degrees90 = 90,
        Degrees180 = 180,
        Degrees270 = 270
    }

    internal static class ViewDirectionMask
    {
        internal const int All = 0b1111;

        internal static int For(ViewDirection direction) => direction switch
        {
            ViewDirection.Degrees0 => 0b0001,
            ViewDirection.Degrees90 => 0b0010,
            ViewDirection.Degrees180 => 0b0100,
            ViewDirection.Degrees270 => 0b1000,
            _ => 0
        };
    }
}
