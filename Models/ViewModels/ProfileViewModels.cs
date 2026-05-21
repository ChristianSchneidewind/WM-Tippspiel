namespace TippSpiel.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int Rank { get; set; }
        public bool IsOwnProfile { get; set; }
        public List<UserTipProfileViewModel> Tips { get; set; } = new();
    }

    public class UserTipProfileViewModel
    {
        public int TipId { get; set; }
        public int GameId { get; set; }
        public DateTimeOffset KickOff { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public int HomeTeamTipp { get; set; }
        public int AwayTeamTipp { get; set; }
        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }
        public int Points { get; set; }
        public bool CanEdit { get; set; }
    }
}
