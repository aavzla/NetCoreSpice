using System.Collections.Generic;

namespace Spice.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public OrderInfo OrderInfo { get; set; }
        public List<OrderDetails> OrderDetails { get; set; }
    }
}
