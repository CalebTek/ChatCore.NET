namespace ChatCore.Tests.Unit.Engine;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Domain.Enums;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Core.Interceptors;
using Moq;
using Xunit;

public class InterceptorPipelineTests
{
    [Fact]
    public async Task ExecuteBeforeAsync_RunsInterceptorsInOrder()
    {
        // Arrange
        var callOrder = new List<int>();

        var interceptor1 = new Mock<IMessageInterceptor>();
        interceptor1
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(1))
            .Returns(Task.CompletedTask);

        var interceptor2 = new Mock<IMessageInterceptor>();
        interceptor2
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(2))
            .Returns(Task.CompletedTask);

        var pipeline = new InterceptorPipeline(new[] { interceptor1.Object, interceptor2.Object });
        var context = new MessageContext
        {
            Message = new ChatMessage(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test",
                1,
                DateTime.UtcNow)
        };

        // Act
        await pipeline.ExecuteBeforeAsync(context);

        // Assert
        Assert.Equal(new[] { 1, 2 }, callOrder);
    }

    [Fact]
    public async Task ExecuteBeforeAsync_StopsWhenCancelled()
    {
        // Arrange
        var callCount = 0;

        var interceptor1 = new Mock<IMessageInterceptor>();
        interceptor1
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback<MessageContext, CancellationToken>((ctx, ct) =>
            {
                callCount++;
                ctx.IsCancelled = true;
                ctx.CancellationReason = "Blocked";
            })
            .Returns(Task.CompletedTask);

        var interceptor2 = new Mock<IMessageInterceptor>();
        interceptor2
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        var pipeline = new InterceptorPipeline(new[] { interceptor1.Object, interceptor2.Object });
        var context = new MessageContext
        {
            Message = new ChatMessage(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test",
                1,
                DateTime.UtcNow)
        };

        // Act
        await pipeline.ExecuteBeforeAsync(context);

        // Assert
        Assert.Equal(1, callCount);
        Assert.True(context.IsCancelled);
    }
}
