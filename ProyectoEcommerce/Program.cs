using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Services;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< Updated upstream
<<<<<<< Updated upstream
//CONFIGURACIÓN DE LA BASE DE DATOS
=======
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
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

//  CONFIGURACIÓN DE IDENTITY CON ROLES 
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Configuración de bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ProyectoEcommerceContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

<<<<<<< Updated upstream
<<<<<<< Updated upstream
// SERVICIOS ADICIONALES
=======
=======
>>>>>>> Stashed changes
//  CONFIGURACIÓN DE SESIÓN
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ProyectoEcommerce.Session";
});

builder.Services.AddHttpContextAccessor();

// --- Servicios adicionales ---
>>>>>>> Stashed changes
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registrar servicio del carrito
builder.Services.AddScoped<ICartService, CartService>();

// CONSTRUCCIÓN DE LA APLICACIÓN
var app = builder.Build();

// INICIALIZACIÓN DE BASE DE DATOS Y ROLES
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
<<<<<<< Updated upstream
<<<<<<< Updated upstream
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
=======
=======
>>>>>>> Stashed changes
        db.Database.Migrate();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
    {
        Console.WriteLine("Advertencia: Hay cambios pendientes en el modelo. Ejecuta 'Add-Migration'.");
    }

    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var users = sp.GetRequiredService<UserManager<IdentityUser>>();

    const string ADMIN = "Admin";
    if (!await roles.RoleExistsAsync(ADMIN))
        await roles.CreateAsync(new IdentityRole(ADMIN));

    var email = "admin@demo.com";
    var pass = "Admin123!";
    var admin = await users.FindByEmailAsync(email);
    if (admin == null)
>>>>>>> Stashed changes
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

// CONFIGURACIÓN DEL PIPELINE HTTP

// Configurar manejo de errores según el entorno
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware básico
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

<<<<<<< Updated upstream
<<<<<<< Updated upstream
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

=======
// USO DE SESIÓN
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// RUTA DEFAULT PRIMERO - ESTO ARREGLA EL PROBLEMA
>>>>>>> Stashed changes
=======
// USO DE SESIÓN
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// RUTA DEFAULT PRIMERO - ESTO ARREGLA EL PROBLEMA
>>>>>>> Stashed changes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

<<<<<<< Updated upstream
<<<<<<< Updated upstream
=======
=======
>>>>>>> Stashed changes
//  RUTA DE ÁREAS DESPUÉS
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
app.MapRazorPages();

app.Run();