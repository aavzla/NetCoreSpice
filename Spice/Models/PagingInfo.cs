using System;

namespace Spice.Models
{
    public class PagingInfo
    {
        public int TotalItem { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int totalPage => (int)Math.Ceiling((decimal)TotalItem / ItemsPerPage);
        /// <summary>
        /// Get the Store URL that will have the actual page for tracking purposes.
        /// </summary>
        public string urlParam { get; set; }
    }
}
