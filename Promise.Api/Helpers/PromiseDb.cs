namespace Promise.Api.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by EF Core dependency injection")]
internal sealed class PromiseDb(DbContextOptions<PromiseDb> options) : DbContext(options)
{
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Balance> Balances { get; set; }
    public DbSet<PromiseLimit> PromiseLimits { get; set; }
    public DbSet<PromiseTransaction> PromiseTransactions { get; set; }
    public DbSet<Rate> Rates { get; set; }
    public DbSet<UserSetting> UserSettings { get; set; }
    public DbSet<PersonalData> PersonalData { get; set; }
    public DbSet<AccessRestore> AccessRestore { get; set; }

    // Merchant tables
    public DbSet<MerchantPaymentRequestType> MerchantPaymentRequestTypes { get; set; }
    public DbSet<MerchantPaymentRequestStatus> MerchantPaymentRequestStatuses { get; set; }
    public DbSet<MerchantSubscriptionStatus> MerchantSubscriptionStatuses { get; set; }
    public DbSet<MerchantTransactionType> MerchantTransactionTypes { get; set; }
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<MerchantPaymentRequest> MerchantPaymentRequests { get; set; }
    public DbSet<MerchantSubscription> MerchantSubscriptions { get; set; }
    public DbSet<MerchantTransaction> MerchantTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Currency>().HasKey(c => c.Id);
        modelBuilder.Entity<Language>().HasKey(l => l.Id);
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<Balance>().HasKey(b => b.UserId);
        modelBuilder.Entity<PromiseLimit>().HasKey(pl => pl.UserId);
        modelBuilder.Entity<PromiseTransaction>().HasKey(pt => pt.Id);
        modelBuilder.Entity<Rate>().HasKey(r => r.CurrencyId);
        modelBuilder.Entity<UserSetting>().HasKey(us => us.UserId);
        modelBuilder.Entity<PersonalData>().HasKey(pd => pd.UserId);
        modelBuilder.Entity<AccessRestore>().HasKey(ar => ar.UserId);

        modelBuilder.Entity<PromiseLimit>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<PromiseLimit>(pl => pl.UserId);

        modelBuilder.Entity<Balance>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Balance>(b => b.UserId);

        modelBuilder.Entity<PromiseTransaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pt => pt.SenderId);

        modelBuilder.Entity<PromiseTransaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pt => pt.ReceiverId);

        modelBuilder.Entity<UserSetting>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<UserSetting>(us => us.UserId);

        modelBuilder.Entity<UserSetting>()
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(us => us.LanguageId);

        modelBuilder.Entity<UserSetting>()
            .HasOne<Currency>()
            .WithMany()
            .HasForeignKey(us => us.CurrencyId);

        modelBuilder.Entity<Rate>()
            .HasOne<Currency>()
            .WithMany()
            .HasForeignKey(r => r.CurrencyId);

        modelBuilder.Entity<PersonalData>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<PersonalData>(pd => pd.UserId);

        modelBuilder.Entity<AccessRestore>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<AccessRestore>(ar => ar.UserId);

        // Merchant lookup tables
        modelBuilder.Entity<MerchantPaymentRequestType>().HasKey(t => t.Id);
        modelBuilder.Entity<MerchantPaymentRequestStatus>().HasKey(s => s.Id);
        modelBuilder.Entity<MerchantSubscriptionStatus>().HasKey(s => s.Id);
        modelBuilder.Entity<MerchantTransactionType>().HasKey(t => t.Id);

        // Merchant
        modelBuilder.Entity<Merchant>().HasKey(m => m.UserId);
        modelBuilder.Entity<Merchant>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Merchant>(m => m.UserId);

        // MerchantPaymentRequest
        modelBuilder.Entity<MerchantPaymentRequest>().HasKey(r => r.Id);
        modelBuilder.Entity<MerchantPaymentRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.MerchantId);
        modelBuilder.Entity<MerchantPaymentRequest>()
            .HasOne<MerchantPaymentRequestType>()
            .WithMany()
            .HasForeignKey(r => r.TypeId);
        modelBuilder.Entity<MerchantPaymentRequest>()
            .HasOne<MerchantPaymentRequestStatus>()
            .WithMany()
            .HasForeignKey(r => r.StatusId);

        // MerchantSubscription
        modelBuilder.Entity<MerchantSubscription>().HasKey(s => s.Id);
        modelBuilder.Entity<MerchantSubscription>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.MerchantId);
        modelBuilder.Entity<MerchantSubscription>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.SubscriberId);
        modelBuilder.Entity<MerchantSubscription>()
            .HasOne<MerchantPaymentRequest>()
            .WithMany()
            .HasForeignKey(s => s.PaymentRequestId);
        modelBuilder.Entity<MerchantSubscription>()
            .HasOne<MerchantSubscriptionStatus>()
            .WithMany()
            .HasForeignKey(s => s.StatusId);

        // MerchantTransaction
        modelBuilder.Entity<MerchantTransaction>().HasKey(t => t.Id);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.MerchantId);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.PayerId);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<MerchantSubscription>()
            .WithMany()
            .HasForeignKey(t => t.SubscriptionId);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<MerchantPaymentRequest>()
            .WithMany()
            .HasForeignKey(t => t.PaymentRequestId);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<PromiseTransaction>()
            .WithMany()
            .HasForeignKey(t => t.PromiseTransactionId);
        modelBuilder.Entity<MerchantTransaction>()
            .HasOne<MerchantTransactionType>()
            .WithMany()
            .HasForeignKey(t => t.TypeId);
    }
}
