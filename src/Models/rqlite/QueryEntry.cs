namespace PowerRqlite.Models.rqlite
{
    /// <summary>
    /// A rqlite statement with its bound arguments. Args are never concatenated into the SQL string.
    /// </summary>
    public record QueryEntry(string Sql, object?[] Args);
}
