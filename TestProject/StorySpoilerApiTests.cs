using StorySpoiler.Models;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;


namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerApiTests
    {
        private RestClient client;
        private static string? createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("yana123", "12344321");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStory_WithRequiredFields_ShouldReturnIdAndMsg() 
        {
            var storyBody = new StoryDTO
            {
                Title = "First Story Added",
                Description = "First Description Added"
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyBody);
            var response = client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse, Has.Property("StoryId"));
            Assert.That(createResponse.StoryId, Is.Not.Null);
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

            createdStoryId = createResponse.StoryId;
        }

        [Test, Order(2)]
        public void EditCreatedStory_ShouldReturnOkAndSuccessMsg()
        {
            var storyBodyUpdate = new StoryDTO
            {
                Title = "First Story Added",
                Description = "First Description Added"
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(storyBodyUpdate);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse?.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnOkAndNonEmptyArray()
        {
            var request = new RestRequest("/api/Story/All");
            var response = client.Execute(request);
            var stories = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(stories, Is.Not.Null.And.Not.Empty);

        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOkAndSuccessMsg()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }   

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyBody = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyBody);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFoundAndMsg()
        {
            var storyBodyUpdate = new StoryDTO
            {
                Title = "First Story Added",
                Description = "First Description Added"
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(storyBodyUpdate);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));

        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequestAndMsg()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));

        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client.Dispose();
        }
    }
}