# Booking StudyRoom

Aplicación local para reemplazar la planilla compartida de reserva de salas de estudio de biblioteca.

## Enfoque visual

La agenda conserva el modelo mental de la planilla actual: horas en filas y salas en columnas, con disponibilidad y reservas visibles de un vistazo. La interfaz moderniza esa estructura sin convertirla en un sistema de booking genérico.

## Funciones de la primera implementación

- 15 salas iniciales y botón `+` para agregar más.
- Agenda diaria de 08:00 a 19:00 en bloques de 30 minutos.
- Navegación semanal familiar.
- Reserva por nombre, RUT chileno, hora de inicio/término y plumón con borrador.
- Validación de RUT y prevención de reservas superpuestas.
- Entrega de sala con comentario opcional.
- Registro de incidencias vinculado al RUT.
- Detección visual de reincidencia desde 2 incidencias.
- Búsqueda e historial por RUT.
- Usuarios de personal con contraseña mediante ASP.NET Core Identity.
- Base de datos SQLite local.

## Cuenta inicial

- Usuario: `admin`
- Contraseña: `biblioteca`

Cambiar la contraseña inicial antes de una puesta en producción real.

## Arquitectura

- .NET 10
- ASP.NET Core / Blazor Web App (Interactive Server)
- Entity Framework Core
- SQLite

La aplicación está pensada para ejecutarse en un equipo de la biblioteca y ser usada desde los demás equipos mediante navegador dentro de la red local.
