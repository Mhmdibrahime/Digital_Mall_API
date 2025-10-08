using Digital_Mall_API.Models.Entities.User___Authentication;

namespace Digital_Mall_API.Models.Entities.Product_Catalog
{
    public class Favorite
    {
        public int Id { get; set; }

      
        public Guid UserId { get; set; }
        public int ProductId { get; set; }

        
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
}