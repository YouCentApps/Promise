using Promise.Api;

public static class SignIn
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            User? user = null;
            try
            {
                user = await context.Request.ReadFromJsonAsync<User>();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error reading user from signin request : " + ex);
                return Results.Json(new { success = false, error = "Server error..." });
            }
            if (user is null || user.Login is null || user.Password is null ||
                user.Login.Length < 1 || user.Password.Length < 1)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided" });
            }
            var dbUser = db.Users.FirstOrDefault(u => u.Login == user.Login);
            if (dbUser is null || dbUser.Password is null || dbUser.Salt is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found" });
            }
            var hash = Security.GetPasswordHash(user.Password, dbUser.Salt);
            if (hash != dbUser.Password)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { auth = false, error = "Wrong password!" });
            }

            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error..." });
            }
            var payload = new Dictionary<string, object>
        {
            { Security.PayLoadFieldLogin, user.Login },
            { Security.PayLoadFieldAuth, true },
            { Security.PayLoadFieldExp, Security.GetAccessTokenLifetimeSeconds() }
        };
            var token = Security.CreateBearerJwt(payload, jwtSecret);
            context.Response.Headers.TryAdd(Security.AuthorizationHttpHeader, token);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Results.Json(new ApiResponseUser
            {
                Success = true,
                Error = "",
                Id = dbUser.Id,
                Login = dbUser.Login
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error signing in user : " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
