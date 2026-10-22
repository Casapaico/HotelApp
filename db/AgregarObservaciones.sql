-- Paso final de la guía: "si la base de datos cambia".
-- Ejecutar DESPUÉS de terminar la aplicación, luego regenerar el modelo
-- (scripts/scaffold.sh) y mostrar la nueva propiedad en el formulario de Create.
USE HotelDB;
GO

ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;
GO
