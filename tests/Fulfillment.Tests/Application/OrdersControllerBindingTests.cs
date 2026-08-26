using FluentAssertions;
using Fulfillment.Api.Contracts;
using Fulfillment.Api.Controllers;
using Fulfillment.Application.Orders.Commands.CreateOrder;
using MediatR;
using Xunit;

namespace Fulfillment.Tests.Application;

/// <summary>
/// Regression cover for the argument-order bug in OrdersController.Create: the
/// Idempotency-Key header was passed as the third positional argument of
/// CreateOrderCommand, which is Notes. The key was therefore never persisted
/// (so replays created duplicate orders) and the body's notes were discarded.
///
/// Asserted at the controller, not by re-constructing the command in the test —
/// a test that repeats the same constructor call would repeat the same mistake.
/// </summary>
public class OrdersControllerBindingTests
{
    /// <summary>Captures the command the controller dispatches.</summary>
    private sealed class CapturingSender : ISender
    {
        public CreateOrderCommand? Captured { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is CreateOrderCommand command)
            {
                Captured = command;
                return Task.FromResult((TResponse)(object)Guid.NewGuid());
            }

            throw new NotSupportedException($"Unexpected request {request.GetType().Name}.");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task Create_PassesTheHeaderToIdempotencyKey_AndTheBodyNotesToNotes()
    {
        var sender = new CapturingSender();
        var controller = new OrdersController(sender);
        var request = new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), "BODY NOTES");

        await controller.Create(request, "HEADER-KEY-123", CancellationToken.None);

        sender.Captured.Should().NotBeNull();
        sender.Captured!.IdempotencyKey.Should().Be("HEADER-KEY-123");
        sender.Captured.Notes.Should().Be("BODY NOTES");
    }

    [Fact]
    public async Task Create_DoesNotPutTheHeaderIntoNotes()
    {
        // The exact shape of the bug: with no notes in the body, the key must not
        // slide into Notes.
        var sender = new CapturingSender();
        var controller = new OrdersController(sender);
        var request = new CreateOrderRequest(Guid.NewGuid(), Guid.NewGuid(), null);

        await controller.Create(request, "HEADER-KEY-456", CancellationToken.None);

        sender.Captured!.Notes.Should().BeNull();
        sender.Captured.IdempotencyKey.Should().Be("HEADER-KEY-456");
    }

    [Fact]
    public async Task Create_ForwardsCustomerAndWarehouseUnchanged()
    {
        var sender = new CapturingSender();
        var controller = new OrdersController(sender);
        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await controller.Create(
            new CreateOrderRequest(customerId, warehouseId, null), null, CancellationToken.None);

        sender.Captured!.CustomerId.Should().Be(customerId);
        sender.Captured.WarehouseId.Should().Be(warehouseId);
        sender.Captured.IdempotencyKey.Should().BeNull();
    }
}
