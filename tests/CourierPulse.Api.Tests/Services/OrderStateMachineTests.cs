using CourierPulse.Api.Models;
using CourierPulse.Api.Services;

namespace CourierPulse.Api.Tests.Services;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(DeliveryOrderStatus.Pending, DeliveryOrderStatus.Assigned)]
    [InlineData(DeliveryOrderStatus.Pending, DeliveryOrderStatus.Cancelled)]
    [InlineData(DeliveryOrderStatus.Assigned, DeliveryOrderStatus.PickedUp)]
    [InlineData(DeliveryOrderStatus.Assigned, DeliveryOrderStatus.Cancelled)]
    [InlineData(DeliveryOrderStatus.PickedUp, DeliveryOrderStatus.InTransit)]
    [InlineData(DeliveryOrderStatus.InTransit, DeliveryOrderStatus.Delivered)]
    [InlineData(DeliveryOrderStatus.InTransit, DeliveryOrderStatus.Failed)]
    public void CanTransition_AllowsEachLegalStep(DeliveryOrderStatus from, DeliveryOrderStatus to)
    {
        Assert.True(OrderStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(DeliveryOrderStatus.Pending, DeliveryOrderStatus.PickedUp)]
    [InlineData(DeliveryOrderStatus.Pending, DeliveryOrderStatus.Delivered)]
    [InlineData(DeliveryOrderStatus.Assigned, DeliveryOrderStatus.InTransit)]
    [InlineData(DeliveryOrderStatus.PickedUp, DeliveryOrderStatus.Cancelled)]
    [InlineData(DeliveryOrderStatus.PickedUp, DeliveryOrderStatus.Delivered)]
    [InlineData(DeliveryOrderStatus.InTransit, DeliveryOrderStatus.Assigned)]
    [InlineData(DeliveryOrderStatus.Delivered, DeliveryOrderStatus.Pending)]
    [InlineData(DeliveryOrderStatus.Failed, DeliveryOrderStatus.InTransit)]
    [InlineData(DeliveryOrderStatus.Cancelled, DeliveryOrderStatus.Assigned)]
    public void CanTransition_RejectsEachIllegalStep(DeliveryOrderStatus from, DeliveryOrderStatus to)
    {
        Assert.False(OrderStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(DeliveryOrderStatus.Delivered)]
    [InlineData(DeliveryOrderStatus.Failed)]
    [InlineData(DeliveryOrderStatus.Cancelled)]
    public void IsTerminal_ReturnsTrueForTerminalStatuses(DeliveryOrderStatus status)
    {
        Assert.True(OrderStateMachine.IsTerminal(status));
    }

    [Theory]
    [InlineData(DeliveryOrderStatus.Pending)]
    [InlineData(DeliveryOrderStatus.Assigned)]
    [InlineData(DeliveryOrderStatus.PickedUp)]
    [InlineData(DeliveryOrderStatus.InTransit)]
    public void IsTerminal_ReturnsFalseForActiveStatuses(DeliveryOrderStatus status)
    {
        Assert.False(OrderStateMachine.IsTerminal(status));
    }

    [Fact]
    public void AllowedNextStates_FromPending_ListsAssignedAndCancelled()
    {
        var next = OrderStateMachine.AllowedNextStates(DeliveryOrderStatus.Pending);

        Assert.Equal(
            new[] { DeliveryOrderStatus.Assigned, DeliveryOrderStatus.Cancelled },
            next);
    }
}
