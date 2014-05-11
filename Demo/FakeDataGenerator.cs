using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Demo.Models;
using FizzWare.NBuilder;
using FizzWare.NBuilder.Generators;

namespace Demo
{
    class FakeDataGenerator
    {
        // Used to maintain consistency between product IDs and their display "name"
        static readonly ConcurrentDictionary<int, string> ProductsCatalogue = new ConcurrentDictionary<int, string>();

        public static ShoppingCart CreateAFakeShoppingCart()
        {
            var fakeCart = Builder<ShoppingCart>
                .CreateNew()
                .With(x => x.CreatedAt = DateTimeOffset.UtcNow)
                .And(x => x.IpAddress = GetRandom.IpAddress()) // TODO map IpAddress to a geo-location
                .Build();

            return fakeCart;
        }

        public static Order CreateAFakeOrder()
        {
            var fakeOrder = Builder<Order>
                .CreateNew()
                .With(x => x.CustomerName = string.Concat(GetRandom.FirstName(), " ", GetRandom.LastName()))
                .With(x => x.CreatedAt = DateTimeOffset.UtcNow)
                .And(x => x.IpAddress = GetRandom.IpAddress()) // TODO map IpAddress to a geo-location
                .Build();

            fakeOrder.OrderLines = new List<OrderLine>();
            for (var i = 0; i < GetRandom.Int(1, 20); i++)
            {
                var productId = GetRandom.Int(1, 30); // Demo for 30 products so we get nicer reporting screens
                fakeOrder.OrderLines.Add(new OrderLine
                {
                    ProductId = "products/" + productId,
                    ProductName = ProductsCatalogue.GetOrAdd(productId, _ => GetRandom.Phrase(15)),
                    Quantity = GetRandom.Int(1, 5),
                    UnitPrice = GetRandom.Int(5, 500) + (GetRandom.Int(0, 99) / 100), // prettier prices
                });
            }

            return fakeOrder;
        }
    }
}
