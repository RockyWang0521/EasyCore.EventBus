using EasyCore.Pulsar;
using Web.Infra.Pulsar.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EasyCore.Pulsar Infra Demo", Version = "v1" });
});

var pulsar = builder.Configuration.GetSection("Pulsar");
builder.Services.EasyCorePulsar(o =>
{
    o.ServiceUrl = pulsar["ServiceUrl"] ?? "pulsar://localhost:6650";
    o.TopicPrefix = pulsar["TopicPrefix"] ?? "persistent://public/default/";
    o.AppName = pulsar["AppName"] ?? "Web.Infra.Pulsar";
    o.EnableClientLog = pulsar.GetValue("EnableClientLog", false);
});

builder.Services.AddHostedService<PulsarSubscribeHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
