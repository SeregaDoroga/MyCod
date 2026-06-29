namespace MyCod.Core.ConcurrentServer;

public static class Server
{
    private static readonly ReaderWriterLockSlim CountLock = new(LockRecursionPolicy.NoRecursion);
    private static int _count;

    public static int GetCount()
    {
        CountLock.EnterReadLock();
        try
        {
            return _count;
        }
        finally
        {
            CountLock.ExitReadLock();
        }
    }

    public static void AddToCount(int value)
    {
        CountLock.EnterWriteLock();
        try
        {
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
