using System.Collections.Concurrent;
using WebSockets.Otp.Core.Services.Utils;

namespace WebSockets.Otp.Core.Tests.Services.Utils;

public sealed class AsyncObjectPoolTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_InitializesSuccessfully()
    {
        // Arrange & Act
        var pool = new AsyncObjectPool<TestObject>(5, () => new TestObject());

        // Assert
        Assert.NotNull(pool);
        Assert.False(pool.IsDisposed());
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AsyncObjectPool<TestObject>(0, () => new TestObject()));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AsyncObjectPool<TestObject>(-1, () => new TestObject()));
    }

    #endregion

    #region Rent Tests

    [Fact]
    public async Task Rent_WhenPoolIsEmpty_CreatesNewObject()
    {
        // Arrange
        var createdCount = 0;
        var pool = new AsyncObjectPool<TestObject>(3, () =>
        {
            createdCount++;
            return new TestObject();
        });

        // Act
        var obj = await pool.Rent();

        // Assert
        Assert.NotNull(obj);
        Assert.Equal(1, createdCount);
    }

    [Fact]
    public async Task Rent_WhenObjectsAreAvailable_ReturnsExistingObject()
    {
        // Arrange
        var createdCount = 0;
        var pool = new AsyncObjectPool<TestObject>(3, () =>
        {
            createdCount++;
            return new TestObject { Id = createdCount };
        });

        var obj1 = await pool.Rent();
        await pool.Return(obj1);

        // Act
        var obj2 = await pool.Rent();

        // Assert
        Assert.Equal(obj1.Id, obj2.Id);
        Assert.Equal(1, createdCount); // No new object created
    }

    [Fact]
    public async Task Rent_WhenCapacityReached_WaitsForReturnedObject()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(2, () => new TestObject());
        var rentedObjects = new TestObject[3];
        var returned = false;

        // Rent all objects to reach capacity
        rentedObjects[0] = await pool.Rent();
        rentedObjects[1] = await pool.Rent();

        // Start a task that will wait for an object
        var waitingTask = Task.Run(async () =>
        {
            rentedObjects[2] = await pool.Rent();
        });

        // Wait a bit to ensure the waiting task is blocked
        await Task.Delay(100);
        Assert.Null(rentedObjects[2]); // Should still be null

        // Return an object
        await pool.Return(rentedObjects[0]);

        // Act - Wait for the waiting task to complete
        await waitingTask;

        // Assert
        Assert.NotNull(rentedObjects[2]);
    }

    [Fact]
    public async Task Rent_WithCancellationToken_CancelsWhenRequested()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(1, () => new TestObject());
        using var cts = new CancellationTokenSource();

        // Rent the only object
        var obj = await pool.Rent();

        // Start a task that will wait for an object
        var waitingTask = Task.Run(async () =>
        {
            await pool.Rent(cts.Token);
        });

        // Wait a bit to ensure the waiting task is blocked
        await Task.Delay(100);

        // Act - Cancel the operation
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => waitingTask);

        // Cleanup
        await pool.Return(obj);
    }

    [Fact]
    public async Task Rent_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(1, () => new TestObject());
        await pool.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pool.Rent().AsTask());
    }

    [Fact]
    public async Task Rent_ConcurrentRentals_CreatesUpToCapacity()
    {
        // Arrange
        const int capacity = 5;
        var creationCount = 0;
        var creationLock = new object();
        var pool = new AsyncObjectPool<TestObject>(capacity, () =>
        {
            lock (creationLock)
            {
                creationCount++;
                return new TestObject();
            }
        });

        // Act
        var tasks = new Task<TestObject>[capacity * 2];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = pool.Rent().AsTask();
        }

        var rentedObjects = await Task.WhenAll(tasks[..capacity]);

        foreach (var rentedObject in rentedObjects)
        {
            await pool.Return(rentedObject);
        }

        rentedObjects = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(capacity, creationCount); // Should only create up to capacity
        Assert.All(rentedObjects, obj => Assert.NotNull(obj));
    }

    #endregion

    #region Return Tests

    [Fact]
    public async Task Return_WithValidObject_AddsToPool()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(2, () => new TestObject());
        var obj = await pool.Rent();

        // Act
        await pool.Return(obj);

        // Assert - Rent again should get the same object
        var obj2 = await pool.Rent();
        Assert.Same(obj, obj2);
    }

    [Fact]
    public async Task Return_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(1, () => new TestObject());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => pool.Return(null!).AsTask());
    }

    [Fact]
    public async Task Return_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(1, () => new TestObject());
        var obj = new TestObject();
        await pool.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pool.Return(obj).AsTask());
    }

    [Fact]
    public async Task Return_WithCancellationToken_CancelsWhenRequested()
    {
        // Arrange
        var pool = new AsyncObjectPool<TestObject>(1, () => new TestObject());
        using var cts = new CancellationTokenSource();

        // Fill the pool
        var obj1 = await pool.Rent();
        await pool.Return(obj1);

        // Rent again so pool is empty
        await pool.Rent();

        // Act - Cancel the operation
        cts.Cancel();

        // Start a task that will try to return (should wait)
        var waitingTask = Task.Run(async () =>
        {
            var obj2 = new TestObject();
            await pool.Return(obj2, cts.Token);
        });

        // Wait a bit to ensure the waiting task is blocked
        await Task.Delay(100);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => waitingTask);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_WhenCalled_DisposesPooledObjects()
    {
        // Arrange
        var disposedCount = 0;
        var pool = new AsyncObjectPool<DisposableTestObject>(3, () =>
            new DisposableTestObject(() => disposedCount++));

        var obj1 = await pool.Rent();
        var obj2 = await pool.Rent();
        await pool.Return(obj1);
        await pool.Return(obj2);

        // Act
        await pool.DisposeAsync();

        // Assert
        Assert.Equal(2, disposedCount);
        Assert.True(pool.IsDisposed());
    }

    [Fact]
    public async Task DisposeAsync_WithAsyncDisposableObjects_AwaitsDisposal()
    {
        // Arrange
        var disposed = false;
        var pool = new AsyncObjectPool<AsyncDisposableTestObject>(1, () =>
            new AsyncDisposableTestObject(() =>
            {
                disposed = true;
                return Task.CompletedTask;
            }));

        var obj = await pool.Rent();
        await pool.Return(obj);

        // Act
        await pool.DisposeAsync();

        // Assert
        Assert.True(disposed);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledTwice_DoesNothingSecondTime()
    {
        // Arrange
        var disposedCount = 0;
        var pool = new AsyncObjectPool<DisposableTestObject>(1, () =>
            new DisposableTestObject(() => disposedCount++));

        var obj = await pool.Rent();
        await pool.Return(obj);

        // Act - First disposal
        await pool.DisposeAsync();
        var firstDisposedCount = disposedCount;

        // Second disposal should do nothing
        await pool.DisposeAsync();

        // Assert
        Assert.Equal(1, firstDisposedCount);
        Assert.Equal(1, disposedCount); // Should not change
    }

    [Fact]
    public async Task DisposeAsync_WhenObjectsAreRented_DoesNotDisposeRentedObjects()
    {
        // Arrange
        var disposedCount = 0;
        var pool = new AsyncObjectPool<DisposableTestObject>(2, () =>
            new DisposableTestObject(() => disposedCount++));

        var obj1 = await pool.Rent();
        var obj2 = await pool.Rent();
        await pool.Return(obj1);
        // obj2 is still rented

        // Act
        await pool.DisposeAsync();

        // Assert
        Assert.Equal(1, disposedCount); // Only obj1 should be disposed
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Concurrent_RentAndReturn_DoesNotExceedCapacity()
    {
        // Arrange
        const int capacity = 10;
        const int iterations = 1000;
        var creationCount = 0;
        var creationLock = new object();

        var pool = new AsyncObjectPool<TestObject>(capacity, () =>
        {
            lock (creationLock)
            {
                creationCount++;
                return new TestObject();
            }
        });

        var tasks = new List<Task>();
        var results = new ConcurrentBag<TestObject>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var obj = await pool.Rent();
                // Simulate some work
                await Task.Delay(1);
                results.Add(obj);
                await pool.Return(obj);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(capacity, creationCount);
        Assert.True(results.Count >= iterations);
    }

    [Fact]
    public async Task MultipleThreads_RentFromEmptyPool_SerializesCreation()
    {
        // Arrange
        const int capacity = 5;
        var concurrentCreations = 0;
        var maxConcurrentCreations = 0;
        var creationLock = new object();

        var pool = new AsyncObjectPool<TestObject>(capacity, () =>
        {
            lock (creationLock)
            {
                concurrentCreations++;
                maxConcurrentCreations = Math.Max(maxConcurrentCreations, concurrentCreations);
                Thread.Sleep(10); // Simulate slow creation
                var result = new TestObject();
                concurrentCreations--;
                return result;
            }
        });

        // Act - Rent all objects at once from empty pool
        var tasks = new Task<TestObject>[capacity];
        for (int i = 0; i < capacity; i++)
        {
            tasks[i] = pool.Rent().AsTask();
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, maxConcurrentCreations); // Should be serialized
        Assert.Equal(capacity, results.Length);
        Assert.All(results, obj => Assert.NotNull(obj));
    }

    #endregion

    #region Test Helper Classes

    private class TestObject
    {
        public int Id { get; set; }
    }

    private class DisposableTestObject : IDisposable
    {
        private readonly Action _onDispose;

        public DisposableTestObject(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose() => _onDispose?.Invoke();
    }

    private class AsyncDisposableTestObject : IAsyncDisposable
    {
        private readonly Func<Task> _onDispose;

        public AsyncDisposableTestObject(Func<Task> onDispose)
        {
            _onDispose = onDispose;
        }

        public async ValueTask DisposeAsync()
        {
            if (_onDispose != null)
                await _onDispose();
        }
    }

    #endregion
}

// Extension method for testing purposes
internal static class AsyncObjectPoolExtensions
{
    public static bool IsDisposed<T>(this AsyncObjectPool<T> pool) where T : notnull
    {
        try
        {
            pool.ThrowIfDisposed();
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}
