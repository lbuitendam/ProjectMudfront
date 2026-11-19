#!/usr/bin/env bash
# ECON-04 real multi-process harness: 1 headless dedicated server + scripted clients, separate processes.
# Usage: tools/net/run_econ04_harness.sh [outdir]   (build first: NetHarnessBuilder.BuildPlayer)
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
EXE="$ROOT/Builds/NetHarness/MudfrontNet.exe"
OUT="${1:-$ROOT/Builds/NetHarness/run}"
rm -rf "$OUT"; mkdir -p "$OUT"
PORT=7791
SECRET="$(python -c 'import secrets;print(secrets.token_hex(16))')"
tok() { python -c "import hmac,hashlib,sys;print(hmac.new(sys.argv[2].encode(),sys.argv[1].encode(),hashlib.sha256).hexdigest())" "$1" "$SECRET"; }
TA="$(tok acc-a)"; TB="$(tok acc-b)"
LEDGER="$OUT/server_ledger.jsonl"
STOP="$OUT/stop"

common=(-batchmode -nographics -mfPort $PORT)
"$EXE" "${common[@]}" -mfRole server -mfSecret "$SECRET" -mfLedger "$LEDGER" -mfExpect 2 -mfRewardAccount acc-b \
  -mfStopFile "$STOP" -mfTimeout 90 -mfOut "$OUT/server.txt" -logFile "$OUT/server.log" &
SP=$!
sleep 4

# B: honest client that should receive a server-side reward; holds its session open.
"$EXE" "${common[@]}" -mfRole client -mfAccount acc-b -mfToken "$TB" -mfScenario rewarded -mfExpectBalance 10150 -mfHold 6 \
  -mfOut "$OUT/client_b.txt" -logFile "$OUT/client_b.log" &
BP=$!
# A: attacker — direct reward claim, forged acting account, forged price.
"$EXE" "${common[@]}" -mfRole client -mfAccount acc-a -mfToken "$TA" -mfScenario attacker -mfVictim acc-b -mfExpectBalance 10000 \
  -mfOut "$OUT/client_a.txt" -logFile "$OUT/client_a.log" &
AP=$!
sleep 3
# Forged identity: claims acc-b using acc-a's token.
"$EXE" "${common[@]}" -mfRole client -mfAccount acc-b -mfToken "$TA" -mfScenario expect_auth_fail \
  -mfOut "$OUT/client_forged.txt" -logFile "$OUT/client_forged.log" &
FP=$!
# Duplicate session: valid acc-b credentials while B is still connected.
"$EXE" "${common[@]}" -mfRole client -mfAccount acc-b -mfToken "$TB" -mfScenario expect_auth_fail \
  -mfOut "$OUT/client_dup.txt" -logFile "$OUT/client_dup.log" &
DP=$!

rc=0
for n in A:$AP B:$BP FORGED:$FP DUP:$DP; do
  wait "${n#*:}"; c=$?; echo "client ${n%%:*} exit=$c"; [ $c -eq 0 ] || rc=1
done
touch "$STOP"; wait $SP; c=$?; echo "server exit=$c"; [ $c -eq 0 ] || rc=1
for f in server client_a client_b client_forged client_dup; do echo "--- $f"; cat "$OUT/$f.txt" 2>/dev/null || echo "(no result file)"; done
echo "HARNESS $([ $rc -eq 0 ] && echo PASS || echo FAIL)"
exit $rc
