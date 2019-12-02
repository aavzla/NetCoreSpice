using System.Collections.Generic;

namespace Spice.Models.ViewModels
{
    public class SubCategoriesCreateViewModel
    {
        public IEnumerable<Category> Categories { get; set; }
        public SubCategory SubCategory { get; set; }
        public List<string> SubCategoryNameList { get; set; }
        public string StatusMessage { get; set; }
    }
}
