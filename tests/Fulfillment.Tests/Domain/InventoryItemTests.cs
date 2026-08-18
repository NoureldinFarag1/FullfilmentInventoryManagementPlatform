using FluentAssertions;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Xunit;

namespace Fulfillment.Tests.Domain;

public class InventoryItemTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private InventoryItem CreateItem() =>
        new(Guid.NewGuid(), Guid.NewGuid());

    private InventoryItem CreateItemWithStock(int quantity)
    {
        var item = CreateItem();
        item.ApplyAdjustment(quantity, MovementType.Receipt, null, _userId);
        return item;
    }

    [Fact]
    public void NewItem_StartsAtZero()
    {
        var item = CreateItem();

        item.Quantity.Should().Be(0);
        item.Movements.Should().BeEmpty();
    }

    [Fact]
    public void Adjustment_ThatWouldGoBelowZero_IsRejected()
    {
        var item = CreateItemWithStock(50);

        var act = () => item.ApplyAdjustment(-60, MovementType.Issue, null, _userId);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*cannot go below zero*");
    }

    [Fact]
    public void RejectedAdjustment_LeavesQuantityUnchanged_AndWritesNoMovement()
    {
        var item = CreateItemWithStock(50);
        var movementCountBefore = item.Movements.Count;

        var act = () => item.ApplyAdjustment(-60, MovementType.Issue, null, _userId);

        act.Should().Throw<BusinessRuleViolationException>();
        item.Quantity.Should().Be(50);
        item.Movements.Count.Should().Be(movementCountBefore);
    }

    [Fact]
    public void SuccessfulAdjustment_RecordsExactlyOneMovement_WithCorrectDetails()
    {
        var item = CreateItem();

        var movement = item.ApplyAdjustment(25, MovementType.Receipt, "Initial delivery", _userId);

        item.Quantity.Should().Be(25);
        item.Movements.Should().HaveCount(1);

        movement.Delta.Should().Be(25);
        movement.QuantityAfter.Should().Be(25);
        movement.Type.Should().Be(MovementType.Receipt);
        movement.Reason.Should().Be("Initial delivery");
        movement.PerformedByUserId.Should().Be(_userId);
        movement.InventoryItemId.Should().Be(item.Id);
    }

    [Fact]
    public void ZeroDelta_IsRejected()
    {
        var item = CreateItem();

        var act = () => item.ApplyAdjustment(0, MovementType.CountCorrection, null, _userId);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void OtherMovementType_WithoutReason_IsRejected()
    {
        var item = CreateItemWithStock(10);

        var act = () => item.ApplyAdjustment(5, MovementType.Other, "   ", _userId);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*reason is required*");
    }

    [Theory]
    [InlineData(MovementType.Receipt, -5)]
    [InlineData(MovementType.Issue, 5)]
    [InlineData(MovementType.Damage, 5)]
    [InlineData(MovementType.Loss, 5)]
    public void MovementType_WithWrongDirection_IsRejected(MovementType type, int delta)
    {
        var item = CreateItemWithStock(100);

        var act = () => item.ApplyAdjustment(delta, type, null, _userId);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void MissingUser_IsRejected()
    {
        var item = CreateItemWithStock(10);

        var act = () => item.ApplyAdjustment(5, MovementType.Receipt, null, Guid.Empty);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*user who performed it*");
    }

    [Fact]
    public void MovementHistory_IsAppendOnly_AndCannotBeMutatedExternally()
    {
        var item = CreateItem();
        item.ApplyAdjustment(10, MovementType.Receipt, null, _userId);
        item.ApplyAdjustment(-3, MovementType.Issue, null, _userId);

        item.Movements.Should().HaveCount(2);
        item.Movements.Should().BeAssignableTo<IReadOnlyCollection<StockMovement>>();
        item.Quantity.Should().Be(7);
        item.Movements.Last().QuantityAfter.Should().Be(7);
    }
}