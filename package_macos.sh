#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$ROOT_DIR/src/Zhijian/Zhijian.csproj"
SOLUTION_PATH="$ROOT_DIR/Zhijian.slnx"
APP_NAME="${APP_NAME:-Zhijian}"
BUNDLE_ID="${BUNDLE_ID:-com.codewf.zhijian}"
ICON_SOURCE="${ICON_SOURCE:-$ROOT_DIR/src/Zhijian/Assets/logo.png}"
PUBLISH_ROOT="${PUBLISH_ROOT:-$ROOT_DIR/publish}"
ARTIFACTS_ROOT="${ARTIFACTS_ROOT:-$ROOT_DIR/artifacts/macos}"
WORK_ROOT="$ARTIFACTS_ROOT/work"
CONFIGURATION="${CONFIGURATION:-Release}"
DOTNET_CMD="${DOTNET_CMD:-}"
CODESIGN_IDENTITY="${CODESIGN_IDENTITY:-}"
ENTITLEMENTS="${ENTITLEMENTS:-}"
NOTARIZE="${NOTARIZE:-0}"
NOTARY_KEYCHAIN_PROFILE="${NOTARY_KEYCHAIN_PROFILE:-}"

usage() {
  cat <<USAGE
Usage:
  ./package_macos.sh [osx-x64|osx-arm64|all]

Examples:
  ./package_macos.sh
  ./package_macos.sh osx-arm64
  CODESIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" ./package_macos.sh all
  CODESIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" NOTARIZE=1 NOTARY_KEYCHAIN_PROFILE=zhijian-notary ./package_macos.sh osx-arm64

Environment:
  APP_NAME                 App bundle name. Default: Zhijian
  BUNDLE_ID                CFBundleIdentifier. Default: com.codewf.zhijian
  DOTNET_CMD               Optional dotnet executable path.
  CODESIGN_IDENTITY        Developer ID identity. Empty means ad-hoc signing.
  ENTITLEMENTS             Optional entitlements plist used with Developer ID signing.
  NOTARIZE                 Set to 1 to submit DMGs with xcrun notarytool.
  NOTARY_KEYCHAIN_PROFILE  notarytool keychain profile name.
USAGE
}

die() {
  echo "Error: $*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "Missing required command: $1"
}

dotnet_has_net10_sdk() {
  local dotnet_path="$1"

  [[ -x "$dotnet_path" ]] || return 1
  "$dotnet_path" --list-sdks 2>/dev/null | grep -Eq '^10\.'
}

resolve_dotnet() {
  local configured="$DOTNET_CMD"
  local path_dotnet
  local home_dotnet="$HOME/.dotnet/dotnet"

  if [[ -n "$configured" ]]; then
    [[ -x "$configured" ]] || die "DOTNET_CMD does not point to an executable: $configured"
    dotnet_has_net10_sdk "$configured" || die "DOTNET_CMD does not have a .NET 10 SDK: $configured"
    DOTNET_CMD="$configured"
    return 0
  fi

  path_dotnet="$(command -v dotnet || true)"
  if [[ -n "$path_dotnet" ]] && dotnet_has_net10_sdk "$path_dotnet"; then
    DOTNET_CMD="$path_dotnet"
    return 0
  fi

  if dotnet_has_net10_sdk "$home_dotnet"; then
    DOTNET_CMD="$home_dotnet"
    return 0
  fi

  die "A .NET 10 SDK is required. Install it first, or set DOTNET_CMD=/path/to/dotnet."
}

resolve_version() {
  local version
  version="$("$DOTNET_CMD" msbuild "$PROJECT_PATH" -getProperty:Version -nologo 2>/dev/null | tail -n 1 | tr -d '\r' || true)"

  if [[ -z "$version" ]]; then
    version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT_DIR/Directory.Build.props" | head -n 1)"
  fi

  [[ -n "$version" ]] || die "Unable to resolve project version."
  echo "$version"
}

to_macos_version() {
  local version="$1"
  local core
  local major
  local minor
  local patch

  core="${version%%[-+]*}"
  IFS='.' read -r major minor patch _ <<<"$core"

  major="${major:-0}"
  minor="${minor:-0}"
  patch="${patch:-0}"

  [[ "$major" =~ ^[0-9]+$ ]] || major="0"
  [[ "$minor" =~ ^[0-9]+$ ]] || minor="0"
  [[ "$patch" =~ ^[0-9]+$ ]] || patch="0"

  echo "$major.$minor.$patch"
}

create_icon() {
  local icon_path="$1"
  local iconset_dir="$2"

  rm -rf "$iconset_dir"
  mkdir -p "$iconset_dir"

  sips -z 16 16 "$ICON_SOURCE" --out "$iconset_dir/icon_16x16.png" >/dev/null
  sips -z 32 32 "$ICON_SOURCE" --out "$iconset_dir/icon_16x16@2x.png" >/dev/null
  sips -z 32 32 "$ICON_SOURCE" --out "$iconset_dir/icon_32x32.png" >/dev/null
  sips -z 64 64 "$ICON_SOURCE" --out "$iconset_dir/icon_32x32@2x.png" >/dev/null
  sips -z 128 128 "$ICON_SOURCE" --out "$iconset_dir/icon_128x128.png" >/dev/null
  sips -z 256 256 "$ICON_SOURCE" --out "$iconset_dir/icon_128x128@2x.png" >/dev/null
  sips -z 256 256 "$ICON_SOURCE" --out "$iconset_dir/icon_256x256.png" >/dev/null
  sips -z 512 512 "$ICON_SOURCE" --out "$iconset_dir/icon_256x256@2x.png" >/dev/null
  sips -z 512 512 "$ICON_SOURCE" --out "$iconset_dir/icon_512x512.png" >/dev/null
  sips -z 1024 1024 "$ICON_SOURCE" --out "$iconset_dir/icon_512x512@2x.png" >/dev/null

  iconutil -c icns "$iconset_dir" -o "$icon_path"
}

write_info_plist() {
  local plist_path="$1"
  local macos_version="$2"

  cat >"$plist_path" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleDisplayName</key>
  <string>$APP_NAME</string>
  <key>CFBundleExecutable</key>
  <string>$APP_NAME</string>
  <key>CFBundleIconFile</key>
  <string>$APP_NAME</string>
  <key>CFBundleIdentifier</key>
  <string>$BUNDLE_ID</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>$APP_NAME</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$macos_version</string>
  <key>CFBundleVersion</key>
  <string>$macos_version</string>
  <key>LSApplicationCategoryType</key>
  <string>public.app-category.productivity</string>
  <key>CFBundleDocumentTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeExtensions</key>
      <array>
        <string>md</string>
        <string>markdown</string>
      </array>
      <key>CFBundleTypeIconFile</key>
      <string>$APP_NAME</string>
      <key>CFBundleTypeName</key>
      <string>Markdown</string>
      <key>CFBundleTypeRole</key>
      <string>Editor</string>
      <key>LSHandlerRank</key>
      <string>Owner</string>
      <key>LSItemContentTypes</key>
      <array>
        <string>net.daringfireball.markdown</string>
      </array>
    </dict>
  </array>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST
}

publish_app() {
  local rid="$1"
  local publish_dir="$PUBLISH_ROOT/$rid/$APP_NAME"

  rm -rf "$publish_dir"
  mkdir -p "$publish_dir"

  echo "Publishing $APP_NAME for $rid..."
  "$DOTNET_CMD" publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -f net10.0 \
    -r "$rid" \
    --self-contained true \
    -p:PublishProfile="FolderProfile_$rid" \
    -p:PublishDir="$publish_dir/"

  [[ -x "$publish_dir/$APP_NAME" ]] || die "Published executable was not found: $publish_dir/$APP_NAME"
}

create_app_bundle() {
  local rid="$1"
  local macos_version="$2"
  local publish_dir="$PUBLISH_ROOT/$rid/$APP_NAME"
  local app_dir="$WORK_ROOT/$rid/$APP_NAME.app"
  local contents_dir="$app_dir/Contents"
  local macos_dir="$contents_dir/MacOS"
  local resources_dir="$contents_dir/Resources"
  local iconset_dir="$WORK_ROOT/$rid/$APP_NAME.iconset"

  rm -rf "$app_dir"
  mkdir -p "$macos_dir" "$resources_dir"

  ditto "$publish_dir" "$macos_dir"
  chmod +x "$macos_dir/$APP_NAME"
  create_icon "$resources_dir/$APP_NAME.icns" "$iconset_dir"
  write_info_plist "$contents_dir/Info.plist" "$macos_version"

  echo "$app_dir"
}

sign_app_bundle() {
  local app_dir="$1"

  if [[ -n "$CODESIGN_IDENTITY" ]]; then
    local sign_args=(--force --deep --options runtime --timestamp --sign "$CODESIGN_IDENTITY")
    if [[ -n "$ENTITLEMENTS" ]]; then
      [[ -f "$ENTITLEMENTS" ]] || die "Entitlements file does not exist: $ENTITLEMENTS"
      sign_args+=(--entitlements "$ENTITLEMENTS")
    fi

    echo "Signing app with Developer ID..."
    codesign "${sign_args[@]}" "$app_dir"
  else
    echo "Ad-hoc signing app..."
    codesign --force --deep --sign - "$app_dir"
  fi

  codesign --verify --deep --strict "$app_dir"
}

create_dmg() {
  local rid="$1"
  local version="$2"
  local app_dir="$3"
  local dmg_stage="$WORK_ROOT/dmg-$rid"
  local dmg_path="$ARTIFACTS_ROOT/$APP_NAME-$version-$rid.dmg"

  rm -rf "$dmg_stage" "$dmg_path"
  mkdir -p "$dmg_stage"

  ditto "$app_dir" "$dmg_stage/$APP_NAME.app"
  ln -s /Applications "$dmg_stage/Applications"

  hdiutil create \
    -volname "$APP_NAME" \
    -srcfolder "$dmg_stage" \
    -ov \
    -format UDZO \
    "$dmg_path" >/dev/null

  if [[ -n "$CODESIGN_IDENTITY" ]]; then
    codesign --force --timestamp --sign "$CODESIGN_IDENTITY" "$dmg_path"
  fi

  echo "$dmg_path"
}

notarize_dmg() {
  local dmg_path="$1"

  [[ "$NOTARIZE" == "1" ]] || return 0
  [[ -n "$CODESIGN_IDENTITY" ]] || die "NOTARIZE=1 requires CODESIGN_IDENTITY."
  [[ -n "$NOTARY_KEYCHAIN_PROFILE" ]] || die "NOTARIZE=1 requires NOTARY_KEYCHAIN_PROFILE."

  echo "Submitting for notarization..."
  xcrun notarytool submit "$dmg_path" --keychain-profile "$NOTARY_KEYCHAIN_PROFILE" --wait
  xcrun stapler staple "$dmg_path"
}

package_rid() {
  local rid="$1"
  local version="$2"
  local macos_version="$3"
  local app_dir
  local dmg_path

  publish_app "$rid"
  app_dir="$(create_app_bundle "$rid" "$macos_version")"
  sign_app_bundle "$app_dir"
  dmg_path="$(create_dmg "$rid" "$version" "$app_dir")"
  notarize_dmg "$dmg_path"

  echo "Created: $dmg_path"
}

main() {
  local target="${1:-all}"
  local rids=()
  local version
  local macos_version

  if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
  fi

  [[ "$(uname -s)" == "Darwin" ]] || die "macOS packaging must run on macOS."
  require_command sips
  require_command iconutil
  require_command ditto
  require_command hdiutil
  require_command codesign
  resolve_dotnet

  case "$target" in
    all)
      rids=(osx-x64 osx-arm64)
      ;;
    osx-x64|osx-arm64)
      rids=("$target")
      ;;
    *)
      usage
      die "Unknown target: $target"
      ;;
  esac

  [[ -f "$ICON_SOURCE" ]] || die "Icon source does not exist: $ICON_SOURCE"
  mkdir -p "$ARTIFACTS_ROOT" "$WORK_ROOT"

  version="$(resolve_version)"
  macos_version="$(to_macos_version "$version")"
  echo "Using dotnet: $DOTNET_CMD"
  echo "Restoring solution..."
  "$DOTNET_CMD" restore "$SOLUTION_PATH"

  for rid in "${rids[@]}"; do
    package_rid "$rid" "$version" "$macos_version"
  done

  echo "Done. DMGs are available in: $ARTIFACTS_ROOT"
}

main "$@"
