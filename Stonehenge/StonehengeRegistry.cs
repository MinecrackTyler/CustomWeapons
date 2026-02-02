using System.Collections.Generic;

namespace CustomWeapons.Stonehenge
{
    public static class StonehengeRegistry
    {
        private static readonly Dictionary<FactionHQ, List<TurretCoordinator>> coordinators =
            new Dictionary<FactionHQ, List<TurretCoordinator>>();

        private static readonly Dictionary<FactionHQ, List<StonehengeControl>> turrets =
            new Dictionary<FactionHQ, List<StonehengeControl>>();

        // --- Coordinator ---
        public static void RegisterCoordinator(TurretCoordinator coord, FactionHQ faction)
        {
            if (faction == null) return;

            if (!coordinators.TryGetValue(faction, out var list))
            {
                list = new List<TurretCoordinator>();
                coordinators[faction] = list;
            }
            if (!list.Contains(coord))
                list.Add(coord);
        }

        public static void DeregisterCoordinator(TurretCoordinator coord, FactionHQ faction)
        {
            if (faction == null) return;

            if (coordinators.TryGetValue(faction, out var list))
                list.Remove(coord);
        }

        public static IReadOnlyList<TurretCoordinator> GetCoordinators(FactionHQ faction)
        {
            return coordinators.TryGetValue(faction, out var list) ? list : new List<TurretCoordinator>();
        }

        // --- Turret ---
        public static void RegisterTurret(StonehengeControl control, FactionHQ faction)
        {
            if (faction == null) return;

            if (!turrets.TryGetValue(faction, out var list))
            {
                list = new List<StonehengeControl>();
                turrets[faction] = list;
            }
            if (!list.Contains(control))
                list.Add(control);
        }

        public static void DeregisterTurret(StonehengeControl control, FactionHQ faction)
        {
            if (faction == null) return;

            if (turrets.TryGetValue(faction, out var list))
                list.Remove(control);
        }

        public static IReadOnlyList<StonehengeControl> GetTurrets(FactionHQ faction)
        {
            return turrets.TryGetValue(faction, out var list) ? list : new List<StonehengeControl>();
        }
    }
}