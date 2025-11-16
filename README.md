InnovaTech – E-Commerce ASP.NET Core MVC

InnovaTech es una plataforma de comercio electrónico desarrollada en ASP.NET Core MVC 8, que permite la gestión completa de productos electrónicos, categorías, clientes, empleados, carritos de compra, compras, cupones, reseñas y más.
Incluye autenticación con Identity, integración con Google Maps, sistema de roles, carrito persistente por sesión y panel administrativo.

 Características principales
 
 Frontend
Diseño responsivo usando Bootstrap 5.

Home con:
Productos destacados dinámicos
Botones clickables de las sucursales
Reseñas de usuarios
Carrusel de productos con imágenes dinámicas.
Barra de búsqueda funcional.
Sistema de accesibilidad: modo oscuro, aumento de fuentes, contraste.

Autenticación y Roles (Identity)
Registro e inicio de sesión con ASP.NET Core Identity.

Roles:
Admin
Customer
Protección de vistas mediante autorización [Authorize(Roles="Admin")].

 Carrito de compras
Carrito persistente por sesión.
Agregar / Editar / Eliminar artículos.
Vista previa del carrito (ViewComponent).
Cálculo de subtotal, IVA y total.
Aplicación de cupones de descuento.

 Sistema de Compras
Registro automático de órdenes.
Factura detallada (Buy/BuyItem).
Descuentos por promociones.
Historial de compras.

 Reseñas
Los clientes pueden dejar reseñas en productos o generales.
Reseñas visibles en Home

Google Maps
Mapa interactivo que muestra la ubicación de la tienda.
API JavaScript integrada desde wwwroot/js/maps.js.

 Panel Administrativo

Incluye CRUD para:

Productos
Categorías
Clientes
Empleados
Carritos
Compras
Preguntas Frecuentes (FAQ)
Cupones
