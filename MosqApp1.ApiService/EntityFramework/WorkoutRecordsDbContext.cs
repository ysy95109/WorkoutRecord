using Microsoft.EntityFrameworkCore;
using MosqApp1.ApiService.Models;

namespace MosqApp1.ApiService.EntityFramework
{
    public class WorkoutRecordsDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<WorkoutRecord> WorkoutRecords { get; set; }
    }
}
