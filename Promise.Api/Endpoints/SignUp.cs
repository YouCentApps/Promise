using Microsoft.Extensions.Options;
namespace Promise.Api;

internal static class SignUp
{
    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            User? user = null;
            try
            {
                user = await context.Request.ReadFromJsonAsync<User>().ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error reading user from signup request : " + ex);
                return Results.Json(new { success = false, error = "Server error..." });
            }
#pragma warning restore CA1031
            if (user is null || user.Login is null || user.Password is null ||
                user.Login.Length < AppPolicy.MinimumUsernameLength || user.Password.Length < AppPolicy.MinimumPasswordLength)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided" });
            }
            var dbUser = db.Users.FirstOrDefault(u => u.Login == user.Login);
            if (dbUser != null)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                return Results.Json(new { success = false, error = "Username already exists, sorry..." });
            }

            var salt = Security.GetSalt();
            var newUser = new User
            {
                Login = user.Login,
                Password = Security.GetPasswordHash(user.Password, salt),
                Salt = salt,
                CreationDate = DateTime.Now
            };
            db.Users.Add(newUser);
            var records = await db.SaveChangesAsync().ConfigureAwait(false);
            dbUser = db.Users.FirstOrDefault(u => u.Login == user.Login);
            if (records > 0 && dbUser != null)
            {
                var balance = new Balance
                {
                    UserId = newUser.Id,
                    Cents = 0
                };
                db.Balances.Add(balance);
                var limit = new PromiseLimit
                {
                    UserId = newUser.Id,
                    Cents = Promo.InitialLimit
                };
                db.PromiseLimits.Add(limit);
                var settings = new UserSetting
                {
                    UserId = newUser.Id,
                    CurrencyId = 1,
                    LanguageId = 1,
                    IsDarkTheme = false
                };
                db.UserSettings.Add(settings);
                records = await db.SaveChangesAsync().ConfigureAwait(false);
                if (records < 3)
                {
                    db.Users.Remove(dbUser);
                    db.Balances.Remove(balance);
                    db.PromiseLimits.Remove(limit);
                    db.UserSettings.Remove(settings);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    MainLogger.LogError("Error saving user related data to the DB");
                    return Results.Json(new { success = false, error = "Server error..." });
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error saving user to DB");
                return Results.Json(new { success = false, error = "Server error..." });
            }
            context.Response.StatusCode = StatusCodes.Status202Accepted;

            #pragma warning disable CA1031
            try
            {
                var mailSettings = context.RequestServices.GetRequiredService<IOptions<MailSettings>>();
                var mailSender = new MailSender(mailSettings);
                List<string> emails = [MailSender.SystemEMail];
                var data = new MailData(emails, "New YouCent 2.0 Sign Up", "Sign Up username: " + user.Login);
                _ = mailSender.SendAsync(data, new CancellationToken()).ConfigureAwait(false);
                MainLogger.Log($"Sign Up via Web App e-mail must be sent! | Username: {user.Login}");
            }
            catch (Exception ex)
            {
                MainLogger.LogError($"Error sending e-mail about sign up with username {user.Login}: " + ex);
            }
#pragma warning restore CA1031

            return Results.Json(new ApiResponseUser
            {
                Success = true,
                Error = "",
                Id = dbUser.Id,
                Login = dbUser.Login
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error signing up user : " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
