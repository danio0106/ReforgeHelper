using ExileCore2;
using ExileCore2.PoEMemory;
using System.Linq;
namespace ReforgeHelper.Helper
{
    public static class ReforgeBenchHelper
    {
        public static void LocateBenchElements(Element reforgeBench, 
            out Element[] itemSlots,
            out Element reforgeButton)
        {
            itemSlots = new Element[3];
            
            // Get the slots container
            var slotsContainer = reforgeBench.Children?.ElementAtOrDefault(3)?
                .Children?.ElementAtOrDefault(1);

            if (slotsContainer != null)
            {
                for (int i = 0; i < 3 && i < slotsContainer.Children.Count; i++)
                {
                    itemSlots[i] = slotsContainer.Children[i];
                }
            }

            // Get the reforge button
            reforgeButton = reforgeBench.Children?.ElementAtOrDefault(3)?
                .Children?.ElementAtOrDefault(1)?
                .Children?.ElementAtOrDefault(0)?
                .Children?.ElementAtOrDefault(0)?
                .Children?.ElementAtOrDefault(1);
        }

        public static Element FindReforgeBench(Element root)
        {
            RFLogger.Debug("Attempting to find reforge bench in UI elements");
            
            // First check root level children (more likely to find it here)
            foreach (var child in root.Children ?? Enumerable.Empty<Element>())
            {
                if (child?.IsVisible == true && IsReforgeBench(child))
                {
                    //DebugWindow.LogMsg($"[ReforgeBenchHelper] Found reforge bench at top level: {child.PathFromRoot}");
                    return child;
                }
            }

            // If not found at root level, search deeper
            foreach (var child in root.Children ?? Enumerable.Empty<Element>())
            {
                var result = SearchRecursively(child);
                if (result != null)
                {
                    return result;
                }
            }

            RFLogger.Debug("No reforge bench found in UI");
            return null;
        }

        private static Element SearchRecursively(Element element)
        {
            if (element == null) return null;

            if (element.IsVisible && IsReforgeBench(element))
            {
                RFLogger.Debug($"Found reforge bench: {element.PathFromRoot}");
                return element;
            }

            foreach (var child in element.Children ?? Enumerable.Empty<Element>())
            {
                var result = SearchRecursively(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static bool IsReforgeBench(Element element)
        {
            if (string.IsNullOrEmpty(element.PathFromRoot)) return false;
            
            // Log the path for debugging
            //DebugWindow.LogMsg($"[ReforgeBenchHelper] Checking element path: {element.PathFromRoot}");
            
            return element.PathFromRoot.Contains("ReforgingBench");
        }
    }
}