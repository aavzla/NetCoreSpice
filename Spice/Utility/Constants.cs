using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public static class Constants
    {
        public const string DefaultFoodImage = "default_food.png";

        //Roles of Users
        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerUser = "Customer";

        //Session Shopping Cart Counts Key
        public const string sessionShoppingCartCounts = "sscc";
        //Session Coupon Code Key
        public const string sessionCouponCode = "scc";
    }
}
