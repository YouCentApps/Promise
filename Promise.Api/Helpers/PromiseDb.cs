namespace Promise.Api;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by EF Core dependency injection")]
internal sealed class PromiseDb : DbContext
{
    public PromiseDb(DbContextOptions<PromiseDb> options) : base(options) { }

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
    }
}
