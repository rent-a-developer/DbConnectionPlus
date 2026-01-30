namespace RentADeveloper.DbConnectionPlus.Configuration;

/// <summary>
/// Represents an object that can be frozen to prevent further modifications.
/// </summary>
public interface IFreezable
{
    /// <summary>
    /// Freezes the object, preventing any further modifications.
    /// </summary>
    public void Freeze();
}
