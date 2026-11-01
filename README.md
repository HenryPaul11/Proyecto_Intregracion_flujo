# Sistema de renta de películas con Sakila

Aplicación académica en ASP.NET Core MVC (.NET 10), Razor, Bootstrap y SQL Server. El acceso a datos se implementa con ADO.NET y Microsoft.Data.SqlClient, sin Entity Framework.

Repositorio: https://github.com/HenryPaul11/Proyecto_Intregracion_flujo

## Funcionalidades

- Inicio y cierre de sesión; contraseñas protegidas mediante PasswordHasher de ASP.NET Core Identity (PBKDF2 y sal individual).
- Session conserva usuario, nombre, rol y la relación con el cliente o empleado.
- Autorización mediante filtros: escribir una URL administrativa como cliente devuelve HTTP 403.
- Catálogo con búsqueda por título, detalle, disponibilidad y paginación.
- Creación y edición de películas; desactivación y reactivación mediante eliminación lógica.
- Inventario por película y tienda. No se puede retirar una copia con alquiler pendiente.
- Alquiler automático de una copia disponible y consulta de los alquileres del cliente autenticado.
- Devolución propia, una sola vez, sin eliminar el historial.
- Consulta administrativa de alquileres y reporte de las diez películas más alquiladas.
- Validaciones de servidor con DataAnnotations y ModelState; formularios con protección antifalsificación.

## Requisitos

- SDK de .NET 10.
- SQL Server con la base Sakila usada en clases, con las tablas dbo.film, inventory, rental, customer, staff, language y store.
- Windows PowerShell 5.1 para los scripts de apoyo incluidos.
- Un cliente Sakila activo y un empleado activo asociados a las cuentas de prueba.

La aplicación se verificó con la variante SQL Server cuyos identificadores son IDENTITY y cuyo customer.active contiene '1' o 'Y' para clientes activos. No se incluye una copia de datos personales ni una base de datos dentro del repositorio.

## Preparación

Desde la raíz del repositorio:

1. Abra Sakila en SQL Server Management Studio y ejecute sql/01_integracion.sql sobre esa base. También puede usar:

   ~~~powershell
   .\scripts\Invoke-Sql.ps1 -File .\sql\01_integracion.sql -Server localhost -Database sakila
   ~~~

   El script agrega dbo.Usuarios y los campos activo de film e inventory. Mantiene los datos y el historial existentes y puede ejecutarse nuevamente.

2. Revise la conexión. La configuración predeterminada usa autenticación de Windows:

   ~~~text
   Server=localhost;Database=sakila;Integrated Security=True;TrustServerCertificate=True
   ~~~

   Para otra instancia o base, use la variable de entorno ConnectionStrings__Sakila o el archivo excluido ProyectoU1/appsettings.Local.json:

   ~~~json
   {
     "ConnectionStrings": {
       "Sakila": "Server=MI_SERVIDOR;Database=MI_SAKILA;Integrated Security=True;TrustServerCertificate=True"
     }
   }
   ~~~

   TrustServerCertificate=True se utiliza para el SQL Server local de desarrollo. Para un servidor desplegado, configure un certificado válido y su conexión correspondiente. Nunca publique credenciales SQL en archivos versionados.

3. Restaure y compile:

   ~~~powershell
   dotnet restore ProyectoU1.slnx
   dotnet build ProyectoU1.slnx --no-restore
   ~~~

4. Con la aplicación detenida, cree las cuentas. Cada comando pide una contraseña de 12 a 128 caracteres sin mostrarla:

   ~~~powershell
   .\scripts\New-Usuario.ps1 -Login administrador -Nombre "Administrador" -Rol Administrador -IdSakila 1
   .\scripts\New-Usuario.ps1 -Login cliente -Nombre "Cliente" -Rol Cliente -IdSakila 1
   ~~~

   IdSakila corresponde a staff_id para Administrador y a customer_id para Cliente. Seleccione registros existentes y activos de su propia base. El login debe ser único. Las cuentas se crean desde esta herramienta local, no mediante registro público.

5. Ejecute la aplicación:

   ~~~powershell
   dotnet run --project ProyectoU1 --launch-profile http
   ~~~

   Abra http://localhost:5236/Cuenta/Login e ingrese con las cuentas creadas. El perfil https también está disponible en launchSettings.json para un entorno con certificado de desarrollo configurado.

## Recorrido de uso

**Administrador:** ingrese, abra Administración, cree o edite una película y agregue copias en una tienda con un empleado activo. Puede retirar copias libres, desactivar o reactivar películas, consultar los alquileres y abrir el reporte.

**Cliente:** ingrese, busque una película en Catálogo, abra su detalle y pulse Alquilar una copia. En Mis alquileres puede devolver sus alquileres pendientes. El formulario no permite seleccionar al cliente; el servidor toma customer_id de Session.

La sesión vence tras 30 minutos de inactividad. Los intentos de inicio de sesión se limitan por dirección IP.

## Disponibilidad y reglas de negocio

Una película tiene varias copias en inventory. Una copia activa está disponible cuando no existe un rental de esa copia con return_date IS NULL. Las películas inactivas no aparecen en el catálogo del cliente.

Al alquilar se verifica que la película y el cliente estén activos y que la copia pertenezca a una tienda con un empleado activo. Se utiliza una transacción SERIALIZABLE y un bloqueo UPDLOCK/HOLDLOCK sobre la película para serializar las asignaciones. El retiro de copias utiliza el mismo bloqueo.

La devolución actualiza únicamente el alquiler cuyo customer_id corresponde a la sesión y cuya return_date todavía es NULL. Las devoluciones ajenas o repetidas no modifican datos.

La eliminación lógica de películas e inventario mantiene las relaciones y permite consultar el historial. El reporte considera todos los alquileres históricos, incluso los de películas o copias retiradas.

## Consultas de exploración

sql/02_exploracion.sql incluye los tres enunciados seleccionados:

1. Diez películas con más alquileres.
2. Cinco clientes con más alquileres.
3. Diez películas con al menos una copia disponible.

~~~powershell
.\scripts\Invoke-Sql.ps1 -File .\sql\02_exploracion.sql
~~~

## Verificación

### Flujo HTTP

Con la aplicación ejecutándose, prepare fuera de Git un JSON de cuentas de prueba con un administrador y dos clientes asociados a customer_id 1 y 2, respectivamente:

~~~json
[
  { "Usuario": "LOGIN_ADMIN", "Password": "CLAVE_LOCAL_ADMIN", "Rol": "Administrador" },
  { "Usuario": "LOGIN_CLIENTE_1", "Password": "CLAVE_LOCAL_CLIENTE_1", "Rol": "Cliente" },
  { "Usuario": "LOGIN_CLIENTE_2", "Password": "CLAVE_LOCAL_CLIENTE_2", "Rol": "Cliente" }
]
~~~

El script usa el catálogo original de Sakila y la tienda e idioma con id 1. Guarde el JSON como ProyectoU1/.local/cuentas-desarrollo.json o indique su ruta:

~~~powershell
.\scripts\Test-Flujo.ps1
# Alternativa:
.\scripts\Test-Flujo.ps1 -AccountsFile C:\ruta\local\cuentas.json
~~~

Comprueba permisos, validaciones, CRUD, inventario, agotamiento de copias, CSRF, aislamiento de clientes, devoluciones, bajas lógicas, reporte y cierre de sesión. Al finalizar, deja la película de prueba desactivada y conserva su historial.

### Solicitudes simultáneas

Con la aplicación detenida para evitar bloquear archivos durante la compilación:

~~~powershell
dotnet run --project tests/Concurrencia
~~~

Esta prueba usa dos clientes activos de Sakila y una película nueva con una única copia. Ejecuta dos solicitudes simultáneas mediante el repositorio real y verifica que se apruebe exactamente una y que SQL Server contenga un solo alquiler pendiente. Después registra la devolución y desactiva sus copias y película.

Para otra instancia, use ConnectionStrings__Sakila en ambos procesos. La prueba de concurrencia no carga appsettings.Local.json de la aplicación.

## Estructura

| Carpeta | Responsabilidad |
| --- | --- |
| ProyectoU1/Models | Entidades, modelos de vistas y validaciones |
| ProyectoU1/Controllers | Navegación, validación de entrada y coordinación |
| ProyectoU1/Data | Conexiones, consultas ADO.NET y reglas transaccionales |
| ProyectoU1/Filters | Autorización centralizada mediante Session |
| ProyectoU1/Views | Razor, Layout y Partial Views |
| sql | Modificaciones de Sakila y consultas de exploración |
| scripts | Ejecución SQL, creación local de usuarios y prueba HTTP |
| tests/Concurrencia | Prueba de integración simultánea con SQL Server |

Los archivos .local, appsettings.Local.json, las claves, bin, obj y la configuración personal de Visual Studio se excluyen de Git. System.Security.Cryptography.Pkcs tiene una referencia directa actualizada para corregir las alertas detectadas en la dependencia transitiva de SqlClient.

El informe académico y el video se elaboran por separado.
