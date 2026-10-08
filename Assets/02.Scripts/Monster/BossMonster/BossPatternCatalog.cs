namespace _02.Scripts.Monster.BossMonster
{
    internal static class BossPatternCatalog
    {
        private sealed class Definition
        {
            public readonly string Name;
            public readonly string Sound;
            public readonly int Phase;
            public readonly int Animation;
            public Definition(string name, string sound, int phase, int animation)
            {
                Name = name;
                Sound = sound;
                Phase = phase;
                Animation = animation;
            }
        }

        private static readonly Definition[] Definitions =
        {
            new Definition("CRESCENT CLEAVE", "Cleave", 1, 1),
            new Definition("RIFT LANES", "Rift", 1, 1),
            new Definition("HELLFIRE RAIN", "Rain", 1, 3),
            new Definition("SPIRAL BARRAGE", "Spiral", 2, 2),
            new Definition("CROSS SWEEP", "Beam", 3, 2),
            new Definition("CATACLYSM", "Doom", 3, 3),
            new Definition("DOUBLE CLAW", "Claw", 1, 1),
            new Definition("GROUND SLAM", "Slam", 1, 1),
            new Definition("SERPENT WAVE", "Serpent", 1, 1),
            new Definition("THORN FAN", "Thorn", 1, 1),
            new Definition("RADIAL SPIKES", "Spikes", 2, 3),
            new Definition("EXPANDING NOVA", "Nova", 2, 3),
            new Definition("TWIN NOVA", "Twin", 3, 3),
            new Definition("TRACKING ERUPTION", "Chase", 1, 3),
            new Definition("METEOR LATTICE", "Grid", 2, 3),
            new Definition("BLOOD POOLS", "Pools", 1, 3),
            new Definition("TARGETED VOLLEY", "Volley", 1, 2),
            new Definition("HOMING ORBS", "Homing", 2, 2),
            new Definition("ECHO TURRETS", "Echo", 2, 2),
            new Definition("ORBIT RELEASE", "Orbit", 2, 2),
            new Definition("PERIMETER BARRAGE", "Edge", 3, 2),
            new Definition("DASH REND", "Dash", 2, 1),
            new Definition("BURROW AMBUSH", "Burrow", 3, 1),
            new Definition("APOCALYPSE", "Finale", 3, 3),
            new Definition("SANGUINE LANCES", "Lances", 1, 1),
            new Definition("STORM PILLARS", "Storm", 2, 3),
            new Definition("ROTATING SCYTHES", "Scythes", 2, 1),
            new Definition("GRAVITY COLLAPSE", "Gravity", 2, 3),
            new Definition("ALTERNATING MAZE", "Maze", 3, 1),
            new Definition("EXECUTION NOVA", "Execution", 2, 3)
        };
        public static int Count => Definitions.Length;

        public static bool IsValid(BossEncounterPattern kind)
        {
            return (int)kind >= 0 && (int)kind < Definitions.Length;
        }

        public static string Name(BossEncounterPattern kind)
        {
            return IsValid(kind) ? Definitions[(int)kind].Name : "UNKNOWN";
        }

        public static string Sound(BossEncounterPattern kind)
        {
            return IsValid(kind) ? Definitions[(int)kind].Sound : "Doom";
        }

        public static int MinimumPhase(BossEncounterPattern kind)
        {
            return IsValid(kind) ? Definitions[(int)kind].Phase : 1;
        }

        public static int Animation(BossEncounterPattern kind)
        {
            return IsValid(kind) ? Definitions[(int)kind].Animation : 3;
        }
    }
}
