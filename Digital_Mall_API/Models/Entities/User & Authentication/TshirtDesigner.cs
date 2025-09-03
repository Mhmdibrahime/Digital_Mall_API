namespace Digital_Mall_API.Models.Entities.User___Authentication
{
    public class TshirtDesigner
    {
        public Guid Id { get; set; }

        
        public virtual ApplicationUser? User { get; set; }
    }
}