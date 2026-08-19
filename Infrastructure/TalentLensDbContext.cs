using C_TalentLens.Domain;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure;

public sealed class TalentLensDbContext(DbContextOptions<TalentLensDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Requisition> Requisitions => Set<Requisition>();

    public DbSet<SourceActivity> SourceActivities => Set<SourceActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(user => user.Department)
                .HasMaxLength(120);

            entity.Property(user => user.ReportingLine)
                .HasMaxLength(120);
        });

        modelBuilder.Entity<Requisition>(entity =>
        {
            entity.HasKey(requisition => requisition.Id);

            entity.Property(requisition => requisition.RequisitionCode)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(requisition => requisition.RequisitionCode)
                .IsUnique();

            entity.Property(requisition => requisition.RoleName)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(requisition => requisition.Department)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(requisition => requisition.HiringManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(requisition => requisition.HiringManager)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(requisition => requisition.RecruiterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(requisition => requisition.Recruiter)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(requisition => requisition.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(requisition => requisition.CurrentStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Navigation(requisition => requisition.StageHistory)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Navigation(requisition => requisition.Bottlenecks)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Navigation(requisition => requisition.ActionItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(requisition => requisition.StageHistory)
                .WithOne()
                .HasForeignKey(stage => stage.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(requisition => requisition.Bottlenecks)
                .WithOne()
                .HasForeignKey(bottleneck => bottleneck.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(requisition => requisition.ActionItems)
                .WithOne()
                .HasForeignKey(action => action.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StageTransition>(entity =>
        {
            entity.HasKey(stage => stage.Id);

            entity.Property(stage => stage.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        });

        modelBuilder.Entity<Bottleneck>(entity =>
        {
            entity.HasKey(bottleneck => bottleneck.Id);

            entity.Property(bottleneck => bottleneck.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Ignore(bottleneck => bottleneck.Reason);

            entity.Property(bottleneck => bottleneck.Category)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(bottleneck => bottleneck.CustomCategory)
                .HasMaxLength(120);

            entity.Property(bottleneck => bottleneck.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(bottleneck => bottleneck.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(bottleneck => bottleneck.BusinessImpact)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(bottleneck => bottleneck.ResolutionSummary)
                .HasMaxLength(1000);

            entity.Property(bottleneck => bottleneck.LessonsLearned)
                .HasMaxLength(1000);

            entity.Property(bottleneck => bottleneck.ResolutionOwner)
                .HasMaxLength(120);

            entity.Ignore(bottleneck => bottleneck.IsUnresolved);

            entity.Property(bottleneck => bottleneck.Owner)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(bottleneck => bottleneck.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(bottleneck => bottleneck.ResolutionOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(bottleneck => bottleneck.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(action => action.Id);

            entity.Property(action => action.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(action => action.Owner)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(action => action.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(action => action.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });

        modelBuilder.Entity<SourceActivity>(entity =>
        {
            entity.HasKey(activity => activity.Id);

            entity.Property(activity => activity.CandidateName)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(activity => activity.Source)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(activity => activity.CustomSource)
                .HasMaxLength(120);

            entity.Property(activity => activity.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Ignore(activity => activity.IsHire);
            entity.Ignore(activity => activity.SourceLabel);

            entity.HasOne<Requisition>()
                .WithMany()
                .HasForeignKey(activity => activity.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
