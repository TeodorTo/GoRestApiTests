using FluentAssertions;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;

namespace GoRestApiTests
{
    public class GoRestApiTests
    {
        private HttpClient _httpClient;
        private const string BaseUrl = "https://gorest.co.in/public/v2";
        private string _apiToken;

        [SetUp]
        public void Setup()
        {
            _httpClient = new HttpClient();
            _apiToken = Environment.GetEnvironmentVariable("GOREST_API_TOKEN");
            if (string.IsNullOrEmpty(_apiToken))
            {
                Assert.Inconclusive("GOREST_API_TOKEN environment variable is not set.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiToken);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
        }

        [Test]
        public async Task GetAllUsers_ShouldReturnSuccessAndUsersList()
        {
            // Arrange
            var requestUrl = $"{BaseUrl}/users";

            // Act
            var response = await _httpClient.GetAsync(requestUrl);
            var users = await response.Content.ReadFromJsonAsync<User[]>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            users.Should().NotBeNull();
            users.Should().NotBeEmpty();
            users.Should().AllSatisfy(u => u.Id.Should().BeGreaterThan(0));
        }

        [Test]
        public async Task GetSpecificUser_WhenUserExists_ShouldReturnUser()
        {
            // Arrange
            var user = await CreateTestUser();
            var requestUrl = $"{BaseUrl}/users/{user.Id}";

            try
            {
                // Act
                var response = await _httpClient.GetAsync(requestUrl);
                var retrievedUser = await response.Content.ReadFromJsonAsync<User>();

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                retrievedUser.Should().NotBeNull();
                retrievedUser.Id.Should().Be(user.Id);
                retrievedUser.Name.Should().Be(user.Name);
                retrievedUser.Email.Should().Be(user.Email);
            }
            finally
            {
                // Cleanup
                await DeleteTestUser(user.Id);
            }
        }

        [Test]
        public async Task CreateUser_ShouldReturnCreatedUser()
        {
            // Arrange
            var newUser = GenerateRandomUser();
            var requestUrl = $"{BaseUrl}/users";

            try
            {
                // Act
                var response = await _httpClient.PostAsJsonAsync(requestUrl, newUser);
                var createdUser = await response.Content.ReadFromJsonAsync<User>();

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Created);
                createdUser.Should().NotBeNull();
                createdUser.Id.Should().BeGreaterThan(0);
                createdUser.Name.Should().Be(newUser.Name);
                createdUser.Email.Should().Be(newUser.Email);
                createdUser.Gender.Should().Be(newUser.Gender);
                createdUser.Status.Should().Be(newUser.Status);
            }
            finally
            {
                // Cleanup: Delete the created user by fetching it first
                var usersResponse = await _httpClient.GetAsync(requestUrl);
                var users = await usersResponse.Content.ReadFromJsonAsync<User[]>();
                var createdUser = users.FirstOrDefault(u => u.Email == newUser.Email);
                if (createdUser != null)
                {
                    await DeleteTestUser(createdUser.Id);
                }
            }
        }

        [Test]
        public async Task UpdateUser_WithPut_ShouldReturnUpdatedUser()
        {
            // Arrange
            var user = await CreateTestUser();
            var updatedUser = new User
            {
                Name = $"Updated {Guid.NewGuid().ToString()[..8]}",
                Email = $"updated{Guid.NewGuid().ToString()[..8]}@test.com",
                Gender = user.Gender,
                Status = user.Status
            };
            var requestUrl = $"{BaseUrl}/users/{user.Id}";

            try
            {
                // Act
                var response = await _httpClient.PutAsJsonAsync(requestUrl, updatedUser);
                var returnedUser = await response.Content.ReadFromJsonAsync<User>();

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                returnedUser.Should().NotBeNull();
                returnedUser.Id.Should().Be(user.Id);
                returnedUser.Name.Should().Be(updatedUser.Name);
                returnedUser.Email.Should().Be(updatedUser.Email);
            }
            finally
            {
                // Cleanup
                await DeleteTestUser(user.Id);
            }
        }

        [Test]
        public async Task DeleteUser_ShouldReturnNoContent()
        {
            // Arrange
            var user = await CreateTestUser();
            var requestUrl = $"{BaseUrl}/users/{user.Id}";

            // Act
            var response = await _httpClient.DeleteAsync(requestUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the user is deleted
            var getResponse = await _httpClient.GetAsync(requestUrl);
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private async Task<User> CreateTestUser()
        {
            var newUser = GenerateRandomUser();
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/users", newUser);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await response.Content.ReadFromJsonAsync<User>();
        }

        private async Task DeleteTestUser(int userId)
        {
            await _httpClient.DeleteAsync($"{BaseUrl}/users/{userId}");
        }

        private User GenerateRandomUser()
        {
            var uniqueId = Guid.NewGuid().ToString()[..8];
            return new User
            {
                Name = $"TestUser_{uniqueId}",
                Email = $"test_{uniqueId}@test.com",
                Gender = "male",
                Status = "active"
            };
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; }
    }
}