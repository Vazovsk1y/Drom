using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class NewsItemComment
{
    public required Guid Id { get; init; }
    
    public required Guid UserId { get; init; }
    
    public required Guid NewsItemId { get; init; }
    
    public required DateTimeOffset PublicationDateTime { get; init; }
    
    public required string Content { get; init; }
    
    public User User { get; init; } = null!;
    
    public NewsItem NewsItem { get; init; } = null!;
}

public class NewsItemCommentConfiguration : IEntityTypeConfiguration<NewsItemComment>
{
    public void Configure(EntityTypeBuilder<NewsItemComment> builder)
    {
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
        
        builder.HasOne(e => e.NewsItem)
            .WithMany()
            .HasForeignKey(e => e.NewsItemId);
    }
}