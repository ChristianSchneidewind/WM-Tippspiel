namespace TippSpiel.Helpers
{
    public static class TippPointsHelper
    {
        public static int CalculatePoints(int tipHome, int tipAway, int? actualHome, int? actualAway)
        {
            if (!actualHome.HasValue || !actualAway.HasValue)
            {
                return 0;
            }

            if (tipHome == actualHome.Value && tipAway == actualAway.Value)
            {
                return 3;
            }

            var actualDiff = actualHome.Value - actualAway.Value;
            var tipDiff = tipHome - tipAway;

            var actualHomeWin = actualDiff > 0;
            var actualAwayWin = actualDiff < 0;
            var tipHomeWin = tipDiff > 0;
            var tipAwayWin = tipDiff < 0;

            if ((actualHomeWin && tipHomeWin) || (actualAwayWin && tipAwayWin))
            {
                return 2;
            }

            return 0;
        }
    }
}
