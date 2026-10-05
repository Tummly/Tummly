using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TummlyBackend.Billing;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Controllers
{
    [ApiController]
    [Route("api/shop/locations")]
    [Authorize]
    public class ShopLocationsController : ControllerBase
    {
        private const int OfferCardPreviewHeadlineMax = 60;

        private readonly IShopLocationRecommendationsService _recommendations;
        private readonly IShopOfferCardOfferService _offerCardOffer;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly PrintTemplatePack _printTemplatePack;
        private readonly IQrCodeRasterizer _qrRasterizer;

        public ShopLocationsController(
            IShopLocationRecommendationsService recommendations,
            IShopOfferCardOfferService offerCardOffer,
            IRestaurantPermissionHelper permissions,
            PrintTemplatePack printTemplatePack,
            IQrCodeRasterizer qrRasterizer
        )
        {
            _recommendations = recommendations;
            _offerCardOffer = offerCardOffer;
            _permissions = permissions;
            _printTemplatePack = printTemplatePack;
            _qrRasterizer = qrRasterizer;
        }

        [HttpPut("{locationId:int}/details")]
        public async Task<IActionResult> SaveDetails(
            int locationId,
            [FromBody] SaveShopLocationDetailsRequest body,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var saved = await _recommendations.SaveDetailsAsync(
                locationId,
                body,
                cancellationToken
            );
            if (saved == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Location was not found.",
                });
            }

            return Ok(new
            {
                success = true,
                locationId,
                basedOn = saved,
            });
        }

        [HttpGet("{locationId:int}/recommendations")]
        public async Task<IActionResult> GetRecommendations(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var payload = await _recommendations.GetRecommendationsAsync(
                locationId,
                cancellationToken
            );
            return Ok(payload);
        }

        [HttpGet("{locationId:int}/offer-card-offer")]
        public async Task<IActionResult> GetOfferCardOffer(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var dto = await _offerCardOffer.GetAsync(
                locationId,
                cancellationToken
            );
            return Ok(new
            {
                success = true,
                offerCardOfferId = dto.OfferCardOfferId,
                offerCardOfferTitle = dto.OfferCardOfferTitle,
                offerCardOfferLive = dto.OfferCardOfferLive,
            });
        }

        [HttpPut("{locationId:int}/offer-card-offer")]
        public async Task<IActionResult> PutOfferCardOffer(
            int locationId,
            [FromBody] SetShopOfferCardOfferRequest body,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.Scoped
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            var result = await _offerCardOffer.SetAsync(
                locationId,
                body.OfferId,
                cancellationToken
            );

            return result switch
            {
                ShopOfferCardOfferSetResult.Ok ok => Ok(new
                {
                    success = true,
                    offerCardOfferId = ok.Value.OfferCardOfferId,
                    offerCardOfferTitle = ok.Value.OfferCardOfferTitle,
                    offerCardOfferLive = ok.Value.OfferCardOfferLive,
                }),
                ShopOfferCardOfferSetResult.LocationNotFound => NotFound(new
                {
                    success = false,
                    message = "Location not found.",
                }),
                ShopOfferCardOfferSetResult.InvalidOffer invalid =>
                    BadRequest(new
                    {
                        success = false,
                        message = invalid.Message,
                    }),
                ShopOfferCardOfferSetResult.CapReached cap => Conflict(new
                {
                    success = false,
                    code = ActiveOfferCapGate.CapReachedCode,
                    cap = cap.Cap,
                    current = cap.Current,
                }),
                ShopOfferCardOfferSetResult.FailClosed => Conflict(new
                {
                    success = false,
                }),
                _ => StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected Offer Card attach result.",
                }),
            };
        }

        /// <summary>
        /// Live Offer Card preview painted with the same template + headline
        /// slot as mint PDF generation. Returns PNG for Shop modal display.
        /// </summary>
        [HttpGet("{locationId:int}/offer-card-preview")]
        public async Task<IActionResult> GetOfferCardPreview(
            int locationId,
            [FromQuery] string? headline,
            CancellationToken cancellationToken
        )
        {
            var gate = await AuthorizeLocationAsync(
                locationId,
                PermissionLevel.View
            );
            if (gate.Denied != null)
            {
                return gate.Denied;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = string.IsNullOrWhiteSpace(headline)
                ? _printTemplatePack.Snapshot.DefaultOfferHeadline
                : headline.Trim();
            if (trimmed.Length > OfferCardPreviewHeadlineMax)
            {
                trimmed = trimmed[..OfferCardPreviewHeadlineMax];
            }

            try
            {
                var qr = _qrRasterizer.Render(
                    PrintReadyQrPdfComposer.PreviewQrPayload
                );
                var png = PrintReadyQrPdfComposer.ComposePng(
                    _printTemplatePack.Snapshot,
                    QrType.OfferCard,
                    qr,
                    trimmed
                );

                Response.Headers.CacheControl = "no-store";
                // No download filename — browsers/axios treat this as inline image.
                return File(png, "image/png");
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Could not render the Offer Card preview.",
                });
            }
        }

        private async Task<(IActionResult? Denied, int RestaurantId)> AuthorizeLocationAsync(
            int locationId,
            PermissionLevel minimum
        )
        {
            var unauthorized = OperatorAuth.TryRequireUserId(User, out _);
            if (unauthorized != null)
            {
                return (unauthorized, 0);
            }

            if (locationId <= 0)
            {
                return (
                    BadRequest(new
                    {
                        success = false,
                        message = "locationId is required.",
                    }),
                    0
                );
            }

            var decision = await _permissions.AuthorizeLocationAsync(
                User,
                OperatorAreaIds.TummlyShop,
                minimum,
                locationId
            );
            var denied = decision.ToHttpResult();
            if (denied != null)
            {
                return (denied, 0);
            }

            return (null, decision.RestaurantId);
        }
    }
}
