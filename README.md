# Sistema CRUD Académico con MongoDB Sharding

Sistema de gestión académica con arquitectura normalizada usando MongoDB Sharding y .NET 9.0.

## 🚀 Configuración Rápida

### Prerrequisitos
- Docker y Docker Compose
- .NET 9.0 SDK

### Instalación
```bash
# 1. Clonar repositorio
git clone [URL_DEL_REPO]
cd InformesDinamicos

# 2. Levantar servicios
docker-compose up -d

# 3. Ejecutar aplicación
cd InformesDinamicos
dotnet run
```

## 📊 Arquitectura de Datos

### Bases de Datos Especializadas
- **General**: Catálogo de instituciones
- **Academico**: Programas y asignaturas
- **Comunidad**: Personas y roles

### Sharding Automático
- Distribución por hash de InstitucionId
- Shard 1_10: Hash par
- Shard 11_20: Hash impar

### Estructura Normalizada
```json
// Institución
{
  "institucionId": "inst_001",
  "nombre": "Universidad Nacional",
  "codigo": "UN"
}

// Programa
{
  "programaId": "prog_001",
  "institucionId": "inst_001",
  "nombre": "Ingeniería de Sistemas",
  "nivel": 5,
  "promedio": 4.2,
  "creditos": 180
}

// Asignatura
{
  "asignaturaId": "asig_001",
  "programaId": "prog_001",
  "institucionId": "inst_001",
  "nombre": "Algoritmos",
  "creditos": 4,
  "semestre": 3
}

// Persona
{
  "personaId": "pers_001",
  "institucionId": "inst_001",
  "nombre": "Juan Pérez",
  "rol": "Estudiante",
  "edad": 22
}
```

## 🎯 Uso del Sistema

1. **Acceder**: http://localhost:5000/Home/Nuevo
2. **Crear datos**: Botón "Crear Datos de Prueba"
3. **Navegar**: Usar tabs para cada CRUD
4. **Filtrar**: Usar selectores para filtrar datos

## 🔧 APIs Disponibles

### Instituciones
- `GET /api/instituciones` - Listar todas
- `GET /api/instituciones/{id}` - Obtener por ID
- `POST /api/instituciones` - Crear nueva
- `PUT /api/instituciones/{id}` - Actualizar
- `DELETE /api/instituciones/{id}` - Eliminar

### Programas
- `GET /api/programas?institucionId={id}` - Filtrar por institución
- `POST /api/programas` - Crear programa
- `PUT /api/programas/{id}` - Actualizar
- `DELETE /api/programas/{id}` - Eliminar

### Asignaturas
- `GET /api/asignaturas?programaId={id}` - Filtrar por programa
- `POST /api/asignaturas` - Crear asignatura
- `PUT /api/asignaturas/{id}` - Actualizar
- `DELETE /api/asignaturas/{id}` - Eliminar

### Personas
- `GET /api/personas?rol={rol}&edadMin={min}&edadMax={max}` - Filtros múltiples
- `POST /api/personas` - Crear persona
- `PUT /api/personas/{id}` - Actualizar
- `DELETE /api/personas/{id}` - Eliminar

### Datos de Prueba
- `POST /api/datosPrueba/crear-todo` - Crear datos completos
- `DELETE /api/datosPrueba/limpiar-todo` - Limpiar todas las colecciones

## 🏗️ Características Técnicas

- **Arquitectura**: Normalizada con referencias por ID
- **Sharding**: Automático por hash de InstitucionId
- **Filtrado**: Dinámico en tiempo real
- **UI**: Bootstrap 5 con tabs y tablas responsivas
- **Performance**: Consultas optimizadas con índices
- **Escalabilidad**: Distribución horizontal automática

## 🔍 Filtros Disponibles

- **Programas**: Por institución
- **Asignaturas**: Por programa
- **Personas**: Por rol, institución, rango de edad

## 📈 Ventajas del Diseño

1. **Normalización**: Elimina duplicación de datos
2. **Escalabilidad**: Sharding automático
3. **Performance**: Consultas rápidas por índices
4. **Mantenibilidad**: Código limpio y modular
5. **Flexibilidad**: Filtros dinámicos múltiples