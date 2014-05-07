using System;
using System.Collections.Generic;
using Demo.Models;
using FizzWare.NBuilder;
using FizzWare.NBuilder.Generators;

namespace Demo
{
    class FakeDataGenerator
    {
        public static Order CreateAFakeOrder()
        {
            var fakeOrder = Builder<Order>
                .CreateNew()
                .With(x => x.CustomerName = string.Concat(GetRandom.FirstName(), " ", GetRandom.LastName()))
                .With(x => x.CreatedAt = DateTimeOffset.UtcNow)
                .And(x => x.IpAddress = GetRandom.IpAddress())
                .Build();

            fakeOrder.OrderLines = new List<OrderLine>();
            for (var i = 0; i < GetRandom.Int(1, 20); i++)
            {
                fakeOrder.OrderLines.Add(new OrderLine
                {
                    ProductId = "products/" + GetRandom.Int(1, 65000),
                    ProductName = GetRandom.Phrase(15),
                    Quantity = GetRandom.Int(1, 5),
                    UnitPrice = GetRandom.PositiveDouble() * GetRandom.Int(100, 10000),
                });
            }

            return fakeOrder;
        }
    }
}
