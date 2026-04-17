namespace Promise.Lib;

public class Currency
{
    public byte Id { get; set; }
    public string? Code { get; set; }
    public string? Number { get; set; }
    public string? Name { get; set; }
}

public class Language
{
    public int Id { get; set; }
    public string? NameEng { get; set; }
    public string? NameCode { get; set; }
    public string? NameNative { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Salt { get; set; }
    public DateTime CreationDate { get; set; }
}

public class Balance
{
    public long UserId { get; set; }
    public long Cents { get; set; }
}

public class PromiseLimit
{
    public long UserId { get; set; }
    public long Cents { get; set; }
}

public class PromiseTransaction
{
    public long Id { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public int Cents { get; set; }
    public DateTime Date { get; set; }
    public string? Hash { get; set; }
    public bool IsBlockchain { get; set; }
    public string? Memo { get; set; }
}

public class Rate
{
    public byte CurrencyId { get; set; }
    public double AmountFor100 { get; set; }
    public DateTime UpdateDate { get; set; }
}

public class UserSetting
{
    public long UserId { get; set; }
    public int LanguageId { get; set; }
    public byte CurrencyId { get; set; }
    public bool IsDarkTheme { get; set; }
}

public class PersonalData
{
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string? Tel { get; set; }
    public string? Secret { get; set; }
    public string? EmailHash { get; set; }
    public string? TelHash { get; set; }
    public string? SecretHash { get; set; }
    public string? Salt { get; set; }
    public string? EmailMasked { get; set; }
    public string? TelMasked { get; set; }
}

public class AccessRestore
{
    public long UserId { get; set; }
    public int UseSecretTryNumber { get; set; }
    public DateTime? UseSecretTryDate { get; set; }
    public int UseEmailTryNumber { get; set; }
    public DateTime? UseEmailTryDate { get; set; }
    public int UseTelTryNumber { get; set; }
    public DateTime? UseTelTryDate { get; set; }
}

