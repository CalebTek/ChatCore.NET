namespace ChatCore.Abstractions.Results;

/// <summary>
/// Represents a paginated result using seek-based pagination.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are more items to fetch.
    /// </summary>
    public bool HasMore { get; private set; }

    /// <summary>
    /// Gets the cursor for the next page (typically the last item's sequence number).
    /// </summary>
    public long? NextCursor { get; private set; }

    /// <summary>
    /// Gets the total count of items in the current page.
    /// </summary>
    public int Count => Items.Count();

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="hasMore">Whether there are more items to fetch.</param>
    /// <param name="nextCursor">The cursor for the next page.</param>
    public PaginatedResult(IEnumerable<T> items, bool hasMore, long? nextCursor = null)
    {
        Items = items ?? Enumerable.Empty<T>();
        HasMore = hasMore;
        NextCursor = nextCursor;
    }
}