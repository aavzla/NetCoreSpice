using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models
{
    public class OrderDetails
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int OrderInfoId { get; set; }
        [Required]
        public int MenuItemId { get; set; }

        //MenuItem Details
        public int Count { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public double Price { get; set; }

        //ForeignKey
        [ForeignKey(nameof(OrderInfoId))]
        public virtual OrderInfo OrderInfo { get; set; }
        [ForeignKey(nameof(MenuItemId))]
        public virtual MenuItem MenuItem { get; set; }
    }
}
