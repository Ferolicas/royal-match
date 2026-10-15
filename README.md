# Mil Gemas

El juego publicado en `/royal/` se construye desde `UnityRoyalMatch/` con Unity 6000.0.84f1 y el módulo Web Build Support de esa misma versión. `royal_match/` conserva la implementación web anterior como referencia; no es la fuente de la publicación actual.

Para generar la web móvil y de escritorio:

```bash
UNITY_EDITOR=/home/ferney_oliveros/Unity/Hub/Editor/6000.0.84f1/Editor/Unity
"$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath UnityRoyalMatch \
  -executeMethod RoyalMatch.EditorTools.BuildScript.BuildWeb \
  -logFile /tmp/royal-web-build.log
```

La salida es `UnityRoyalMatch/Builds/RoyalMatch_Web/`. La plantilla `Assets/WebGLTemplates/RoyalMobile` ocupa toda la pantalla y conserva las rutas relativas para el montaje de Caddy en `/royal/`. Caddy debe servir `Build/*.gz` con `Content-Encoding: gzip`, `*.wasm.gz` con `Content-Type: application/wasm` y `*.js.gz` con `Content-Type: application/javascript`. Los archivos de build se despliegan al directorio estático servido por Caddy; no se versionan en Git. La versión anterior se puede reconstruir desde el historial de `royal_match/` si hace falta revertir.

## Publicación automática

Un push a `main` que cambie el proyecto Unity o el workflow ejecuta
`.github/workflows/mil-gemas-web.yml`. El runner propio `milgemas-unity-ferney`
compila con Unity 6000.0.84f1, pasa las pruebas EditMode y publica el build
WebGL en <https://olcas.app/royal/>. La publicación cambia de versión tras
copiar todos los archivos y comprueba el SHA servido por Caddy. El runner es
un servicio de usuario de este ordenador; para que el push se publique, el
ordenador debe estar encendido y con conexión. El estado y los logs de cada
publicación están en GitHub Actions. También puede lanzarse manualmente con
`workflow_dispatch`.

Esto entrega la versión **web móvil**. Los paquetes nativos de Android/iOS
requieren sus respectivas cadenas de compilación y firma. Las compras con
dinero real permanecen desactivadas según `UnityRoyalMatch/STORE_SETUP.md`.
