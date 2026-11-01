-- Ejecutar sobre la base Sakila de SQL Server usada en clases.
-- No borra películas, inventario ni alquileres existentes.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.film', 'U') IS NULL OR OBJECT_ID('dbo.rental', 'U') IS NULL
    THROW 50001, 'Seleccione una base Sakila de SQL Server antes de ejecutar.', 1;

IF COL_LENGTH('dbo.film', 'activo') IS NULL
    ALTER TABLE dbo.film ADD activo bit NOT NULL CONSTRAINT DF_film_activo DEFAULT (1);
IF COL_LENGTH('dbo.inventory', 'activo') IS NULL
    ALTER TABLE dbo.inventory ADD activo bit NOT NULL CONSTRAINT DF_inventory_activo DEFAULT (1);

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre nvarchar(100) NOT NULL,
        Login nvarchar(80) NOT NULL UNIQUE,
        PasswordHash nvarchar(512) NOT NULL,
        Rol varchar(20) NOT NULL,
        CustomerId int NULL REFERENCES dbo.customer(customer_id),
        StaffId tinyint NULL REFERENCES dbo.staff(staff_id),
        Activo bit NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        CONSTRAINT CK_Usuarios_Rol CHECK (
            (Rol = 'Cliente' AND CustomerId IS NOT NULL AND StaffId IS NULL) OR
            (Rol = 'Administrador' AND StaffId IS NOT NULL AND CustomerId IS NULL))
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_rental_copia_pendiente' AND object_id=OBJECT_ID('dbo.rental'))
    CREATE INDEX IX_rental_copia_pendiente ON dbo.rental(inventory_id) WHERE return_date IS NULL;

COMMIT;
