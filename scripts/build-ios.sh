#!/bin/zsh
# Unity → Xcodeプロジェクト → 署名済みビルド → .ipa まで一括で作る。
# Xcode 27 は iPadOS 16 の実機に直接転送できないため、できた .ipa を
# Finder（iPadの「一般」タブへドラッグ＆ドロップ）か Apple Configurator で入れる。
# 使い方: scripts/build-ios.sh [-development]
set -eu
ROOT=${0:A:h:h}
UNITY=/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity
BUILDS="$ROOT/KidsDrawing/Builds"

# Apple の Team ID は公開リポジトリに含めない。環境変数か、git管理外の scripts/local.env で渡す
#   例: echo 'APPLE_TEAM_ID=XXXXXXXXXX' > scripts/local.env
[[ -f "$ROOT/scripts/local.env" ]] && source "$ROOT/scripts/local.env"
: ${APPLE_TEAM_ID:?"APPLE_TEAM_ID を環境変数か scripts/local.env に設定してください"}

echo "==> Unity: Xcodeプロジェクトを出力"
"$UNITY" -batchmode -quit \
  -projectPath "$ROOT/KidsDrawing" \
  -buildTarget iOS \
  -executeMethod KidsDrawing.Editor.BuildScript.BuildIOS "$@" \
  -logFile "$ROOT/KidsDrawing/Logs/build-ios.log"

echo "==> Xcode: ビルドと署名"
xcodebuild -project "$BUILDS/iOS/Unity-iPhone.xcodeproj" -scheme Unity-iPhone \
  -configuration Release -destination 'generic/platform=iOS' \
  -derivedDataPath "$BUILDS/DerivedData" -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$APPLE_TEAM_ID" CODE_SIGN_STYLE=Automatic \
  build > "$BUILDS/xcodebuild.log" 2>&1 || { tail -30 "$BUILDS/xcodebuild.log"; exit 1; }

echo "==> .ipa を作成"
APP="$BUILDS/DerivedData/Build/Products/Release-iphoneos/ProductName.app"
STAGE=$(mktemp -d)
mkdir "$STAGE/Payload"
cp -R "$APP" "$STAGE/Payload/"
rm -f "$BUILDS/KidsDrawing.ipa"
(cd "$STAGE" && zip -qry "$BUILDS/KidsDrawing.ipa" Payload)
rm -rf "$STAGE"

echo "完成: $BUILDS/KidsDrawing.ipa"
