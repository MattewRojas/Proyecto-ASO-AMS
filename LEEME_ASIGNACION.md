# AMS.OS: asignación de archivos

## Abrir el proyecto

1. Extraer el ZIP en una carpeta nueva.
2. Abrir MiniOS.sln con Visual Studio 2022 y la carga de trabajo Desarrollo de escritorio de .NET (.NET 8).
3. Establecer MiniOS.Simulator como proyecto de inicio y ejecutar con F5.
4. Ir a SISTEMA DE ARCHIVOS > Asignación de archivos.

El menú ofrece asignación contigua / adyacente, por listas ligadas y por i-nodos.
Memoria conserva Multiprogramación y Memoria virtual. El explorador real permanece disponible.
Cada ventana inicia un disco virtual independiente de 20 bloques; el estado se descarta al cerrarla. No se crean ni eliminan archivos reales.

## Cambios para integrar en otra copia del repositorio

- Reemplazar MiniOS.Simulator/Interfaz/FrmPrincipal.cs.
- Añadir MiniOS.Simulator/Archivos/DiscoAsignacion.cs.
- Añadir MiniOS.Simulator/Interfaz/FrmAsignacionArchivos.cs.
- Mantener FrmAsignacionINodos.cs existente.

El proyecto SDK incluye automáticamente los nuevos archivos .cs. No requiere paquetes adicionales.

## Demostración según la guía del avance 5

### 8.1 Asignación contigua / adyacente

Crear reserva el primer tramo consecutivo suficiente. El directorio conserva inicio y cantidad.
Consultar muestra el rango; un bloque lógico k se localiza como inicio + k.
Eliminar libera todo el rango.

Pulse Cargar ejemplo. Quedan A, C y E con cuatro bloques cada uno, y ocho bloques libres repartidos en dos huecos de cuatro. El campo propone F.txt de seis bloques. Al intentar crearlo se informa fragmentación externa: el espacio total alcanza, pero ningún hueco es suficientemente grande. Pulse Compactar y vuelva a crear F.txt: ocupará 12 a 17.

Compactar mueve los archivos simulados y actualiza sus inicios, reuniendo el espacio libre al final. No aumenta la cantidad total de bloques libres. Es una operación adicional y explícita, no una condición automática de la asignación contigua.

### 8.2 Asignación por listas ligadas

Crear toma bloques libres y escribe en cada uno el enlace al siguiente. El directorio conserva el inicio y el tamaño. Consultar recorre los enlaces; FIN representa un puntero nulo. Eliminar primero recorre la cadena y después libera propietarios y enlaces.

Pulse Cargar ejemplo y cree F.txt de seis bloques. La cadena será 4 → 5 → 6 → 7 → 12 → 13 → FIN. Consulte y elimine F.txt para mostrar la recuperación de los ocho bloques libres.

Esta lista ligada corresponde a almacenamiento de archivos en disco. Es distinta de las listas de huecos de memoria usadas para First Fit, Next Fit, Best Fit y Worst Fit, que permanecen en Memoria.

### 8.3 Asignación por i-nodos

Se conserva la implementación anterior y sus operaciones Crear, Consultar, Eliminar, Mostrar disco, Espacio libre, Cargar ejemplo y Reiniciar. El cambio es su acceso desde Sistema de archivos.

La implementación didáctica guarda una lista de referencias en el i-nodo; no implementa todos los niveles de indirección y metadatos de un sistema de archivos real.

### Evidencias para el informe

Capturar el menú bajo Sistema de archivos; creación, consulta y eliminación de cada algoritmo; rechazo por fragmentación y resultado de compactación; cadena dispersa y sus enlaces; y referencias del i-nodo. La guía PDF de la docente se utilizó como especificación y no se modificó.

## Validación

- Pruebas ejecutadas sobre el modelo C# con .NET 8: 14 037 comprobaciones correctas, incluyendo secuencias reproducibles de creación/eliminación y conservación de bloques.
- Compilación directa con Roslyn del código del simulador, exceptuando Program.cs generado/arranque, contra referencias .NET 8 y Windows Forms: sin errores. Cinco advertencias de nulabilidad corresponden a código existente.
- El comando estándar dotnet build no pudo ejecutarse en este entorno por una limitación de lectura de información de procesos. La compilación directa comprobó tipos y sintaxis, pero no sustituye la compilación completa con recursos de Visual Studio.
- No se ejecutó la interfaz gráfica Windows Forms en este entorno Linux. Comprobar visualmente en Windows, especialmente si se usa escalado de pantalla elevado.

Para repetir las pruebas del modelo en un equipo con .NET 8:

```sh
dotnet run --project PruebasAsignacion/PruebasAsignacion.csproj
```

Los archivos de esta actualización incluyen el código, las pruebas del modelo y esta guía de demostración.

## Diseño común y diferencia de código

Los tres simuladores usan la distribución del formulario de i-nodos: panel izquierdo con entrada, operaciones, directorio y detalle; panel derecho con mapa de 20 bloques en dos filas, herramientas e historial. La contigua conserva Compactar disco.

| Método | Estructura persistente | Consulta |
| --- | --- | --- |
| Contigua | Entrada.Inicio y Entrada.Cantidad | Calcula inicio + desplazamiento. |
| Ligada | Entrada con inicio/tamaño y BloqueLigado.Siguiente por bloque | Recorrer sigue Siguiente hasta null (FIN). |
| I-nodos | INodo.Id, metadatos e INodo.Bloques | Consulta las referencias del i-nodo; Bloques[k] identifica el bloque lógico k. |

En DiscoAsignacion.cs, BloqueLigado hace explícito que el enlace pertenece al bloque. La lista local de Recorrer es temporal, sirve para mostrar y validar el recorrido, y no se almacena como índice del archivo. En FrmAsignacionINodos.cs, INodo.Bloques sí es una lista persistente de referencias y los bloques de datos no guardan Siguiente. El diseño visual compartido no cambia estas estructuras.
