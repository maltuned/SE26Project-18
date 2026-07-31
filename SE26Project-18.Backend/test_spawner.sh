#!/bin/bash
# ============================================================
# 测试数据生成脚本（完全动态 ID，可重复运行）
# 用法: bash test_spawner.sh
# 前提: 后端已启动，数据库已导入 seed_data.sql
# ============================================================
set -e

BASE_URL="http://localhost:5111/api/v1"

# ---------- reliable JSON parsing via Python ----------
# jval <key> reads JSON from stdin, looks in data.<key> then top-level <key>
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

# ---------- helpers ----------
reg() {
  curl -s -X POST "$BASE_URL/auth/register" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$1\",\"password\":\"$2\"}"
}

login() {
  curl -s -X POST "$BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$1\",\"password\":\"$2\"}"
}

me() {
  curl -s "$BASE_URL/auth/me" -H "Authorization: Bearer $1"
}

new_rec() {
  printf '{"publisher_id":%s,"game_id":%s,"title":"%s","description":"%s","status":"open","tags_id":[%s],"max_participants":%s,"current_participants":0,"expired_at":"%s"}' \
    "$2" "$3" "$4" "$5" "$6" "$7" "$8" \
  | curl -s -X POST "$BASE_URL/recruitments" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

respond() {
  printf '{"recruitment_id":%s,"responser_id":%s}' \
    "$2" "$3" \
  | curl -s -X POST "$BASE_URL/responses" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

accept() {
  printf '{"id":%s,"response_status":"accepted"}' \
    "$2" \
  | curl -s -X POST "$BASE_URL/responses/status" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

create_chat() {
  printf '{"recruitment_id":%s,"user1_id":%s,"user2_id":%s}' \
    "$2" "$3" "$4" \
  | curl -s -X POST "$BASE_URL/chats/create" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

msg() {
  printf '{"chat_id":%s,"sender_id":%s,"receiver_id":%s,"content":"%s"}' \
    "$2" "$3" "$4" "$5" \
  | curl -s -X POST "$BASE_URL/messages" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

review() {
  printf '{"reviewee_id":%s,"content":"%s"}' \
    "$2" "$3" \
  | curl -s -X POST "$BASE_URL/review" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

report() {
  printf '{"target_type":"%s","target_id":%s,"violation_type":"%s","content":"%s"}' \
    "$2" "$3" "$4" "$5" \
  | curl -s -X POST "$BASE_URL/report" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

feedback() {
  printf '{"type":"%s","content":"%s"}' \
    "$2" "$3" \
  | curl -s -X POST "$BASE_URL/feedback" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $1" \
    -d @-
}

echo "============================================"
echo "  Step 1: Register test1 ~ test9"
echo "============================================"
for i in $(seq 1 9); do
  echo -n "  Registering test$i ... "
  reg "test$i" "123456"
  echo ""
done

echo ""
echo "============================================"
echo "  Step 2: Login & get user IDs"
echo "============================================"
T1=$(login test1 123456 | jval "access_token")
T2=$(login test2 123456 | jval "access_token")
T3=$(login test3 123456 | jval "access_token")
T4=$(login test4 123456 | jval "access_token")
T5=$(login test5 123456 | jval "access_token")
T6=$(login test6 123456 | jval "access_token")
T7=$(login test7 123456 | jval "access_token")
T8=$(login test8 123456 | jval "access_token")
T9=$(login test9 123456 | jval "access_token")

U1=$(me "$T1" | jval "id")
U2=$(me "$T2" | jval "id")
U3=$(me "$T3" | jval "id")
U4=$(me "$T4" | jval "id")
U5=$(me "$T5" | jval "id")
U6=$(me "$T6" | jval "id")
U7=$(me "$T7" | jval "id")
U8=$(me "$T8" | jval "id")
U9=$(me "$T9" | jval "id")
echo "  U1=$U1 U2=$U2 U3=$U3 U4=$U4 U5=$U5 U6=$U6 U7=$U7 U8=$U8 U9=$U9"

echo ""
echo "============================================"
echo "  Step 3: Create recruitments"
echo "============================================"
echo -n "  test1 - 英雄联盟 ... "
REC1=$(new_rec "$T1" "$U1" 1 "钻石段位找双排队友" "主打AD位，意识好不喷人，周末晚上稳定在线" "1,4,16" 2 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC1"

echo -n "  test1 - 原神 ... "
REC2=$(new_rec "$T1" "$U1" 2 "新周本刷材料队伍" "世界等级8，每天上线清体力，需要奶妈和辅助" "5,6,12" 4 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC2"

echo -n "  test2 - 崩坏星穹铁道 ... "
REC3=$(new_rec "$T2" "$U2" 3 "模拟宇宙速刷车队" "熟练模拟宇宙各命途，速刷位面饰品，有符玄/银狼优先" "6,13,20" 3 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC3"

echo -n "  test2 - 英雄联盟(大乱斗) ... "
REC4=$(new_rec "$T2" "$U2" 1 "大乱斗娱乐开黑" "不排位，只打极地大乱斗，开心就好" "3,7,15" 5 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC4"

echo -n "  test3 - 绝区零 ... "
REC5=$(new_rec "$T3" "$U3" 4 "空洞探险固定队" "每晚8点开始，稳定清空洞，来有练度的绳匠" "10,11,8" 3 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC5"

echo -n "  test3 - 和平精英 ... "
REC6=$(new_rec "$T3" "$U3" 5 "战神冲分车队" "KD3.0以上，会报点会配合，海岛图专场" "4,6,9" 4 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC6"

echo -n "  test4 - 无畏契约 ... "
REC7=$(new_rec "$T4" "$U4" 8 "白金上钻石车队" "主玩决斗，枪法稳定，需要烟位和信息位" "4,6,8" 5 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC7"

echo -n "  test5 - 艾尔登法环 ... "
REC8=$(new_rec "$T5" "$U5" 9 "联机打Boss互助" "卡在女武神了，求大佬带，或者一起探索圣树分支" "2,5,9" 3 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC8"

echo -n "  test6 - 我的世界 ... "
REC9=$(new_rec "$T6" "$U6" 10 "纯净生存服招新" "长期服务器，有商店和传送系统，来爱建筑或红石的玩家" "7,10,11" 10 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC9"

echo -n "  test7 - 明日方舟 ... "
REC10=$(new_rec "$T7" "$U7" 11 "集成战略打法交流" "肉鸽模式深度玩家，分享各分队开局思路" "9,11,13" 4 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC10"

echo -n "  test8 - 韵律源点 ... "
REC11=$(new_rec "$T8" "$U8" 12 "音游同好交流" "Arcaea谱面研究，PTT11.5，一起练歌互相鼓励" "3,7,15" 5 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC11"

echo -n "  test9 - 舞萌DX ... "
REC12=$(new_rec "$T9" "$U9" 14 "周末出勤街机厅" "上海徐汇附近，周末一起去打mai，我请饮料" "3,7,9" 4 "2026-12-31 23:59:59" | jval "id")
echo "id=$REC12"

echo ""
echo "============================================"
echo "  Step 4: Create responses"
echo "============================================"
echo -n "  test2 -> test1-rec1 (英雄联盟) ... "
RES1=$(respond "$T2" "$REC1" "$U2" | jval "id")
echo "id=$RES1"

echo -n "  test3 -> test1-rec1 (英雄联盟) ... "
RES2=$(respond "$T3" "$REC1" "$U3" | jval "id")
echo "id=$RES2"

echo -n "  test1 -> test2-rec1 (崩坏星穹铁道) ... "
RES3=$(respond "$T1" "$REC3" "$U1" | jval "id")
echo "id=$RES3"

echo -n "  test4 -> test2-rec1 (崩坏星穹铁道) ... "
RES4=$(respond "$T4" "$REC3" "$U4" | jval "id")
echo "id=$RES4"

echo -n "  test5 -> test3-rec1 (绝区零) ... "
RES5=$(respond "$T5" "$REC5" "$U5" | jval "id")
echo "id=$RES5"

echo -n "  test6 -> test3-rec2 (和平精英) ... "
RES6=$(respond "$T6" "$REC6" "$U6" | jval "id")
echo "id=$RES6"

echo -n "  test7 -> test4-rec1 (无畏契约) ... "
RES7=$(respond "$T7" "$REC7" "$U7" | jval "id")
echo "id=$RES7"

echo -n "  test8 -> test5-rec1 (艾尔登法环) ... "
RES8=$(respond "$T8" "$REC8" "$U8" | jval "id")
echo "id=$RES8"

echo -n "  test9 -> test6-rec1 (我的世界) ... "
RES9=$(respond "$T9" "$REC9" "$U9" | jval "id")
echo "id=$RES9"

echo -n "  test2 -> test7-rec1 (明日方舟) ... "
RES10=$(respond "$T2" "$REC10" "$U2" | jval "id")
echo "id=$RES10"

echo ""
echo "============================================"
echo "  Step 5: Accept responses & create chats"
echo "============================================"
echo -n "  test1 accepts RES1 (test2) ... "
accept "$T1" "$RES1"
echo "ok, creating chat..."
CHAT1=$(create_chat "$T1" "$REC1" "$U1" "$U2" | jval "id")
echo "  chat_id=$CHAT1"

echo -n "  test1 accepts RES2 (test3) ... "
accept "$T1" "$RES2"
echo "ok, creating chat..."
CHAT2=$(create_chat "$T1" "$REC1" "$U1" "$U3" | jval "id")
echo "  chat_id=$CHAT2"

echo -n "  test2 accepts RES3 (test1) ... "
accept "$T2" "$RES3"
echo "ok, creating chat..."
CHAT3=$(create_chat "$T2" "$REC3" "$U2" "$U1" | jval "id")
echo "  chat_id=$CHAT3"

echo -n "  test2 accepts RES4 (test4) ... "
accept "$T2" "$RES4"
echo "ok, creating chat..."
CHAT4=$(create_chat "$T2" "$REC3" "$U2" "$U4" | jval "id")
echo "  chat_id=$CHAT4"

echo -n "  test3 accepts RES5 (test5) ... "
accept "$T3" "$RES5"
echo "ok, creating chat..."
CHAT5=$(create_chat "$T3" "$REC5" "$U3" "$U5" | jval "id")
echo "  chat_id=$CHAT5"

echo -n "  test4 accepts RES7 (test7) ... "
accept "$T4" "$RES7"
echo "ok, creating chat..."
CHAT6=$(create_chat "$T4" "$REC7" "$U4" "$U7" | jval "id")
echo "  chat_id=$CHAT6"

echo ""
echo "============================================"
echo "  Step 6: Send messages"
echo "============================================"
echo -n "  test1 -> test2 (chat$CHAT1) ... "
msg "$T1" "$CHAT1" "$U1" "$U2" "你好！你主打什么位置？"
echo ""
echo -n "  test2 -> test1 (chat$CHAT1) ... "
msg "$T2" "$CHAT1" "$U2" "$U1" "我辅助，锤石和露露都行，你呢？"
echo ""
echo -n "  test1 -> test2 (chat$CHAT1) ... "
msg "$T1" "$CHAT1" "$U1" "$U2" "我AD，正好。周末晚上8点开打？"
echo ""

echo -n "  test3 -> test1 (chat$CHAT2) ... "
msg "$T3" "$CHAT2" "$U3" "$U1" "大佬还缺人吗？我打野位"
echo ""
echo -n "  test1 -> test3 (chat$CHAT2) ... "
msg "$T1" "$CHAT2" "$U1" "$U3" "缺！你主玩什么打野？"
echo ""

echo -n "  test2 -> test1 (chat$CHAT3) ... "
msg "$T2" "$CHAT3" "$U2" "$U1" "星铁一起刷模拟宇宙，我有符玄"
echo ""
echo -n "  test1 -> test2 (chat$CHAT3) ... "
msg "$T1" "$CHAT3" "$U1" "$U2" "太好了！晚上组队"
echo ""

echo -n "  test4 -> test2 (chat$CHAT4) ... "
msg "$T4" "$CHAT4" "$U4" "$U2" "请问模拟宇宙需要什么命途？"
echo ""

echo -n "  test3 -> test5 (chat$CHAT5) ... "
msg "$T3" "$CHAT5" "$U3" "$U5" "空洞探险固定队，你练度怎么样？"
echo ""
echo -n "  test5 -> test3 (chat$CHAT5) ... "
msg "$T5" "$CHAT5" "$U5" "$U3" "角色都拉满了，放心！"
echo ""

echo -n "  test7 -> test4 (chat$CHAT6) ... "
msg "$T7" "$CHAT6" "$U7" "$U4" "无畏契约一起上分吗？"
echo ""

echo ""
echo "============================================"
echo "  Step 7: Create reviews"
echo "============================================"
echo -n "  test1 reviews test2 ... "
review "$T1" "$U2" "技术很棒，配合默契，语音沟通也很顺畅，强烈推荐！"
echo ""
echo -n "  test2 reviews test1 ... "
review "$T2" "$U1" "AD打得很稳，心态好，逆风也不喷人，靠谱队友"
echo ""
echo -n "  test3 reviews test4 ... "
review "$T3" "$U4" "一起刷模拟宇宙效率很高，理解到位"
echo ""
echo -n "  test4 reviews test3 ... "
review "$T4" "$U3" "绝区零固定队友，每晚准时上线，非常稳定"
echo ""
echo -n "  test5 reviews test6 ... "
review "$T5" "$U6" "吃鸡配合不错，报点及时，会拉枪线"
echo ""
echo -n "  test6 reviews test5 ... "
review "$T6" "$U5" "艾尔登法环带了我好多boss，大佬牛逼"
echo ""
echo -n "  test7 reviews test8 ... "
review "$T7" "$U8" "一起研究明日方舟肉鸽打法，思路清晰"
echo ""
echo -n "  test8 reviews test7 ... "
review "$T8" "$U7" "Arcaea谱面分析很专业，帮我突破了好多瓶颈"
echo ""
echo -n "  test9 reviews test1 ... "
review "$T9" "$U1" "街机厅一起打mai很开心，技术好还愿意教"
echo ""

echo ""
echo "============================================"
echo "  Step 8: Submit reports"
echo "============================================"
REV_ID=$(curl -s "$BASE_URL/review/user/$U2" -H "Authorization: Bearer $T1" | jval "id")
echo "  Using review id=$REV_ID for report target"

echo -n "  test1 reports REC6 (广告) ... "
report "$T1" "招募" "$REC6" "广告" "这个招募里面包含代练广告信息，请管理员处理"
echo ""
echo -n "  test2 reports user-U4 (谩骂) ... "
report "$T2" "用户" "$U4" "谩骂" "该用户在聊天中多次辱骂队友，态度恶劣"
echo ""
echo -n "  test3 reports REC4 (欺诈) ... "
report "$T3" "招募" "$REC4" "欺诈" "这个招募涉嫌虚假信息，实际游戏水平和描述不符"
echo ""
echo -n "  test5 reports CHAT1 (色情) ... "
report "$T5" "聊天" "$CHAT1" "色情" "聊天内容包含不适当信息"
echo ""
echo -n "  test6 reports review-$REV_ID (谩骂) ... "
report "$T6" "评价" "$REV_ID" "谩骂" "这条评价含有恶意人身攻击"
echo ""
echo -n "  test8 reports user-U3 (涉政) ... "
report "$T8" "用户" "$U3" "涉政" "该用户发布不当言论"
echo ""
echo -n "  test9 reports REC7 (其他) ... "
report "$T9" "招募" "$REC7" "其他" "该招募引导用户使用外部平台，存在安全隐患"
echo ""

echo ""
echo "============================================"
echo "  Step 9: Submit feedback"
echo "============================================"
echo -n "  test1 - 内容反馈 ... "
feedback "$T1" "内容反馈" "希望游戏列表能增加更多独立游戏，比如Hades、空洞骑士等"
echo ""
echo -n "  test2 - 体验反馈 ... "
feedback "$T2" "体验反馈" "聊天界面希望增加消息撤回功能，有时候发错了很尴尬"
echo ""
echo -n "  test5 - 内容反馈 ... "
feedback "$T5" "内容反馈" "关于游戏「艾尔登法环」的反馈：希望能增加联机密码功能说明"
echo ""
echo -n "  test8 - 体验反馈 ... "
feedback "$T8" "体验反馈" "应用整体流畅度很好，但音游分类下希望能有更细的难度标注"
echo ""

echo ""
echo "============================================"
echo "  Done! All test data created."
echo "  U1=$U1 U2=$U2 U3=$U3 U4=$U4 U5=$U5 U6=$U6 U7=$U7 U8=$U8 U9=$U9"
echo "  REC1=$REC1  REC3=$REC3  REC5=$REC5  REC6=$REC6  REC7=$REC7"
echo "  CHAT1=$CHAT1  CHAT2=$CHAT2  CHAT3=$CHAT3  CHAT4=$CHAT4  CHAT5=$CHAT5  CHAT6=$CHAT6"
echo "============================================"