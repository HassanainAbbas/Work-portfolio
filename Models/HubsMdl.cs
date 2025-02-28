namespace Organify.Models
{
    public class HubsMdl
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
