---
name: playtest-report
description: "플레이 소감과 클리어 로그를 정리해 조정 항목과 다음 작업으로 바꾼다."
argument-hint: "<플레이 소감 또는 클리어 로그>"
disable-model-invocation: true
---

`$ARGUMENTS`(플레이하며 느낀 점, 클리어 로그 `{level, rotations, jumps, time, resets}`)를 정리한다.

1. 소감을 "조작감 / 퍼즐 / 카메라·회전 / 화면(디더링) / 시네마틱·서사 / 버그"로 분류한다.
2. 클리어 로그가 있으면 구간별로 "솔버 최소 회전 수 / 실제 회전 수 / 효율(최소÷실제) / 리셋 수" 표를 만든다. 솔버 값은 `production/metrics/zones.md`에서 읽는다.
3. 각 항목을 구체적인 조정으로 바꾼다: 어떤 컴포넌트의 어떤 필드를 얼마로(예: `PlayerController2D.coyoteTime` 0.08 → 0.12), 또는 어떤 구간을 어떻게(예: zone-04 순서를 zone-05 뒤로).
4. 버그는 `/bug-report` 대상으로, 새 기능은 `/quick-design` 대상으로, 구간 수정은 `/new-zone` 대상으로 표시한다.
5. 결과를 `production/playtests/<날짜>.md`에 쓰고, 우선순위 상위 3개를 보고한다. 효율 표는 README의 비교표 원본이 된다.
