# Codex CLI 하네스

칠석 프로젝트의 개발 지침, 스킬, 사용자 정의 에이전트와 검증 도구를 관리한다.

## 구성

| 위치 | 역할 |
| --- | --- |
| `AGENTS.md` | 프로젝트 개요, 절대 규칙, 구조, 사람과 AI의 역할, 여러 경로에 걸친 Unity 규칙 |
| `Assets/Chilseok/**/AGENTS.md` | 런타임, 에디터, 테스트, 구간 데이터, 렌더링 지침. 셰이더와 시네마틱도 렌더링 지침을 따른다 |
| `.agents/skills/*/SKILL.md` | `$dev-task`, `$new-zone`, `$quick-design`, `$cinematic-cut`, `$bug-report`, `$playtest-report`, `$team-qa` 워크플로우 |
| `.codex/agents/*.toml` | `game-designer`, `gameplay-programmer`, `unity-specialist`, `qa-lead`, `qa-tester`, `unity-quality-gate` |
| `.codex/config.toml` | 프로젝트 샌드박스 프로필과 승인 정책 |
| `.codex/rules/protect-commands.rules` | 파괴적 셸 명령의 알려진 인자 접두사 차단 |
| `.codex/hooks.json` | `SessionStart`와 `PreToolUse` 훅 등록 |
| `.codex/hooks/session-start.ps1` | 현재 브랜치·변경·최근 커밋, Unity 상태, `docs/progress.md` 앞부분을 세션에 전달 |
| `.codex/hooks/protect-files.ps1` | `apply_patch`에서 Unity 직렬화 에셋과 보호 경로의 편집·생성·삭제를 차단 |
| `tools/harness/quality-gate.ps1` | Unity 배치 모드로 컴파일·EditMode·PlayMode를 실행해 PASS/FAIL/INCOMPLETE 판정 |
| `docs/adr/` | 설계 결정 기록 |

## Claude Code 하네스에서 Codex로 옮긴 이유와 바뀐 점

프로젝트에서 쓰는 Codex CLI가 저장소의 지침과 검증 절차를 직접 읽도록 Project-Soul에서 이어받은 하네스를 이전했다. 구간 설계는 `$new-zone`으로 초안 → 솔버 → 사람 선택 순서를 따르고, 시네마틱 컷은 `$cinematic-cut`으로 준비한다. 품질 게이트는 `UNITY_EDITOR_PATH` 또는 Unity Hub 기본 경로에서 에디터를 찾는다.

- Codex는 시작 디렉터리까지 경로상 지침을 읽는다. 레포 루트에서 작업할 때는 루트 지침에 나열된 대상 폴더의 `AGENTS.md`도 직접 읽는다.
- 스킬은 프롬프트에 `$스킬이름`과 인자를 적어 호출한다. 각 스킬의 `agents/openai.yaml`에서 자동 호출을 끈다.
- 사용자 정의 에이전트는 상위 세션의 권한을 상속한다. 지원되는 기능은 TOML에서 끄며, 품질 게이트의 파일 수정 금지와 역할별 도구 범위는 지침으로 유지한다.
- 샌드박스 프로필은 `.env*` 읽기와 지정 폴더 쓰기를 제한한다. 환경 파일 글롭은 프로세스 시작 시 깊이 8까지 평가되며, Windows에서는 MXC 또는 elevated 샌드박스가 해당 제한을 지원한다. 명령 규칙은 인자 앞부분이 일치할 때 적용된다.
- 보호 훅은 `apply_patch` 호출에 적용한다. 셸이나 다른 쓰기 경로에도 루트 `AGENTS.md`의 금지 규칙을 적용한다. 하네스 편집은 루트 지침에 따라 사용자의 명시적 승인을 먼저 받는다.

## 훅 신뢰와 확인

1. Windows에서는 Windows PowerShell 5.1의 `powershell.exe`와 `git`이 PATH에 있어야 한다. 그 외 환경의 훅 명령은 PowerShell 7의 `pwsh`를 사용한다.
2. 저장소 루트에서 `codex`를 실행하고 프로젝트를 신뢰한다. 사용자 설정으로 지정할 경우 `~/.codex/config.toml`에서 `[projects.'C:/절대/경로/Project-Chilseok']` 아래 `trust_level = "trusted"`를 설정한다.
3. CLI의 `/hooks`에서 프로젝트의 두 훅과 스크립트를 검토하고 신뢰한다. 정의가 바뀌면 다시 검토한다. 훅이 목록에 없으면 프로젝트 신뢰와 `[features].hooks` 설정을 확인한다.
4. 새 세션을 열어 `Loading Chilseok project status` 훅이 성공하고 브랜치·Unity 상태를 전달하는지 확인한다.
5. 폐기 가능한 체크아웃에서 `apply_patch`로 `Assets/Chilseok/HookProbe.meta` 생성을 시도한다. `[protect-files] BLOCKED`가 표시되고 파일이 생성되지 않아야 한다. 같은 도구로 보호 대상이 아닌 일반 Markdown 생성이 가능한지도 확인한다.

[OpenAI 훅 문서](https://developers.openai.com/codex/hooks) · [설정 참조](https://developers.openai.com/codex/config-reference) · [샌드박스 권한](https://developers.openai.com/codex/permissions) · [명령 규칙](https://developers.openai.com/codex/rules)
