using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.Domain.Interfaces;
using Wonderland.GameServer.Systems;
using Xunit;

namespace Wonderland.Application.Tests.Services;

public class InventoryServiceTests
{
    private readonly IInventoryRepository _repo = Substitute.For<IInventoryRepository>();
    private readonly ILogger<InventoryService> _logger = Substitute.For<ILogger<InventoryService>>();
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _service = new InventoryService(_repo, _logger);
    }

    #region GetInventoryAsync / GetEquippedAsync

    [Fact]
    public async Task GetInventory_ShouldReturnAllItems()
    {
        var items = new List<InventoryItem> { new() { CharId = 1, Slot = 1, ItemId = 100 } };
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _service.GetInventoryAsync(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEquipped_ShouldReturnEquippedOnly()
    {
        var items = new List<InventoryItem>
        {
            new() { CharId = 1, Slot = 1, ItemId = 100, IsEquipped = true, EquipSlot = 1 }
        };
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _service.GetEquippedAsync(1);
        result.Should().HaveCount(1);
    }

    #endregion

    #region AddItemAsync

    [Fact]
    public async Task AddItem_ShouldReturnSuccess_WhenSlotAvailable()
    {
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());

        var result = await _service.AddItemAsync(1, 5001, 3);

        result.Status.Should().Be(InventoryStatus.Success);
        result.Item.Should().NotBeNull();
        result.Item!.ItemId.Should().Be(5001);
        result.Item.Quantity.Should().Be(3);
        result.Item.Slot.Should().Be(1); // First empty slot
        await _repo.Received(1).AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItem_ShouldReturnInventoryFull_WhenAllSlotsTaken()
    {
        var items = Enumerable.Range(1, 50).Select(i => new InventoryItem
        {
            CharId = 1, Slot = (byte)i, ItemId = 100
        }).ToList();
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _service.AddItemAsync(1, 5001);
        result.Status.Should().Be(InventoryStatus.InventoryFull);
    }

    [Fact]
    public async Task AddItem_ShouldReturnFailed_OnDbException()
    {
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());
        _repo.When(r => r.AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.AddItemAsync(1, 5001);
        result.Status.Should().Be(InventoryStatus.Failed);
    }

    #endregion

    #region RemoveItemAsync

    [Fact]
    public async Task RemoveItem_ShouldReturnSuccess_WhenFullRemoval()
    {
        var item = new InventoryItem { CharId = 1, Slot = 5, ItemId = 100, Quantity = 3 };
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _service.RemoveItemAsync(1, 5, 3);
        result.Status.Should().Be(InventoryStatus.Success);
        await _repo.Received(1).DeleteAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItem_ShouldReduceQuantity_WhenPartialRemoval()
    {
        var item = new InventoryItem { CharId = 1, Slot = 5, ItemId = 100, Quantity = 10 };
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _service.RemoveItemAsync(1, 5, 3);
        result.Status.Should().Be(InventoryStatus.Success);
        item.Quantity.Should().Be(7);
        await _repo.Received(1).UpdateAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItem_ShouldReturnSlotEmpty_WhenNoItem()
    {
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await _service.RemoveItemAsync(1, 5);
        result.Status.Should().Be(InventoryStatus.SlotEmpty);
    }

    [Fact]
    public async Task RemoveItem_ShouldReturnInsufficientQuantity_WhenNotEnough()
    {
        var item = new InventoryItem { CharId = 1, Slot = 5, ItemId = 100, Quantity = 2 };
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _service.RemoveItemAsync(1, 5, 5);
        result.Status.Should().Be(InventoryStatus.InsufficientQuantity);
    }

    [Fact]
    public async Task RemoveItem_ShouldReturnInvalidSlot_WhenSlot0()
    {
        var result = await _service.RemoveItemAsync(1, 0);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task RemoveItem_ShouldReturnInvalidSlot_WhenSlotOver50()
    {
        var result = await _service.RemoveItemAsync(1, 51);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task RemoveItem_ShouldReturnFailed_OnDbException()
    {
        var item = new InventoryItem { CharId = 1, Slot = 5, ItemId = 100, Quantity = 1 };
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item);
        _repo.When(r => r.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.RemoveItemAsync(1, 5);
        result.Status.Should().Be(InventoryStatus.Failed);
    }

    #endregion

    #region MoveItemAsync

    [Fact]
    public async Task MoveItem_ShouldMoveToEmptySlot()
    {
        var item = new InventoryItem { CharId = 1, Slot = 1, ItemId = 100, Quantity = 5 };
        _repo.GetBySlotAsync(1, 1, Arg.Any<CancellationToken>()).Returns(item);
        _repo.GetBySlotAsync(1, 10, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await _service.MoveItemAsync(1, 1, 10, 5);
        result.Status.Should().Be(InventoryStatus.Success);
        item.Slot.Should().Be(10);
    }

    [Fact]
    public async Task MoveItem_ShouldSwapWithExistingItem()
    {
        var item1 = new InventoryItem { CharId = 1, Slot = 1, ItemId = 100 };
        var item2 = new InventoryItem { CharId = 1, Slot = 5, ItemId = 200 };
        _repo.GetBySlotAsync(1, 1, Arg.Any<CancellationToken>()).Returns(item1);
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item2);

        var result = await _service.MoveItemAsync(1, 1, 5);
        result.Status.Should().Be(InventoryStatus.Success);
        item1.Slot.Should().Be(5);
        item2.Slot.Should().Be(1);
    }

    [Fact]
    public async Task MoveItem_ShouldSplitStack_WhenPartialQuantity()
    {
        var item = new InventoryItem { CharId = 1, Slot = 1, ItemId = 100, Quantity = 10 };
        _repo.GetBySlotAsync(1, 1, Arg.Any<CancellationToken>()).Returns(item);
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await _service.MoveItemAsync(1, 1, 5, 3);
        result.Status.Should().Be(InventoryStatus.Success);
        item.Quantity.Should().Be(7);
        await _repo.Received(1).AddAsync(Arg.Is<InventoryItem>(i => i.Slot == 5 && i.Quantity == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveItem_ShouldReturnSlotEmpty_WhenSourceEmpty()
    {
        _repo.GetBySlotAsync(1, 1, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await _service.MoveItemAsync(1, 1, 5);
        result.Status.Should().Be(InventoryStatus.SlotEmpty);
    }

    [Fact]
    public async Task MoveItem_ShouldReturnInvalidSlot_WhenOutOfRange()
    {
        var result = await _service.MoveItemAsync(1, 0, 5);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);

        result = await _service.MoveItemAsync(1, 1, 51);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task MoveItem_ShouldReturnFailed_OnDbException()
    {
        var item = new InventoryItem { CharId = 1, Slot = 1, ItemId = 100, Quantity = 5 };
        _repo.GetBySlotAsync(1, 1, Arg.Any<CancellationToken>()).Returns(item);
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);
        _repo.When(r => r.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.MoveItemAsync(1, 1, 5, 5);
        result.Status.Should().Be(InventoryStatus.Failed);
    }

    #endregion

    #region EquipItemAsync

    [Fact]
    public async Task EquipItem_ShouldReturnSuccess_WhenValid()
    {
        var item = new InventoryItem { CharId = 1, Slot = 3, ItemId = 100, EquipSlot = 2 };
        _repo.GetBySlotAsync(1, 3, Arg.Any<CancellationToken>()).Returns(item);
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());

        var result = await _service.EquipItemAsync(1, 3);
        result.Status.Should().Be(InventoryStatus.Success);
        result.EquipSlot.Should().Be(2);
        result.InventorySlot.Should().Be(3);
        item.IsEquipped.Should().BeTrue();
    }

    [Fact]
    public async Task EquipItem_ShouldSwapWithExistingEquip()
    {
        var newItem = new InventoryItem { CharId = 1, Slot = 3, ItemId = 100, EquipSlot = 2 };
        var oldEquip = new InventoryItem { CharId = 1, Slot = 3, ItemId = 200, EquipSlot = 2, IsEquipped = true };
        _repo.GetBySlotAsync(1, 3, Arg.Any<CancellationToken>()).Returns(newItem);
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem> { oldEquip });

        var result = await _service.EquipItemAsync(1, 3);
        result.Status.Should().Be(InventoryStatus.Success);
        oldEquip.IsEquipped.Should().BeFalse();
        newItem.IsEquipped.Should().BeTrue();
    }

    [Fact]
    public async Task EquipItem_ShouldReturnSlotEmpty_WhenNoItem()
    {
        _repo.GetBySlotAsync(1, 3, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);
        var result = await _service.EquipItemAsync(1, 3);
        result.Status.Should().Be(InventoryStatus.SlotEmpty);
    }

    [Fact]
    public async Task EquipItem_ShouldReturnNotEquippable_WhenEquipSlot0()
    {
        var item = new InventoryItem { CharId = 1, Slot = 3, ItemId = 100, EquipSlot = 0 };
        _repo.GetBySlotAsync(1, 3, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _service.EquipItemAsync(1, 3);
        result.Status.Should().Be(InventoryStatus.ItemNotEquippable);
    }

    [Fact]
    public async Task EquipItem_ShouldReturnInvalidSlot_WhenOutOfRange()
    {
        var result = await _service.EquipItemAsync(1, 0);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);

        result = await _service.EquipItemAsync(1, 51);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task EquipItem_ShouldReturnFailed_OnDbException()
    {
        var item = new InventoryItem { CharId = 1, Slot = 3, ItemId = 100, EquipSlot = 1 };
        _repo.GetBySlotAsync(1, 3, Arg.Any<CancellationToken>()).Returns(item);
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());
        _repo.When(r => r.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.EquipItemAsync(1, 3);
        result.Status.Should().Be(InventoryStatus.Failed);
    }

    #endregion

    #region UnequipItemAsync

    [Fact]
    public async Task UnequipItem_ShouldReturnSuccess_WhenValid()
    {
        var item = new InventoryItem { CharId = 1, Slot = 0, ItemId = 100, EquipSlot = 2, IsEquipped = true };
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem> { item });
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        var result = await _service.UnequipItemAsync(1, 2, 5);
        result.Status.Should().Be(InventoryStatus.Success);
        result.EquipSlot.Should().Be(2);
        result.InventorySlot.Should().Be(5);
        item.IsEquipped.Should().BeFalse();
        item.Slot.Should().Be(5);
        item.EquipSlot.Should().Be(0);
    }

    [Fact]
    public async Task UnequipItem_ShouldReturnSlotEmpty_WhenNothingEquipped()
    {
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());

        var result = await _service.UnequipItemAsync(1, 2, 5);
        result.Status.Should().Be(InventoryStatus.SlotEmpty);
    }

    [Fact]
    public async Task UnequipItem_ShouldReturnSlotOccupied_WhenTargetTaken()
    {
        var equipped = new InventoryItem { Id = 1, CharId = 1, ItemId = 100, EquipSlot = 2, IsEquipped = true };
        var blocker = new InventoryItem { Id = 2, CharId = 1, Slot = 5, ItemId = 200 };
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem> { equipped });
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(blocker);

        var result = await _service.UnequipItemAsync(1, 2, 5);
        result.Status.Should().Be(InventoryStatus.SlotOccupied);
    }

    [Fact]
    public async Task UnequipItem_ShouldReturnInvalidSlot_WhenEquipSlotOutOfRange()
    {
        var result = await _service.UnequipItemAsync(1, 0, 5);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);

        result = await _service.UnequipItemAsync(1, 7, 5);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task UnequipItem_ShouldReturnInvalidSlot_WhenInvSlotOutOfRange()
    {
        var result = await _service.UnequipItemAsync(1, 1, 0);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);

        result = await _service.UnequipItemAsync(1, 1, 51);
        result.Status.Should().Be(InventoryStatus.InvalidSlot);
    }

    [Fact]
    public async Task UnequipItem_ShouldReturnFailed_OnDbException()
    {
        var item = new InventoryItem { CharId = 1, ItemId = 100, EquipSlot = 1, IsEquipped = true };
        _repo.GetEquippedAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem> { item });
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);
        _repo.When(r => r.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.UnequipItemAsync(1, 1, 5);
        result.Status.Should().Be(InventoryStatus.Failed);
    }

    #endregion

    #region DestroyItemAsync

    [Fact]
    public async Task DestroyItem_ShouldDelegateToRemoveItem()
    {
        var item = new InventoryItem { CharId = 1, Slot = 5, ItemId = 100, Quantity = 1 };
        _repo.GetBySlotAsync(1, 5, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _service.DestroyItemAsync(1, 5);
        result.Status.Should().Be(InventoryStatus.Success);
        await _repo.Received(1).DeleteAsync(item, Arg.Any<CancellationToken>());
    }

    #endregion

    #region FindEmptySlotAsync

    [Fact]
    public async Task FindEmptySlot_ShouldReturn1_WhenEmpty()
    {
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>());
        var slot = await _service.FindEmptySlotAsync(1);
        slot.Should().Be(1);
    }

    [Fact]
    public async Task FindEmptySlot_ShouldSkipUsedSlots()
    {
        var items = new List<InventoryItem>
        {
            new() { CharId = 1, Slot = 1, ItemId = 100 },
            new() { CharId = 1, Slot = 2, ItemId = 200 },
        };
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);
        var slot = await _service.FindEmptySlotAsync(1);
        slot.Should().Be(3);
    }

    [Fact]
    public async Task FindEmptySlot_ShouldReturnNull_WhenAllSlotsFull()
    {
        var items = Enumerable.Range(1, 50).Select(i => new InventoryItem
        {
            CharId = 1, Slot = (byte)i, ItemId = 100
        }).ToList();
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);
        var slot = await _service.FindEmptySlotAsync(1);
        slot.Should().BeNull();
    }

    [Fact]
    public async Task FindEmptySlot_ShouldIgnoreEquippedItems()
    {
        var items = new List<InventoryItem>
        {
            new() { CharId = 1, Slot = 1, ItemId = 100 },
            new() { CharId = 1, Slot = 2, ItemId = 200, IsEquipped = true }, // Should be ignored
        };
        _repo.GetByCharIdAsync(1, Arg.Any<CancellationToken>()).Returns(items);
        var slot = await _service.FindEmptySlotAsync(1);
        slot.Should().Be(2); // Slot 2 is "free" since that item is equipped
    }

    #endregion
}
