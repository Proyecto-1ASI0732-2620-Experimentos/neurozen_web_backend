using System.Reflection;
using Moq;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Tests.Unit.Support;

/// <summary>Helpers for unit tests: ids that normally come from the database and a pass-through unit of work.</summary>
public static class TestEntities
{
    /// <summary>Sets the private "Id" of an entity, as the database would do after inserting it.</summary>
    public static T WithId<T>(this T entity, long id) where T : class
    {
        var property = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"{typeof(T).Name} has no Id property");
        property.SetValue(entity, id);
        return entity;
    }

    /// <summary>
    ///     Unit of work whose transaction simply runs the operation. <paramref name="insideTransaction" />
    ///     is true while the operation runs, so tests can check what happened inside the transaction.
    /// </summary>
    public static Mock<IUnitOfWork> PassThroughUnitOfWork(CapturedValue<bool>? insideTransaction = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CompleteAsync()).Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (operation, _) =>
            {
                if (insideTransaction is not null) insideTransaction.Value = true;
                await operation();
                if (insideTransaction is not null) insideTransaction.Value = false;
            });
        return unitOfWork;
    }
}

/// <summary>Mutable holder for a value captured inside a callback.</summary>
public sealed class CapturedValue<T>(T value)
{
    public T Value { get; set; } = value;
}
