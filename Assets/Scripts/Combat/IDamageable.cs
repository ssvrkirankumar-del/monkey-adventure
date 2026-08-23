namespace GuardianSystem.Combat
{
    /// <summary>
    /// Interface for any entity in the game that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies the specified amount of damage to this entity.
        /// </summary>
        /// <param name="amount">Amount of damage points.</param>
        void TakeDamage(int amount);
    }
}
