namespace TippSpiel.Helpers
{
    public static class GameHelper
    {
        public static string FixTeamName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var trimmed = name.Trim();

            // Behandlung von Platzhaltern aus der API oder Excel
            if (trimmed.StartsWith("Winner Match", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ');
                if (parts.Length >= 3) return $"Sieger aus Spiel #{parts[2]}";
            }
            if (trimmed.StartsWith("Loser Match", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(' ');
                if (parts.Length >= 3) return $"Verlierer aus Spiel #{parts[2]}";
            }
            if (trimmed.Equals("TBD", StringComparison.OrdinalIgnoreCase))
            {
                return "Noch offen";
            }

            return trimmed switch
            {
                "Saudiarabien" => "Saudi-Arabien",
                "IR Iran" => "Iran",
                "Curacao" => "Curaçao",
                "Republik Korea" => "Südkorea",
                _ => trimmed
            };
        }

        public static string GetPlaceholderName(int? matchNumber, bool isHome)
        {
            if (!matchNumber.HasValue) return "TBD";

            return matchNumber switch
            {
                // --- SECHZEHNTELFINALE (1/16) ---
                73 => isHome ? "2. Gruppe A" : "2. Gruppe B",
                74 => isHome ? "1. Gruppe A" : "3. Gruppe C/E/F",
                75 => isHome ? "1. Gruppe B" : "3. Gruppe A/C/D",
                76 => isHome ? "1. Gruppe C" : "3. Gruppe A/B/F",
                77 => isHome ? "1. Gruppe F" : "2. Gruppe C",
                78 => isHome ? "2. Gruppe E" : "2. Gruppe F",
                79 => isHome ? "1. Gruppe E" : "3. Gruppe A/B/D",
                80 => isHome ? "1. Gruppe D" : "3. Gruppe B/E/F",
                81 => isHome ? "1. Gruppe G" : "3. Gruppe A/B/C",
                82 => isHome ? "1. Gruppe H" : "2. Gruppe G",
                83 => isHome ? "1. Gruppe I" : "3. Gruppe D/E/G",
                84 => isHome ? "1. Gruppe J" : "2. Gruppe I",
                85 => isHome ? "1. Gruppe L" : "2. Gruppe K",
                86 => isHome ? "2. Gruppe L" : "2. Gruppe H",
                87 => isHome ? "1. Gruppe K" : "3. Gruppe G/H/I",
                88 => isHome ? "2. Gruppe J" : "2. Gruppe D",

                // --- ACHTELFINALE (1/8) ---
                89 => isHome ? "Sieger aus Spiel #74" : "Sieger aus Spiel #77",
                90 => isHome ? "Sieger aus Spiel #73" : "Sieger aus Spiel #75",
                91 => isHome ? "Sieger aus Spiel #76" : "Sieger aus Spiel #78",
                92 => isHome ? "Sieger aus Spiel #79" : "Sieger aus Spiel #80",
                93 => isHome ? "Sieger aus Spiel #83" : "Sieger aus Spiel #84",
                94 => isHome ? "Sieger aus Spiel #81" : "Sieger aus Spiel #82",
                95 => isHome ? "Sieger aus Spiel #86" : "Sieger aus Spiel #88",
                96 => isHome ? "Sieger aus Spiel #85" : "Sieger aus Spiel #87",

                // --- VIERTELFINALE (1/4) ---
                97 => isHome ? "Sieger aus Spiel #89" : "Sieger aus Spiel #90",
                98 => isHome ? "Sieger aus Spiel #93" : "Sieger aus Spiel #94",
                99 => isHome ? "Sieger aus Spiel #91" : "Sieger aus Spiel #92",
                100 => isHome ? "Sieger aus Spiel #95" : "Sieger aus Spiel #96",

                // --- HALBFINALE ---
                101 => isHome ? "Sieger aus Spiel #97" : "Sieger aus Spiel #98",
                102 => isHome ? "Sieger aus Spiel #99" : "Sieger aus Spiel #100",

                // --- SPIEL UM PLATZ 3 ---
                103 => isHome ? "Verlierer aus Spiel #101" : "Verlierer aus Spiel #102",

                // --- FINALE ---
                104 => isHome ? "Sieger aus Spiel #101" : "Sieger aus Spiel #102",

                // Standard-Rückfallwert
                _ => "TBD"
            };
        }
    }
}