using ReforgeHelper.Helper;
using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using System.Drawing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ExileCore2.Shared.Enums;
using System.Windows.Forms;
using System.Threading;
using ExileCore2.Shared.Cache;

namespace ReforgeHelper;

public class ReforgeHelper : BaseSettingsPlugin<ReforgeHelperSettings>
{
    private TripletManager _tripletManager;
    private Element _lastFoundBench = null;
    private Element _reforgeButton = null;
    private Element[] _itemSlots = new Element[3];
    private DateTime _lastBenchLog = DateTime.MinValue;
    private const int LOG_INTERVAL_MS = 1000;
    private Random _random = new Random();
    private bool _isProcessing = false;
    private int _currentTripletIndex = 0;
    private CancellationTokenSource _processingCts;
    private List<List<TripletData>> _currentTriplets = new();

    public ReforgeHelper()
    {
        Name = "Reforge Helper";
    }

    public override bool Initialise()
    {
        RFLogger.Info("Starting initialization...");
        try
        {
            if (GameController == null || Settings == null)
            {
                RFLogger.Error("GameController or Settings is null!");
                return false;
            }

            _tripletManager = new TripletManager(GameController, Settings);
            RFLogger.Info("Successfully initialized");
            RFLogger.Initialize(Settings);
            return true;
        }
        catch (Exception ex)
        {
            RFLogger.Error($"Initialization failed: {ex.Message}");
            return false;
        }
    }

    private async Task MoveCursorSmoothly(Vector2 targetPos, CancellationToken cancellationToken = default)
    {
        var currentPos = Input.ForceMousePosition;
        var distance = Vector2.Distance(currentPos, targetPos);
        var steps = Math.Clamp((int)(distance / 15), 8, 20);

        var totalTime = _random.Next(30, 80);
        var stepDelay = totalTime / steps;

        for (var i = 0; i < steps; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var t = (i + 1) / (float)steps;
            var randomOffset = new Vector2(
                ((float)_random.NextDouble() * 2.5f) - 1.25f,
                ((float)_random.NextDouble() * 2.5f) - 1.25f
            );
            var nextPos = Vector2.Lerp(currentPos, targetPos, t) + randomOffset;
            Input.SetCursorPos(nextPos);
            await Task.Delay(_random.Next(2, 5), cancellationToken);
        }

        Input.SetCursorPos(targetPos);
        await Task.Delay(25, cancellationToken);
    }

    private async Task ClickElement(Element element, CancellationToken cancellationToken = default)
    {
        if (element == null) return;

        try
        {
            var rect = element.GetClientRect();
            var center = rect.Center;

            var targetPos = new Vector2(
                center.X + _random.Next(-3, 4),
                center.Y + _random.Next(-3, 4)
            );

            await MoveCursorSmoothly(targetPos, cancellationToken);

            Input.KeyDown(Keys.LButton);
            await Task.Delay(_random.Next(15, 30), cancellationToken);
            Input.KeyUp(Keys.LButton);

            await Task.Delay(_random.Next(50, 100), cancellationToken);
        }
        catch (Exception ex)
        {
            LogDebug($"Error clicking element: {ex.Message}");
        }
    }

    private bool IsReforgeBench(Element element)
    {
        try
        {
            if (element == null) return false;
            if (!element.IsValid || !element.IsVisible) return false;
            if (element.PathFromRoot == null) return false;

            return element.PathFromRoot.Contains("ReforgingBench");
        }
        catch (Exception ex)
        {
            if (Settings.EnableDebug)
            {
                DebugWindow.LogError($"[ReforgeHelper] Error in IsReforgeBench: {ex.Message}");
            }
            return false;
        }
    }

    private Element FindReforgeButton(Element bench)
    {
        try
        {
            var btn = bench?.Children?.ElementAtOrDefault(3)?.
                Children?.ElementAtOrDefault(1)?.
                Children?.ElementAtOrDefault(0)?.
                Children?.ElementAtOrDefault(0)?.
                Children?.ElementAtOrDefault(1);

            if (btn != null && btn.IsVisible)
            {
                var rect = btn.GetClientRect();
                LogDebug($"Found reforge button at: {rect.Center.X}, {rect.Center.Y}");
                return btn;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Error finding reforge button: {ex.Message}");
        }
        return null;
    }

    private void UpdateBenchElements(Element bench)
    {
        try
        {
            ReforgeBenchHelper.LocateBenchElements(bench, out _itemSlots, out _reforgeButton);

            if (Settings.EnableDebug)
            {
                for (int i = 0; i < _itemSlots.Length; i++)
                {
                    if (_itemSlots[i] != null)
                    {
                        var rect = _itemSlots[i].GetClientRect();
                        RFLogger.Debug($"Item slot {i} found at position: {rect.Center.X}, {rect.Center.Y}");
                    }
                }

                if (_reforgeButton != null)
                {
                    var rect = _reforgeButton.GetClientRect();
                    RFLogger.Debug($"Reforge button found at: {rect.Center.X}, {rect.Center.Y}");
                }
            }
        }
        catch (Exception ex)
        {
            RFLogger.Error($"Error updating bench elements: {ex.Message}");
        }
    }

    public async Task ClickReforgeButton(CancellationToken cancellationToken = default)
    {
        if (_reforgeButton == null || !_reforgeButton.IsVisible)
        {
            LogDebug("Reforge button not found or not visible");
            return;
        }

        LogDebug("Clicking reforge button...");
        await ClickElement(_reforgeButton, cancellationToken);
    }

    private void CheckReforgeBench()
    {
        try
        {
            if ((DateTime.Now - _lastBenchLog).TotalMilliseconds < LOG_INTERVAL_MS) return;

            var bench = FindReforgeBenchElement();
            if (bench != null)
            {
                RFLogger.Debug($"Found bench with path: {bench.PathFromRoot}");
                if (bench != _lastFoundBench)
                {
                    _lastFoundBench = bench;
                    LogDebug($"Found Reforge Bench at address: {bench.Address}");
                    LogDebug($"Bench Path: {bench.PathFromRoot}");
                    UpdateBenchElements(bench);
                }
            }
            else if (_lastFoundBench != null)
            {
                _lastFoundBench = null;
                _reforgeButton = null;
                Array.Clear(_itemSlots, 0, _itemSlots.Length);
                LogDebug("Reforge Bench no longer visible");
            }

            _lastBenchLog = DateTime.Now;
        }
        catch (Exception ex)
        {
            LogDebug($"Error checking reforge bench: {ex.Message}");
        }
    }

    private Element FindReforgeBenchElement()
    {
        try
        {
            RFLogger.Debug("Searching for reforge bench...");
            var ingameUi = GameController?.Game?.IngameState?.IngameUi;
            if (ingameUi == null)
            {
                RFLogger.Debug("IngameUi is null");
                return null;
            }

            return ReforgeBenchHelper.FindReforgeBench(ingameUi);
        }
        catch (Exception ex)
        {
            RFLogger.Error($"Error finding reforge bench: {ex.Message}");
            return null;
        }
    }

    private void LogDebug(string message)
    {
        RFLogger.Debug(message);
    }

    private Element GetReforgeButton()
    {
        try
        {
            // Adjusted path to locate the enabled reforge button
            var button = _lastFoundBench?.Children?.ElementAtOrDefault(3)?
                .Children?.ElementAtOrDefault(1)?
                .Children?.ElementAtOrDefault(0)?
                .Children?.ElementAtOrDefault(0);

            if (button != null && button.IsVisible)
            {
                LogDebug("[ReforgeHelper] Enabled reforge button found and visible.");
                return button;
            }
            else
            {
                LogDebug("[ReforgeHelper] Enabled reforge button not found or not visible.");
                return null;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"[ReforgeHelper] Error locating reforge button: {ex.Message}");
            return null;
        }
    }

    private async Task ProcessNextTriplet(CancellationToken ct)
    {
        try
        {
            if (_currentTripletIndex >= _currentTriplets.Count) return;
            
            // Make sure bench is still available during processing
            if (IsReforgeBenchUiOpen())
            {
                // Refresh bench reference if needed
                if (_lastFoundBench == null || !_lastFoundBench.IsVisible)
                {
                    CheckReforgeBench();
                    
                    if (_lastFoundBench == null || !_lastFoundBench.IsVisible)
                    {
                        LogDebug("Lost reference to reforge bench during processing");
                        StopProcessing();
                        return;
                    }
                }
            }
            else
            {
                LogDebug("Reforge bench UI closed during processing");
                StopProcessing();
                return;
            }

            var triplet = _currentTriplets[_currentTripletIndex];
            LogDebug($"Processing triplet {_currentTripletIndex + 1}/{_currentTriplets.Count}");

            // Safety check before starting the sequence
            if (!IsBenchAndInventoryOpen())
            {
                LogDebug("Reforge bench or inventory not open, stopping process.");
                StopProcessing();
                return;
            }

            // Step 1: Place the triplet into the reforge bench slots
            for (int i = 0; i < triplet.Count; i++)
            {
                if (ct.IsCancellationRequested) return;

                var rect = triplet[i].ClientRect;

                // Click on the item to move it to the reforge bench
                await MoveCursorSmoothly(rect.Center, ct);
                Input.KeyDown(Keys.ControlKey);
                await Task.Delay(50, ct);
                Input.Click(MouseButtons.Left);
                await Task.Delay(100, ct);
                Input.KeyUp(Keys.ControlKey);

                LogDebug($"Moved item {i + 1}/{triplet.Count} of triplet to the reforge bench.");
            }

            var isLiquid = triplet.Any(x => ItemSubtypes.GetBaseCategory(x.BaseType) == "Liquid Emotions");

            if (isLiquid)
            {
                while (!ct.IsCancellationRequested)
                {
                    var button = GetReforgeButton();
                    if (button == null)
                        break;

                    LogDebug("Clicking the reforge button...");
                    var rect = button.GetClientRect();
                    await MoveCursorSmoothly(rect.Center, ct);
                    Input.Click(MouseButtons.Left);
                    await Task.Delay(1000, ct);

                    await MoveResultItem(ct);

                    if (_itemSlots.Any(slot => GetBenchSlotItemCount(slot) < 3))
                        break;
                }

                await ClearBench(ct);
            }
            else
            {
                var reforgeButton = GetReforgeButton();
                if (reforgeButton != null)
                {
                    LogDebug("Clicking the reforge button...");
                    var reforgeButtonRect = reforgeButton.GetClientRect();
                    await MoveCursorSmoothly(reforgeButtonRect.Center, ct);
                    Input.Click(MouseButtons.Left);
                    await Task.Delay(1000, ct); // Wait for the reforging process to complete
                }
                else
                {
                    LogDebug("Reforge button not found or not visible.");
                    StopProcessing();
                    return;
                }

                await MoveResultItem(ct);
            }

            // Step 4: Process the next triplet
            _currentTripletIndex++;
            if (_currentTripletIndex < _currentTriplets.Count)
            {
                await Task.Delay(200, ct);
                _ = ProcessNextTriplet(ct); // Continue to the next triplet
            }
            else
            {
                // All initially queued triplets processed – rescan inventory
                await Task.Delay(200, ct);
                var newTriplets = _tripletManager.FormTriplets();
                if (newTriplets.Count > 0)
                {
                    _currentTriplets = newTriplets;
                    _currentTripletIndex = 0;
                    LogDebug($"Found {newTriplets.Count} new triplets");
                    _ = ProcessNextTriplet(ct);
                }
                else
                {
                    _isProcessing = false;
                    LogDebug("Finished processing all triplets.");
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Error in ProcessNextTriplet: {ex.Message}");
            StopProcessing();
        }
    }



    private bool IsBenchAndInventoryOpen()
    {
        try
        {
            var ingameUi = GameController?.Game?.IngameState?.IngameUi;
            if (ingameUi == null) return false;

            var inventoryPanel = ingameUi.InventoryPanel;
            if (inventoryPanel == null || !inventoryPanel.IsVisible) return false;

            if (_lastFoundBench == null || !_lastFoundBench.IsVisible) return false;

            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Error checking UI state: {ex.Message}");
            return false;
        }
    }

    private ExileCore2.Shared.RectangleF? GetResultItemRect()
    {
        try
        {
            var resultSlot = _lastFoundBench?.Children?.ElementAtOrDefault(3)?
                .Children?.ElementAtOrDefault(1)?
                .Children?.ElementAtOrDefault(1);

            if (resultSlot?.IsVisible == true)
            {
                var rect = resultSlot.GetClientRect();
                RFLogger.Debug($"Found result item at: {rect.Center.X}, {rect.Center.Y}");
                return rect;
            }
            else
            {
                RFLogger.Debug("Result item slot not found or not visible");
            }
        }
        catch (Exception ex)
        {
            RFLogger.Error($"Error getting result item rect: {ex.Message}");
        }
        return null;
    }

    private async Task MoveResultItem(CancellationToken cancellationToken)
    {
        var resultRect = GetResultItemRect();
        if (resultRect.HasValue)
        {
            // Move cursor to the result slot
            await MoveCursorSmoothly(resultRect.Value.Center, cancellationToken);
            // Hold CTRL, click, release CTRL to send the item to the next open inventory slot
            Input.KeyDown(Keys.ControlKey);
            await Task.Delay(50, cancellationToken); // Small delay to ensure proper input
            Input.Click(MouseButtons.Left);          // Perform the click
            await Task.Delay(50, cancellationToken); // Ensure the action is registered
            Input.KeyUp(Keys.ControlKey);

            RFLogger.Debug("Result item moved to the next open inventory slot");
        }
        else
        {
            RFLogger.Debug("No result item found to move");
        }
    }

    private bool BenchSlotHasItem(Element slot)
    {
        if (slot?.IsVisible != true)
            return false;

        // A slot with an item will have a child element with either text (stack size)
        // or its own children (the item element). Empty slots typically have only
        // placeholder children without text and no further descendants.
        return slot.Children.Any(c => c.Children.Count > 0 || !string.IsNullOrEmpty(c.Text));
    }

    private int GetBenchSlotItemCount(Element slot)
    {
        if (!BenchSlotHasItem(slot))
            return 0;

        var stackElement = slot.Children
            .SelectMany(c => c.Children)
            .FirstOrDefault(c => !string.IsNullOrEmpty(c.Text));

        if (stackElement != null && int.TryParse(stackElement.Text, out int stackSize))
            return stackSize;

        return 1;
    }

    private async Task ClearBench(CancellationToken ct)
    {
        foreach (var slot in _itemSlots)
        {
            if (!BenchSlotHasItem(slot))
                continue;

            var rect = slot.GetClientRect();
            await MoveCursorSmoothly(rect.Center, ct);
            Input.KeyDown(Keys.ControlKey);
            await Task.Delay(50, ct);
            Input.Click(MouseButtons.Left);
            await Task.Delay(100, ct);
            Input.KeyUp(Keys.ControlKey);
        }
    }

    private void StartProcessing()
    {
        RFLogger.Debug("Attempting to start processing...");
        if (_isProcessing)
        {
            RFLogger.Debug("Already processing, skipping");
            return;
        }
        
        // Ensure bench is still valid
        if (_lastFoundBench == null || !_lastFoundBench.IsVisible)
        {
            LogDebug("Reforge bench no longer available");
            return;
        }

        _currentTriplets = _tripletManager.FormTriplets();
        if (_currentTriplets.Count == 0)
        {
            LogDebug("No triplets found");
            return;
        }

        _isProcessing = true;
        _currentTripletIndex = 0;
        _processingCts = new CancellationTokenSource();
        _ = ProcessNextTriplet(_processingCts.Token);
        LogDebug($"Started processing {_currentTriplets.Count} triplets");
    }

    private void StopProcessing()
    {
        if (!_isProcessing) return;
        _processingCts?.Cancel();
        _isProcessing = false;
        LogDebug("Processing stopped");
    }

    private bool IsInSafeZone()
    {
        var area = GameController.Area.CurrentArea;
        return area?.IsHideout == true || area?.IsTown == true;
    }

    public override void Tick()
    {
        try
        {
            if (!Settings.Enable || !GameController.Window.IsForeground()) return;
            if (_tripletManager == null) return;

            // Only process in safe zones
            if (!IsInSafeZone())
            {
                _lastFoundBench = null;
                return;
            }

            // We'll check for the bench only when processing or when hotkey is pressed
            // No continuous bench checking on every tick

            // Process hotkeys
            if (Settings.StartReforgeKey.PressedOnce())
            {
                if (!_isProcessing)
                {
                // Only check for bench when hotkey is pressed and not already processing
                if (IsReforgeBenchUiOpen())
                {
                    CheckReforgeBench();
                        
                    if (_lastFoundBench?.IsVisible == true)
                    {
                        StartProcessing();
                    }
                    else
                    {
                        LogDebug("Reforge bench not found or not visible. Please open the reforge bench.");
                    }
                }
                else
                {
                    LogDebug("Reforge bench UI is not open. Please open the reforge bench first.");
                }
            }
                else
                {
                    StopProcessing();
                }
            }

            if (Settings.EmergencyStopKey.PressedOnce())
            {
                StopProcessing();
            }
        }
        catch (Exception ex)
        {
            RFLogger.Error($"Tick error: {ex.Message}");
        }
    }

    public override void Render()
    {
        try
        {
            if (!Settings.Enable) return;
            if (_tripletManager == null) return;

            if (_lastFoundBench != null && _lastFoundBench.IsVisible)
            {

                if (_reforgeButton != null && _reforgeButton.IsVisible)
                {
                    var rect = _reforgeButton.GetClientRect();
                    Graphics.DrawFrame(rect, Color.Green, 2);
                }
            }

            if (_isProcessing)
            {
                var statusText = $"Processing triplet {_currentTripletIndex + 1}/{_currentTriplets.Count}";
                var pos = new Vector2(GameController.Window.GetWindowRectangle().Width / 2, 100);
                Graphics.DrawText(statusText, pos, Color.Yellow);
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Render error: {ex.Message}");
        }
    }

    public class TripletData
    {
        public Entity Entity { get; }
        public string BaseName { get; }
        public string BaseType { get; }
        public int ItemLevel { get; }
        public ItemRarity Rarity { get; }
        public ExileCore2.Shared.RectangleF ClientRect { get; }
        public int StackSize { get; set; }
        private readonly GameController _gc;

        public TripletData(Entity entity, GameController gc, ExileCore2.Shared.RectangleF rect, int stackSize = 1)
        {
            Entity = entity;
            _gc = gc;
            ClientRect = rect;
            StackSize = stackSize;

            var baseComponent = entity.GetComponent<Base>();
            var modsComponent = entity.GetComponent<Mods>();

            BaseName = baseComponent?.Name ?? string.Empty;
            BaseType = GetBaseType(BaseName);
            ItemLevel = modsComponent?.ItemLevel ?? 0;
            Rarity = modsComponent?.ItemRarity ?? ItemRarity.Normal;
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
    }
    private bool IsReforgeBenchUiOpen()
        {
            try
            {
                var ingameUi = GameController?.Game?.IngameState?.IngameUi;
                if (ingameUi == null) return false;

                // Loop through visible panels to find reforge UI
                foreach (var panel in ingameUi.Children)
                {
                    if (panel?.IsVisible == true &&
                        panel.PathFromRoot?.Contains("ReforgingBench") == true)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                RFLogger.Error($"Error checking reforge UI: {ex.Message}");
            }
            return false;
        }
    public class TripletManager
    {
        private readonly TimeCache<List<TripletData>> _inventoryItems;
        private readonly GameController _gameController;
        private readonly ReforgeHelperSettings _settings;

        public TripletManager(GameController gc, ReforgeHelperSettings settings)
        {
            _gameController = gc;
            _settings = settings;
            _inventoryItems = new TimeCache<List<TripletData>>(ScanInventory, 200);
        }

        private List<TripletData> ScanInventory()
        {
            var items = new List<TripletData>();
            var inventory = _gameController?.Game?.IngameState?.Data?.ServerData?.PlayerInventories[0]?.Inventory;

            if (inventory?.InventorySlotItems == null)
            {
                RFLogger.Debug("Inventory or InventorySlotItems is null.");
                return items;
            }

            var itemCount = inventory.InventorySlotItems.Count;
            RFLogger.Debug($"Scanning inventory - Found {itemCount} total items");

            foreach (var item in inventory.InventorySlotItems)
            {
                if (item.Item == null || item.Address == 0) continue;

                var baseComp = item.Item.GetComponent<Base>();
                var modsComp = item.Item.GetComponent<Mods>();
                var stackComp = item.Item.GetComponent<Stack>();
                var stackSize = stackComp?.Size ?? 1;

                RFLogger.Debug(
                    $"Item: {baseComp?.Name} | Stack: {stackSize} | Level: {modsComp?.ItemLevel} | Rarity: {modsComp?.ItemRarity}"
                );

                items.Add(new TripletData(item.Item, _gameController, item.GetClientRect(), stackSize));
            }

            return items;
        }

        private List<TripletData> FindBestTriplet(List<TripletData> availableItems)
        {
            if (availableItems.Count < 3) return null;

            // Sort by item level for better triplet selection
            var sortedItems = availableItems.OrderBy(x => x.ItemLevel).ToList();

            for (int i = 0; i < sortedItems.Count - 2; i++)
            {
                var triplet = new List<TripletData> { sortedItems[i], sortedItems[i + 1], sortedItems[i + 2] };

                var levelDiff = Math.Max(
                    Math.Max(
                        Math.Abs(triplet[0].ItemLevel - triplet[1].ItemLevel),
                        Math.Abs(triplet[1].ItemLevel - triplet[2].ItemLevel)
                    ),
                    Math.Abs(triplet[0].ItemLevel - triplet[2].ItemLevel)
                );

                if (levelDiff <= _settings.MaxItemLevelDisparity.Value)
                {
                    return triplet;
                }
            }

            return null;
        }


        private bool IsValidForReforge(TripletData item)
        {
            // Add check for unidentified items
            var modsComponent = item.Entity.GetComponent<Mods>();
            if (modsComponent?.Identified == false)
            {
                RFLogger.Debug($"Item {item.BaseName} is unidentified - skipping");
                return false;
            }
            // Ensure item has necessary components
            var baseComponent = item.Entity.GetComponent<Base>();
            if (baseComponent == null || modsComponent == null)
            {
                DebugWindow.LogMsg($"Item {item.BaseName} missing required components - skipping");
                return false;
            }

            // Check if item is corrupted using the Base component
            if (baseComponent.isCorrupted)
            {
                DebugWindow.LogMsg($"Item {item.BaseName} is corrupted - skipping");
                return false;
            }

            var baseCategory = ItemSubtypes.GetBaseCategory(item.BaseType);

            // Category enabled check
            bool isCategoryEnabled = baseCategory switch
            {
                "Soul Core" => _settings.ItemCategories.EnableSoulCores,
                "Jewel" => _settings.ItemCategories.EnableJewels,
                "Ring" => _settings.ItemCategories.EnableRings,
                "Amulet" => _settings.ItemCategories.EnableAmulets,
                "Waystone" => _settings.ItemCategories.EnableWaystones,
                "Relic" => _settings.ItemCategories.EnableRelics,
                "Tablet" => _settings.ItemCategories.EnableTablets,
                _ => false
            };

            if (!isCategoryEnabled)
                return false;

            // Item level check
            if (item.ItemLevel < _settings.MinItemLevel.Value ||
                item.ItemLevel > _settings.MaxItemLevel.Value)
                return false;

            // Rarity check
            bool needsMinMagicRarity = baseCategory is "Jewel" or "Ring" or "Amulet" or "Wand";
            if (needsMinMagicRarity)
            {
                return item.Rarity == ItemRarity.Magic ||
                       item.Rarity == ItemRarity.Rare ||
                       item.Rarity == ItemRarity.Unique;
            }
            else
            {
                return true; // Any rarity allowed for Waystones and Soul Cores
            }
        }

        public List<List<TripletData>> FormTriplets()
        {
            var triplets = new List<List<TripletData>>();

            // Handle Liquid Emotions separately
            if (_settings.ItemCategories.EnableLiquidEmotions)
            {
                var emotionItems = _inventoryItems.Value
                    .Where(item => ItemSubtypes.GetBaseCategory(item.BaseType) == "Liquid Emotions")
                    .Where(item => item.StackSize >= 3)
                    .GroupBy(item => item.BaseType);

                foreach (var group in emotionItems)
                {
                    var sorted = group.ToList();
                    while (sorted.Count >= 3)
                    {
                        var triplet = sorted.Take(3)
                            .Select(item => new TripletData(item.Entity, _gameController, item.ClientRect, item.StackSize))
                            .ToList();
                        sorted.RemoveRange(0, 3);
                        triplets.Add(triplet);
                        RFLogger.Debug($"Formed Liquid Emotion triplet: {triplet[0].BaseType}");
                    }
                }
            }

            var items = _inventoryItems.Value
                .Where(IsValidForReforge)
                .ToList();

            RFLogger.Debug($"Forming triplets from {items.Count} valid items");

            var itemsByBaseType = items
                .GroupBy(x => new { x.BaseType, x.Rarity })
                .Where(g => g.Count() >= 3);

            foreach (var group in itemsByBaseType)
            {
                RFLogger.Debug($"Processing group: {group.Key.BaseType} (Rarity: {group.Key.Rarity}) with {group.Count()} items");
                var sortedItems = group.OrderBy(x => x.ItemLevel).ToList();

                while (sortedItems.Count >= 3)
                {
                    var triplet = sortedItems.Take(3)
                        .Select(item => new TripletData(item.Entity, _gameController, item.ClientRect, item.StackSize))
                        .ToList();
                    sortedItems.RemoveRange(0, 3);
                    triplets.Add(triplet);
                }
            }

            RFLogger.Debug($"Formed {triplets.Count} triplets");
            return triplets;
        }
    }
}
