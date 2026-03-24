using DeliverTableServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliverTableServer.Data.ModelConfiguration;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasKey(lt => lt.Id);

        builder.HasOne(lt => lt.LoyaltyAccount)
            .WithMany(la => la.Transactions)
            .HasForeignKey(lt => lt.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lt => lt.Order)
            .WithMany()
            .HasForeignKey(lt => lt.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(lt => lt.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(lt => lt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.HasIndex(lt => lt.LoyaltyAccountId);
    }
}
