using Microsoft.EntityFrameworkCore;
using Promise.Lib;
using System.Security.Cryptography;
using System.Text;

namespace Promise.Api;

internal static class RestoreAccessUseSecret
{
    private static string errorReason = string.Empty;
    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var request = await context.Request.ReadFromJsonAsync<RestoreAccessInfo>().ConfigureAwait(false);

            if (request is null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.UseData))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Username and secret word are required." });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Login == request.Username).ConfigureAwait(false);
            if (user is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }

            var personalData = await db.PersonalData.FirstOrDefaultAsync(pd => pd.UserId == user.Id).ConfigureAwait(false);
            if (personalData is null || string.IsNullOrEmpty(personalData.SecretHash))
            {
                return Results.Json(new { success = false, error = "Sorry, you did not save a secret word before." });
            }

            var secretHash = Security.GetHash(request.UseData + personalData.Salt);
            if (secretHash != personalData.SecretHash)
            {
                await TrackFailedAttempt(db, user.Id).ConfigureAwait(false);
                return Results.Json(new { success = false, error = "Incorrect secret word." });
            }

            var newPassword = Security.GenerateTemporaryPassword();
            var newPasswordHash = Security.GetPasswordHash(newPassword, user.Salt!);
            user.Password = newPasswordHash;

            var accessRestore = await db.Set<AccessRestore>().FirstOrDefaultAsync(ar => ar.UserId == user.Id).ConfigureAwait(false);
            if (accessRestore != null)
            {
                accessRestore.UseSecretTryNumber = 0;
                accessRestore.UseEmailTryNumber = 0;
                accessRestore.UseTelTryNumber = 0;
                accessRestore.UseSecretTryDate = null;
                accessRestore.UseEmailTryDate = null;
                accessRestore.UseTelTryDate = null;
                db.Set<AccessRestore>().Update(accessRestore);
            }

            db.Users.Update(user);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new { success = true, newPassword });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError($"An error occurred while restoring access. Exception: {ex.Message}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Something went wrong... " + errorReason });
        }
#pragma warning restore CA1031
    }

    private static async Task TrackFailedAttempt(PromiseDb db, long userId)
    {
        var accessRestore = await db.Set<AccessRestore>().FirstOrDefaultAsync(ar => ar.UserId == userId).ConfigureAwait(false);
        if (accessRestore == null)
        {
            accessRestore = new AccessRestore
            {
                UserId = userId,
                UseSecretTryDate = DateTime.UtcNow,
                UseSecretTryNumber = 1
            };
            db.Set<AccessRestore>().Add(accessRestore);
        }
        else
        {
            accessRestore.UseSecretTryNumber++;
            accessRestore.UseSecretTryDate = DateTime.UtcNow;
            db.Set<AccessRestore>().Update(accessRestore);
        }
        await db.SaveChangesAsync().ConfigureAwait(false);

        if (accessRestore.UseSecretTryNumber >= 3)
        {
            errorReason = "You have reached the maximum number of attempts. Please contact us or use other way to restore access.";
            throw new InvalidOperationException("Maximum number of attempts reached for user ID: " + userId);
        }
    }



}


