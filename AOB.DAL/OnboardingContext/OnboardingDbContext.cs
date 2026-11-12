using AOB.BAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.OnboardingContext
{
    /// <summary>
    /// EF Core code-first context for the onboarding database.
    ///
    /// Relationships:
    ///   UserAuth 1 ── 0..1 InternalUser   (InternalUser.UserAuthId, required + unique)
    ///   UserAuth 1 ── 0..1 Partner        (Partner.UserAuthId, required + unique)
    ///   UserAuth 1 ── 0..1 Customer       (Customer.UserAuthId, nullable + unique)
    ///   Partner  1 ── *    Customer       (owning partner; PCIPAL for direct customers)
    ///
    /// Changing the model (a new entity, property or relationship) needs a migration. Run from the solution folder:
    ///   dotnet ef migrations add &lt;Name&gt; --project AOB.DAL --startup-project AOB.WEB
    ///   (add "--configuration Release" if the running app locks the Debug build)
    /// Migrations are applied automatically when the app starts (DbSeeder). To apply them by hand:
    ///   dotnet ef database update --project AOB.DAL --startup-project AOB.WEB
    /// To undo the last migration that has not been applied: dotnet ef migrations remove --project AOB.DAL --startup-project AOB.WEB
    /// More notes: AOB.DAL/Migrations/README.md
    /// </summary>
    public class OnboardingDbContext : DbContext
    {
        public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options) : base(options)
        {
        }

        public DbSet<UserAuth> UserAuths => Set<UserAuth>();

        public DbSet<InternalUser> InternalUsers => Set<InternalUser>();

        public DbSet<Partner> Partners => Set<Partner>();

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

        public DbSet<CustomerDocument> CustomerDocuments => Set<CustomerDocument>();

        public DbSet<Project> Projects => Set<Project>();

        public DbSet<ProjectDiscovery> ProjectDiscoveries => Set<ProjectDiscovery>();

        public DbSet<ProjectTechnicalEnvironment> ProjectTechnicalEnvironments => Set<ProjectTechnicalEnvironment>();

        public DbSet<ProjectPaymentConfiguration> ProjectPaymentConfigurations => Set<ProjectPaymentConfiguration>();

        public DbSet<ProjectGatewayConfiguration> ProjectGatewayConfigurations => Set<ProjectGatewayConfiguration>();

        public DbSet<ProjectGatewayFields> ProjectGatewayFields => Set<ProjectGatewayFields>();

        public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();

        public DbSet<ProjectProvisioningDetail> ProjectProvisioningDetails => Set<ProjectProvisioningDetail>();

        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<VisitorIp> VisitorIps => Set<VisitorIp>();

        public DbSet<CustomerUser> CustomerUsers => Set<CustomerUser>();

        public DbSet<ProjectInformationRequest> ProjectInformationRequests => Set<ProjectInformationRequest>();

        public DbSet<ProjectInformationRequestItem> ProjectInformationRequestItems => Set<ProjectInformationRequestItem>();
        public DbSet<ProjectInformationResponse> ProjectInformationResponses => Set<ProjectInformationResponse>();

        public DbSet<PaymentGateway> PaymentGateways => Set<PaymentGateway>();

        public DbSet<PaymentGatewayField> PaymentGatewayFields => Set<PaymentGatewayField>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAuth>(entity =>
            {
                // Store the enum as a readable string.
                entity.Property(u => u.UserType).HasConversion<string>().HasMaxLength(20);

                // One account per email within each user type.
                entity.HasIndex(u => new { u.UserType, u.NormalizedEmail }).IsUnique();

                // Deleting the login removes the internal/partner profile with it.
                entity.HasOne(u => u.InternalUser)
                      .WithOne(i => i.Auth)
                      .HasForeignKey<InternalUser>(i => i.UserAuthId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Partner)
                      .WithOne(p => p.Auth)
                      .HasForeignKey<Partner>(p => p.UserAuthId)
                      .OnDelete(DeleteBehavior.Cascade);

                // A customer record outlives its login, so the link is just cleared.
                entity.HasOne(u => u.Customer)
                      .WithOne(c => c.Auth)
                      .HasForeignKey<Customer>(c => c.UserAuthId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Partner>(entity =>
            {
                entity.HasIndex(p => p.UserAuthId).IsUnique();
                entity.HasIndex(p => p.Name).IsUnique();


                // A partner with customers cannot be deleted.
                entity.HasMany(p => p.Customers)
                      .WithOne(c => c.Partner)
                      .HasForeignKey(c => c.PartnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OtpToken>(entity =>
            {
                entity.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(30);

                // Verification links are looked up by hash; OTPs by user + purpose.
                entity.HasIndex(t => new { t.Purpose, t.TokenHash });
                entity.HasIndex(t => new { t.UserAuthId, t.Purpose });

                // Tokens are meaningless without their user, so they go with the login.
                entity.HasOne(t => t.Auth)
                      .WithMany()
                      .HasForeignKey(t => t.UserAuthId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();

                // Starting checklist; internal users can change it from the Document types screen.
                var seeded = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
                entity.HasData(
                    new DocumentType { Id = new Guid("a1000000-0000-4000-8000-000000000001"), Name = "Data Processing Agreement", Description = "Signed DPA between your organisation and PCI Pal", SortOrder = 10, CreatedAt = seeded },
                    new DocumentType { Id = new Guid("a1000000-0000-4000-8000-000000000002"), Name = "Business Description / Company Overview", Description = "A brief overview of your organisation and payment operations", SortOrder = 20, CreatedAt = seeded },
                    new DocumentType { Id = new Guid("a1000000-0000-4000-8000-000000000003"), Name = "Network Topology Diagram", Description = "Diagram showing how your systems connect to PCI Pal endpoints", SortOrder = 30, CreatedAt = seeded },
                    new DocumentType { Id = new Guid("a1000000-0000-4000-8000-000000000004"), Name = "PCI DSS Self-Assessment Questionnaire (SAQ)", Description = "Most recent SAQ or equivalent PCI compliance evidence", SortOrder = 40, CreatedAt = seeded },
                    new DocumentType { Id = new Guid("a1000000-0000-4000-8000-000000000005"), Name = "Certificate of Incorporation", Description = "Official company registration documentation", SortOrder = 50, CreatedAt = seeded });
            });

            modelBuilder.Entity<CustomerDocument>(entity =>
            {
                // One file per customer and document type; uploading again replaces it.
                entity.HasIndex(d => new { d.CustomerId, d.DocumentTypeId }).IsUnique();

                entity.HasOne(d => d.Customer)
                      .WithMany()
                      .HasForeignKey(d => d.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // A type that has uploads cannot be deleted (deactivate it instead).
                entity.HasOne(d => d.DocumentType)
                      .WithMany(t => t.CustomerDocuments)
                      .HasForeignKey(d => d.DocumentTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            ConfigureProjects(modelBuilder);

            // Payment gateways and the fields each one exchanges. Ids are generated by the code, so tell
            // EF the keys are never database-generated (see ConfigureProjects for why).
            modelBuilder.Entity<PaymentGateway>(entity =>
            {
                entity.Property(g => g.Id).ValueGeneratedNever();
                entity.HasIndex(g => g.Name).IsUnique();

                // Existing gateways stay standard until someone marks them otherwise.
                entity.Property(g => g.IsStandard).HasDefaultValue(true);

                // Deleting a gateway deletes its fields.
                entity.HasMany(g => g.Fields)
                      .WithOne(f => f.PaymentGateway)
                      .HasForeignKey(f => f.PaymentGatewayId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PaymentGatewayField>(entity =>
            {
                entity.Property(f => f.Id).ValueGeneratedNever();
                // FieldType stays a numeric column: 0 Authentication, 1 Required, 2 Conditional, 3 Optional.
                entity.Property(f => f.IsStandard).HasDefaultValue(true);

                // The same field name may be both an input and an output, but never listed twice alike.
                entity.HasIndex(f => new { f.PaymentGatewayId, f.Name, f.FieldType, f.IsInput }).IsUnique();
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(40);

                // Unique (filtered to non-null by EF on SQL Server): one login per customer.
                entity.HasIndex(c => c.UserAuthId).IsUnique();
                entity.HasIndex(c => c.PartnerId);

                // Direct customers must have a login; indirect (partner-created) customers must not.
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Customers_DirectMatchesLogin",
                    "([IsDirect] = 1 AND [UserAuthId] IS NOT NULL) OR ([IsDirect] = 0 AND [UserAuthId] IS NULL)"));
            });
        }

        /// <summary>
        /// Project (onboarding case) tables. Enums are stored as readable text, except
        /// ProjectGatewayFields.FieldType which stays numeric (0 = Authentication, 1 = Required,
        /// 2 = Conditional). Multiple-choice answers are plain comma-separated strings.
        /// </summary>
        private static void ConfigureProjects(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VisitorIp>(entity =>
            {
                entity.Property(v => v.Id).ValueGeneratedNever();
                entity.HasIndex(v => v.IpAddress).IsUnique();
                entity.HasIndex(v => v.LastSeenAt);
            });

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.Property(a => a.Target).HasConversion<string>().HasMaxLength(20);

                // Newest-first feeds per customer and per project.
                entity.HasIndex(a => new { a.CustomerId, a.CreatedAt });
                entity.HasIndex(a => new { a.ProjectId, a.CreatedAt });

                // Deleting a customer deletes their history. ProjectId is a plain id (no second cascade path).
                entity.HasOne<Customer>()
                      .WithMany()
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(p => p.CrmSystem).HasConversion<string>().HasMaxLength(40);
                entity.Property(p => p.CrmPlatform).HasConversion<string>().HasMaxLength(40);
                entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(40);
                entity.Property(p => p.CurrentStep).HasConversion<string>().HasMaxLength(40);
                entity.Property(p => p.BuildType).HasConversion<string>().HasMaxLength(20);

                // Requests for more information are deleted with the project.
                entity.HasMany(p => p.InformationRequests).WithOne(r => r.Project)
                      .HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.CustomerId);

                // The background job looks up projects whose deletion time has passed.
                entity.HasIndex(p => p.DeletionScheduledFor);

                // Deleting a customer deletes their projects.
                entity.HasOne(p => p.Customer)
                      .WithMany()
                      .HasForeignKey(p => p.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // One row per screen, created when the customer first saves that screen.
                entity.HasOne(p => p.Discovery).WithOne(d => d.Project)
                      .HasForeignKey<ProjectDiscovery>(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.TechnicalEnvironment).WithOne(t => t.Project)
                      .HasForeignKey<ProjectTechnicalEnvironment>(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.PaymentConfiguration).WithOne(c => c.Project)
                      .HasForeignKey<ProjectPaymentConfiguration>(c => c.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.GatewayConfiguration).WithOne(g => g.Project)
                      .HasForeignKey<ProjectGatewayConfiguration>(g => g.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.ProvisioningDetail).WithOne(d => d.Project)
                      .HasForeignKey<ProjectProvisioningDetail>(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);

                // Many gateway-field rows per project.
                entity.HasMany(p => p.GatewayFields).WithOne(f => f.Project)
                      .HasForeignKey(f => f.ProjectId).OnDelete(DeleteBehavior.Cascade);

                // Many uploaded documents per project.
                entity.HasMany(p => p.Documents).WithOne(d => d.Project)
                      .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectDocument>(entity =>
            {
                entity.Property(d => d.Type).HasConversion<string>().HasMaxLength(40);
                entity.HasIndex(d => d.ProjectId);
            });

            // The users the customer wants accounts for. They belong to a project and go with it.
            modelBuilder.Entity<CustomerUser>(entity =>
            {
                entity.Property(u => u.Id).ValueGeneratedNever();
                entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

                // One row per email within a project.
                entity.HasIndex(u => new { u.ProjectId, u.Email }).IsUnique();

                entity.HasOne(u => u.Project).WithMany(p => p.CustomerUsers)
                      .HasForeignKey(u => u.ProjectId).OnDelete(DeleteBehavior.Cascade);
            });

            // Information requests. Ids come from the code (see the note on the project tables above).
            modelBuilder.Entity<ProjectInformationRequest>(entity =>
            {
                entity.Property(r => r.Id).ValueGeneratedNever();
                entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(r => new { r.ProjectId, r.CreatedAt });

                entity.HasMany(r => r.Items).WithOne(i => i.Request)
                      .HasForeignKey(i => i.RequestId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectInformationRequestItem>(entity =>
            {
                entity.Property(i => i.Id).ValueGeneratedNever();
                entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasMany(i => i.Responses).WithOne(r => r.Item)
                      .HasForeignKey(r => r.ItemId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectInformationResponse>(entity =>
            {
                entity.Property(r => r.Id).ValueGeneratedNever();
                entity.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(r => new { r.ItemId, r.SubmittedAt });
            });

            // These rows are created by adding them to a project that is already tracked, with an Id the
            // code generates. Telling EF the key is never database-generated makes it INSERT them
            // (otherwise it would assume a row with that Id already exists and try to UPDATE it).
            modelBuilder.Entity<ProjectDiscovery>().Property(d => d.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectTechnicalEnvironment>().Property(t => t.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectPaymentConfiguration>().Property(c => c.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectGatewayConfiguration>().Property(g => g.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectGatewayFields>().Property(f => f.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectDocument>().Property(d => d.Id).ValueGeneratedNever();
            modelBuilder.Entity<ProjectProvisioningDetail>().Property(d => d.Id).ValueGeneratedNever();

            modelBuilder.Entity<ProjectDiscovery>(entity =>
            {
                entity.Property(d => d.CrmPaymentExperience).HasConversion<string>().HasMaxLength(40);
            });

            modelBuilder.Entity<ProjectTechnicalEnvironment>(entity =>
            {
                entity.Property(t => t.OutboundAccess).HasConversion<string>().HasMaxLength(40);
            });

            modelBuilder.Entity<ProjectPaymentConfiguration>(entity =>
            {
                // The chosen gateway. A gateway that projects use cannot be deleted.
                entity.HasOne(c => c.PaymentGateway)
                      .WithMany()
                      .HasForeignKey(c => c.PaymentGatewayId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(c => c.PaymentGatewayId);

                entity.Property(c => c.CardNumberMasking).HasConversion<string>().HasMaxLength(40);
                entity.Property(c => c.MerchantIdentification).HasConversion<string>().HasMaxLength(40);
                entity.Property(c => c.SmsServiceProvider).HasConversion<string>().HasMaxLength(40);
                entity.Property(c => c.OtherSmsProvider).HasConversion<string>().HasMaxLength(40);
                entity.Property(c => c.EmailServiceProvider).HasConversion<string>().HasMaxLength(40);
                entity.Property(c => c.OtherEmailProvider).HasConversion<string>().HasMaxLength(200);
            });

            modelBuilder.Entity<ProjectGatewayConfiguration>(entity =>
            {
                entity.Property(g => g.ContactCentrePlatform).HasConversion<string>().HasMaxLength(40);
                entity.Property(g => g.ConnectionType).HasConversion<string>().HasMaxLength(40);
            });

            modelBuilder.Entity<ProjectGatewayFields>(entity =>
            {
                // FieldType deliberately stays an int column: 0 Authentication, 1 Required, 2 Conditional, 3 Optional.

                // The gateway field the row came from. The row keeps its own copy of the name, type and
                // description, so deleting a gateway field only clears this link (the submitted data stays).
                entity.HasOne(f => f.PaymentGatewayField)
                      .WithMany()
                      .HasForeignKey(f => f.PaymentGatewayFieldId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(f => f.PaymentGatewayFieldId);

                // The same field may appear as input and output (or authentication), but never twice alike.
                entity.HasIndex(f => new { f.ProjectId, f.Name, f.FieldType, f.IsInput }).IsUnique();
            });
        }
    }
}
