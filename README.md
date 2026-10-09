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

La salida es `UnityRoyalMatch/Builds/RoyalMatch_Web/`. La plantilla `Assets/WebGLTemplates/RoyalMobile` ocupa toda la pantalla y conserva las rutas relativas para el montaje de Caddy en `/royal/`. Los archivos de build se despliegan al directorio estático servido por Caddy; no se versionan en Git. La versión anterior se puede reconstruir desde el historial de `royal_match/` si hace falta revertir.
