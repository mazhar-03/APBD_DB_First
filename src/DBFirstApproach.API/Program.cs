using System.Text.Json;
using DBFirstApproach.API;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("UniversityConnection")
                       ?? throw new InvalidOperationException("University connection string not found");
builder.Services.AddDbContext<DeviceContext>(options => options.UseSqlServer(connectionString));
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

//Get all devices, which returns: ID and Name
app.MapGet("/api/devices", async (DeviceContext db) =>
{
    try
    {
        var devices = await db.Devices
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();
        return Results.Ok(devices);
    }
    catch
    {
        return Results.BadRequest();
    }
});

//Get device by ID, which returns: §Device type name, §Is device enabled,
//§Additional properties (in JSON response, it must have object type)
app.MapGet("/api/devices/{id}", async (DeviceContext db, int id) =>
{
    try
    {
        var device = await db.Devices
            .Include(d => d.DeviceType)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return Results.NotFound($"Device with ID {id} not found.");

        //current user must not be return the device
        var currentUser = await db.DeviceEmployees
            .Include(de => de.Employee)
            .Include(de => de.Employee.Person)
            .FirstOrDefaultAsync(de => de.Id == id && de.ReturnDate == null);

        var employee = currentUser != null
            ? new
            {
                id = currentUser.Employee.Id,
                name = $"{currentUser.Employee.Person.FirstName} {currentUser.Employee.Person.MiddleName} {currentUser.Employee.Person.LastName}"
            } 
            : null;

        object additionalProps;
        try
        {
            additionalProps = JsonSerializer.Deserialize<object>(device.AdditionalProperties);
        }
        catch
        {
            additionalProps = null;
        }

        var result = new
        {
            deviceTypeName = device.DeviceType?.Name,
            isEnabled = device.IsEnabled,
            properties = additionalProps,
            employeeInfo = employee
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.Run();