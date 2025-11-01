using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;

var builder = WebApplication.CreateBuilder(args);

//CONFIGURACI�N DE LA BASE DE DATOS
builder.Services.AddDbContext<ProyectoEcommerceContext>(opciones =>
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

//  CONFIGURACI�N DE IDENTITY CON ROLES 
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configuraci�n de cuenta
    options.SignIn.RequireConfirmedAccount = false; // Cambiar a true en producci�n

    // Configuraci�n de contrase�a
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Configuraci�n de bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configuraci�n de usuario
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ProyectoEcommerceContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// SERVICIOS ADICIONALES
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// CONSTRUCCI�N DE LA APLICACI�N
var app = builder.Build();

// INICIALIZACI�N DE BASE DE DATOS Y ROLES
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // 1. Aplicar migraciones pendientes
        var context = services.GetRequiredService<ProyectoEcommerceContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("Migraciones aplicadas correctamente");

        // 2. Inicializar roles y administrador usando DbInitializer
        await DbInitializer.Initialize(services);
        Console.WriteLine("Roles y usuario administrador inicializados");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
    {
        Console.WriteLine("Advertencia: Hay cambios pendientes en el modelo. Ejecuta 'dotnet ef migrations add [NombreMigracion]'");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");

        // Mostrar error en consola para debugging
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Detalle: {ex.InnerException.Message}");
        }
    }
}

// CONFIGURACI�N DEL PIPELINE HTTP

// Configurar manejo de errores seg�n el entorno
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware b�sico
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

//ORDEN CR�TICO: Authentication debe ir ANTES de Authorization
app.UseAuthentication();
app.UseAuthorization();

// CONFIGURACI�N DE RUTAS

// Ruta para �reas (Admin)
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear p�ginas Razor (Identity)
app.MapRazorPages();

app.Run();