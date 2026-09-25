namespace Match3.Battle.Buffs
{
    /// <summary>
    /// The 4 named Effect (Control) Buffs from the design doc. Each has a
    /// fixed, hardcoded effect rather than configurable stat modifiers
    /// (unlike Normal Buff/Debuff) — that's what makes it a "Control"
    /// buff: it always does exactly one specific thing.
    /// </summary>
    public enum ControlBuffType
    {
        /// <summary>Immediately skips the target's next turn(s). Negative, targets Opponent.</summary>
        Stun,

        /// <summary>All HP/VHP healing on the target is reduced to 0 while active.</summary>
        RecoveryBlock,

        /// <summary>Locks skill casting (and any future transformation mechanic) for the target. No consumer yet — the Skill System hasn't been built.</summary>
        Silences,

        /// <summary>Clears all current negative buffs and grants immunity to negative buffs and all incoming damage. Positive, targets Self.</summary>
        Invincible
    }
}
