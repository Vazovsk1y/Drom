using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class NewsItem
{
    public required Guid Id { get; init; }
    
    public required DateTimeOffset PublicationDateTime { get; init; }
    
    public required string Title { get; set; }
    
    public required string Content { get; set; }
    
    public required byte[] CoverImage { get; set; }
}

public class NewsItemConfiguration : IEntityTypeConfiguration<NewsItem>
{
    public void Configure(EntityTypeBuilder<NewsItem> builder)
    {
        //
    }
}