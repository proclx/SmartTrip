using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrip.Data;
using SmartTrip.Models;
using SmartTrip.UI.Controllers;
using Xunit;

namespace SmartTrip.Tests.Controllers
{
    public class DreamPlaceControllerTests
    {
        [Fact]
        public async Task PlanTrip_UsesDreamPlaceDetailsForNotes()
        {
            var options = new DbContextOptionsBuilder<SmartTripDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new SmartTripDbContext(options);
            var userId = "user1";

            var dreamPlace = new DreamPlace
            {
                Name = "San Francisco",
                Description = "«робити фото на фон≥ Golden Gate Bridge!",
                LocationInfo = "Golden Gate Bridge",
                UserId = userId
            };

            context.DreamPlaces.Add(dreamPlace);
            await context.SaveChangesAsync();

            var controller = new DreamPlaceController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }))
                    }
                }
            };

            var result = await controller.PlanTrip(dreamPlace.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Create", redirectResult.ActionName);
            Assert.Equal("Trip", redirectResult.ControllerName);

            Assert.NotNull(redirectResult.RouteValues);
            Assert.Equal(dreamPlace.Name, redirectResult.RouteValues["destinationName"]);

            var expectedNotes = string.Join(Environment.NewLine,
                $"ќпис: {dreamPlace.Description}",
                $"Ћокац≥€: {dreamPlace.LocationInfo}");

            Assert.Equal(expectedNotes, redirectResult.RouteValues["notes"]);
        }
    }
}
