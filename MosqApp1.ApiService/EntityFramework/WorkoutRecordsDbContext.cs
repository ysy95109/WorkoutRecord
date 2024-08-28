using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MosqApp1.ApiService.Models;

namespace MosqApp1.ApiService.EntityFramework
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }

    public class WorkoutRecordsDbContext(DbContextOptions<WorkoutRecordsDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<WorkoutRecord> WorkoutRecords { get; set; }
    }
}
