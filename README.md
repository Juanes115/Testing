# 🧪 Laboratorio de Pruebas Unitarias en .NET (C#)
### xUnit + Moq + FluentAssertions + Cobertura de Código (Coverage Gutters)

Este repositorio contiene una arquitectura modular orientada a pruebas unitarias en **C# (.NET 9 / .NET 8)**, estructurada bajo las mejores prácticas de la industria (principios SOLID, Inyección de Dependencias y patrón AAA: Arrange-Act-Assert).

---

## 📁 Estructura del Proyecto

```text
c:\pruebas-Test/
├── BaseTestingLab.sln                   # Solución .NET
├── .vscode/
│   ├── settings.json                   # Configuración para Coverage Gutters y C# DevKit
│   └── tasks.json                      # Tareas automáticas de compilación y prueba
├── test-coverage.ps1                   # Script PowerShell para ejecutar pruebas y sincronizar cobertura
├── coverage.cobertura.xml              # Archivo de cobertura listo para Coverage Gutters
│
├── src/BaseTestingLab.Core/            # PROYECTO 1: LÓGICA BASE Y MODELOS
│   ├── Common/
│   │   ├── Exceptions/                 # Excepciones de dominio (ConnectionException, etc.)
│   │   └── Providers/                  # Abstracciones de tiempo (IDateTimeProvider)
│   ├── Text/                           # Textos, Comodines (Wildcards) y Regex
│   │   ├── TextProcessor.cs
│   │   ├── WildcardMatcher.cs
│   │   └── RegexValidator.cs
│   ├── DateTimeUtils/                  # Fechas, Días Hábiles, Calendarios y Rangos
│   │   ├── DateCalculator.cs
│   │   ├── DateRange.cs
│   │   └── HolidayCalendar.cs
│   ├── Connections/                    # Conexiones y operaciones entre conexiones
│   │   ├── IConnection.cs
│   │   ├── ConnectionState.cs
│   │   ├── SendResult.cs
│   │   ├── ConnectionTransferOptions.cs
│   │   ├── ConnectionBridge.cs
│   │   └── ConnectionManager.cs
│   ├── Financial/                      # Escenario adicional 1: Facturación y Descuentos
│   │   ├── FinancialCalculator.cs
│   │   └── DiscountModels.cs
│   ├── Collections/                    # Escenario adicional 2: Paginación y Colecciones
│   │   ├── CollectionHelper.cs
│   │   └── PagedResult.cs
│   └── Security/                       # Escenario adicional 3: Autenticación y Mocks
│       ├── SecurityContracts.cs
│       └── AuthenticationService.cs
│
└── tests/BaseTestingLab.Tests/         # PROYECTO 2: SUITE DE PRUEBAS UNITARIAS
    ├── Text/
    │   ├── TextProcessorTests.cs
    │   ├── WildcardMatcherTests.cs
    │   └── RegexValidatorTests.cs
    ├── DateTimeUtils/
    │   ├── DateCalculatorTests.cs
    │   ├── DateRangeTests.cs
    │   └── HolidayCalendarTests.cs
    ├── Connections/
    │   ├── ConnectionBridgeTests.cs
    │   └── ConnectionManagerTests.cs
    ├── Financial/
    │   ├── FinancialCalculatorTests.cs
    ├── Collections/
    │   ├── CollectionHelperTests.cs
    └── Security/
        └── AuthenticationServiceTests.cs
```

---

## 🎯 Escenarios Implementados y Clases Base

### 1. Cadenas de Texto Simples y Complejas (`TextProcessor`)
- **Simples**: Sanitización de espacios en blanco redundantes, normalización de saltos de línea (`\r\n` a `\n`), conversión a `kebab-case`, `snake_case` y `Title Case`, detección estricta de palíndromos (ignorando acentos, puntuación y mayúsculas).
- **Protección de Datos (Enmascaramiento)**: Enmascaramiento de correos electrónicos (`c***a@empresa.com`) y tarjetas de crédito (`****-****-****-1234`).
- **Complejas**:
  - Truncado inteligente preservando límites de palabras completas (`Truncate`).
  - Motor de plantillas con marcadores `{placeholder}` con soporte para modo estricto (`RenderTemplate`).
  - Analizador sintáctico de pares clave-valor respetando valores entre comillas (`ParseKeyValuePairs`).

### 2. Comodines / Wildcards tipo Glob (`WildcardMatcher`)
- Soporte para patrones tipo shell/glob:
  - `*`: Coincide con 0 o más caracteres (ej. `reporte_*.csv`).
  - `?`: Coincide exactamente con 1 carácter (ej. `log?.txt`).
  - `[a-z]` / `[0-9]`: Coincide con rangos o clases de caracteres específicos.
- Conversión transparente de comodines a expresiones regulares (`WildcardToRegex`).
- Filtrado directo sobre colecciones con `Filter()`.

### 3. Expresiones Regulares (`RegexValidator`)
- Validaciones para:
  - Correos electrónicos conformes a estándares.
  - URLs HTTP y HTTPS.
  - Teléfonos internacionales en formato E.164 (`+573001234567`).
  - Identificadores únicos globales (`Guid`/`UUID`).
  - Slugs web limpios (`articulo-tecnico-2026`).
- Validador de robustez de contraseñas con reporte detallado de incumplimientos (mínimo de caracteres, mayúscula, minúscula, dígito, símbolo especial).
- Extracción de grupos con nombre en cadenas estructuradas (`ExtractNamedGroups`).

### 4. Fechas, Días Hábiles y Rangos Temporales (`DateCalculator`, `DateRange`)
- **Desacoplamiento temporal**: Inyección de `IDateTimeProvider` para poder mockear la fecha actual en pruebas sin depender del reloj del sistema.
- **Cálculo de días hábiles**: Omite fines de semana (sábados y domingos) y feriados configurables vía `HolidayCalendar`.
- **Edad exacta**: Desglose preciso en años, meses y días teniendo en cuenta meses de longitud variable y años bisiestos.
- **Operaciones de rangos (`DateRange`)**: Detección de solapamiento (`OverlapsWith`), contigüidad (`IsContiguousWith`) e inclusión de instantes de tiempo (`Contains`).

### 5. Conexiones y Operaciones entre Conexiones (`ConnectionBridge`, `ConnectionManager`)
- **Interfaz `IConnection`**: Diseñada para ser mockeada con **Moq**, controlando estados (`Connected`, `Disconnected`, `Faulted`), envío (`SendAsync`), recepción (`ReceiveAsync`) y latencia (`PingAsync`).
- **Operaciones entre conexiones (`ConnectionBridge`)**:
  - `TransferAsync`: Transfiere cargas útiles de una conexión origen a otra destino.
  - Reconexión automática transparente (`AutoReconnect = true`).
  - Reintentos exponenciales/lineales ante fallos transitorios (`MaxRetries`, `RetryDelay`).
  - Transformación en tránsito (`PayloadTransformer`).
  - Validación de conectividad previa (`ValidatePingBeforeSend`).
  - Sincronización continua de colas (`SyncQueueAsync`).
- **Gestión y Failover (`ConnectionManager`)**:
  - Conmutación por error ante caídas del servidor primario (`ExecuteWithFailoverAsync`).
  - Difusión masiva simultánea (`BroadcastAsync`).

### 6. Escenario Adicional 1: Cálculos Financieros (`FinancialCalculator`)
- Resumen de facturación (`InvoiceSummary`) con precio unitario, cantidad e impuestos.
- Cascada de descuentos (porcentuales y montos fijos) con umbral mínimo de compra (`MinSubtotalRequired`) y tope máximo (`MaxDiscountLimit`).
- Redondeo bancario estándar (`MidpointRounding.ToEven`) para evitar sesgos acumulativos de redondeo.

### 7. Escenario Adicional 2: Colecciones y Paginación (`CollectionHelper`)
- Paginación tipada `PagedResult<T>` con cálculo automático de `TotalPages`, `HasPreviousPage` y `HasNextPage`.
- Particionamiento en lotes (`ChunkBySize`).
- Ordenamiento dinámico reflexivo por nombre de propiedad (`SortByProperty`).
- Detección de elementos duplicados mediante selectores de clave (`FindDuplicates`).

### 8. Escenario Adicional 3: Seguridad y Autenticación con Mocks (`AuthenticationService`)
- Dependencias desacopladas:
  - `IUserRepository`: Consulta y actualización de intentos.
  - `ITokenGenerator`: Emisión de JWT.
  - `IAuditLogger`: Bitácora inmutable de eventos de seguridad.
  - `ICacheService`: Lista negra de IPs en memoria/Redis.
- Regla de negocio de bloqueo: tras 3 intentos fallidos consecutivos, bloquea la cuenta y genera alerta de auditoría.

---

## 🛠️ Cómo Ejecutar las Pruebas y Cobertura

### Opción 1: Comando Oficial de .NET CLI
Ejecuta en tu terminal PowerShell desde la raíz del proyecto (`c:\pruebas-Test`):

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

---

### Opción 2: Script Automatizado (Recomendado para Coverage Gutters)
```powershell
./test-coverage.ps1
```

---

## 🔍 Cómo Usar Coverage Gutters en Visual Studio Code

1. Ejecuta `./test-coverage.ps1`.
2. En la **barra de estado inferior** (a la izquierda de VS Code), haz clic en el botón `Watch`.
   - O presiona `Ctrl + Shift + P` y selecciona `Coverage Gutters: Display Coverage`.
3. Abre cualquier archivo en `src/BaseTestingLab.Core/`:
   - 🟩 **Verde**: Línea cubierta por pruebas.
   - 🟥 **Rojo**: Línea sin cobertura.
   - 🟨 **Amarillo**: Rama condicional evaluada parcialmente.
