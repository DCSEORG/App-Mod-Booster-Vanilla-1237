using ExpenseApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Add API controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Expense Management API",
        Version = "v1",
        Description = "REST API for the modern expense management application"
    });
});

// Register services
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IChatService, ChatService>();

// HttpClient for internal use if needed
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Redirect root to Index
app.MapGet("/", () => Results.Redirect("/Index"));

app.Run();
