using Ecommerce.Api.Data;
using Ecommerce.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<Collections>();
builder.Services.AddSingleton<OutboxChannel>();

builder.Services.AddSingleton<WebhookSigner>();
builder.Services.AddSingleton<IdempotencyService>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddHttpClient(); // default factory

builder.Services.AddHostedService<PaymentSimulationWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// BaseUrl for worker to call back (optional)
// builder.Configuration["Webhook:BaseUrl"] = "http://localhost:5000";

// The Middleware Pipeline - requests flow through these in order
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware pipeline order matters!
// 1. HTTPS redirection
// 2. Routing
// 3. Authorization
// Why - because middleware can short circuit the pipeline
// If a middleware short circuits, it means it doesn't let the request continue to the next middleware in the pipeline
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
