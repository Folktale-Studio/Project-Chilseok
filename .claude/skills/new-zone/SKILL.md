---
name: new-zone
description: "별무리 구간 하나를 설계한다. 초안 2~3개를 텍스트 데이터로 쓰고, 솔버로 풀이 가능 여부와 최소 회전 수를 검증해 사람이 고르게 한다."
argument-hint: "<구간 번호> [새로 배우는 것]"
disable-model-invocation: true
---

`$ARGUMENTS` 구간을 설계한다. 이 스킬이 "AI가 초안을 내고, 솔버가 검증하고, 사람이 고른다"는 워크플로우의 핵심이다.

1. **목표 확인**: `design/game-design.md`의 구간 구성 표에서 이 구간의 "새로 배우는 것"과 목표 최소 회전 수를 읽는다. 인자로 받은 내용이 있으면 그것을 우선한다. 앞 구간 파일을 읽어 이미 가르친 것을 확인한다.
2. **초안**: `game-designer` 에이전트에게 초안 2~3개를 맡긴다. 각 초안은 `design/perspective-core.md`의 구간 데이터 형식으로, `intent`(의도한 풀이)를 반드시 포함한다. 초안은 `production/zones/zone-NN-draft-{a,b,c}.txt`에 쓴다(이 경로가 쓰기 승인이다).
3. **검증**: 솔버 실행 메뉴나 EditMode 테스트로 초안마다 다음을 구한다. 솔버가 아직 없으면(progress.md Phase 1 전) 손으로 풀이를 추적하고 "솔버 미검증"이라고 표시한다.
   - 풀이 가능 여부, 최소 회전 수, 최소 행동 수, 탐색 상태 수, 막다른 상태 비율
   - 의도한 풀이와 솔버 최단 풀이가 다른지(지름길 발견)
4. **비교표**를 사용자에게 보여 준다: 초안 | 최소 회전 | 최소 행동 | 막다른 비율 | 지름길 | 한 줄 평. 추천 하나와 이유를 붙인다.
5. 사용자가 고르면 `Assets/Chilseok/Levels/zone-NN.txt`로 옮기고 `unity-quality-gate`(`-Checks compile,editmode`)로 `ZoneSolverTests`를 통과시킨다.
6. `production/metrics/zones.md`의 구간 지표 표에 한 줄을 추가한다(README의 지표 표가 여기서 나온다).
