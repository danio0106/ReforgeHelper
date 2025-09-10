using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;
using System.Drawing;

namespace ReforgeHelper
{
    public class ReforgeHelperSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        [Menu(null, "Hotkey to start reforging process")]
        public HotkeyNodeV2 StartReforgeKey { get; set; } = new(Keys.F6);

        [Menu(null, "Emergency stop hotkey")]
        public HotkeyNodeV2 EmergencyStopKey { get; set; } = new(Keys.F7);

        [Menu(null, "Maximum allowed level difference in a triplet")]
        public RangeNode<int> MaxItemLevelDisparity { get; set; } = new(3, 0, 10);

        [Menu(null, "Minimum item level to consider")]
        public RangeNode<int> MinItemLevel { get; set; } = new(60, 1, 100);

        [Menu(null, "Maximum item level to consider")]
        public RangeNode<int> MaxItemLevel { get; set; } = new(100, 1, 100);

        [Menu(null, "Enable debug logging")]
        public ToggleNode EnableDebug { get; set; } = new ToggleNode(false);

        public ItemCategories ItemCategories { get; set; } = new ItemCategories();
        public MovementSettings Movement { get; set; } = new MovementSettings();
        public DisplaySettings Display { get; set; } = new DisplaySettings();
    }

    [Submenu]
    public class ItemCategories
    {
        public ToggleNode EnableSoulCores { get; set; } = new ToggleNode(true);
        public ToggleNode EnableJewels { get; set; } = new ToggleNode(true);
        public ToggleNode EnableRings { get; set; } = new ToggleNode(true);
        public ToggleNode EnableAmulets { get; set; } = new ToggleNode(true);
        public ToggleNode EnableWaystones { get; set; } = new ToggleNode(true);
        public ToggleNode EnableRelics { get; set; } = new ToggleNode(true);
        public ToggleNode EnableTablets { get; set; } = new ToggleNode(true);
        public ToggleNode EnableLiquidEmotions { get; set; } = new ToggleNode(true);
    }

    [Submenu]
    public class MovementSettings
    {
        [Menu(null, "Random variance in mouse movement speed (%)")]
        public RangeNode<int> SpeedVariance { get; set; } = new(20, 0, 50);

        [Menu(null, "Delay between clicks (ms)")]
        public RangeNode<int> ClickDelay { get; set; } = new(150, 50, 500);

        [Menu(null, "Random pause between movements (ms)")]
        public RangeNode<int> MovementPause { get; set; } = new(100, 50, 300);
    }

    [Submenu]
    public class DisplaySettings
    {
        public StatusText StatusText { get; set; } = new StatusText();
    }

    [Submenu]
    public class StatusText
    {
        public ToggleNode Enabled { get; set; } = new ToggleNode(true);
        public TextNode ProcessingText { get; set; } = "REFORGING";
        public ColorNode ProcessingColor { get; set; } = Color.Yellow;
        public TextNode IdleText { get; set; } = "READY";
        public ColorNode IdleColor { get; set; } = Color.Green;
        public RangeNode<int> PositionX { get; set; } = new(1230, 0, 2000);
        public RangeNode<int> PositionY { get; set; } = new(434, 0, 2000);
        public ToggleNode Background { get; set; } = new ToggleNode(true);
        public ColorNode BackgroundColor { get; set; } = Color.FromArgb(197, 0, 0, 0);
    }
}
