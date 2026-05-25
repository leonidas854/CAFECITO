# 04 — Flujo de Trabajo con Git

Reglas operativas para mantener el repo limpio y el historial presentable ante los docentes.

---

## 1. Estrategia de ramas

```text
main         ──┐  Releases. Solo se actualiza desde develop al final del Sprint.
               │
               ▼
develop      ──┐  Integración. Siempre debe compilar. Todos los PR llegan aquí.
               │
               ├── feature/I1-logica-colas       (Integrante 1)
               ├── feature/I2-ui-dashboard       (Integrante 2)
               ├── feature/I3-entidades          (Integrante 3)
               ├── feature/I4-promodel-export    (Integrante 4)
               └── bugfix/I[N]-descripcion-bug   (correcciones)

hotfix/<desc> Bugs urgentes que salen directo desde main si una entrega ya está hecha.
```

### Crear tu rama de trabajo

```bash
git checkout develop
git pull origin develop
git checkout -b feature/I2-slider-lambda
```

### Mantener tu rama actualizada

Antes de empezar a trabajar cada día:

```bash
git checkout develop
git pull origin develop
git checkout feature/I2-slider-lambda
git merge develop          # trae lo último de develop a tu rama
# resuelve conflictos si los hay, commit, sigue trabajando
```

---

## 2. Conventional Commits (en español, minúsculas)

### Formato

```text
tipo(alcance): descripción corta en presente, minúsculas, sin punto final
```

### Tipos permitidos

| Tipo | Para qué |
|---|---|
| `feat` | Nueva funcionalidad visible al usuario o al simulador |
| `fix` | Corrección de bug |
| `docs` | Cambios solo en documentación (README, /docs, comentarios) |
| `style` | Formateo, espacios, sin cambio de lógica |
| `refactor` | Reescritura sin cambiar comportamiento externo |
| `test` | Añadir o modificar tests |
| `chore` | Mantenimiento: Unity settings, packages, CI |

### Alcances (`scope`) sugeridos

`core`, `lcg`, `queue`, `ui`, `dashboard`, `entities`, `customer`, `barista`, `spawner`, `events`, `config`, `scene`, `art`, `promodel`, `git`, `readme`, `docs`.

### Ejemplos correctos

```text
feat(lcg): implementar generador congruencial con semilla configurable
feat(ui): agregar slider de lambda vinculado al simulation config
fix(customer): cliente no se destruia al abandonar la cola
test(lcg): verificar uniformidad con chi-cuadrada para semilla 12345
docs(readme): agregar guia de instalacion para windows
refactor(queue): extraer interfaz iqueuediscipline
chore(git): actualizar gitattributes para fbx binarios
style(dashboard): alinear paneles de metricas a 8px de margen
```

### Ejemplos incorrectos (no aceptados en revisión)

```text
Update files                         ← sin tipo, sin alcance, en inglés genérico
fix: arreglé el bug del slider.      ← con punto final, sin alcance
FEAT(UI): SLIDER LISTO               ← mayúsculas
feat(ui): added a new slider         ← inglés (decidimos español)
WIP                                  ← no se commitea WIP a develop/main
```

### Granularidad

**Un commit, un cambio atómico.** Si tu PR tiene 1 commit con 800 líneas tocando 20 archivos, hazlo en commits separados:

```text
feat(core): definir interfaz iqueuediscipline
feat(core): implementar fifoqueue por defecto
refactor(queue): inyectar disciplina por constructor
test(queue): cobertura basica de fifoqueue
```

---

## 3. Pull Requests

### Antes de abrir el PR

1. ✅ Tu rama está al día con `develop` (`git merge develop` sin conflictos).
2. ✅ El proyecto compila en Unity sin errores.
3. ✅ Probaste el feature en **Play Mode**.
4. ✅ Los `.meta` están commitidos.
5. ✅ No subiste archivos de la carpeta `Library/` ni binarios temporales (revísalo con `git status`).

### Crear el PR

Push y abre PR en GitHub:

```bash
git push -u origin feature/I2-slider-lambda
# GitHub te imprimirá un link tipo:
# https://github.com/leonidas854/CAFECITO/pull/new/feature/I2-slider-lambda
```

### Plantilla de descripción del PR

Copia esto al abrir el PR:

```markdown
## Qué hace este PR

(2-3 líneas describiendo el cambio funcional)

## Por qué

(Motivación: qué problema resuelve o qué feature del sprint cubre)

## Cómo lo probé

- [ ] Play Mode: caso A — ...
- [ ] Play Mode: caso B — ...
- [ ] Compila sin warnings nuevos
- [ ] (Si aplica) tests pasan: `Window → General → Test Runner`

## Checklist de DoD

- [ ] Sin paths absolutos
- [ ] Sin magic numbers (todos en SimulationConfig)
- [ ] Comentarios XML en clases públicas del Core
- [ ] .meta incluidos
- [ ] Complejidad ciclomática ≤ 12 por método

## Capturas / GIF

(Opcional. Si es UI, agrega una captura del antes/después)
```

### Revisión

- **Mínimo 1 aprobador** distinto al autor.
- El reviewer ejecuta el branch localmente, verifica que el feature funciona, marca **Approve** o **Request changes**.
- Si pide cambios: el autor hace nuevos commits (no `--amend` ni `force push`), responde el comentario, vuelve a pedir revisión.
- Una vez aprobado: el autor hace **Squash and merge** (o **Merge commit** si los commits son atómicos y bien escritos) hacia `develop`.

### Cuándo NO se aprueba

- Falta cualquier ítem del [Definition of Done](03-clean-code.md#5-definition-of-done-dod).
- El PR mezcla dos features no relacionadas. → Pide al autor que las separe.
- Hay archivos de la carpeta `Library/` o binarios temporales. → Pide limpiar el `.gitignore`.
- Modifica `MainSimulation.unity` y el autor no es el Integrante 3.

---

## 4. Conflictos de merge

### Conflictos en código C# (`.cs`)

VS Code los marca con `<<<<<<<`, `=======`, `>>>>>>>`. Resuélvelos, prueba en Unity, commit.

```bash
# Después de resolver
git add Assets/_Project/Scripts/Core/SimulationManager.cs
git commit          # git autocompleta el mensaje de merge; déjalo
```

### Conflictos en escenas/prefabs (`.unity`, `.prefab`, `.asset`)

**No intentes resolverlos a mano.** Unity tiene una herramienta llamada `UnityYAMLMerge` que entiende el formato YAML serializado de Unity.

#### Configurar UnityYAMLMerge una vez (cada integrante)

**Linux:**

```bash
# Ruta típica de Unity Hub en Arch
UNITY=~/Unity/Hub/Editor/6000.3.16f1/Editor/Data/Tools/UnityYAMLMerge

git config --global merge.tools.unityyamlmerge.cmd "$UNITY merge -p \$BASE \$REMOTE \$LOCAL \$MERGED"
git config --global merge.tools.unityyamlmerge.trustExitCode false
```

**Windows (PowerShell):**

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Data\Tools\UnityYAMLMerge.exe"
git config --global merge.tools.unityyamlmerge.cmd "`"$Unity`" merge -p `$BASE `$REMOTE `$LOCAL `$MERGED"
git config --global merge.tools.unityyamlmerge.trustExitCode false
```

Cuando aparezca un conflicto en `.unity`:

```bash
git mergetool --tool=unityyamlmerge
```

#### Si el conflicto es grande o no estás seguro

**Regla de oro**: avisa al equipo (10 min de reunión rápida), descarta tus cambios locales en la escena, vuelve a aplicar tu cambio manualmente sobre la versión más reciente.

```bash
git checkout --theirs Assets/_Project/Scenes/MainSimulation.unity
# Vuelve a Unity, vuelve a hacer tu modificación, commit
```

---

## 5. Comandos cotidianos

```bash
# Ver en qué rama estás
git status

# Ver el historial gráfico de las últimas 20 commits
git log --oneline --graph --all -n 20

# Deshacer un cambio NO commiteado de un archivo
git checkout -- Assets/_Project/Scripts/Core/Foo.cs

# Deshacer el último commit (manteniendo los cambios)
git reset --soft HEAD~1

# Ver qué cambia tu PR respecto a develop
git diff develop...HEAD --stat
```

---

## 6. Reglas inviolables

1. ❌ **No `git push --force` a `main` o `develop`.** Jamás. Si necesitas reescribir historial, abre una discusión.
2. ❌ **No commit directo a `main` ni a `develop`.** Siempre por PR.
3. ❌ **No mezcles `git pull --rebase` con merges de escenas.** Mantén `pull.rebase = false`.
4. ❌ **No commitees credenciales** (tokens, contraseñas, API keys). Si pasa, **rota el secreto inmediatamente** y abre un issue.
5. ❌ **No edites el `.gitattributes` ni el `.gitignore` sin aviso al equipo** — afectan a todos.
6. ✅ **Pull antes de empezar a trabajar.** Cada mañana.
7. ✅ **Push al menos una vez al día**, aunque sea trabajo intermedio en tu rama feature.
