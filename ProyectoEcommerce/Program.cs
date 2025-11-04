using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Data;
using ProyectoEcommerce.Services;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

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

//  CONFIGURACI?N DE IDENTITY CON ROLES 
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Configuraci?n de bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Configuraci?n de usuario
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ProyectoEcommerceContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ProyectoEcommerce.Session";
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registrar servicio del carrito
builder.Services.AddScoped<ICartService, CartService>();

// CONSTRUCCI?N DE LA APLICACI?N
var app = builder.Build();

// INICIALIZACI?N DE BASE DE DATOS Y ROLES
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
    catch (Exception ex) { 

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

// CONFIGURACI?N DEL PIPELINE HTTP

// Configurar manejo de errores seg?n el entorno
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware b?sico
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


// USO DE SESI?N
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();


app.UseSession();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


app.MapRazorPages();

app.Run();