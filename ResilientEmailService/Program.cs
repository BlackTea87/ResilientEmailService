using ResilientEmailService.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add email services
builder.Services.AddSingleton<IEmailProvider, MockEmailProvider1>();
builder.Services.AddSingleton<IEmailProvider, MockEmailProvider2>();
builder.Services.AddSingleton<EmailService>();

// Add queue service
builder.Services.AddSingleton<EmailQueueService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<EmailQueueService>());

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

// Add Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();