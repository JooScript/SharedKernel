using Moq;
using SharedKernel.Api.Middlewares;
using SharedKernel.Bl.Contracts.Events;

namespace SharedKernel.Api.Tests.Middlewares;

public class EventsSubscriptionMiddlewareTests
{
    [Fact]
    public async Task Invoke_SubscribesSubscriberToPublisher()
    {
        var publisher = new Mock<ICrudPublisher>();
        var subscriber = new Mock<IAuditLogSubscriber>();
        var nextCalled = false;

        var sut = new EventsSubscriptionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await sut.Invoke(new DefaultHttpContext(), publisher.Object, subscriber.Object);

        subscriber.Verify(s => s.Subscribe(publisher.Object), Times.Once);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_DoesNotCallNextWhenSubscribeThrows()
    {
        var publisher = new Mock<ICrudPublisher>();
        var subscriber = new Mock<IAuditLogSubscriber>();
        subscriber
            .Setup(s => s.Subscribe(It.IsAny<ICrudPublisher>()))
            .Throws(new InvalidOperationException("subscribe failed"));

        var nextCalled = false;
        var sut = new EventsSubscriptionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Invoke(new DefaultHttpContext(), publisher.Object, subscriber.Object));

        Assert.False(nextCalled);
    }
}
