using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tasks.Api.Features.Projects;
using Tasks.Api.Features.Tasks;
using Tasks.Api.Features.Users;
using Xunit;

namespace Tasks.Api.Tests;

public class EndToEndTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private HttpClient Client => factory.CreateClient();

    private async Task<string> RegisterAndGetToken(string email, string password = "s3cret-pass!")
    {
        var r = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
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
        var token = await RegisterAndGetToken("alice@example.com");
        var client = Authed(token);

        var pResp = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Launch"));
        Assert.Equal(HttpStatusCode.Created, pResp.StatusCode);
        var project = await pResp.Content.ReadFromJsonAsync<ProjectDto>();

        var tResp = await client.PostAsJsonAsync($"/api/projects/{project!.Id}/tasks",
            new CreateTaskRequest("Write launch plan", null, Tasks.Api.Domain.TaskPriority.High, null));
        Assert.Equal(HttpStatusCode.Created, tResp.StatusCode);
        var task = await tResp.Content.ReadFromJsonAsync<TaskDto>();

        var uResp = await client.PutAsJsonAsync($"/api/tasks/{task!.Id}",
            new UpdateTaskRequest("Write launch plan", "v1", Tasks.Api.Domain.TaskStatus.InProgress,
                Tasks.Api.Domain.TaskPriority.High, DateTime.UtcNow.AddDays(3)));
        uResp.EnsureSuccessStatusCode();
        var updated = await uResp.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal(Tasks.Api.Domain.TaskStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task UserCannotSeeAnotherUsersProjects()
    {
        var aToken = await RegisterAndGetToken("a@example.com");
        var bToken = await RegisterAndGetToken("b@example.com");

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
        await RegisterAndGetToken("dup@example.com");
        var r = await Client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@example.com", "s3cret-pass!"));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }
}
