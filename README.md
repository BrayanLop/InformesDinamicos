# Microservicio de Informes Dinámicos

Sistema de datos distribuidos con MongoDB Sharding y RabbitMQ.

## Configuración del Entorno

### Prerrequisitos
- Docker y Docker Compose
- .NET 9.0 SDK

### Instalación

1. **Clonar el repositorio**
```bash
git clone [URL_DEL_REPO]
cd InformesDinamicos
```

2. **Levantar servicios con Docker**
```bash
docker-compose up -d
```

Esto iniciará:
- MongoDB en puerto 27017 (usuario: admin, password: password)
- RabbitMQ en puerto 5672 (Management UI: http://localhost:15672)

3. **Ejecutar la aplicación**
```bash
cd InformesDinamicos
dotnet run
```

## Estructura de Datos

### Bases de Datos
- **Académico**: Datos académicos de estudiantes
- **Comunidad**: Datos de participación comunitaria

### Sharding
- Shard 1_10: ClienteId 1-10
- Shard 11_20: ClienteId 11-20

### Estructura de Registro
```json
{
  "_id": "ObjectId",
  "ClienteId": "5",
  "InstitucionId": "INST_001", 
  "Datos": {
    "programa": "Ingeniería",
    "semestre": 8,
    "materias": ["Cálculo", "Física"],
    "notas": { "parcial1": 4.1, "parcial2": 4.3 }
  },
  "LastUpdated": "2024-01-01T00:00:00Z",
  "Version": 1
}
```

## Uso

1. Ir a http://localhost:5000
2. Hacer clic en "Crear Datos de Prueba"
3. Buscar por Cliente ID (ej: "5", "15", "18")
4. Ver datos por shard completo

## APIs

- `POST /api/Insert/datos` - Insertar datos
- `GET /api/Insert/datos-prueba` - Crear datos de prueba
- `POST /api/Insert/crear-cliente` - Crear cliente individual