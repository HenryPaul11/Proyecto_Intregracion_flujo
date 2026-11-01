# Sistema de renta de películas con Sakila

Proyecto académico ASP.NET Core MVC sobre .NET 10. La carpeta actual contiene la base inicial de la aplicación.

## Ejecución de la base

Instalar el SDK de .NET 10 y ejecutar:

```powershell
dotnet restore ProyectoU1.slnx
dotnet run --project ProyectoU1
```

La terminal mostrará la dirección local de la aplicación.

## Etapas de desarrollo

1. Base MVC y organización del repositorio.
2. Exploración de Sakila y scripts SQL de usuarios y eliminación lógica.
3. Acceso ADO.NET, autenticación, Session y autorización por roles.
4. Catálogo, administración de películas e inventario.
5. Alquiler y devolución con control de disponibilidad y propiedad.
6. Reportes, validación del flujo y documentación de evidencias.

Cada etapa se registrará con commits que describan el trabajo efectivamente realizado. Las funcionalidades pendientes no forman parte de esta base inicial.

## Configuración y seguridad

Las credenciales y las cadenas de conexión personales deben configurarse fuera de los archivos versionados, mediante variables de entorno o secretos de usuario. No incluir bases de datos, archivos de Visual Studio ni resultados de compilación en Git.

Repositorio: https://github.com/HenryPaul11/Proyecto_Intregracion_flujo
