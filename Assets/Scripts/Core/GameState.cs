namespace Game.Core
{
    public enum GameState
    {
        Ready,      // waiting for ENGAGE tap
        Traveling,  // hero walks forward, world scrolls — the event fires when he arrives
        Battling,   // auto-battle running on the beat
        Choosing,   // upgrade pick or sidekick swap — ENGAGE locked
        Dead        // run over, ENGAGE restarts
    }
}
