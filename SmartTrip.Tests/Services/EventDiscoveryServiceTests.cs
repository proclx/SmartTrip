using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using SmartTrip.Application.Services;
using Xunit;

namespace SmartTrip.Tests.Services
{
    public class EventDiscoveryServiceTests
    {
        private readonly Mock<IConfiguration> _configMock;

        public EventDiscoveryServiceTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["Ticketmaster:ApiKey"]).Returns("fake-api-key");
        }

        [Fact]
        public async Task GetEventsAsync_WithValidResponse_ReturnsEvents()
        {
            // Arrange
            var jsonResponse = @"
            {
                ""_embedded"": {
                    ""events"": [
                        {
                            ""name"": ""Test Concert"",
                            ""url"": ""https://test.com"",
                            ""dates"": { ""start"": { ""localDate"": ""2026-05-10"" } },
                            ""classifications"": [{ ""genre"": { ""name"": ""Music"" } }],
                            ""images"": [{ ""url"": ""img.png"" }]
                        }
                    ]
                }
            }";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://app.ticketmaster.com/")
            };

            var service = new EventDiscoveryService(httpClient, _configMock.Object);

            // Act
            var events = await service.GetEventsAsync(1, "London", DateTime.UtcNow, DateTime.UtcNow.AddDays(5));

            // Assert
            Assert.NotNull(events);
            Assert.Single(events);
            Assert.Equal("Test Concert", events.First().Title);
            Assert.Equal("Music", events.First().Description);
        }

        [Fact]
        public async Task GetEventsAsync_ApiError_ReturnsEmptyList()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Forbidden
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://app.ticketmaster.com/")
            };

            var service = new EventDiscoveryService(httpClient, _configMock.Object);

            // Act
            var events = await service.GetEventsAsync(1, "London", DateTime.UtcNow, DateTime.UtcNow.AddDays(5));

            // Assert
            Assert.NotNull(events);
            Assert.Empty(events);
        }
    }
}