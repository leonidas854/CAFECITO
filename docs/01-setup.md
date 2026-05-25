# 01 — Setup del Entorno (Linux + Windows)

Este documento es **obligatorio** para los 4 integrantes antes del primer commit. Si lo saltas, romperás algo en la máquina del compañero — garantizado.

---

## 1. Unity 6.3 LTS (versión exacta)

### Versión obligatoria

```text
Unity 6000.3.16f1   (revision a56f230f6470)
```

> Una diferencia de revisión (`.16f1` vs `.18f1`, por ejemplo) corrompe el proyecto al hacer `git checkout`. Verifica antes de instalar abriendo `ProjectSettings/ProjectVersion.txt` en el repo y comparándolo.

### Instalación en Arch Linux

```bash
# 1. Instalar Unity Hub (AUR)
yay -S unityhub
# Si no usas yay:
# git clone https://aur.archlinux.org/unityhub.git && cd unityhub && makepkg -si

# 2. Abrir Unity Hub
unityhub
```

Dentro de Unity Hub:

1. **Installs → Install Editor → Archive** (porque no aparecerá en la lista oficial).
2. Pega esta URL: `https://download.unity.com/download_unity/a56f230f6470/UnityHubReleaseId/`
   - O bien: ve a <https://unity.com/releases/editor/whats-new/6000.3.16> y descarga el instalador desde "Download → Unity Hub".
3. Al instalar, marca los módulos:
   - **Linux Build Support (IL2CPP, Mono)** ✅
   - **Windows Build Support (Mono)** ✅
   - **WebGL Build Support** ✅
   - Documentation (opcional)

### Instalación en Windows

1. Descarga Unity Hub desde <https://unity.com/download> e instálalo.
2. Abre Unity Hub → **Installs → Install Editor**.
3. En la pestaña **Archive** busca exactamente `6000.3.16f1`. Si no aparece, usa el link directo: <https://unity.com/releases/editor/whats-new/6000.3.16>.
4. Módulos a marcar:
   - **Windows Build Support (IL2CPP)** ✅
   - **Linux Build Support (Mono)** ✅
   - **WebGL Build Support** ✅

### Verificación

```bash
# Después de instalar y abrir el proyecto al menos una vez
cat ProjectSettings/ProjectVersion.txt
```

Debe imprimir:
```text
m_EditorVersion: 6000.3.16f1
m_EditorVersionWithRevision: 6000.3.16f1 (a56f230f6470)
```

---

## 2. Git

### Linux (Arch / Manjaro)

```bash
sudo pacman -S git
git --version   # debe ser >= 2.40
```

### Windows

1. Descarga "Git for Windows" desde <https://git-scm.com/download/win>.
2. Durante la instalación:
   - **Default editor**: VS Code (si lo tienes) o Notepad++.
   - **PATH**: "Git from the command line and also from 3rd-party software".
   - **Line endings**: "Checkout as-is, commit as-is" (importante: el `.gitattributes` del proyecto se encarga, no Git Bash).
   - **Terminal emulator**: MinTTY.

### Configuración global (correr **una sola vez** por máquina)

```bash
# Identidad — usar el mismo email del commit en GitHub
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"

# Estrategia de merge por defecto (no rebase automático)
git config --global pull.rebase false

# Editor por defecto
git config --global core.editor "code --wait"
```

**En Linux** añade además:

```bash
git config --global core.autocrlf false
git config --global core.eol lf
```

**En Windows** añade en su lugar:

```bash
git config --global core.autocrlf input
git config --global core.safecrlf false
```

> Por qué: el repo tiene `.gitattributes` que fuerza LF en todos los archivos de texto y Unity YAML. Si Windows convierte a CRLF detrás del telón, los merges de `.unity`/`.prefab` se corrompen.

### (Opcional) Git LFS

**No es obligatorio** para este proyecto: el pixel art pesa <100 KB por sprite y el repo se mantendrá pequeño. Solo actívalo si añadirán audio largo o fuentes pesadas.

```bash
# Linux
sudo pacman -S git-lfs
git lfs install   # una vez por máquina

# Windows: ya viene incluido con Git for Windows
git lfs install
```

---

## 3. IDE — VS Code

### Instalación

- **Arch Linux**: `sudo pacman -S code` (versión open-source) o `yay -S visual-studio-code-bin` (versión Microsoft con telemetría).
- **Windows**: <https://code.visualstudio.com/Download>.

### Extensiones obligatorias

```text
ms-dotnettools.csdevkit         # C# Dev Kit (oficial Microsoft, incluye debugger)
visualstudiotoolsforunity.vstuc # Visual Studio Tools for Unity
editorconfig.editorconfig       # Respeta el .editorconfig del repo
davidanson.vscode-markdownlint  # Lint para los docs
```

Instalar todas con un comando:

```bash
code --install-extension ms-dotnettools.csdevkit \
     --install-extension visualstudiotoolsforunity.vstuc \
     --install-extension editorconfig.editorconfig \
     --install-extension davidanson.vscode-markdownlint
```

### Conectar VS Code a Unity

En Unity: **Edit → Preferences → External Tools → External Script Editor → Visual Studio Code**. Luego clic en **Regenerate project files**.

---

## 4. Clonar el proyecto

```bash
cd ~/Development     # o donde guardes tus proyectos
git clone https://github.com/leonidas854/CAFECITO.git
cd CAFECITO
```

### Abrir en Unity por primera vez

1. Unity Hub → **Open → Add project from disk** → selecciona la carpeta `CAFECITO`.
2. Confirma que la versión que se muestra a la derecha del proyecto es `6000.3.16f1`. Si Unity Hub te pide actualizar, **no aceptes** — instala primero la versión correcta.
3. Abre el proyecto. La primera vez tarda 3-5 minutos reconstruyendo la carpeta `Library/`.
4. Cuando termine, abre la escena `Assets/_Project/Scenes/MainSimulation.unity` (se creará durante el Sprint 1; antes, abre `Assets/Scenes/SampleScene.unity` para verificar que el proyecto compila).

### Verificación de salud

Dentro de Unity, abre la consola (`Ctrl+Shift+C` / `Cmd+Shift+C`):

- ✅ No debe haber errores rojos.
- ⚠️ Warnings amarillos: aceptables si son de paquetes oficiales (`com.unity.*`).
- 🚨 Si ves errores de Burst Compiler en Linux: ignóralos, son de los toolchains opcionales.

---

## 5. Project Settings de Unity (validar una vez)

Estos ya deberían estar bien si todos clonan el repo, pero confírmalo el día 1:

| Setting | Ruta en Unity | Valor esperado |
|---|---|---|
| Asset Serialization | Edit → Project Settings → Editor | **Force Text** |
| Version Control | Edit → Project Settings → Editor | **Visible Meta Files** |
| Color Space | Edit → Project Settings → Player → Other Settings | **Linear** |
| Sprite Packer | Edit → Project Settings → Editor | **Sprite Atlas V2 - Enabled For Builds** |

Si alguno está mal, **avisa al equipo antes de cambiarlo** — modificar Project Settings genera diffs en archivos que afectan a todos.

---

## 6. Paquetes a agregar después (no ahora)

El día que se necesiten, **un solo integrante** los añade y hace PR:

- **DOTween** (animaciones tween): vía Asset Store → "DOTween (HOTween v2)" → Free version.
  - Alternativa OpenUPM: `openupm add com.demigiant.dotween`
- Cualquier paquete adicional debe ser discutido en el daily standup antes de añadirse.

---

## 7. Checklist final del Day 1

- [ ] Unity Hub instalado.
- [ ] Unity `6000.3.16f1` instalado con los 3 módulos de build (Linux, Windows, WebGL).
- [ ] Git instalado y configurado (`user.name`, `user.email`, `core.autocrlf` según SO).
- [ ] VS Code con las 4 extensiones.
- [ ] Proyecto clonado y abierto sin errores en consola.
- [ ] Te asignaron tu número de integrante (I1, I2, I3 o I4) y leíste tu rol en [05-roles.md](05-roles.md).
- [ ] Creaste tu rama local: `git checkout -b feature/I[N]-mi-primer-tarea`.

Si los 8 ítems están marcados, estás listo para programar.
