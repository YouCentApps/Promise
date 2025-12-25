using Promise.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
.WithOpenApi();

// SignIn endpoint
app.MapPost("/signin", async (HttpContext context) =>
{
    //var secret = configuration["Jwt:Secret"];
#pragma warning disable CS0612 // Type or member is obsolete
    return await SignIn.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json", "User data for Sign In")
.WithOpenApi();

// SignUp endpoint
app.MapPost("/signup", async (HttpContext context) =>
{
    return await SignUp.Run(context);
})
.Accepts<User>("application/json", "User data for Sign Up")
.WithOpenApi();

// UserInfo endpoint
app.MapPost("/userinfo", async (HttpContext context) =>
{
    //var secret = configuration["Jwt:Secret"];
#pragma warning disable CS0612 // Type or member is obsolete
    return await UserInfo.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json", "User data for User Info")
.WithOpenApi();

// DataUpdate endpoint
app.MapPut("/dataupdate", async (HttpContext context) =>
{
    return await DataUpdate.Run(context);
})
.Accepts<UserData>("application/json", "User data for Data Update")
.WithOpenApi();

// DeleteUser endpoint
app.MapDelete("/deleteuser", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await DeleteUser.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<User>("application/json", "User data for Delete User")
.WithOpenApi();

// UpdatePassword endpoint
app.MapPut("/updatepassword", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await UpdatePassword.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<UserUpdate>("application/json", "User data for Update Password")
.WithOpenApi();

// SendPromises endpoint
app.MapPost("/sendpromises", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await SendPromises.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<UserTransaction>("application/json", "User data for Send Promises")
.WithOpenApi();

// GetTransactions endpoint
app.MapPost("/gettransactions", async (HttpContext context) =>
{
#pragma warning disable CS0612 // Type or member is obsolete
    return await GetTransactions.Run(context, jwtSecret);
#pragma warning restore CS0612 // Type or member is obsolete
})
.Accepts<TransactionsHistoryInfo>("application/json", "Info info Get Transactions")
.WithOpenApi();

// RestoreAccessUseSecret endpoint
app.MapPut("/restoreaccessusesecret", async (HttpContext context) =>
{
    return await RestoreAccessUseSecret.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json", "Data for restoring access using secret word")
.WithOpenApi();

// RestoreAccessUseEmail endpoint
app.MapPut("/restoreaccessuseemail", async (HttpContext context) =>
{
    return await RestoreAccessUseEmail.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json", "Data for restoring access using email")
.WithOpenApi();

// RestoreAccessUseTel endpoint
app.MapPost("/restoreaccessusetel", async (HttpContext context) =>
{
    return await RestoreAccessUseTel.Run(context);
})
.Accepts<RestoreAccessInfo>("application/json", "Data for restoring access using telephone number")
.WithOpenApi();


// RUN!
app.Run();
