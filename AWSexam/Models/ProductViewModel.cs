namespace AWSexam.Models
{
    public record ProductCardViewModel(Product product, string imageUrl);
    public class ProductListVm
    {
        public List<ProductCardViewModel> Items { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string SelectedCategory { get; set; } = string.Empty;
    }
}
