# Booking StudyRoom

Aplicación interna para reemplazar la planilla compartida de reservas de salas de estudio de una biblioteca.

## Objetivo visual

La agenda conserva deliberadamente el modelo mental de la planilla actual: horas en vertical y salas en horizontal, de Sala 1 en adelante. La diferencia es que cada reserva se convierte en un bloque consistente, clickeable y con historial asociado al RUT.

## Funcionalidad implementada en la primera maqueta

- 15 salas iniciales ordenadas horizontalmente.
- Botón `+ Agregar sala` después de la última sala.
- Agenda diaria con bloques de 30 minutos entre 08:00 y 20:00.
- Navegación semanal de lunes a sábado.
- Reserva desde cualquier celda horaria.
- Nombre, RUT chileno y plumón con borrador.
- Validación de RUT y bloqueo de traslapes de horarios.
- Edición y cancelación de reservas.
- Entrega de sala con comentario opcional.
- Registro de incidencia al entregar.
- Historial por RUT.
- Advertencia automática desde 2 incidencias previas.
- Primera configuración sin contraseña predeterminada: el primer acceso crea al administrador.
- Gestión de funcionarios, administradores, activación y cambio de contraseña.
- Contraseñas almacenadas con hash mediante `PasswordHasher` de ASP.NET Core.
- SQLite local; la base no se sube al repositorio.
- Kestrel configurado para escuchar en `http://0.0.0.0:5080`, pensado para una red local.

## Arquitectura

- .NET 10
- ASP.NET Core
- Blazor Web App (Interactive Server)
- Entity Framework Core
- SQLite

## Funcionamiento en red local

La aplicación se ejecuta en un PC de la biblioteca que permanezca encendido. Los demás equipos abren en el navegador:

```text
http://NOMBRE-DEL-PC:5080
```

o la IP local del equipo servidor.

No se necesita instalar la aplicación ni .NET en los equipos clientes.

## Datos

La base se crea automáticamente en:

```text
DataStore/booking-studyroom.db
```

Ese archivo contiene información personal (RUT, nombres e incidencias) y está excluido de Git mediante `.gitignore`.

## Respaldo

`scripts/backup-db.ps1` crea una copia fechada del archivo SQLite. La copia resultante puede guardarse en una ubicación sincronizada con OneDrive/SharePoint. No se recomienda ejecutar la base activa directamente desde una carpeta sincronizada.

## Publicación para Windows

Una vez finalizada la fase de pruebas puede utilizarse:

```powershell
.\\scripts\\publish-win.ps1
```

para obtener una publicación `win-x64` autocontenida.
