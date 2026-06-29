namespace MyCod.Core.ConcurrentServer;

/// <summary>
/// Thread-safe static "server" from task 2. The class stores a single integer
/// counter and exposes only two operations: read the value and add to it.
/// </summary>
public static class Server
{
    // ReaderWriterLockSlim is used because the task has many readers and fewer
    // writers. Several readers can hold the read lock at the same time, while a
    // writer receives an exclusive lock and blocks both other writers and readers.
    private static readonly ReaderWriterLockSlim CountLock = new(LockRecursionPolicy.NoRecursion);
    private static int _count;

    /// <summary>
    /// Reads the current counter value. Parallel readers do not block each
    /// other, but they wait if a writer is currently changing the counter.
    /// </summary>
    public static int GetCount()
    {
        CountLock.EnterReadLock();
        try
        {
            return _count;
        }
        finally
        {
            // Locks are always released in finally so that an exception cannot
            // leave the server permanently locked.
            CountLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds <paramref name="value"/> to the counter. Only one writer can execute
    /// this block at a time, therefore the read-modify-write operation is atomic
    /// for all clients of the class.
    /// </summary>
    public static void AddToCount(int value)
    {
        CountLock.EnterWriteLock();
        try
        {
            // checked makes integer overflow visible instead of silently wrapping
            // around to a negative or otherwise incorrect value.
            checked
            {
                _count += value;
            }
        }
        finally
        {
            CountLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Test helper. It is internal and visible only to the test project through
    /// InternalsVisibleTo, so normal users still see exactly the required API.
    /// </summary>
    internal static void ResetForTests(int value = 0)
    {
        CountLock.EnterWriteLock();
        try
        {
            _count = value;
        }
        finally
        {
            CountLock.ExitWriteLock();
        }
    }
}
