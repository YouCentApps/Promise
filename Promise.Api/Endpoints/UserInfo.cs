using Microsoft.EntityFrameworkCore;
namespace Promise.Api;

public static class UserInfo
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (jwt is null || jwt.Length < 1)
            {
                MainLogger.LogError("No token provided for user info request");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "No token provided" });
            }

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
            if (user is null || user.Login is null || user.Login.Length < 1)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided" });
            }

            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error..." });
            }
            if (!Security.ValidateBearerAccessToken(jwt, user.Login, jwtSecret))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid token" });
            }

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Login == user.Login);
            if (dbUser is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found" });
            }
            var balance = await db.Balances.FirstOrDefaultAsync(b => b.UserId == dbUser.Id);
            if (balance is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Balance not found" });
            }
            var limit = await db.PromiseLimits.FirstOrDefaultAsync(l => l.UserId == dbUser.Id);
            if (limit is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Promise limit not found" });
            }
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Results.Json(new ApiResponseUserInfo
            {
                Success = true,
                Error = "",
                Id = dbUser.Id,
                Login = dbUser.Login,
                Balance = balance.Cents,
                PromiseLimit = limit.Cents
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error getting user info : " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
