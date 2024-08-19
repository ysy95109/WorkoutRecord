using Microsoft.EntityFrameworkCore;
using MosqApp1.ApiService.EntityFramework;
using MosqApp1.ApiService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<WorkoutRecordsDbContext>(options =>
    options.UseSqlite("Data Source=workoutRecords.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Get all workout records
app.MapGet("/workoutrecords", async (WorkoutRecordsDbContext db) =>
    await db.WorkoutRecords.ToListAsync());

// Get a specific workout record by ID
app.MapGet("/workoutrecords/{id}", async (int id, WorkoutRecordsDbContext db) =>
    await db.WorkoutRecords.FindAsync(id) is WorkoutRecord record
        ? Results.Ok(record)
        : Results.NotFound());

// Create a new workout record
app.MapPost("/workoutrecords", async (WorkoutRecord record, WorkoutRecordsDbContext db) =>
{
    db.WorkoutRecords.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/workoutrecords/{record.Id}", record);
});

// Update an existing workout record
app.MapPut("/workoutrecords/{id}", async (int id, WorkoutRecord inputRecord, WorkoutRecordsDbContext db) =>
{
    var record = await db.WorkoutRecords.FindAsync(id);
    if (record is null) return Results.NotFound();

    record.User = inputRecord.User;
    record.Description = inputRecord.Description;
    record.DateUpdated = DateTime.Now;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Delete a workout record
app.MapDelete("/workoutrecords/{id}", async (int id, WorkoutRecordsDbContext db) =>
{
    var record = await db.WorkoutRecords.FindAsync(id);
    if (record is null) return Results.NotFound();

    db.WorkoutRecords.Remove(record);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDefaultEndpoints();

// Apply database migrations and run the app
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutRecordsDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
