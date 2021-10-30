namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Terminator : BaseGameMode
    {
        public override string Id => "terminator";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Terminator;
    }
}
