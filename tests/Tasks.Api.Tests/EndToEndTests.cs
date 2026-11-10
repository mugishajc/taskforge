using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tasks.Api.Features.Comments;
using Tasks.Api.Features.Projects;
using Tasks.Api.Features.Tasks;
using Tasks.Api.Features.Users;
using Xunit;

namespace Tasks.Api.Tests;

public class EndToEndTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private HttpClient Client => factory.CreateClient();

    private async Task<string> RegisterAndGetToken(string email, string password = "s3cret-pass!", string? name = null)
    {
        var r = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, name));
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    private HttpClient Authed(string token)
    {
        var c = factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<(HttpClient client, ProjectDto project)> SetupProjectAsync(string email)
    {
        var token = await RegisterAndGetToken(email);
        var client = Authed(token);
        var pResp = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("TestProject"));
        var project = await pResp.Content.ReadFromJsonAsync<ProjectDto>();
        return (client, project!);
    }

    [Fact]
    public async Task Healthz_IsOpen()
    {
        var r = await Client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedProjectsListIs401()
    {
        var r = await Client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task RegisterLoginCreateProjectCreateTaskUpdateTask()
    {
        var token = await RegisterAndGetToken("e2e-alice@example.com");
        var client = Authed(token);

        var pResp = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Launch"));
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var project = await pResp.Content.ReadFromJsonAsync<ProjectDto>();

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks",
            new CreateTaskRequest("Write launch plan", null, Tasks.Api.Domain.TaskPriority.High, null, null));
        Assert.Equal(HttpStatusCode.Created, tResp.StatusCode);
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        var uResp = await client.PutAsJsonAsync($"/api/tasks/{task!.Id}",
            new UpdateTaskRequest("Write launch plan", "v1", Tasks.Api.Domain.TaskStatus.InProgress,
                Tasks.Api.Domain.TaskPriority.High, DateTime.UtcNow.AddDays(3), null));
        uResp.EnsureSuccessStatusCode();
        var updated = await uResp.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal(Tasks.Api.Domain.TaskStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task UserCannotSeeAnotherUsersProjects()
    {
        var aToken = await RegisterAndGetToken("e2e-a@example.com");
        var bToken = await RegisterAndGetToken("e2e-b@example.com");

        var a = Authed(aToken);
        var pResp = await a.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Secret"));
        var project = await pResp.Content.ReadFromJsonAsync<ProjectDto>();

        var b = Authed(bToken);
        var bList = await b.GetFromJsonAsync<List<ProjectDto>>("/api/projects");
        Assert.DoesNotContain(bList!, p => p.Id == project!.Id);

        var bGet = await b.GetAsync($"/api/projects/{project!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, bGet.StatusCode);
    }

    [Fact]
    public async Task DuplicateRegistrationIs409()
    {
        await RegisterAndGetToken("e2e-dup@example.com");
        var r = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("e2e-dup@example.com", "s3cret-pass!", null));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    // --- New tests ---

    [Fact]
    public async Task GetProfile_ReturnsCurrentUser()
    {
        var token = await RegisterAndGetToken("e2e-profile@example.com", name: "Test User");
        var client = Authed(token);

        var r = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var profile = await r.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("e2e-profile@example.com", profile!.Email);
        Assert.Equal("Test User", profile.Name);
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var token = await RegisterAndGetToken("e2e-upd-profile@example.com", name: "Old Name");
        var client = Authed(token);

        var r = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest("New Name"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var profile = await r.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("New Name", profile!.Name);
    }

    [Fact]
    public async Task ListUsers_ReturnsRegisteredUsers()
    {
        var token = await RegisterAndGetToken("e2e-listusers@example.com");
        var client = Authed(token);

        var r = await client.GetFromJsonAsync<List<UserProfileDto>>("/api/users");
        Assert.NotNull(r);
        Assert.True(r!.Count >= 1);
    }

    [Fact]
    public async Task AssignTask_SetsAssignee()
    {
        var (client, project) = await SetupProjectAsync("e2e-assign@example.com");

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Assignable task", null, Tasks.Api.Domain.TaskPriority.Medium, null, null));
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        // Assign to user id 1 (any registered user)
        var aResp = await client.PatchAsync($"/api/tasks/{task!.Id}/assign",
            JsonContent.Create(new AssignTaskRequest(task.Id)));
        Assert.Equal(HttpStatusCode.OK, aResp.StatusCode);
    }

    [Fact]
    public async Task CreateAndListComments()
    {
        var (client, project) = await SetupProjectAsync("e2e-comments@example.com");

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Commentable task", null, Tasks.Api.Domain.TaskPriority.Medium, null, null));
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        var cResp = await client.PostAsJsonAsync($"/api/tasks/{task!.Id}/comments",
            new CreateCommentRequest("First comment"));
        Assert.Equal(HttpStatusCode.Created, cResp.StatusCode);
        var comment = await cResp.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("First comment", comment!.Body);

        var listResp = await client.GetFromJsonAsync<List<CommentDto>>($"/api/tasks/{task.Id}/comments");
        Assert.Single(listResp!);
    }

    [Fact]
    public async Task DeleteComment_ByAuthor()
    {
        var (client, project) = await SetupProjectAsync("e2e-delcomment@example.com");

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Task with comment", null, Tasks.Api.Domain.TaskPriority.Low, null, null));
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        var cResp = await client.PostAsJsonAsync($"/api/tasks/{task!.Id}/comments",
            new CreateCommentRequest("To be deleted"));
        var comment = await cResp.Content.ReadFromJsonAsync<CommentDto>();

        var dResp = await client.DeleteAsync($"/api/tasks/{task.Id}/comments/{comment!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, dResp.StatusCode);
    }

    [Fact]
    public async Task FilterTasks_ByStatus()
    {
        var (client, project) = await SetupProjectAsync("e2e-filter@example.com");

        await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Todo task", null, Tasks.Api.Domain.TaskPriority.Low, null, null));
        var t2Resp = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Done task", null, Tasks.Api.Domain.TaskPriority.Low, null, null));
        var t2 = await t2Resp.Content.ReadFromJsonAsync<TaskDto>();

        await client.PutAsJsonAsync($"/api/tasks/{t2!.Id}",
            new UpdateTaskRequest("Done task", null, Tasks.Api.Domain.TaskStatus.Done,
                Tasks.Api.Domain.TaskPriority.Low, null, null));

        var r = await client.GetAsync($"/api/tasks?status=Done&projectId={project.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("Done task", body);
    }

    [Fact]
    public async Task Pagination_RespectsPageSize()
    {
        var (client, project) = await SetupProjectAsync("e2e-page@example.com");

        for (int i = 0; i < 5; i++)
            await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
                new CreateTaskRequest($"Task {i}", null, Tasks.Api.Domain.TaskPriority.Medium, null, null));

        var r = await client.GetAsync($"/api/tasks?projectId={project.Id}&pageSize=2&page=1");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("\"totalCount\":5", body);
        Assert.Contains("\"pageSize\":2", body);
    }

    [Fact]
    public async Task GetSingleTask_ReturnsDetails()
    {
        var (client, project) = await SetupProjectAsync("e2e-gettask@example.com");

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Specific task", "With a description", Tasks.Api.Domain.TaskPriority.High, null, null));
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        var r = await client.GetAsync($"/api/tasks/{task!.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var fetched = await r.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal("Specific task", fetched!.Title);
        Assert.Equal("With a description", fetched.Description);
    }

    [Fact]
    public async Task InvalidLogin_Returns401()
    {
        await RegisterAndGetToken("e2e-badlogin@example.com");
        var r = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("e2e-badlogin@example.com", "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task CreateTask_InNonexistentProject_Returns404()
    {
        var token = await RegisterAndGetToken("e2e-noproject@example.com");
        var client = Authed(token);

        var r = await client.PostAsJsonAsync("/api/projects/99999/tasks",
            new CreateTaskRequest("Orphan", null, Tasks.Api.Domain.TaskPriority.Low, null, null));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task SearchTasks_ByTitle()
    {
        var (client, project) = await SetupProjectAsync("e2e-search@example.com");

        await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Deploy to production", null, Tasks.Api.Domain.TaskPriority.High, null, null));
        await client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new CreateTaskRequest("Write unit tests", null, Tasks.Api.Domain.TaskPriority.Medium, null, null));

        var r = await client.GetAsync($"/api/tasks?search=Deploy&projectId={project.Id}");
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("Deploy to production", body);
        Assert.DoesNotContain("Write unit tests", body);
    }
}
