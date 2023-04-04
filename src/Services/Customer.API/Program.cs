using Common.Logging;
using Contracts.Common.Interfaces;
using Customer.API.Persistence;
using Customer.API.Repositories;
using Customer.API.Repositories.Interfaces;
using Customer.API.Services;
using Customer.API.Services.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);

Log.Information("Starting Customer API up");

try
{
  // Add services to the container.

  builder.Services.AddControllers();
  // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen();

  var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionString");
  builder.Services.AddDbContext<CustomerContext>(options => options.UseNpgsql(connectionString));
  // Add inject
  builder.Services.AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBaseAsync<,,>))
                     .AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
                     .AddScoped<ICustomerRepository, CustomerRepository>()
                     .AddScoped<ICustomerService, CustomerService>()
                     ;

  var app = builder.Build();

  app.MapGet("/", () => "Welcome to customer API");
  app.MapGet("/api/customers", async (ICustomerService customerService) => await customerService.GetCustomersAsync());
  app.MapGet("/api/customers/{userName}", async (string userName, ICustomerService customerService) =>
  {
    var customer = await customerService.GetCustomerByUserNameAsync(userName);
    return customer != null ? Results.Ok(customer) : Results.NoContent();
  });

  app.MapDelete("api/customers/{id}", (int id, ICustomerService customerService) => customerService.DeleteCustomerAsync(id));

  // Phan duoi nay nho tao dto va su dung mapper nhe o day tui luoi :>
  app.MapPost("/api/customers", (Customer.API.Entities.Customer customer, ICustomerService customerService) 
    => customerService.CreateCustomerAsync(customer));

  app.MapPut("/api/customers", (Customer.API.Entities.Customer customer, ICustomerService customerService) 
    => customerService.UpdateCustomerAsync(customer));

  // Configure the HTTP request pipeline.
  if (app.Environment.IsDevelopment())
  {
    app.UseSwagger();
    app.UseSwaggerUI();
  }

  app.UseHttpsRedirection();

  app.UseAuthorization();

  app.MapControllers();

  app.SeedCustomerData();

  app.Run();
}
catch (Exception ex)
{
  string type = ex.GetType().Name;
  if (type.Equals("HostAbortedException", StringComparison.Ordinal)) throw;

  Log.Fatal(ex, "Unhandlerd exception");
}
finally
{
  Log.Information("Shut down Customer API complete");
  Log.CloseAndFlush();
}

