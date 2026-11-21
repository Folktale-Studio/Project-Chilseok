# Unity CLI와 품질 게이트

Codex CLI에서 쓰려면 `unity setup codex`가 필요하다.

## 설치된 명령

Unity CLI 1.0.1과 Unity 6000.3.28f1에서 `unity command`로 38개 그룹의 명령 184개를 확인했다. 프로젝트를 지정하려면 `--project-path <path>`를 쓴다. 에디터 명령에는 `--caller plugin --skill unity-cli`를 붙이고, 자동 처리에는 `--format json`을 쓴다. 정확한 인자는 `unity command <그룹> <명령> --help`에서 확인한다.

| 명령 | 용도 |
| --- | --- |
| `unity status` | 현재 프로젝트 에디터의 `ready` 상태를 확인한다 |
| `unity command pipeline list_commands --detail compact` | 에디터가 노출한 명령 목록을 확인한다 |
| `unity command script recompile` | 스크립트 재컴파일을 요청한다 |
| `unity command script recompile_status` | 재컴파일 완료와 오류를 확인한다 |
| `unity command console status` | 에디터 콘솔과 스크립트 컴파일 상태를 확인한다 |
| `unity command console get --level error` | 콘솔 오류를 읽는다. `--tail <N>`으로 항목 수를 제한한다 |
| `unity command test run --mode editor --async-tests true` | EditMode 테스트를 시작한다. PlayMode는 `--mode playmode`를 쓴다 |
| `unity command test status --detail failures` | 테스트 완료, 집계, 실패한 테스트의 전체 이름을 확인한다 |
| `unity command capture game_view --output-path production/qa/captures/<file>.png` | Visual·Feel 증거를 PNG로 저장한다 |

## 작업별 실행 절차

| 작업 | 사용할 스킬 또는 도구 |
| --- | --- |
| 에디터 상태·로그 확인, 테스트·빌드·캡처 실행 | 공식 `unity:unity-cli` |
| `DitherRenderFeature` 수정 후 Render Graph 검증 | 공식 `unity:validate-urp-render-graph-renderer-feature` |
| 기능 승인과 구현, 구간 설계, 이슈 QA의 완료 조건 | 칠석의 `$dev-task`, `$new-zone`, `$team-qa` |
| Unity 검증 결과 판정과 기록 | `unity-quality-gate`와 `tools/harness/quality-gate.ps1` |

에셋 수정은 공식 명령이나 에디터 메뉴로 한다. `eval`은 조회에 사용한다. 사용자의 Unity 에디터를 종료하지 않는다.

## 연결 상태별 게이트 동작

품질 게이트는 칠석 프로젝트의 에디터를 열어 둔 CLI 모드를 기본으로 한다. `unity status`에서 `ready`를 확인한 뒤 같은 에디터에서 검증한다.

| 상태 | `GATE_RESULT.mode` | 동작 |
| --- | --- | --- |
| 대상 에디터가 `ready` | `cli` | 재컴파일 상태와 콘솔을 확인하고, 선택한 테스트를 실행한다. 명령 응답은 `Logs/QualityGate/*-cli.log`에 기록한다 |
| 대상 에디터에 연결할 수 없음 | `batch` | Unity 에디터를 배치 모드로 실행한다. `UNITY_EDITOR_PATH`, `-UnityPath` 또는 기본 설치 경로에서 실행 파일을 찾는다. 열린 에디터의 프로젝트 잠금이 있으면 INCOMPLETE를 반환한다 |

마지막 줄의 `GATE_RESULT`와 종료 코드로 판정한다. 0은 PASS, 1은 FAIL, 2는 인자 오류, 3은 INCOMPLETE다. 테스트가 0건이면 PASS와 `WARNING: 0 tests found`를 출력한다. 기능 검증에 필요한 테스트가 0건이면 인수인계에 미실행으로 기록한다.

배치 모드의 알려진 제약은 외부 접속이다. Unity CDN 등으로의 접속이 샌드박스에서 차단되어 결과를 받지 못하면 `INCOMPLETE`로 기록한다. 에디터를 열고 CLI가 연결된 뒤 다시 검증한다.

EditMode와 PlayMode는 작업을 마칠 때 게이트를 명시적으로 실행하고, 검사와 `mode`, 판정, 실패 테스트 이름을 브랜치 인수인계에 기록한다.

[Unity CLI 공식 문서](https://docs.unity.com/en-us/unity-cli/use-unity-cli) · [Codex 네트워크 권한](https://developers.openai.com/codex/permissions#local-and-private-networks)
