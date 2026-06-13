namespace ChatCore.Tests.Unit.Engine;

using ChatCore.Abstractions.Domain.Entities;
using ChatCore.Abstractions.Interceptors;
using ChatCore.Core.Interceptors;
using Moq;
using Xunit;

public class InterceptorPipelineTests
{
    private static MessageContext MakeContext() => new()
    {
        Message = new ChatMessage(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test", 1, DateTime.UtcNow)
    };

    // =========================================================================
    // ExecuteBeforeAsync
    // =========================================================================

    [Fact]
    public async Task ExecuteBeforeAsync_RunsInterceptorsInOrder()
    {
        var callOrder = new List<int>();

        var i1 = MockInterceptor(before: () => callOrder.Add(1));
        var i2 = MockInterceptor(before: () => callOrder.Add(2));
        var i3 = MockInterceptor(before: () => callOrder.Add(3));

        var pipeline = new InterceptorPipeline(new[] { i1.Object, i2.Object, i3.Object });

        await pipeline.ExecuteBeforeAsync(MakeContext());

        Assert.Equal(new[] { 1, 2, 3 }, callOrder);
    }

    [Fact]
    public async Task ExecuteBeforeAsync_StopsWhenCancelled()
    {
        var callCount = 0;

        var cancelling = new Mock<IMessageInterceptor>();
        cancelling
            .Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback<MessageContext, CancellationToken>((ctx, _) =>
            {
                callCount++;
                ctx.IsCancelled        = true;
                ctx.CancellationReason = "Blocked";
            })
            .Returns(Task.CompletedTask);

        var neverCalled = MockInterceptor(before: () => callCount++);
        var pipeline    = new InterceptorPipeline(new[] { cancelling.Object, neverCalled.Object });

        var context = MakeContext();
        await pipeline.ExecuteBeforeAsync(context);

        Assert.Equal(1,       callCount);
        Assert.True(context.IsCancelled);
        Assert.Equal("Blocked", context.CancellationReason);
    }

    [Fact]
    public async Task ExecuteBeforeAsync_EmptyPipeline_CompletesWithoutError()
    {
        var pipeline = new InterceptorPipeline(Enumerable.Empty<IMessageInterceptor>());
        var context  = MakeContext();

        var ex = await Record.ExceptionAsync(() => pipeline.ExecuteBeforeAsync(context));

        Assert.Null(ex);
        Assert.False(context.IsCancelled);
    }

    [Fact]
    public async Task ExecuteBeforeAsync_SingleInterceptor_IsInvoked()
    {
        var invoked = false;
        var i       = MockInterceptor(before: () => invoked = true);
        var pipeline = new InterceptorPipeline(new[] { i.Object });

        await pipeline.ExecuteBeforeAsync(MakeContext());

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteBeforeAsync_ContextPropertiesArePropagated()
    {
        var i = new Mock<IMessageInterceptor>();
        i.Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
         .Callback<MessageContext, CancellationToken>((ctx, _) => ctx.Properties["key"] = "value")
         .Returns(Task.CompletedTask);

        string? captured = null;
        var i2 = new Mock<IMessageInterceptor>();
        i2.Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
          .Callback<MessageContext, CancellationToken>((ctx, _) => captured = ctx.Properties["key"]?.ToString())
          .Returns(Task.CompletedTask);

        var pipeline = new InterceptorPipeline(new[] { i.Object, i2.Object });

        await pipeline.ExecuteBeforeAsync(MakeContext());

        Assert.Equal("value", captured);
    }

    // =========================================================================
    // ExecuteAfterAsync
    // =========================================================================

    [Fact]
    public async Task ExecuteAfterAsync_RunsInterceptorsInOrder()
    {
        var callOrder = new List<int>();

        var i1 = MockInterceptor(after: () => callOrder.Add(1));
        var i2 = MockInterceptor(after: () => callOrder.Add(2));

        var pipeline = new InterceptorPipeline(new[] { i1.Object, i2.Object });

        await pipeline.ExecuteAfterAsync(MakeContext());

        Assert.Equal(new[] { 1, 2 }, callOrder);
    }

    [Fact]
    public async Task ExecuteAfterAsync_StopsWhenCancelled()
    {
        var callCount = 0;

        var cancelling = new Mock<IMessageInterceptor>();
        cancelling
            .Setup(x => x.OnAfterSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback<MessageContext, CancellationToken>((ctx, _) =>
            {
                callCount++;
                ctx.IsCancelled = true;
            })
            .Returns(Task.CompletedTask);

        var neverCalled = MockInterceptor(after: () => callCount++);
        var pipeline    = new InterceptorPipeline(new[] { cancelling.Object, neverCalled.Object });

        await pipeline.ExecuteAfterAsync(MakeContext());

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteAfterAsync_EmptyPipeline_CompletesWithoutError()
    {
        var pipeline = new InterceptorPipeline(Enumerable.Empty<IMessageInterceptor>());

        var ex = await Record.ExceptionAsync(() => pipeline.ExecuteAfterAsync(MakeContext()));

        Assert.Null(ex);
    }

    // =========================================================================
    // Before + After together
    // =========================================================================

    [Fact]
    public async Task BeforeAndAfter_BothInvokedInOrder_WhenNotCancelled()
    {
        var events = new List<string>();

        var i = new Mock<IMessageInterceptor>();
        i.Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
         .Callback(() => events.Add("before"))
         .Returns(Task.CompletedTask);
        i.Setup(x => x.OnAfterSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
         .Callback(() => events.Add("after"))
         .Returns(Task.CompletedTask);

        var pipeline = new InterceptorPipeline(new[] { i.Object });
        var context  = MakeContext();

        await pipeline.ExecuteBeforeAsync(context);
        await pipeline.ExecuteAfterAsync(context);

        Assert.Equal(new[] { "before", "after" }, events);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static Mock<IMessageInterceptor> MockInterceptor(
        Action? before = null,
        Action? after  = null)
    {
        var mock = new Mock<IMessageInterceptor>();

        mock.Setup(x => x.OnBeforeSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => before?.Invoke())
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.OnAfterSendAsync(It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => after?.Invoke())
            .Returns(Task.CompletedTask);

        return mock;
    }
}
