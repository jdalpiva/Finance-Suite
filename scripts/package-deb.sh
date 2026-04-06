#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_NAME="sme-finance-suite"
PACKAGE_ARCH="amd64"
INSTALL_DIR="/opt/sme-finance-suite"
EXECUTABLE_NAME="SMEFinanceSuite.Desktop"
BUILD_ARTIFACT_DIR="$ROOT_DIR/artifacts/desktop/linux-x64"
OUTPUT_DIR="$ROOT_DIR/artifacts/packages"
STAGING_DIR="$ROOT_DIR/artifacts/deb/staging"
CONTROL_TEMPLATE="$ROOT_DIR/packaging/deb/control.template"
LAUNCHER_TEMPLATE="$ROOT_DIR/packaging/deb/launcher.template"
DESKTOP_ENTRY_TEMPLATE="$ROOT_DIR/packaging/deb/sme-finance-suite.desktop"
PROJECT_FILE="$ROOT_DIR/src/App.Desktop/App.Desktop.csproj"

while (($# > 0)); do
    case "$1" in
        *)
            echo "Opcao desconhecida: $1" >&2
            echo "Uso: ./scripts/package-deb.sh" >&2
            exit 1
            ;;
    esac
done

if ! command -v dpkg-deb >/dev/null 2>&1; then
    echo "dpkg-deb nao encontrado. Instale o pacote dpkg antes de gerar o .deb." >&2
    exit 1
fi

PACKAGE_VERSION="$(grep -oPm1 '(?<=<Version>)[^<]+' "$PROJECT_FILE")"

if [[ -z "$PACKAGE_VERSION" ]]; then
    echo "Nao foi possivel determinar a versao em $PROJECT_FILE." >&2
    exit 1
fi

if [[ ! -x "$BUILD_ARTIFACT_DIR/$EXECUTABLE_NAME" ]]; then
    echo "Executavel nao encontrado em $BUILD_ARTIFACT_DIR/$EXECUTABLE_NAME." >&2
    echo "Gere primeiro o release linux-x64 com os comandos documentados no README." >&2
    exit 1
fi

PACKAGE_FILE="$OUTPUT_DIR/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCH}.deb"
PACKAGE_ROOT="$STAGING_DIR/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCH}"

rm -rf "$PACKAGE_ROOT"
mkdir -p "$PACKAGE_ROOT/DEBIAN"
mkdir -p "$PACKAGE_ROOT$INSTALL_DIR"
mkdir -p "$PACKAGE_ROOT/usr/bin"
mkdir -p "$PACKAGE_ROOT/usr/share/applications"
mkdir -p "$OUTPUT_DIR"

cp -R "$BUILD_ARTIFACT_DIR"/. "$PACKAGE_ROOT$INSTALL_DIR/"

sed \
    -e "s|{{PACKAGE_NAME}}|$PACKAGE_NAME|g" \
    -e "s|{{VERSION}}|$PACKAGE_VERSION|g" \
    "$CONTROL_TEMPLATE" > "$PACKAGE_ROOT/DEBIAN/control"

sed \
    -e "s|{{INSTALL_DIR}}|$INSTALL_DIR|g" \
    -e "s|{{APP_EXECUTABLE}}|$EXECUTABLE_NAME|g" \
    "$LAUNCHER_TEMPLATE" > "$PACKAGE_ROOT/usr/bin/$PACKAGE_NAME"

cp "$DESKTOP_ENTRY_TEMPLATE" "$PACKAGE_ROOT/usr/share/applications/$PACKAGE_NAME.desktop"

chmod 0755 "$PACKAGE_ROOT/usr/bin/$PACKAGE_NAME"
chmod 0755 "$PACKAGE_ROOT$INSTALL_DIR/$EXECUTABLE_NAME"
chmod 0644 "$PACKAGE_ROOT/DEBIAN/control"
chmod 0644 "$PACKAGE_ROOT/usr/share/applications/$PACKAGE_NAME.desktop"

dpkg-deb --root-owner-group --build "$PACKAGE_ROOT" "$PACKAGE_FILE"

echo "Pacote gerado em: $PACKAGE_FILE"
