using System.Collections.Generic;
using Harmony;
using Network.Visibility;
using System;

namespace Auxide.Hooks.Server
{
    [HarmonyPatch(typeof(NetworkVisibilityGrid), "GetVisibleFrom", new[] { typeof(Group), typeof(List<Group>), typeof(int) })]
    public class CircularNetworkDistance
    {
        // By Vice
        // hardcoded values because this is probably fastest
        private static readonly List<int> EightCircle = new List<int>() { 2, 4, 6, 6, 7, 7, 8, 8, 8, 8, 8, 7, 7, 6, 6, 4, 2 };
        private static readonly List<int> SevenCircle = new List<int>() { 2, 4, 5, 6, 6, 7, 7, 7, 7, 7, 6, 6, 5, 4, 2 };
        private static readonly List<int> SixCircle = new List<int>() { 2, 4, 5, 5, 6, 6, 6, 6, 6, 5, 5, 4, 2 };
        private static readonly List<int> FiveCircle = new List<int>() { 2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2 };
        private static readonly List<int> FourCircle = new List<int>() { 2, 3, 4, 4, 4, 4, 4, 3, 2 };
        private static readonly List<int> ThreeCircle = new List<int>() { 1, 2, 3, 3, 3, 2, 1 };
        private static readonly List<int> TwoCircle = new List<int>() { 1, 2, 2, 2, 1 };

        public static bool Prefix(NetworkVisibilityGrid __instance, Group group, List<Group> groups, int radius)
        {
            if (Auxide.config["UseCircularNetworkDistance"] != null && Convert.ToBoolean(Auxide.config["UseCircularNetworkDistance"].Value))
            {
                return GetVisibleFromCircle(__instance, group, groups, radius);
            }
            return true;
        }

        private static List<int> GetCircleSizeLookup(int radius)
        {
            switch (radius)
            {
                case 8: return EightCircle;
                case 7: return SevenCircle;
                case 6: return SixCircle;
                case 5: return FiveCircle;
                case 4: return FourCircle;
                case 3: return ThreeCircle;
                case 2: return TwoCircle;

                default: return null;
            }
        }
        private static bool GetVisibleFromCircle(NetworkVisibilityGrid grid, Group group, List<Group> groups, int radius)
        {
            List<int> lookup = GetCircleSizeLookup(radius);
            if (lookup == null)
            {
                return true; // this should be a NotImplementedException but y'all are retarded
            }

            // Global netgroup
            groups.Add(Network.Net.sv.visibility.Get(0U));

            uint id = group.ID;
            if (id < grid.startID)
            {
                return false;
            }

            uint num = id - (uint)grid.startID;
            int x = (int)(num / grid.cellCount);
            int y = (int)(num % grid.cellCount);

            for (int deltaY = -radius; deltaY <= radius; deltaY++)
            {
                int bounds = lookup[deltaY + radius];
                for (int deltaX = -bounds; deltaX <= bounds; deltaX++)
                {
                    groups.Add(Network.Net.sv.visibility.Get(grid.CoordToID(x + deltaX, y + deltaY)));
                }
            }

            return false;
        }
    }
}
