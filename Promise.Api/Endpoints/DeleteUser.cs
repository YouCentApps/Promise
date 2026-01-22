using System.Reflection.PortableExecutable;

namespace Promise.Api;
public class DeleteUser
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error..." });
            }
            var user = await context.Request.ReadFromJsonAsync<User>();
            if (user is null || user.Login is null || user.Password is null ||
                user.Login.Length < 1 || user.Password.Length < 1)
            {
                MainLogger.LogError("Error reading user from delete user request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided" });
            }
            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (jwt is null || jwt.Length < 1)
            {
                MainLogger.LogError("No JWT provided for delete user request");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "No JWT provided" });
            }
            if (!Security.ValidateBearerAccessToken(jwt, user.Login, jwtSecret))
            {
                MainLogger.LogError("Invalid JWT provided for delete user request");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid JWT" });
            }
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var dbUser = db.Users.FirstOrDefault(u => u.Login == user.Login && u.Id == user.Id);
            if (dbUser is null || dbUser.Password is null || dbUser.Salt is null)
            {
                MainLogger.LogError("User not found for delete user request");
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found" });
            }
            var hash = Security.GetPasswordHash(user.Password, dbUser.Salt);
            if (hash != dbUser.Password)
            {
                MainLogger.LogError("Wrong password for delete user request");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { success = false, error = "Wrong password" });
            }

            // Delete records from UserSettings table
            var userSettings = db.UserSettings.Where(us => us.UserId == dbUser.Id);
            db.UserSettings.RemoveRange(userSettings);

            // Delete records from PersonalData table
            var personalData = db.PersonalData.Where(pd => pd.UserId == dbUser.Id);
            db.PersonalData.RemoveRange(personalData);

            // Delete records from PromiseLimit table
            var promiseLimits = db.PromiseLimits.Where(pl => pl.UserId == dbUser.Id);
            db.PromiseLimits.RemoveRange(promiseLimits);

            // Delete records from Balance table
            var balances = db.Balances.Where(b => b.UserId == dbUser.Id);
            db.Balances.RemoveRange(balances);

            var transactionsCount = db.PromiseTransactions.Count(t => t.SenderId == dbUser.Id || t.ReceiverId == dbUser.Id);
            if (transactionsCount > 0)
            {
                var deletedData = db.SaveChanges();
                var checkData = db.PersonalData.FirstOrDefault(pd => pd.UserId == dbUser.Id);
                if (deletedData > 0 && checkData is null)
                {
                    dbUser.Login = "(deleted)" + dbUser.Login;
                    var markedDeleted = db.SaveChanges();
                    if (markedDeleted > 0)
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return Results.Json(new { success = true, message = "User marked as deleted and its data removed except the transactions history" });
                    }
                    else
                    {
                        MainLogger.LogError("Error marked user as deleted after deleting data delete user request (100) : " + user.Login);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        return Results.Json(new { success = false, error = "The user data was deleted but user was not marked as deleted yet, please try again later..." });
                    }
                }
                else
                {
                    MainLogger.LogError("Error deleting user and its data for delete user request (200) : " + user.Login);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return Results.Json(new { success = false, error = "Error deleting user and its data, please try again later..." });
                }
            }
            db.Users.Remove(dbUser);
            var deleted = db.SaveChanges();
            if (deleted > 0)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Results.Json(new { success = true, message = "User and all its data deleted successfully" });
            }
            else
            {
                MainLogger.LogError("Error deleting user and its data for delete user request (300) : " + user.Login);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Results.Json(new { success = false, error = "Error deleting user and its data, please try again later..." });

            }
        }
        catch (Exception ex)
        {
            MainLogger.LogError("Error deleting user and its data for delete user request (400) : " + ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Error deleting user and its data, please try again later..." });
        }
    }
}
