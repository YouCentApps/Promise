using Promise.Api;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Helper method to add request body descriptions
static Func<Microsoft.OpenApi.OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> CreateRequestBodyDescriptionTransformer(string description)
{
    return (operation, context, ct) =>
    {
        if (operation.RequestBody != null)
        {
            operation.RequestBody.Description = description;
        }
        return Task.CompletedTask;
    };
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Initialize components and security schemes if null
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        
        // Add Bearer token security scheme
        var bearerScheme = new Microsoft.OpenApi.OpenApiSecurityScheme();
        bearerScheme.Type = Microsoft.OpenApi.SecuritySchemeType.Http;
        bearerScheme.Scheme = "bearer";
        bearerScheme.BearerFormat = "JWT";
        bearerScheme.Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"";
        
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        // Create security requirement reference
        var securitySchemeRef = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document);
        
        var securityRequirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        securityRequirement.Add(securitySchemeRef, new List<string>());

        // Apply to all operations
        foreach (var pathItem in document.Paths.Values)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                // Initialize Security collection if null
                operation.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();
                operation.Security.Add(securityRequirement);
            }
        }

        return Task.CompletedTask;
    });
});

// Add DbContext to the DI container
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PromiseDb>(options => options.UseSqlServer(connectionString));
var mailSettings = configuration.GetSection(nameof(MailSettings));
builder.Services.Configure<MailSettings>(mailSettings);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("YouCent Promise API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}
else
{
    app.UseHttpsRedirection();
}

string? jwtSecret = configuration["Jwt:Secret"];

// MinVerSup endpoint. It checks if the version of the APP is supported.
app.MapGet("/minversup", () =>
{
    return new { major = 2, minor = 0, build = 10 };
})
.WithName("GetMinimumVersion")
.WithSummary("Check minimum supported app version")
.WithDescription("Returns the minimum version of the mobile app that is currently supported.");

// SignIn endpoint
app.MapPost("/signin", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await SignIn.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json")
.WithName("SignIn")
.WithSummary("Sign in user")
.WithDescription("Authenticates a user and returns a JWT token.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials for authentication"));

// SignUp endpoint
app.MapPost("/signup", async (HttpContext context) =>
{
    return await SignUp.Run(context);
})
.Accepts<User>("application/json")
.WithName("SignUp")
.WithSummary("Register new user")
.WithDescription("Creates a new user account.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User information for account registration"));

// UserInfo endpoint
app.MapPost("/userinfo", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await UserInfo.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json")
.WithName("GetUserInfo")
.WithSummary("Get user information")
.WithDescription("Retrieves user information including balance and promise limit. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials to retrieve account information"));

// DataUpdate endpoint
app.MapPut("/dataupdate", async (HttpContext context) =>
{
    return await DataUpdate.Run(context);
})
.Accepts<UserData>("application/json")
.WithName("UpdateUserData")
.WithSummary("Update user personal data")
.WithDescription("Updates user's personal information.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Updated user personal information data"));

// DeleteUser endpoint
app.MapDelete("/deleteuser", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await DeleteUser.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json")
.WithName("DeleteUser")
.WithSummary("Delete user account")
.WithDescription("Permanently deletes a user account. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials for account deletion"));

// UpdatePassword endpoint
app.MapPut("/updatepassword", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await UpdatePassword.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<UserUpdate>("application/json")
.WithName("UpdatePassword")
.WithSummary("Update user password")
.WithDescription("Changes the user's password. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and new password information"));

// SendPromises endpoint
app.MapPost("/sendpromises", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await SendPromises.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<UserTransaction>("application/json")
.WithName("SendPromises")
.WithSummary("Send promises to another user")
.WithDescription("Transfers YouCent Promises (YCP) from one user to another. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Transaction details including recipient and amount"));

// GetTransactions endpoint
app.MapPost("/gettransactions", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await GetTransactions.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<TransactionsHistoryInfo>("application/json")
.WithName("GetTransactions")
.WithSummary("Get transaction history")
.WithDescription("Retrieves the user's transaction history. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and transaction history query parameters"));

// RestoreAccessUseSecret endpoint
app.MapPut("/restoreaccessusesecret", async (HttpContext context) =>
{
    return await RestoreAccessUseSecret.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithSecret")
.WithSummary("Restore access using secret word")
.WithDescription("Restores account access by verifying the user's secret word.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User identifier and secret word for account recovery"));

// RestoreAccessUseEmail endpoint
app.MapPut("/restoreaccessuseemail", async (HttpContext context) =>
{
    return await RestoreAccessUseEmail.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithEmail")
.WithSummary("Restore access using email")
.WithDescription("Restores account access by sending a verification email.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User email address for account recovery"));

// RestoreAccessUseTel endpoint
app.MapPost("/restoreaccessusetel", async (HttpContext context) =>
{
    return await RestoreAccessUseTel.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithPhone")
.WithSummary("Restore access using phone number")
.WithDescription("Restores account access by verifying the user's phone number.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User phone number for account recovery"));

// RUN!
app.Run();
