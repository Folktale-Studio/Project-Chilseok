# Project-Chilseok

견우직녀 설화를 바탕으로 한 **시점 회전 퍼즐 플랫포머**다. 2D 횡스크롤로 걷다가 별무리 구간에서 세상을 90°씩 돌려,
투영으로 깊이가 사라진 별들을 한 줄로 이어 건넌다. 화면은 1비트 디더링(먹·한지 2색)이다.

- 엔진: Unity 6 (6000.3) / URP 17 / 새 Input System / Windows PC(x86-64) 빌드
- 기간: 2026-11-20 ~ 11-24 (넷마블네오 AI 인턴십 지원작). 마감 11/24 23:59
- 기획 기준 문서는 레포의 `design/`에 둔다. 전체 기획은 `design/game-design.md`, 핵심 규칙은 `design/perspective-core.md`를 따른다.
- 진행 상황: `docs/progress.md` — 세션을 시작하면 먼저 읽고, 끝낼 때 갱신한다

## 절대 규칙

1. **계획 먼저, 코드는 승인 후.** 바꿀 파일, 새 클래스의 목적과 관계, 모르는 점을 먼저 보여 주고 승인을 받는다.
2. **한 세션 = `docs/progress.md`의 한 단계.** 범위 밖 문제는 "후속 작업"으로 적기만 한다.
3. **"완료"라고 말하기 전에 `unity-quality-gate`를 돌린다.** INCOMPLETE를 PASS라고 하지 않는다.
4. **Unity 직렬화 파일(.unity .prefab .asset .controller .meta .mat .anim .inputactions)은 텍스트로 고치지 않는다.**
   에디터 메뉴 스크립트(`Assets/Chilseok/Editor/`)나 Unity MCP로 바꾸고, 안 되면 인스펙터에서 바꿀 값을 사람에게 알린다. (ADR-0003)
5. **게임 규칙은 순수 C#, MonoBehaviour는 연결만.** 솔버와 게임이 같은 규칙 코드를 쓴다. (ADR-0001)
6. **디더링은 화면 마지막 패스에서 한 번만.** 텍스처나 그림에 디더를 굽지 않는다. (ADR-0005)
7. `Assets/ThirdParty/`, `Packages/`, `ProjectSettings/`, `Library/`, `UserSettings/`, `Temp/`의 파일을 직접 편집·생성·삭제하지 않는다.
8. **모든 도구에서 `.env*` 읽기와 파괴적 명령 실행을 금지한다.** `rm -rf`, 재귀 `Remove-Item`, 강제 `git push`, `git reset --hard`, 강제 `git clean`을 실행하지 않는다.
9. **하네스를 편집·생성·삭제하기 전에 사람의 명시적 승인을 받는다.** 모든 `AGENTS.md`, `.agents/`, `.codex/`에 적용한다.

## 기획 변경 관리

기획이 바뀌면 다음 순서로 반영한다.

1. 레포의 `design/`에서 관련 문서를 갱신한다.
2. `design/decisions.md`에 날짜, 변경 사항, 이유, 영향을 한 줄로 기록한다. 포트폴리오에도 남길 결정은 `$notion-log`를 따라 Notion 결정 로그에 반영한다.
3. 영향받는 GitHub 이슈를 수정·추가·닫는다. `not planned`로 닫는 이슈에는 이유를 기록한다.
4. `docs/progress.md`를 갱신한다.

- 구조에 미치는 영향이 크면 `docs/adr/`에 ADR을 추가한다.
- 수치만 바뀌면 해당 작업의 PR에서 `design/perspective-core.md`의 수치 표도 함께 갱신한다.

## 구조

```
Assets/
  Chilseok/
    Scripts/          런타임 (asmdef: Chilseok.Runtime, namespace Chilseok.*)
      Puzzle/         ZoneData, ProjectionMap, Reprojector, PuzzleSolver  ← 순수 C#
      Player/         PlayerController2D, PlayerInputReader
      Camera/         ViewRotator, ZoneTracker
      Rendering/      DitherRenderFeature, DitherSettings
      Cinematic/      도입 시네마틱(Timeline) 보조 코드
      Flow/           LevelFlow, 글 카드, 클리어 로그
    Editor/           에디터 메뉴 (asmdef: Chilseok.Editor, namespace Chilseok.EditorTools)
    Levels/           구간 데이터 zone-NN.txt (텍스트, ADR-0004)
    Shaders/          디더링 셰이더
    Tests/EditMode/   순수 로직 테스트 (Chilseok.Tests.EditMode)
    Tests/PlayMode/   생명주기·물리 필요 시만 (Chilseok.Tests.PlayMode)
  Art/                캐릭터 FBX, 시네마틱 레이어, 스프라이트 (흑백 원본)
  ThirdParty/         외부 에셋. 수정 금지
  Scenes/             Main.unity, ZoneTest.unity
design/               기획 노트
docs/                 ADR, progress, 하네스 설명
production/           QA 문서, 버그, 플레이테스트, 지표
tools/harness/        quality-gate.ps1
```

## 사람과 AI의 역할

이 프로젝트는 "AI와 사람의 역할을 나눈 워크플로우"를 보여 주는 포트폴리오이기도 하다. 역할을 지킨다.

| 일 | 사람 | AI |
| --- | --- | --- |
| 장르·컨셉·재미 판단 | 결정 | 선택지와 근거 제시 |
| 구간 설계 | 초안 중 고르기, 플레이 감 확인 | 초안 작성, 솔버로 풀이 가능·난이도 검증 |
| 코드 | 계획 승인, 플레이 확인 | 구현, 테스트, 품질 게이트 |
| 아트 | 이미지 생성·선택, 최종 판단 | 프롬프트, 레이어 분리·임포트 메뉴 |
| QA | 수동 케이스 실행, 판정 승인 | 계획, 케이스, 정적 분석, 보고서 |
| Unity 에디터 조작 | 패키지 설치, 메뉴 실행, 인스펙터 값 | 메뉴 스크립트 작성, 바꿀 값 안내 |

AI가 한 일과 사람이 고친 일은 커밋 메시지와 `docs/progress.md`에 남긴다. README의 "AI 활용" 절이 여기서 나온다.

## 일을 나누는 기준 (세 단계)

| 단계 | 기준 | 예 |
| --- | --- | --- |
| 자동 (AI 혼자) | 정답을 기계로 확인할 수 있다 | 컴파일·테스트, 솔버 검증, 화면 캡처, 형식 검사 |
| 승인 (AI가 하고 사람 확인) | 틀려도 쉽게 되돌릴 수 있다 | 코드 구현, 이슈 쪼개기, 문서 수정, 버그 리포트 |
| 결정 (사람이 정하고 AI 보조) | 취향·방향·감각에 달렸거나 되돌리기 어렵다 | 컨셉·각색, 손맛 수치, 화면 느낌, 범위, 완료 조건, 머지·푸시 |

결정 단계에서 AI는 선택지 2~3개와 추천 근거만 제시한다. 하네스 파일을 바꾸거나 결정 단계의 일을 실행하기 전에 사람의 명시적 승인을 받는다.

## 감각 판단과 수치 조정

- 시간·높이·속도·해상도·강도·흔들림 크기처럼 체감으로 정하는 값은 사람이 최종 결정한다. AI는 `design/perspective-core.md` 수치 표의 초기값을 `[SerializeField]` 필드로 노출하고, 한국어 `[Tooltip]`에 단위와 조정 범위를 적는다. 이슈를 마칠 때 **사람이 조정할 값**에 항목, 위치, 기본값, 범위를 보고한다.
- 솔버의 1칸 오르기와 1칸 틈 건너기가 성립하고 2칸 높이와 2칸 틈은 넘을 수 없는 범위만 허용한다. 수치 표와 관련 Tooltip에 이 조건과 검증한 범위를 적고, PlayMode에서 확인한다.
- 사람이 값을 정하면 해당 PR에서 `design/perspective-core.md`의 초기값을 갱신하고 `production/tuning.md`에 결정값과 날짜를 적는다. `$notion-log`로 AI 작업 로그에 `감각 판단` 1건을 남기고 **조정 전 → 후**를 실제 값과 단위로 채운다.
- 사람이 기능을 써 보고 빼기로 하면 `$notion-log`로 AI 작업 로그에 `감각 판단`과 `폐기`를, 결정 로그에 `범위 축소`를 각각 1건 기록한다.

## 포트폴리오 기록

기록은 레포가 원본이며, Notion은 포트폴리오 정리본이다. AI 작업은 `production/ai-log.md`에, 기획 결정은 `design/decisions.md`에 남기고 `$notion-log`로 반영한다. 사람의 판단은 채팅에서 확인한 발언에 근거한다.

[Notion 포트폴리오 허브](https://app.notion.com/p/41e498d79c6881f6829de517ffb00f99) · [Codex 기록 규칙](https://app.notion.com/p/41e498d79c6881769c1cdbca957a3e99)

## 자주 쓰는 것

- 품질 게이트: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/harness/quality-gate.ps1` (`-Checks compile,editmode`처럼 범위 지정)
- 스킬: `$dev-task` 기능 구현, `$quick-design` 기획 노트, `$new-zone` 구간 설계·검증, `$cinematic-cut` 시네마틱, `$bug-report`, `$playtest-report`, `$team-qa`
- Codex 사용자 정의 에이전트: `gameplay-programmer`, `game-designer`, `unity-specialist`, `qa-lead`, `qa-tester`, `unity-quality-gate`

## 말투

사용자에게는 한국어로 쓴다. 문서와 보고는 "~한다" 체. 코드 주석은 "왜"를 한국어로 짧게.

## 경로별 지침

레포 루트에서 시작한 세션도 파일을 바꾸기 전에 해당 경로의 `AGENTS.md`를 읽는다.
- `Assets/Chilseok/Scripts/AGENTS.md`
- `Assets/Chilseok/Editor/AGENTS.md`
- `Assets/Chilseok/Tests/AGENTS.md`
- `Assets/Chilseok/Levels/AGENTS.md`
- `Assets/Chilseok/Scripts/Rendering/AGENTS.md`
- `Assets/Chilseok/Shaders/` 또는 `Assets/Chilseok/Scripts/Cinematic/`를 다룰 때도 `Assets/Chilseok/Scripts/Rendering/AGENTS.md`를 읽는다.

## Unity 에셋·서드파티 규칙

직렬화 확장자는 `.unity`, `.prefab`, `.controller`, `.asset`, `.meta`, `.mat`, `.anim`, `.playable`, `.inputactions`, `.overridecontroller`, `.mask`, `.signal`이다.

- 이 확장자와 절대 규칙의 보호 경로는 **읽기만** 한다. 어떤 도구로도 텍스트 편집·생성·삭제하지 않는다. `protect-files.ps1`은 지원되는 `apply_patch` 호출에서 차단한다. (ADR-0003)
- 바꿔야 하면 에디터 메뉴 스크립트(`Assets/Chilseok/Editor/`)를 고치거나 Unity MCP 도구를 쓴다. 둘 다 안 되면 사람에게 인스펙터에서 바꿀 값을 정확히 알려준다.
- `.meta` 파일은 Unity가 만든다. `.cs`를 새로 만들어도 `.meta`는 만들지 않는다. `.meta`를 지우거나 다시 만들면 참조가 끊긴다.
- 패키지 추가·제거는 사람이 Package Manager에서 한다. 필요하면 패키지 이름과 이유를 알려준다.
- 씬 YAML을 읽을 때는 필요한 부분만 검색한다. 씬 파일은 크다.
- `Assets/ThirdParty/`는 외부 에셋(폰트, 대체용 캐릭터 등)이다. 절대 수정하지 않고, 쓰면 `CREDITS.md`에 출처와 라이선스를 적는다.

## Unity C# 규칙

`Assets/Chilseok/` 아래 C# 파일에 적용한다.

- 네임스페이스: 런타임 `Chilseok.*`, 에디터 `Chilseok.EditorTools`. 에디터 코드는 `Editor/` 폴더에만 둔다.
- 어셈블리: `Chilseok.Runtime`, `Chilseok.Editor`, `Chilseok.Tests.EditMode`, `Chilseok.Tests.PlayMode`. `.asmdef`는 텍스트로 만들어도 되지만 `.meta`는 만들지 않는다.
- 필드: `[SerializeField] private`(접근자 생략 가능) + 한국어 `[Tooltip]`. 비공개 필드는 `_camelCase`, 공개 멤버는 `PascalCase`.
- 컴포넌트 참조는 Awake에서 캐시하고, 비어 있을 때만 자동으로 찾는다.
- 이벤트 구독은 해제 지점(OnDisable/OnDestroy)을 함께 작성한다.
- 핫패스에서 LINQ·new 배열·문자열 연결을 쓰지 않는다.
- 실행 순서에 의존하면 `[DefaultExecutionOrder]`로 명시한다. 기준: 입력 -100, 플레이어 0, 구간 추적 10, 카메라 LateUpdate.
- 격자 좌표는 `Vector3Int`, 화면 투영 좌표는 `Vector2Int`로 구분한다. 시점은 `ViewDirection` enum(0·90·180·270)만 쓴다.
- 주석은 "왜"를 한국어로 짧게 적는다.
