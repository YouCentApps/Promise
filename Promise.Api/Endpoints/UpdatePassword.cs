namespace Promise.Api;

public class UpdatePassword
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error... Please try again later." });
            }
            var userUpdate = await context.Request.ReadFromJsonAsync<UserUpdate>();
            if (userUpdate is null || userUpdate.OldUser is null || userUpdate.NewUser is null ||
                userUpdate.OldUser.Login is null || userUpdate.OldUser.Password is null ||
                userUpdate.NewUser.Login is null || userUpdate.NewUser.Password is null ||
                userUpdate.OldUser.Login.Length < 1 || userUpdate.OldUser.Password.Length < 1 ||
                userUpdate.NewUser.Login.Length < 1 || userUpdate.NewUser.Password.Length < 1)
            {
                MainLogger.LogError("Error reading user from update password request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }
            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (jwt is null || jwt.Length < 1)
            {
                MainLogger.LogError("No JWT provided for update password request for username: " + userUpdate.OldUser.Login);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Looks like you are not signed in, sorry." });
            }
            if (!Security.ValidateBearerAccessToken(jwt, userUpdate.OldUser.Login, jwtSecret))
            {
                MainLogger.LogError("Invalid JWT provided for update password request for username: " + userUpdate.OldUser.Login);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Your session is expired or invalid, please sign in again." });
            }
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var dbUser = db.Users.FirstOrDefault(u => u.Login == userUpdate.OldUser.Login && u.Id == userUpdate.OldUser.Id);
            if (dbUser is null || dbUser.Password is null || dbUser.Salt is null)
            {
                MainLogger.LogError("User not found for update password request for username: " + userUpdate.OldUser.Login);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }
            var hash = Security.GetPasswordHash(userUpdate.OldUser.Password, dbUser.Salt);
            if (hash != dbUser.Password)
            {
                MainLogger.LogError("Wrong password for update password request for username: " + userUpdate.OldUser.Login);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { success = false, error = "Wrong old password, sorry!" });
            }
            var newHash = Security.GetPasswordHash(userUpdate.NewUser.Password, dbUser.Salt);
            dbUser.Password = newHash;
            db.Users.Update(dbUser);
            int updated = await db.SaveChangesAsync();
            if (updated < 1)
            {
                MainLogger.LogError("Error updating password for user: " + userUpdate.OldUser.Login);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Results.Json(new { success = false, error = "Server error... Please try agein later." });
            }
            return Results.Json(new { success = true, error = "" });
        }
        catch (Exception ex)
        {
            MainLogger.LogError("Error updating password: " + ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error... Please try again later." });
        }
    }
}
