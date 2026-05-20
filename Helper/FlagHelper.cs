namespace TippSpiel.Helpers
{
    public static class FlagHelper
    {
        public static string GetFlagClass(string? teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName)) return "fi-un";

            return teamName.Trim() switch
            {
                // --- GASTGEBER ---
                "Kanada" or "Canada" or "CAN" => "fi-ca",
                "Mexiko" or "Mexico" or "MEX" => "fi-mx",
                "USA" or "United States" or "United States of America" => "fi-us",

                // --- EUROPA (UEFA) ---
                "Belgien" or "Belgium" or "BEL" => "fi-be",
                "Bosnien und Herzegowina" or "Bosnia and Herzegovina" or "Bosnien/Herzeg." or "BIH" => "fi-ba",
                "Tschechien" or "Czech Republic" or "CZE" => "fi-cz",
                "Deutschland" or "Germany" or "GER" => "fi-de",
                "Dänemark" or "Denmark" or "DEN" => "fi-dk",
                "England" or "ENG" => "fi-gb-eng",
                "Frankreich" or "France" or "FRA" => "fi-fr",
                "Italien" or "Italy" or "ITA" => "fi-it",
                "Kroatien" or "Croatia" or "CRO" => "fi-hr",
                "Niederlande" or "Netherlands" or "NED" => "fi-nl",
                "Norwegen" or "Norway" or "NOR" => "fi-no",
                "Österreich" or "Austria" or "AUT" => "fi-at",
                "Polen" or "Poland" or "POL" => "fi-pl",
                "Portugal" or "POR" => "fi-pt",
                "Schottland" or "Scotland" or "SCO" => "fi-gb-sct",
                "Schweden" or "Sweden" or "SWE" => "fi-se",
                "Schweiz" or "Switzerland" or "SUI" => "fi-ch",
                "Serbien" or "Serbia" or "SRB" => "fi-rs",
                "Spanien" or "Spain" or "ESP" => "fi-es",
                "Türkei" or "Turkey" or "TUR" => "fi-tr",
                "Ukraine" or "UKR" => "fi-ua",
                "Wales" or "WAL" => "fi-gb-wls",

                // --- SÜDAMERIKA (CONMEBOL) ---
                "Argentinien" or "Argentina" or "ARG" => "fi-ar",
                "Brasilien" or "Brazil" or "BRA" => "fi-br",
                "Chile" or "CHI" => "fi-cl",
                "Ecuador" or "ECU" => "fi-ec",
                "Kolumbien" or "Colombia" or "COL" => "fi-co",
                "Paraguay" or "PAR" => "fi-py",
                "Peru" or "PER" => "fi-pe",
                "Uruguay" or "URU" => "fi-uy",
                "Venezuela" or "VEN" => "fi-ve",

                // --- AFRIKA (CAF) ---
                "Ägypten" or "Egypt" or "EGY" => "fi-eg",
                "Algerien" or "Algeria" or "ALG" => "fi-dz",
                "DR Kongo" or "DR Congo" or "COD" => "fi-cd",
                "Elfenbeinküste" or "Ivory Coast" or "CIV" => "fi-ci",
                "Ghana" or "GHA" => "fi-gh",
                "Kamerun" or "Cameroon" or "CMR" => "fi-cm",
                "Cape Verde" or "Kap Verde" or "CPV" => "fi-cv",
                "Marokko" or "Morocco" or "MAR" => "fi-ma",
                "Nigeria" or "NGA" => "fi-ng",
                "Senegal" or "SEN" => "fi-sn",
                "Südafrika" or "South Africa" or "RSA" => "fi-za",
                "Tunesien" or "Tunisia" or "TUN" => "fi-tn",

                // --- ASIEN (AFC) ---
                "Australien" or "Australia" or "AUS" => "fi-au",
                "Irak" or "Iraq" or "IRQ" => "fi-iq",
                "Iran" or "IR Iran" or "IRN" => "fi-ir",
                "Japan" or "JPN" => "fi-jp",
                "Jordan" or "JOR" or "Jordanien" => "fi-jo",
                "Katar" or "Qatar" or "QAT" => "fi-qa",
                "Saudi-Arabien" or "Saudiarabien" or "Saudi Arabia" or "KSA" => "fi-sa",
                "Südkorea" or "Republik Korea" or "South Korea" or "KOR" => "fi-kr",
                "Usbekistan" or "Uzbekistan" or "UZB" => "fi-uz",

                // --- NORD-, MITTELAMERIKA & KARIBIK (CONCACAF) ---
                "Costa Rica" or "CRC" => "fi-cr",
                "Curaçao" or "Curacao" or "CUW" => "fi-cw",
                "Haiti" or "HAI" => "fi-ht",
                "Panama" or "PAN" => "fi-pa",

                // --- OZEANIEN (OFC) ---
                "Neuseeland" or "New Zealand" or "NZL" => "fi-nz",

                // --- SONSTIGES ---
                "Play-Off A" or "Play-Off B" or "Play-Off C" => "fi-un",
                _ => "fi-xx"
            };
        }
    }
}