using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System; // Add this for Math.Max

namespace ReforgeHelper;

public class ItemProcessor
{
    private readonly GameController _gameController;
    private readonly ReforgeHelperSettings  _settings;
    private readonly TimeCache<List<ValidItem>> _inventoryItems;

    public ItemProcessor(GameController gameController, ReforgeHelperSettings  settings)
    {
        _gameController = gameController;
        _settings = settings;
        _inventoryItems = new TimeCache<List<ValidItem>>(ScanInventory, 200);
    }

    public class ValidItem
    {
        public Entity Entity { get; }
        public string BaseName { get; }
        public string BaseType { get; }
        public int ItemLevel { get; }
        public ItemRarity Rarity { get; }
        public ExileCore2.Shared.RectangleF ClientRect { get; }

        public ValidItem(Entity entity, string baseName, string baseType, int itemLevel, ItemRarity rarity, ExileCore2.Shared.RectangleF clientRect)
        {
            Entity = entity;
            BaseName = baseName;
            BaseType = baseType;
            ItemLevel = itemLevel;
            Rarity = rarity;
            ClientRect = clientRect;
        }
    }

    private void LogMessage(string message)
    {
        if (_settings.EnableDebug)
        {
            DebugWindow.LogMsg($"[ItemProcessor] {message}");
        }
    }

    private List<ValidItem> ScanInventory()
    {
        var validItems = new List<ValidItem>();

        if (!_gameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible)
        {
            LogMessage("Inventory panel not visible");
            return validItems;
        }

        var inventory = _gameController?.Game?.IngameState?.Data?.ServerData?.PlayerInventories[0]?.Inventory;
        if (inventory == null)
        {
            LogMessage("Inventory is null");
            return validItems;
        }

        var items = inventory.InventorySlotItems;
        if (items == null)
        {
            LogMessage("InventorySlotItems is null");
            return validItems;
        }

        LogMessage($"Found {items.Count} total items in inventory");

        foreach (var item in items)
        {
            if (item.Item == null || item.Address == 0)
            {
                LogMessage("Skipping null/invalid item");
                continue;
            }

            var entity = item.Item;
            var baseComponent = entity.GetComponent<Base>();
            var modsComponent = entity.GetComponent<Mods>();

            if (baseComponent == null || modsComponent == null)
            {
                LogMessage($"Missing component for item at {item.Address}");
                continue;
            }

            var baseName = baseComponent.Name;
            LogMessage($"Processing item: {baseName}");
            
            var baseType = GetBaseType(baseName);
            if (string.IsNullOrEmpty(baseType))
            {
                LogMessage($"No matching base type for {baseName}");
                continue;
            }

            var itemLevel = modsComponent.ItemLevel;
            var rarity = modsComponent.ItemRarity;

            if (!IsItemInLevelRange(itemLevel))
            {
                LogMessage($"Item level {itemLevel} outside range [{_settings.MinItemLevel.Value}-{_settings.MaxItemLevel.Value}]");
                continue;
            }

            if (!IsItemTypeEnabled(baseType))
            {
                LogMessage($"Item type {baseType} not enabled");
                continue;
            }

            var rect = item.GetClientRect();
            LogMessage($"Adding valid item: {baseType} (iLvl:{itemLevel}, Rarity:{rarity}) at {rect}");

            validItems.Add(new ValidItem(
                entity,
                baseName,
                baseType,
                itemLevel,
                rarity,
                rect));
        }

        LogMessage($"Found {validItems.Count} valid items");
        return validItems;
    }

    private string GetBaseType(string itemName)
    {
        // First, try to get the exact subtype
        var exactSubtype = ItemSubtypes.GetExactSubtype(itemName);
        if (!string.IsNullOrEmpty(exactSubtype))
            return exactSubtype;

        // Fallback to base category matching
        return ItemSubtypes.GetBaseCategory(itemName);
    }

    private bool IsItemTypeEnabled(string baseType)
    {
        var baseCategory = ItemSubtypes.GetBaseCategory(baseType);

        return baseCategory switch
        {
            "Soul Core" => _settings.ItemCategories.EnableSoulCores,
            "Jewel" => _settings.ItemCategories.EnableJewels,
            "Ring" => _settings.ItemCategories.EnableRings,
            "Amulet" => _settings.ItemCategories.EnableAmulets,
            "Waystone" => _settings.ItemCategories.EnableWaystones,
            _ => false
        };
    }

    private bool IsItemInLevelRange(int itemLevel)
    {
        return itemLevel >= _settings.MinItemLevel.Value && 
               itemLevel <= _settings.MaxItemLevel.Value;
    }

    public IEnumerable<ValidItem> GetValidItems()
    {
        return _inventoryItems.Value;
    }

    public List<List<ValidItem>> FormTriplets()
    {
        var triplets = new List<List<ValidItem>>();
        var items = _inventoryItems.Value;
        
        LogMessage($"FormTriplets called with {items.Count} items");

        var itemsByBaseType = items
            .GroupBy(x => new { x.BaseType, x.Rarity })
            .Where(g => g.Count() >= 3);

        foreach (var group in itemsByBaseType)
        {
            LogMessage($"Processing group: {group.Key.BaseType} (Rarity: {group.Key.Rarity}) with {group.Count()} items");
            
            var sortedItems = group.OrderBy(x => x.ItemLevel).ToList();
            
            // Generate ALL possible 3-item combinations within the group
            for (var i = 0; i < sortedItems.Count - 2; i++)
            {
                for (var j = i + 1; j < sortedItems.Count - 1; j++)
                {
                    for (var k = j + 1; k < sortedItems.Count; k++)
                    {
                        var levelDiff = Math.Max(
                            Math.Max(
                                Math.Abs(sortedItems[i].ItemLevel - sortedItems[j].ItemLevel),
                                Math.Abs(sortedItems[j].ItemLevel - sortedItems[k].ItemLevel)
                            ),
                            Math.Abs(sortedItems[i].ItemLevel - sortedItems[k].ItemLevel)
                        );

                        LogMessage($"Checking triplet: levels [{sortedItems[i].ItemLevel}, {sortedItems[j].ItemLevel}, {sortedItems[k].ItemLevel}] diff:{levelDiff}");
                        
                        if (levelDiff <= _settings.MaxItemLevelDisparity.Value)
                        {
                            triplets.Add(new List<ValidItem> 
                            { 
                                sortedItems[i], 
                                sortedItems[j], 
                                sortedItems[k] 
                            });
                        }
                    }
                }
            }
            }

        LogMessage($"Formed {triplets.Count} triplets");
        return triplets;
    }
}