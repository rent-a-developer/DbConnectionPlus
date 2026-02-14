namespace RentADeveloper.DbConnectionPlus.Exceptions;

/// <summary>
/// An exception that is thrown when a concurrency violation is encountered while deleting or updating an entity in a
/// database. A concurrency violation occurs when an unexpected number of rows are affected by a delete or update
/// operation. This is usually because the data in the database has been modified since the entity has been loaded.
/// </summary>
public class DbUpdateConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbUpdateConcurrencyException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="entity">The entity that was involved in the concurrency violation.</param>
    public DbUpdateConcurrencyException(String message, Object entity) : base(message) =>
        this.Entity = entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbUpdateConcurrencyException" /> class.
    /// </summary>
    public DbUpdateConcurrencyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbUpdateConcurrencyException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DbUpdateConcurrencyException(String message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbUpdateConcurrencyException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DbUpdateConcurrencyException(String message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// The entity that was involved in the concurrency violation.
    /// </summary>
    public Object? Entity { get; set; }
}
