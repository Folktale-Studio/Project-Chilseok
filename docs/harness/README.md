# 하네스 구조

Claude Code가 이 프로젝트에서 따르는 규칙과 도구다. Project-Soul(소울라이크) 하네스를 칠석에 맞게 옮겼다.

## 구성

| 위치 | 역할 |
| --- | --- |
| `CLAUDE.md` | 프로젝트 개요, 절대 규칙, 구조, 사람·AI 역할 |
| `.claude/settings.json` | 금지 명령(deny), 훅 등록 |
| `.claude/hooks/session-start.ps1` | 세션 시작 시 브랜치·변경·커밋·Unity 상태·progress.md를 보여 준다 |
| `.claude/hooks/protect-files.ps1` | Unity 직렬화 파일·ThirdParty·설정 편집을 막고, 하네스 파일 편집은 사람에게 묻는다 |
| `.claude/rules/*.md` | 경로별 규칙(런타임, 에디터, C#, 테스트, 에셋, 구간 데이터, 렌더링) |
| `.claude/agents/*.md` | 서브에이전트: game-designer, gameplay-programmer, unity-specialist, qa-lead, qa-tester, unity-quality-gate |
| `.claude/skills/*/SKILL.md` | 슬래시 명령: dev-task, new-zone, quick-design, cinematic-cut, bug-report, playtest-report, team-qa |
| `tools/harness/quality-gate.ps1` | Unity 배치 모드로 컴파일·EditMode·PlayMode를 돌려 PASS/FAIL/INCOMPLETE 판정 |
| `docs/adr/` | 설계 결정 기록 |

## 소울라이크 대비 바뀐 점

- 네임스페이스·폴더: `Soulslike` → `Chilseok`. 메뉴 `Tools/Soulslike` → `Tools/Chilseok`.
- `Assets/ThirdParty`는 서브모듈이 아니라 일반 외부 에셋 폴더다. 수정 금지는 그대로다.
- 루트 모션·록온 규칙을 빼고, "솔버와 게임이 같은 규칙 코드"(ADR-0001), "구간 데이터는 텍스트"(ADR-0004), "디더는 마지막 패스"(ADR-0005)를 넣었다.
- 새 스킬: `/new-zone`(AI 초안 → 솔버 검증 → 사람 선택), `/cinematic-cut`.
- QA 유형에 `Level`(구간 풀이 가능·난이도)을 추가했다.
- 품질 게이트는 Unity 에디터 실행 파일을 직접 찾는다(`UNITY_EDITOR_PATH` 환경 변수 → Unity Hub 기본 경로).

## 훅이 동작하지 않을 때

- Windows PowerShell 5.1 기준이다. `powershell`이 PATH에 있어야 한다.
- 훅 명령은 프로젝트 루트 기준 상대 경로다. Claude Code를 레포 루트에서 실행한다.
