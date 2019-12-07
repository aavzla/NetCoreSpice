using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models
{
    public class OrderInfo
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ApplicationUserId { get; set; }
        public string Status { get; set; }

        //Order Pick-up DateTimes, Name and Phone Number
        [Required]
        public DateTime OrderDate { get; set; }
        [Required, Display(Name = "Pick-up Time")]
        public DateTime PickUpTime { get; set; }
        [Required, NotMapped]
        public DateTime PickUpDate { get; set; }
        [Display(Name = "Pick-up Name")]
        public string PickUpName { get; set; }
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        //Order Amount and Discounts
        [Required]
        public double OrderSubTotal { get; set; }
        [Display(Name = "Coupon Code")]
        public string CouponCode { get; set; }
        public double CouponCodeDiscount { get; set; }

        [Required, Display(Name = "Order Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public double OrderTotal { get; set; }

        //Payment
        public string PaymentStatus { get; set; }
        public string TransactionId { get; set; }

        //Order Additional Info
        public string Comments { get; set; }

        //Foreign Keys
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
