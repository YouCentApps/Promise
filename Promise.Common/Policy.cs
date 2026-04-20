namespace Promise.Common;

public static class AppPolicy
{
    public const int MinimumPasswordLength = 8;

    public const int MaximumPasswordLength = 40;

    public const int MinimumUsernameLength = 5;

    public const int MaximumUsernameLength = 50;

    public const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).*$";

    public const string UsernameRegex = @"^[a-zA-Z0-9]+$";

    public const string SecretWordRegex = @"^[a-zA-Z0-9]+$";
}



