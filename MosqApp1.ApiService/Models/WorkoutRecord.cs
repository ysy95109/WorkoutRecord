namespace MosqApp1.ApiService.Models
{
    public record WorkoutRecord
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateUpdated { get; set; }
    }
}
