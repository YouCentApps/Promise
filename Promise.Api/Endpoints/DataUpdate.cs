namespace Promise.Api;
internal static class DataUpdate
{
    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            UserData? userData = null;
            try
            {
                userData = await context.Request.ReadFromJsonAsync<UserData>().ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error reading user and personal data from request : " + ex);
                return Results.Json(new { success = false, error = "Server error..." });
            }
#pragma warning restore CA1031

            var user = userData?.User;
            if (userData is null || user is null || user.Id < 1 ||
                string.IsNullOrWhiteSpace(user.Login) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                MainLogger.LogError("Error reading user from user data update request");
                return Results.Json(new { success = false, error = "No data or wrong data provided for User" });
            }

            var personalData = userData.PersonalData;
            if (personalData is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                MainLogger.LogError("Error reading personal data from personal data update request");
                return Results.Json(new { success = false, error = "No data or wrong data provided for PersonalData" });
            }

            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Login == user.Login).ConfigureAwait(false);
            if (dbUser is null || dbUser.Password is null || dbUser.Salt is null || dbUser.Id != user.Id)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found or login and Id don't match" });
            }
            var hash = Security.GetPasswordHash(user.Password, dbUser.Salt);
            if (hash != dbUser.Password)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { auth = false, error = "Wrong password!" });
            }

            var dbPersonalData = await db.PersonalData.FirstOrDefaultAsync(pd => pd.UserId == dbUser.Id).ConfigureAwait(false);
            string salt;
            if (dbPersonalData is null)
            {
                salt = Security.GetSalt();
                dbPersonalData = new PersonalData
                {
                    UserId = dbUser.Id,
                    Email = "",
                    Tel = "",
                    Secret = "",
                    EmailHash = "",
                    TelHash = "",
                    SecretHash = "",
                    Salt = salt,
                    EmailMasked = "",
                    TelMasked = ""
                };
                db.PersonalData.Add(dbPersonalData);
            }
            else
            {
                if(string.IsNullOrWhiteSpace(dbPersonalData.Salt))
                {
                    dbPersonalData.Salt = Security.GetSalt();
                }
                salt = dbPersonalData.Salt;
            }

            if (personalData.Email is not null)
            {
                dbPersonalData.Email = personalData.Email;
                if(!string.IsNullOrWhiteSpace(personalData.Email))
                {
                    dbPersonalData.EmailHash = Security.GetHash(personalData.Email + salt);
                    dbPersonalData.EmailMasked = Security.GetMaskedEmail(personalData.Email);
                }
                else
                {
                    dbPersonalData.EmailHash = "";
                    dbPersonalData.EmailMasked = "";
                }
            }
            if (personalData.Tel is not null)
            {
                dbPersonalData.Tel = personalData.Tel;
                if (!string.IsNullOrWhiteSpace(personalData.Tel))
                {
                    dbPersonalData.TelHash = Security.GetHash(personalData.Tel + salt);
                    dbPersonalData.TelMasked = Security.GetMaskedTel(personalData.Tel);
                }
                else
                {
                    dbPersonalData.TelHash = "";
                    dbPersonalData.TelMasked = "";
                }
            }
            if (personalData.Secret is not null)
            {
                dbPersonalData.Secret = personalData.Secret;
                if (!string.IsNullOrWhiteSpace(personalData.Secret))
                {
                    dbPersonalData.SecretHash = Security.GetHash(personalData.Secret + salt);
                }
                else
                {
                    dbPersonalData.SecretHash = "";
                }
            }
            var records = await db.SaveChangesAsync().ConfigureAwait(false);
            if (records < 1)
            {
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                MainLogger.LogError("Error saving personal data to DB. Possibly trying to save same personal data which is already there.");
                return Results.Json(new { success = false, error = "Nothing saved to the DB most likely because you try to save same data again." });
            }
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Results.Json(new ApiResponse
            {
                Success = true,
                Error = ""
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error updating user data : " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}