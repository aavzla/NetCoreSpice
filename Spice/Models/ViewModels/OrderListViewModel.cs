using System.Collections.Generic;

namespace Spice.Models.ViewModels
{
    public class OrderListViewModel
    {
        public IList<OrderDetailsViewModel> OrderDetailsViewModels { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
