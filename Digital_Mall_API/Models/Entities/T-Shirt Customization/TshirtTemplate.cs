namespace Digital_Mall_API.Models.Entities.T_Shirt_Customization
{
    public class TshirtTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string? SizeChartUrl { get; set; }
        public string? FrontImageUrl { get; set; }
        public string? BackImageUrl { get; set; }
        public string? LeftImageUrl { get; set; }
        public string? RightImageUrl { get; set; }
    }
}