using System.Text.Json;
using DBFirstApproach.API;
using DBFirstApproach.API.DTOs;
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

app.MapGet("/api/devices", async (DeviceContext db) =>
{
    try
    {
        var devices = await db.Devices
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();
        return Results.Ok(devices);
    }
    catch(Exception ex)
    {
        return Results.Problem($"Loading devices FAILED: {ex.Message}");
    }
});

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
            .FirstOrDefaultAsync(de => de.DeviceId == id && de.ReturnDate == null);

        var employee = currentUser != null
            ? new
            {
                id = currentUser.Employee.Id,
                name =
                    $"{currentUser.Employee.Person.FirstName} {currentUser.Employee.Person.MiddleName} {currentUser.Employee.Person.LastName}"
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
            device.Name,
            deviceTypeName = device.DeviceType?.Name,
            isEnabled = device.IsEnabled,
            properties = additionalProps,
            employeeInfo = employee
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Loading SPECIFIC device FAILED: {ex.Message}");
    }
});

app.MapPost("/api/devices", async (DeviceCreationAndUpdateDto dto, DeviceContext dbContext) =>
{
    try
    {
        var type = await dbContext.DeviceTypes
            .FirstOrDefaultAsync(t => t.Name == dto.DeviceTypeName);

        if (type == null)
            return Results.BadRequest($"Device type '{dto.DeviceTypeName}' not EXIST.");

        var newDevice = new Device
        {
            Name = dto.Name,
            IsEnabled = dto.IsEnabled,
            AdditionalProperties = dto.AdditionalProperties,
            DeviceTypeId = type.Id
        };

        await dbContext.Devices.AddAsync(newDevice);
        await dbContext.SaveChangesAsync();

        return Results.Created();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPut("/api/devices/{id}", async (int id, DeviceCreationAndUpdateDto dto, DeviceContext dbContext) =>
{
    try
    {
        var device = await dbContext.Devices.FindAsync(id);

        if (device == null)
            return Results.NotFound($"Device with ID {id} not found.");

        var type = await dbContext.DeviceTypes
            .FirstOrDefaultAsync(t => t.Name == dto.DeviceTypeName);

        if (type == null)
            return Results.BadRequest($"Device type '{dto.DeviceTypeName}' not EXIST.");

        device.Name = dto.Name;
        device.IsEnabled = dto.IsEnabled;
        device.AdditionalProperties = dto.AdditionalProperties;
        device.DeviceTypeId = type.Id;

        await dbContext.SaveChangesAsync();

        return Results.Ok("Device updated successfully.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Update failed: {ex.Message}");
    }
});

// Since it wasn't specified, instead of deleting child rows, i decided to send an error if we try to delete a device that belongs to any employee
app.MapDelete("/api/devices/{id}", async (DeviceContext dbContext, int id) =>
{
    try
    {
        var isAssigned = dbContext.DeviceEmployees
            .Any(de => de.DeviceId == id);
        if (isAssigned)
        {
            return Results.BadRequest(
                $"Device {id} can not be deleted because it is associated with an employee.");
        }

        var device = await dbContext.Devices.FindAsync(id);
        if (device == null)
            return Results.NotFound($"Device with ID {id} not found.");

        dbContext.Devices.Remove(device);
        await dbContext.SaveChangesAsync();
        return Results.Ok("Device deleted successfully.");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"DELETE FAILED: {ex.Message}");
    }
});


app.MapGet("/api/employees", async (DeviceContext dbContext) =>
{
    try
    {
        var employees = await dbContext.Employees
            .Include(e => e.Person)
            .Select(e => new
            {
                id = e.Id,
                Name = e.Person.FirstName + " " + e.Person.MiddleName + " " + e.Person.LastName
            })
            .ToListAsync();

        return Results.Ok(employees);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Loading employees FAILED: {ex.Message}");
    }
});

app.MapGet("/api/employees/{id}", async (DeviceContext dbContext, int id) =>
{
    try
    {
        var employee = await dbContext.Employees
            .Include(e => e.Person)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);
        ;
        if (employee == null)
            return Results.NotFound($"Employee with ID {id} not found.");

        var result = new
        {
            employee.Person.FirstName,
            employee.Person.MiddleName,
            employee.Person.LastName,
            employee.Person.PassportNumber,
            employee.Person.PhoneNumber,
            employee.Person.Email,
            salary = employee.Salary,
            positionInfo = new
            {
                id = employee.Position.Id,
                name = employee.Position.Name,
            },
            hireDate = employee.HireDate,
        };
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Loading SPECIFIC employee FAILED: {ex.Message}");
    }
});

app.Run();