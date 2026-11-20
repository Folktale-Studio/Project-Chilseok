# ADR-0003: Unity 직렬화 에셋은 에디터 코드로 만든다

- 상태: 채택 (2026-11-20, 칠석 하네스에 승계)

## 맥락
씬·프리팹·애니메이터·Timeline·ScriptableObject·머티리얼·`.meta`는 Unity가 관리하는 YAML이다. AI가 텍스트로 고치면 GUID·fileID가 깨지거나 Unity와 충돌하기 쉽다.

## 결정
- 이 파일들은 텍스트로 편집·생성·삭제하지 않는다. 신뢰한 `.codex/hooks/protect-files.ps1`은 Codex CLI의 `PreToolUse`에서 `apply_patch` 편집을 차단한다. 셸 등 훅을 거치지 않는 경로에도 루트 `AGENTS.md`의 편집 금지 규칙을 적용한다.
- 만들거나 바꿀 때는 `Assets/Chilseok/Editor/`의 멱등 에디터 메뉴(`Tools/Chilseok/...`)나 Unity MCP를 쓴다.
- 둘 다 안 되면 사람에게 인스펙터에서 바꿀 값을 정확히 알려 준다.

## 결과
- 사람이 메뉴를 한 번 실행해야 결과가 생긴다. 보고에 "Unity에서 사용자가 할 일"을 항상 적는다.
