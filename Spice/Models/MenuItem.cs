using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Spicyness { get; set; }
        public enum ESpicy
        {
            NA,
            NotSpicy,
            Spicy,
            VerySpicy
        }
        public string ImagePath { get; set; }
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [Display(Name = "SubCategory")]
        public int SubCategoryId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Price should be greater than ${1}")]
        public double Price { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }
        [ForeignKey(nameof(SubCategoryId))]
        public virtual SubCategory SubCategory { get; set; }
    }
}
