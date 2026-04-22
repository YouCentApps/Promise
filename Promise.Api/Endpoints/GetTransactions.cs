namespace Promise.Api.Endpoints;

internal static class GetTransactions
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error... Please try again later." });
            }
            var transactionsHistory = await context.Request.ReadFromJsonAsync<TransactionsHistoryInfo>().ConfigureAwait(false);
            if (transactionsHistory is null || transactionsHistory.User is null || transactionsHistory.User.Login is null)
            {
                MainLogger.LogError("Error reading transactions history from get transactions request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }
            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (jwt is null || jwt.Length < 1)
            {
                MainLogger.LogError("No JWT provided for get transactions request for user with username: " + transactionsHistory.User.Login);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Looks like you are not signed in, sorry." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var user = db.Users.FirstOrDefault(u => u.Login == transactionsHistory.User.Login);
            if (user is null || user.Login is null)
            {
                MainLogger.LogError("User not found for get transactions request for username: " + transactionsHistory.User.Login);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Something went wrong. Please try again later." });
            }

            if (!Security.ValidateBearerAccessToken(jwt, user.Login, jwtSecret))
            {
                MainLogger.LogError("Invalid JWT provided for get transactions request for username: " + user.Login);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Your session is expired or invalid, please sign in again." });
            }

            var transactions = db.PromiseTransactions
                .Join(db.Users, t => t.SenderId, u => u.Id, (t, us) => new { t, us })
                .Join(db.Users, ts => ts.t.ReceiverId, u => u.Id, (ts, ur) => new { ts, ur })
                .Where(ts => ts.ts.t.SenderId == user.Id || ts.ts.t.ReceiverId == user.Id)
                .Where(ts => ts.ts.t.Date >= transactionsHistory.From && ts.ts.t.Date <= transactionsHistory.To)
                ;

            if (transactionsHistory.IsOldFirst)
            {
                transactions = transactions.OrderBy(ts => ts.ts.t.Date);
            }
            else
            {
                transactions = transactions.OrderByDescending(ts => ts.ts.t.Date);
            }

            var retrievedTransactions = transactions
            .Skip(transactionsHistory.Offset)
            .Take(transactionsHistory.Limit)
            .Select(ts => new SimpleUserTransaction()
            {
                Id = ts.ts.t.Id,
                SenderLogin = ts.ts.us.Login,
                ReceiverLogin = ts.ur.Login,
                Cents = ts.ts.t.Cents,
                Date = ts.ts.t.Date,
                Memo = ts.ts.t.Memo
            }).ToList()
            ;

            return Results.Json(new ApiResponseUserTransactions
            {
                Success = true,
                Error = "",
                Id = user.Id,
                Login = user.Login,
                Transactions = new System.Collections.ObjectModel.Collection<SimpleUserTransaction>(retrievedTransactions)
            });

        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in get transactions endpoint: " + ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error... Please try again later." });
        }
#pragma warning restore CA1031
    }

}
