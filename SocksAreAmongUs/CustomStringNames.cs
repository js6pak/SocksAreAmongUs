using Reactor;

namespace SocksAreAmongUs
{
    public static class CustomStringNames
    {
        public static CustomStringName Infinite { get; } = CustomStringName.Register("∞");
        public static CustomStringName GameMode { get; } = CustomStringName.Register("Game mode");
        public static CustomStringName MaxPlayers { get; } = CustomStringName.Register("Max players");
    }
}
