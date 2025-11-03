using Ecommerce.Api.Data;
using Ecommerce.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
