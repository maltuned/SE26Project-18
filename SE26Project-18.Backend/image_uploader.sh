#!/bin/bash
# ============================================================
#  image_uploader.sh — 批量上传游戏封面和图标
#  用法: bash image_uploader.sh
#  前提: 后端已启动, assets/covers/ 和 assets/icons/ 已准备好
# ============================================================
set -e

BASE_URL="http://localhost:5111/api/v1"
ASSETS_DIR="$(cd "$(dirname "$0")/assets" && pwd)"

# ---------- JSON parsing ----------
jval() {
  python -c "
import sys, json
d = json.load(sys.stdin)
data = d.get('data', d)
if isinstance(data, dict):
    v = data.get('$1', '')
elif isinstance(data, list) and len(data) > 0:
    v = data[0].get('$1', '')
else:
    v = ''
print(v)
" 2>/dev/null
}

echo "============================================"
echo "  Image Uploader"
echo "============================================"

# Step 1: 登录获取 token
echo ""
echo "  Logging in as admin..."
LOGIN_RES=$(curl -s --connect-timeout 5 --max-time 10 -X POST "$BASE_URL/Admin/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"123456"}')

TOKEN=$(echo "$LOGIN_RES" | jval "token")

if [ -z "$TOKEN" ]; then
  echo "  ERROR: 登录失败"
  echo "  Response: $LOGIN_RES"
  exit 1
fi
echo "  Token obtained."

# Step 2: 从 API 获取全部游戏 ID
echo ""
echo "  Fetching game IDs..."
GAMES_RES=$(curl -s --connect-timeout 5 --max-time 10 "$BASE_URL/Games" \
  -H "Authorization: Bearer $TOKEN")
GAME_IDS=$(echo "$GAMES_RES" | python -c "
import sys, json
d = json.load(sys.stdin)
ids = [str(item['id']) for item in d.get('data', [])]
print(' '.join(ids))
" 2>/dev/null)

if [ -z "$GAME_IDS" ]; then
  echo "  ERROR: 获取游戏列表失败"
  echo "  Response: $GAMES_RES"
  exit 1
fi
echo "  Found games: $GAME_IDS"

# Step 3: 批量上传封面和图标
echo ""
echo "  Uploading covers & icons..."
echo ""

SUCCESS=0
FAILED=0

for id in $GAME_IDS; do
  COVER_URL=""
  ICON_URL=""

  # 上传封面
  COVER="$ASSETS_DIR/covers/${id}.jpg"
  if [ -f "$COVER" ]; then
    echo -n "  Game $id - cover ... "
    RES=$(curl -s --connect-timeout 5 --max-time 30 -X POST "$BASE_URL/Image/upload" \
      -H "Authorization: Bearer $TOKEN" \
      -F "file=@$COVER" \
      -F "folder=covers" \
      -F "name=$id")
    if echo "$RES" | grep -q '"status":200'; then
      COVER_URL=$(echo "$RES" | python -c "import sys,json; print(json.load(sys.stdin)['data'])" 2>/dev/null)
      echo "OK"
      SUCCESS=$((SUCCESS + 1))
    else
      echo "FAILED: $RES"
      FAILED=$((FAILED + 1))
    fi
  else
    echo "  Game $id - cover ... SKIP (file not found: $COVER)"
  fi

  # 上传图标
  ICON="$ASSETS_DIR/icons/${id}.jpg"
  if [ -f "$ICON" ]; then
    echo -n "  Game $id - icon  ... "
    RES=$(curl -s --connect-timeout 5 --max-time 30 -X POST "$BASE_URL/Image/upload" \
      -H "Authorization: Bearer $TOKEN" \
      -F "file=@$ICON" \
      -F "folder=icons" \
      -F "name=$id")
    if echo "$RES" | grep -q '"status":200'; then
      ICON_URL=$(echo "$RES" | python -c "import sys,json; print(json.load(sys.stdin)['data'])" 2>/dev/null)
      echo "OK"
      SUCCESS=$((SUCCESS + 1))
    else
      echo "FAILED: $RES"
      FAILED=$((FAILED + 1))
    fi
  else
    echo "  Game $id - icon  ... SKIP (file not found: $ICON)"
  fi

  # 同步 URL 到数据库
  if [ -n "$COVER_URL" ] || [ -n "$ICON_URL" ]; then
    echo -n "  Game $id - sync  ... "
    SYNC_RES=$(curl -s --connect-timeout 5 --max-time 10 -X PUT "$BASE_URL/Admin/games/${id}/image" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "{\"cover\":\"$COVER_URL\",\"icon\":\"$ICON_URL\"}")
    if echo "$SYNC_RES" | grep -q '"status":200'; then
      echo "OK"
    else
      echo "FAILED: $SYNC_RES"
    fi
  fi
done

echo ""
echo "============================================"
echo "  Done! 成功: $SUCCESS  失败: $FAILED"
echo "============================================"
