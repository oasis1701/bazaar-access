# Bazaar Access

Plugin de BepInEx para hacer el juego "The Bazaar" accesible para personas ciegas usando Tolk como lector de pantalla.

## Estructura del proyecto

```
BazaarAccess/
├── Plugin.cs              # Punto de entrada del plugin, manejo de teclas
├── MenuNavigator.cs       # Lógica de navegación por menús
└── Patches/               # Parches de Harmony por menú
    ├── MainMenuViewPatch.cs
    ├── HeroSelectViewPatch.cs
    ├── ViewPatch.cs           # Parche genérico para otras vistas
    ├── PopupPatch.cs          # Popups (Show/Hide)
    ├── OptionsDialogPatch.cs  # Menú de opciones
    └── HeroChangedPatch.cs    # Cambio de héroe
```

## Arquitectura

### Parches modulares
- Cada tipo de menú tiene su propio parche en `Patches/`
- `ViewPatch.cs` es genérico pero ignora vistas con parches específicos
- Esto permite mantener/actualizar menús individualmente sin afectar otros

### Sistema de navegación
- `MenuNavigator` mantiene el estado del menú actual
- Guarda/restaura menú anterior al abrir/cerrar popups
- Valida que el menú siga activo antes de cada acción
- Ordena elementos de arriba a abajo, izquierda a derecha

### Controles
- Flechas arriba/abajo: Navegar por opciones
- Flechas izquierda/derecha: Ajustar sliders/dropdowns/toggles
- Enter: Activar botón/toggle
- F5: Refrescar y leer elemento actual

## Buenas prácticas

### Al añadir un nuevo menú
1. Crear un nuevo archivo en `Patches/` con el nombre del menú
2. Usar `[HarmonyPatch(typeof(ClaseDelMenu), "Show")]` o el método apropiado
3. En el Postfix, llamar a:
   - `MenuNavigator.AnnounceMenuTitle(__instance.transform)`
   - `MenuNavigator.SetMenuRoot(__instance.transform, "NombreDelMenu")`
4. Añadir el nombre a `IgnoredViews` en `ViewPatch.cs` si hereda de `View`

### Para popups/diálogos
1. Si hereda de `PopupBase`, ya está cubierto por `PopupPatch`
2. Si no, crear parche para `OnEnable`/`OnDisable`:
   - En OnEnable: `SavePreviousMenu()` antes de `SetMenuRoot()`
   - En OnDisable: `RestorePreviousMenu()`

### Detección de elementos
- Solo se detectan: Button, Toggle, Slider, TMP_Dropdown
- Se filtran elementos decorativos (background, border, etc.)
- Los botones sin texto se ignoran
- BazaarButtonController usa `ButtonText` para obtener el texto

### Validación de menú
- Antes de cada acción, se verifica que el menú actual siga activo
- Si el menú se cerró, se limpia el estado automáticamente
- Esto evita mezclar elementos de menús diferentes

## Convenciones de código

- No decir el tipo de elemento (botón, slider) - solo estados (activado/desactivado) y valores (%)
- Al cambiar de menú, solo leer el título
- Usar `Plugin.Logger.LogInfo()` para debug
- Mantener el código modular: un archivo por parche

## Dependencias

- BepInEx 5.x
- Harmony (incluido en BepInEx)
- Tolk (TolkDotNet.dll en carpeta references/ - no incluida en git)
- Referencias del juego en `TheBazaar_Data/Managed/`

## Compilación

```bash
cd BazaarAccess
dotnet build
```

El DLL se copia automáticamente a la carpeta de plugins de BepInEx configurada en el .csproj.

## Notas

- El código descompilado del juego está en `bazaar code/` (no incluido en git)
- Las referencias de Tolk están en `references/` (no incluido en git)
