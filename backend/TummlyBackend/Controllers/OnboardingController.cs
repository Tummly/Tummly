using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Onboarding;
using TummlyBackend.Models;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/onboarding")]
    [Authorize]
    public class OnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OnboardingController(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        /*
         =========================================
         SINGLE SETUP
         =========================================
        */

        [HttpPost("single-setup")]
        public async Task<IActionResult> SingleSetup(
            [FromBody] CreateRestaurantSetupDto dto
        )
        {
            try
            {
                /*
                 =========================================
                 VALIDATIONS
                 =========================================
                */

                if (string.IsNullOrWhiteSpace(dto.RestaurantName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Restaurant name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.LocationName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Location name is required."
                    });
                }

                /*
                 =========================================
                 GET USER ID
                 =========================================
                */

                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not authenticated."
                    });
                }

                var userId =
                    int.Parse(userIdClaim);

                /*
                 =========================================
                 CHECK EXISTING RESTAURANT
                 =========================================
                */

                var existingRestaurant =
                    await _context.Restaurants
                        .FirstOrDefaultAsync(x =>
                            x.OwnerUserId == userId
                        );

                if (existingRestaurant != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Restaurant already exists."
                    });
                }

                /*
                 =========================================
                 CREATE RESTAURANT
                 =========================================
                */

                var restaurant =
                    new Restaurant
                    {
                        Name =
                            dto.RestaurantName,

                        AccountType =
                            "Single",

                        OwnerUserId =
                            userId,

                        BusinessCategory =
                            dto.BusinessCategory,

                        BusinessLink =
                            dto.BusinessLink,

                        PublicPhoneNumber =
                            dto.Phone
                    };

                await _context.Restaurants
                    .AddAsync(restaurant);

                await _context.SaveChangesAsync();

                /*
                 =========================================
                 CREATE LOCATION
                 =========================================
                */

                var location =
                    new RestaurantLocation
                    {
                        LocationName =
                            dto.LocationName,

                        Address =
                            dto.Address,

                        Postcode =
                            dto.Postcode,

                        RestaurantId =
                            restaurant.Id
                    };

                await _context.RestaurantLocations
                    .AddAsync(location);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Single setup completed.",

                    restaurantId =
                        restaurant.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /*
         =========================================
         MULTI SETUP
         =========================================
        */

        [HttpPost("multi-setup")]
        public async Task<IActionResult> MultiSetup(
            [FromBody] CreateRestaurantSetupDto dto
        )
        {
            try
            {
                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not authenticated."
                    });
                }

                var userId =
                    int.Parse(userIdClaim);

                var existingRestaurant =
                    await _context.Restaurants
                        .FirstOrDefaultAsync(x =>
                            x.OwnerUserId == userId
                        );

                if (existingRestaurant != null)
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            "Restaurant already exists."
                    });
                }

                /*
                 =========================================
                 CREATE RESTAURANT
                 =========================================
                */

                var restaurant =
                    new Restaurant
                    {
                        Name =
                            dto.RestaurantName,

                        AccountType =
                            "Multi",

                        OwnerUserId =
                            userId,

                        BusinessCategory =
                            dto.BusinessCategory,

                        BusinessLink =
                            dto.BusinessLink,

                        PublicPhoneNumber =
                            dto.Phone
                    };

                await _context.Restaurants
                    .AddAsync(restaurant);

                await _context.SaveChangesAsync();

                /*
                 =========================================
                 CREATE LOCATION
                 =========================================
                */

                var location =
                    new RestaurantLocation
                    {
                        LocationName =
                            dto.LocationName,

                        Address =
                            dto.Address,

                        Postcode =
                            dto.Postcode,

                        RestaurantId =
                            restaurant.Id
                    };

                await _context.RestaurantLocations
                    .AddAsync(location);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Multi setup completed.",

                    restaurantId =
                        restaurant.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /*
         =========================================
         GUEST LOOP SETUP
         =========================================
        */

        [HttpPost("guest-loop")]
        public async Task<IActionResult> GuestLoopSetup(
            [FromBody] CreateGuestLoopDto dto
        )
        {
            try
            {
                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not authenticated."
                    });
                }

                var userId =
                    int.Parse(userIdClaim);

                var restaurant =
                    await _context.Restaurants
                        .FirstOrDefaultAsync(x =>
                            x.OwnerUserId == userId
                        );

                if (restaurant == null)
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            "Restaurant not found."
                    });
                }

                var existingGuestLoop =
                    await _context.GuestLoopSetups
                        .FirstOrDefaultAsync(x =>
                            x.RestaurantId ==
                            restaurant.Id
                        );

                if (existingGuestLoop != null)
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            "Guest loop already setup."
                    });
                }

                var guestLoop =
                    new GuestLoopSetup
                    {
                        RestaurantId =
                            restaurant.Id,

                        SendPhysicalQrMaterials =
                            dto.SendPhysicalQrMaterials,

                        AutoSendReviewRequests =
                            dto.AutoSendReviewRequests,

                        Touchpoints =
                            dto.Touchpoints,

                        FeedbackTags =
                            dto.FeedbackTags,

                        ThankYouMessage =
                            dto.ThankYouMessage,

                        OfferHeadline =
                            dto.OfferHeadline,

                        OfferDetails =
                            dto.OfferDetails,

                        OfferExpiry =
                            dto.OfferExpiry,

                        OfferRedemption =
                            dto.OfferRedemption,

                        OfferUsageLimit =
                            dto.OfferUsageLimit
                    };

                await _context.GuestLoopSetups
                    .AddAsync(guestLoop);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Guest loop setup completed."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /*
         =========================================
         GET STATUS
         =========================================
        */

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var userIdClaim =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        success = false
                    });
                }

                var userId =
                    int.Parse(userIdClaim);

                var restaurant =
                    await _context.Restaurants
                        .Include(x => x.GuestLoopSetup)
                        .FirstOrDefaultAsync(x =>
                            x.OwnerUserId == userId
                        );

                if (restaurant == null)
                {
                    return Ok(new
                    {
                        completed = false
                    });
                }

                return Ok(new
                {
                    completed = true,

                    restaurantName =
                        restaurant.Name,

                    accountType =
                        restaurant.AccountType
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}