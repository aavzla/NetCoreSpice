using System.Collections.Generic;

namespace Spice.Models.ViewModels
{
    public class OrderShoppingCartsViewModel
    {
        public OrderInfo OrderInfo { get; set; }
        public List<ShoppingCart> ShoppingCarts { get; set; }
    }
}
