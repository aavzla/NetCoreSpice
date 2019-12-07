using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spice.Models;

namespace Spice.Utility
{
    public static class StaticMethods
    {
        public static string ConvertToRawHtml(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public static double DiscountedPrice(Coupon coupon, double subTotalOrder)
        {
            //if (coupon == null)
            //{
            //    return subTotalOrder;
            //}
            //else
            //{
            //    if (coupon.MinimumAmount > subTotalOrder)
            //    {
            //        return subTotalOrder;
            //    }
            //    else
            //    {
            //        //everything is valid
            //        if (Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Dollar)
            //        {
            //            //$10 off $100
            //            return Math.Round(subTotalOrder - coupon.Discount, 2);
            //        }
            //        if (Convert.ToInt32(coupon.CouponType) == (int)Coupon.ECouponType.Percent)
            //        {
            //            //10% off $100
            //            return Math.Round(subTotalOrder - (subTotalOrder * coupon.Discount / 100), 2);
            //        }
            //    }
            //}
            //return subTotalOrder;


            if (coupon != null)
            {
                if (subTotalOrder >= coupon.MinimumAmount && int.TryParse(coupon.CouponType, out int couponType))
                {
                    if (couponType == (int)Coupon.ECouponType.Dollar)
                    {
                        //$10 off $100
                        return Math.Round(subTotalOrder - coupon.Discount, 2);
                    }

                    if (couponType == (int)Coupon.ECouponType.Percent)
                    {
                        //10% off $100
                        return Math.Round(subTotalOrder - (subTotalOrder * coupon.Discount / 100), 2);
                    }
                }
            }

            return subTotalOrder;
        }
    }
}
