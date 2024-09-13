using System.Net.Http.Headers;

namespace MosqApp1.Web;

public class WorkoutRecordsApiClient(HttpClient httpClient)
{

    // Get all workout records
    public async Task<WorkoutRecord[]> GetWorkoutRecordsAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WorkoutRecord>? records = null;

        await foreach (var record in httpClient.GetFromJsonAsAsyncEnumerable<WorkoutRecord>("/workoutrecords", cancellationToken))
        {
            if (records?.Count >= maxItems)
            {
                break;
            }
            if (record is not null)
            {
                records ??= [];
                records.Add(record);
            }
        }

        return records?.ToArray() ?? [];
    }

    // Get a specific workout record by ID
    public async Task<WorkoutRecord?> GetWorkoutRecordAsync(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<WorkoutRecord>($"/workoutrecords/{id}", cancellationToken);
    }

    // Create a new workout record
    public async Task<WorkoutRecord?> CreateWorkoutRecordAsync(WorkoutRecord newRecord, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/workoutrecords", newRecord, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WorkoutRecord>(cancellationToken: cancellationToken);
        }

        return null;
    }

    // Update an existing workout record
    public async Task<bool> UpdateWorkoutRecordAsync(int id, WorkoutRecord updatedRecord, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/workoutrecords/{id}", updatedRecord, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    // Delete a workout record
    public async Task<bool> DeleteWorkoutRecordAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/workoutrecords/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<HttpResponseMessage> RequestLogin(LoginModel loginModel)
    {
        return await httpClient.PostAsJsonAsync("/login", loginModel);
    }

    public async Task<HttpResponseMessage> RequestRegistration(RegisterModel registerModel)
    {
        return await httpClient.PostAsJsonAsync("/register", registerModel);
    }

    public async Task<HttpResponseMessage> RequestUserInfo(string token)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await httpClient.GetAsync("/userinfo");
    }
}

// Record class definition
public record WorkoutRecord(int Id, string UserId, string UserDisplayName, string Description, DateTime DateCreated, DateTime DateUpdated)
{
    public int Id { get; set; } = Id;
    public string UserId { get; set; } = UserId;
    public string UserDisplayName { get; set; } = UserDisplayName;
    public string Description { get; set; } = Description;
    public DateTime DateCreated { get; set; } = DateCreated;
    public DateTime DateUpdated { get; set; } = DateUpdated;
}

public class LoginModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterModel
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
