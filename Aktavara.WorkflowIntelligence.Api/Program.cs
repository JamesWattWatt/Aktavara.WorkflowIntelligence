using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register core services
builder.Services.AddScoped<IActivityLogParser, ActivityLogParser>();
builder.Services.AddScoped<IWorkflowProvider, StaticWorkflowProvider>();
builder.Services.AddScoped<IWorkflowMatcher, WorkflowMatcher>();
builder.Services.AddScoped<IAssistantContextBuilder, AssistantContextBuilder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
