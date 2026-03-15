namespace Promise.Api;

public class SendPromises
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
            var userTransaction = await context.Request.ReadFromJsonAsync<UserTransaction>();
            if (
                userTransaction is null || userTransaction.Cents < 1 ||
                userTransaction.Sender is null || userTransaction.Sender.Login is null || userTransaction.Sender.Password is null ||
                userTransaction.Receiver is null || userTransaction.Receiver.Login is null
            )
            {
                MainLogger.LogError("Error reading promise transaction from send promises request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }
            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (jwt is null || jwt.Length < 1)
            {
                MainLogger.LogError("No JWT provided for send promises request for sender with ID: " + userTransaction.Sender.Id);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Looks like you are not signed in, sorry." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var sender = db.Users.FirstOrDefault(u => u.Id == userTransaction.Sender.Id);
            var receiver = db.Users.FirstOrDefault(u => u.Login == userTransaction.Receiver.Login);
            if (sender is null || sender.Salt is null || sender.Login is null || sender.Password is null)
            {
                MainLogger.LogError("User not found for send promises request for sender with ID: " + userTransaction.Sender.Id);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Something went wrong. Please try again later." });
            }
            if (receiver is null || receiver.Login is null)
            {
                MainLogger.LogError("User not found for send promises request for receiver with username: " + userTransaction.Receiver.Login);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "The user you want to send Promises was not found. Please double check his/her username or try again later..." });
            }

            if (!Security.ValidateBearerAccessToken(jwt, sender.Login, jwtSecret))
            {
                MainLogger.LogError("Invalid JWT provided for send promises request for username: " + sender.Login);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Your session is expired or invalid, please sign in again." });
            }

            var hash = Security.GetPasswordHash(userTransaction.Sender.Password, sender.Salt);
            if (hash != sender.Password)
            {
                MainLogger.LogError("Wrong password for send promises request for username: " + sender.Login);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { success = false, error = "Wrong password, sorry!" });
            }

            if (sender.Id == receiver.Id)
            {
                MainLogger.LogError("Sender and receiver cannot be the same for send promises request for username: " + sender.Login);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Sender and receiver cannot be the same." });
            }

            var balance = db.Balances.FirstOrDefault(b => b.UserId == sender.Id);
            var limit = db.PromiseLimits.FirstOrDefault(l => l.UserId == sender.Id);
            if (balance is null || limit is null || (balance.Cents + limit.Cents) < userTransaction.Cents)
            {
                MainLogger.LogError("Not enough promises for send promises request for username: " + sender.Login);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { success = false, error = "Sorry! You don't have enough Promises (balance and limit combined) to send this amount." });
            }
            var recieverBalance = db.Balances.FirstOrDefault(b => b.UserId == receiver.Id);
            if (recieverBalance is null)
            {
                MainLogger.LogError("Receiver balance not found for send promises request for username: " + receiver.Login);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Something went wrong. Please try again later." });
            }
            var transaction = new PromiseTransaction
            {
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Cents = userTransaction.Cents,
                Date = DateTime.Now,
                Memo = userTransaction.Memo ?? "",
                IsBlockchain = false,
                Hash = ""
            };
            db.PromiseTransactions.Add(transaction);
            balance.Cents -= userTransaction.Cents;
            recieverBalance.Cents += userTransaction.Cents;
            db.Entry(balance).Property(x => x.Cents).IsModified = true;
            db.Entry(recieverBalance).Property(x => x.Cents).IsModified = true;
            await db.SaveChangesAsync();
            return Results.Json(new { success = true, error = "" });
        }
        catch (Exception ex)
        {
            MainLogger.LogError("Error in send promises request: " + ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error... Please try again later." });
        }

    }
}
