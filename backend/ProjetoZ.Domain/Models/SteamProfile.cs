namespace ProjetoZ.Domian.Models
{
    public class SteamProfile
    {
        public string SteamId { get; set; } = "";

        public string Name { get; set; } = "";

        public string Avatar { get; set; } = "";

        public string ProfileUrl { get; set; } = "";
    }

    public class SteamResponse
    {
        public SteamPlayers Response { get; set; } = new();
    }


    public class SteamPlayers
    {
        public List<SteamProfile> Players { get; set; } = new();
    }

}
