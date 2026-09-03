using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopOrderNumberAllocator : IShopOrderNumberAllocator
    {
        private readonly ApplicationDbContext _context;

        public ShopOrderNumberAllocator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> AllocateNextOrderNumberAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var sequence = await _context.ShopOrderSequences
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (sequence == null)
            {
                sequence = new ShopOrderSequence
                {
                    RestaurantId = restaurantId,
                    NextNumber = 1,
                };
                _context.ShopOrderSequences.Add(sequence);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var allocated = sequence.NextNumber;
            sequence.NextNumber = checked(allocated + 1);
            await _context.SaveChangesAsync(cancellationToken);

            return string.Format(
                CultureInfo.InvariantCulture,
                "ORD-{0}",
                allocated
            );
        }
    }
}
