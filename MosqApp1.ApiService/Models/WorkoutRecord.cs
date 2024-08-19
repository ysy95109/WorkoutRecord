namespace MosqApp1.ApiService.Models
{
    public class WorkoutRecord
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
