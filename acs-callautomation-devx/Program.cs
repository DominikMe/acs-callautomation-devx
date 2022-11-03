using Azure.Communication.CallAutomation;
using CallAutomation.DevX;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["CommunicationConnectionString"]));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.AddCurrentApproach();
app.AddEventAwaiter();
app.AddImplicitEventHandling();

app.Run();
