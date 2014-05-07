using System;
using System.Collections.Generic;

namespace Demo.Models
{
    public class Order
    {
        public string CustomerName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<OrderLine> OrderLines { get; set; }
        public string IpAddress { get; set; }
    }

    public class OrderLine
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }    
}
