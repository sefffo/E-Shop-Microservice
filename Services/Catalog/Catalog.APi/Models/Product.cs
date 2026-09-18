namespace Catalog.APi.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default !;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    // gonna change for the relation with category
    public List<string> Category { get; set; } = new List<string>();
    public string ImageFile { set; get; } = default!;
}