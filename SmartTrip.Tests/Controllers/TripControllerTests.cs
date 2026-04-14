using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartTrip.Application.Interfaces;
using SmartTrip.Models;
using SmartTrip.UI.Controllers;
using SmartTrip.UI.ViewModels;
using Xunit;

namespace SmartTrip.Tests.Controllers
{
    public class TripControllerTests
    {
        private readonly Mock<ITripService> _tripServiceMock;
        private readonly Mock<IGalleryService> _galleryServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IPackingService> _packingServiceMock;
        private readonly Mock<IEventDiscoveryService> _eventDiscoveryServiceMock;
        private readonly TripController _controller;

        public TripControllerTests()
        {
            _tripServiceMock = new Mock<ITripService>();
            _galleryServiceMock = new Mock<IGalleryService>();
            _packingServiceMock = new Mock<IPackingService>();
            _eventDiscoveryServiceMock = new Mock<IEventDiscoveryService>();

            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));

            _controller = new TripController(
                _tripServiceMock.Object, 
                _galleryServiceMock.Object, 
                _userManagerMock.Object, 
                _packingServiceMock.Object, 
                _eventDiscoveryServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };

            _userManagerMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");
        }

        [Fact]
        public async Task Details_ValidId_ReturnsViewWithEvents()
        {
            // Arrange
            var tripId = 1;
            var mockTrip = new Trip 
            { 
                Id = tripId, 
                CityId = 1, 
                City = new City { Name = "Paris" },
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2)
            };

            var mockEvents = new List<LocalEvent>
            {
                new LocalEvent { Title = "Art Expo" }
            };

            _tripServiceMock.Setup(s => s.GetTripByIdAsync(tripId, "user1")).ReturnsAsync(mockTrip);
            _eventDiscoveryServiceMock
                .Setup(s => s.GetEventsAsync(1, "Paris", mockTrip.StartDate, mockTrip.EndDate))
                .ReturnsAsync(mockEvents);

            // Act
            var result = await _controller.Details(tripId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<TripViewModel>(viewResult.ViewData.Model);
            Assert.Equal("Paris", model.City);
            Assert.Single(model.SuggestedEvents);
        }

        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _tripServiceMock.Setup(s => s.GetTripByIdAsync(99, "user1")).ReturnsAsync((Trip)null);

            // Act
            var result = await _controller.Details(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}