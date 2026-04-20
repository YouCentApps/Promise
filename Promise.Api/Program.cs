using Promise.Api;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.OpenApi;


var builder = WebApplication.CreateBuilder(args);

// Helper method to add request body descriptions
static Func<Microsoft.OpenApi.OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> CreateRequestBodyDescriptionTransformer(string description)
{
    return (operation, context, ct) =>
    {
        operation.RequestBody?.Description = description;
        return Task.CompletedTask;
    };
}

// Add services to the container.
// Learn more about configuring OpenAPI with ASP.NET Core and Scalar at https://aka.ms/aspnetcore/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Initialize components and security schemes if null
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

        // Add Bearer token security scheme
        var bearerScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        };

        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        // Create security requirement using a referenced security scheme
        var securitySchemeRef = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document);

        var securityRequirement = new Microsoft.OpenApi.OpenApiSecurityRequirement();
        securityRequirement.Add(securitySchemeRef, new List<string>());


        // Apply to all operations
        foreach (var pathItem in document.Paths.Values)
        {
            foreach (var operation in pathItem.Operations?.Values ?? Enumerable.Empty<Microsoft.OpenApi.OpenApiOperation>())
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
    return await SignIn.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("SignIn")
.WithSummary("Sign in user")
.WithDescription("Authenticates a user and returns a JWT token.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials for authentication"));

// SignUp endpoint
app.MapPost("/signup", async (HttpContext context) =>
{
    return await SignUp.Run(context).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("SignUp")
.WithSummary("Register new user")
.WithDescription("Creates a new user account.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User information for account registration"));

// UserInfo endpoint
app.MapPost("/userinfo", async (HttpContext context) =>
{
    return await UserInfo.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("GetUserInfo")
.WithSummary("Get user information")
.WithDescription("Retrieves user information including balance and promise limit. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials to retrieve account information"));

// DataUpdate endpoint
app.MapPut("/dataupdate", async (HttpContext context) =>
{
    return await DataUpdate.Run(context).ConfigureAwait(false);
})
.Accepts<UserData>("application/json")
.WithName("UpdateUserData")
.WithSummary("Update user personal data")
.WithDescription("Updates user's personal information.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Updated user personal information data"));

// DeleteUser endpoint
app.MapDelete("/deleteuser", async (HttpContext context) =>
{
    return await DeleteUser.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("DeleteUser")
.WithSummary("Delete user account")
.WithDescription("Permanently deletes a user account. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials for account deletion"));

// UpdatePassword endpoint
app.MapPut("/updatepassword", async (HttpContext context) =>
{
    return await UpdatePassword.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<UserUpdate>("application/json")
.WithName("UpdatePassword")
.WithSummary("Update user password")
.WithDescription("Changes the user's password. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and new password information"));

// SendPromises endpoint
app.MapPost("/sendpromises", async (HttpContext context) =>
{
    return await SendPromises.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<UserTransaction>("application/json")
.WithName("SendPromises")
.WithSummary("Send promises to another user")
.WithDescription("Transfers YouCent Promises (YCP) from one user to another. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Transaction details including recipient and amount"));

// GetTransactions endpoint
app.MapPost("/gettransactions", async (HttpContext context) =>
{
    return await GetTransactions.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<TransactionsHistoryInfo>("application/json")
.WithName("GetTransactions")
.WithSummary("Get transaction history")
.WithDescription("Retrieves the user's transaction history. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and transaction history query parameters"));

// RestoreAccessUseSecret endpoint
app.MapPut("/restoreaccessusesecret", async (HttpContext context) =>
{
    return await RestoreAccessUseSecret.Run(context).ConfigureAwait(false);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithSecret")
.WithSummary("Restore access using secret word")
.WithDescription("Restores account access by verifying the user's secret word.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User identifier and secret word for account recovery"));

// RestoreAccessUseEmail endpoint
app.MapPut("/restoreaccessuseemail", async (HttpContext context) =>
{
    return await RestoreAccessUseEmail.Run(context).ConfigureAwait(false);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithEmail")
.WithSummary("Restore access using email")
.WithDescription("Restores account access by sending a verification email.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User email address for account recovery"));

// RestoreAccessUseTel endpoint
app.MapPost("/restoreaccessusetel", async (HttpContext context) =>
{
    return await RestoreAccessUseTel.Run(context).ConfigureAwait(false);
})
.Accepts<RestoreAccessInfo>("application/json")
.WithName("RestoreAccessWithPhone")
.WithSummary("Restore access using phone number")
.WithDescription("Restores account access by verifying the user's phone number.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User phone number for account recovery"));

// GetUserCurrency endpoint
app.MapPost("/getusercurrency", async (HttpContext context) =>
{
    return await GetUserCurrency.Run(context).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("GetUserCurrency")
.WithSummary("Get user's currency preference")
.WithDescription("Retrieves the user's selected currency and exchange rate.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials to retrieve currency information"));

// UpdateCurrencyPreference endpoint
app.MapPut("/updatecurrencypreference", async (HttpContext context) =>
{
    return await UpdateCurrencyPreference.Run(context).ConfigureAwait(false);
})
.Accepts<CurrencyPreferenceUpdate>("application/json")
.WithName("UpdateCurrencyPreference")
.WithSummary("Update user's currency preference")
.WithDescription("Changes the user's preferred currency for display.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and new currency ID"));

// ===== MERCHANT ENDPOINTS =====

// Merchant Registration
app.MapPost("/merchant/register", async (HttpContext context) =>
{
    return await MerchantRegister.Run(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<MerchantRegisterRequest>("application/json")
.WithName("MerchantRegister")
.WithSummary("Register as a merchant")
.WithDescription("Registers the authenticated user as a merchant and returns API credentials.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and merchant info"));

// Create Payment Request
app.MapPost("/merchant/payment-request", async (HttpContext context) =>
{
    return await MerchantCreatePaymentRequest.Run(context).ConfigureAwait(false);
})
.Accepts<CreatePaymentRequestInput>("application/json")
.WithName("MerchantCreatePaymentRequest")
.WithSummary("Create a payment request")
.WithDescription("Creates a one-time or subscription payment request. Returns a token and payment URL.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials and payment request details"));

// Get Payment Request Info (public, no auth)
app.MapGet("/pay/{token}", async (HttpContext context, string token) =>
{
    return await PayByToken.RunGet(context, token).ConfigureAwait(false);
})
.WithName("GetPaymentRequestInfo")
.WithSummary("Get payment request details")
.WithDescription("Returns public info about a payment request by token.");

// Pay by Token
app.MapPost("/pay/{token}", async (HttpContext context, string token) =>
{
    return await PayByToken.RunPost(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<PayByTokenRequest>("application/json")
.WithName("PayByToken")
.WithSummary("Pay a payment request")
.WithDescription("Executes payment for a one-time request or sets up a subscription. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials and payment token"));

// Charge Subscription
app.MapPost("/merchant/subscription/charge", async (HttpContext context) =>
{
    return await MerchantChargeSubscription.Run(context).ConfigureAwait(false);
})
.Accepts<SubscriptionChargeRequest>("application/json")
.WithName("MerchantChargeSubscription")
.WithSummary("Charge a subscription")
.WithDescription("Charges the next interval on an active subscription. Merchant API auth required.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials and subscription ID"));

// Refund
app.MapPost("/merchant/refund", async (HttpContext context) =>
{
    return await MerchantRefund.Run(context).ConfigureAwait(false);
})
.Accepts<MerchantRefundRequest>("application/json")
.WithName("MerchantRefund")
.WithSummary("Refund a transaction")
.WithDescription("Refunds a charge transaction. Merchant API auth required.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials and transaction ID to refund"));

// Merchant Query: Payment Requests
app.MapPost("/merchant/payment-requests", async (HttpContext context) =>
{
    return await MerchantQueries.GetPaymentRequests(context).ConfigureAwait(false);
})
.Accepts<MerchantApiAuth>("application/json")
.WithName("MerchantGetPaymentRequests")
.WithSummary("List merchant payment requests")
.WithDescription("Returns the merchant's payment requests. Merchant API auth required.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials"));

// Merchant Query: Subscriptions
app.MapPost("/merchant/subscriptions", async (HttpContext context) =>
{
    return await MerchantQueries.GetSubscriptions(context).ConfigureAwait(false);
})
.Accepts<MerchantApiAuth>("application/json")
.WithName("MerchantGetSubscriptions")
.WithSummary("List merchant subscriptions")
.WithDescription("Returns the merchant's subscriptions. Merchant API auth required.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials"));

// Merchant Query: Transactions
app.MapPost("/merchant/transactions", async (HttpContext context) =>
{
    return await MerchantQueries.GetTransactions(context).ConfigureAwait(false);
})
.Accepts<MerchantApiAuth>("application/json")
.WithName("MerchantGetTransactions")
.WithSummary("List merchant transactions")
.WithDescription("Returns the merchant's transaction history. Merchant API auth required.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("Merchant API credentials"));

// User Subscriptions: List
app.MapPost("/user/subscriptions", async (HttpContext context) =>
{
    return await UserSubscriptions.GetSubscriptions(context, jwtSecret).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("UserGetSubscriptions")
.WithSummary("List user subscriptions")
.WithDescription("Returns the user's active subscriptions. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials"));

// User Subscriptions: Cancel
app.MapDelete("/user/subscriptions/{subscriptionId}", async (HttpContext context, long subscriptionId) =>
{
    return await UserSubscriptions.CancelSubscription(context, jwtSecret, subscriptionId).ConfigureAwait(false);
})
.Accepts<User>("application/json")
.WithName("UserCancelSubscription")
.WithSummary("Cancel a subscription")
.WithDescription("Cancels an active subscription. Requires authentication.")
.AddOpenApiOperationTransformer(CreateRequestBodyDescriptionTransformer("User credentials"));

// RUN!
app.Run();
