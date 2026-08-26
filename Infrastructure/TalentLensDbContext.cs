using C_TalentLens.Domain;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure;

public class TalentLensDbContext(DbContextOptions<TalentLensDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Requisition> Requisitions => Set<Requisition>();

    public DbSet<SourceActivity> SourceActivities => Set<SourceActivity>();

    public DbSet<Referral> Referrals => Set<Referral>();

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
                .HasConversion<string>()
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

            entity.Property(requisition => requisition.OpeningReason)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(requisition => requisition.CustomOpeningReason)
                .HasMaxLength(120);

            entity.Property(requisition => requisition.PostingType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(requisition => requisition.StatusComment)
                .HasMaxLength(1000);

            entity.Property(requisition => requisition.HiringManagerNotes)
                .HasMaxLength(1000);

            entity.Property(requisition => requisition.CurrentStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(requisition => requisition.CurrentStage)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(requisition => requisition.ExternalSource)
                .HasMaxLength(80);

            entity.Property(requisition => requisition.ExternalId)
                .HasMaxLength(120);

            entity.HasIndex(requisition => new { requisition.ExternalSource, requisition.ExternalId })
                .IsUnique()
                .HasFilter("\"ExternalSource\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

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

            entity.Property(action => action.Title)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(action => action.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(action => action.Category)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(action => action.CustomCategory)
                .HasMaxLength(120);

            entity.Property(action => action.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(action => action.Owner)
                .HasMaxLength(120)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(action => action.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(action => action.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(action => action.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(action => action.CompletedBy)
                .HasMaxLength(120);

            entity.Property(action => action.CompletionNotes)
                .HasMaxLength(1000);

            entity.Ignore(action => action.IsOpen);

            entity.Navigation(action => action.History)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(action => action.History)
                .WithOne()
                .HasForeignKey(history => history.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemHistory>(entity =>
        {
            entity.HasKey(history => history.Id);

            entity.Property(history => history.EventType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(history => history.ChangedBy)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(history => history.FromValue)
                .HasMaxLength(160);

            entity.Property(history => history.ToValue)
                .HasMaxLength(160);

            entity.Property(history => history.Notes)
                .HasMaxLength(1000);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(referral => referral.Id);

            entity.Property(referral => referral.ReferrerName)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(referral => referral.ReferrerEmployeeId)
                .HasMaxLength(40);

            entity.Property(referral => referral.ReferrerDepartment)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(referral => referral.CandidateName)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(referral => referral.CandidateEmail)
                .HasMaxLength(180)
                .IsRequired();

            entity.Property(referral => referral.CandidatePhoneNumber)
                .HasMaxLength(40);

            entity.Property(referral => referral.ResumeUrl)
                .HasMaxLength(500);

            entity.Property(referral => referral.SubmitterEmail)
                .HasMaxLength(180);

            entity.Property(referral => referral.SubmitterName)
                .HasMaxLength(160);

            entity.Property(referral => referral.CandidateRelationship)
                .HasMaxLength(500);

            entity.Property(referral => referral.CandidateKnownDuration)
                .HasMaxLength(120);

            entity.Property(referral => referral.CandidateAlignmentComment)
                .HasMaxLength(1000);

            entity.Property(referral => referral.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(referral => referral.HiringOutcome)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Ignore(referral => referral.IsHire);
            entity.Ignore(referral => referral.IsActive);

            entity.Navigation(referral => referral.History)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(referral => referral.History)
                .WithOne()
                .HasForeignKey(history => history.ReferralId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Requisition>()
                .WithMany()
                .HasForeignKey(referral => referral.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReferralHistory>(entity =>
        {
            entity.HasKey(history => history.Id);

            entity.Property(history => history.EventType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(history => history.ChangedBy)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(history => history.FromValue)
                .HasMaxLength(160);

            entity.Property(history => history.ToValue)
                .HasMaxLength(160);

            entity.Property(history => history.Notes)
                .HasMaxLength(1000);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
