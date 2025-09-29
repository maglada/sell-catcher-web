namespace DiscApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool Is18 { get; set; } = false;
    }
}