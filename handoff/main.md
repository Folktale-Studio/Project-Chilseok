# main 인수인계

## 2026-11-20 · Unity CLI 연동

- 적용 중인 Codex 권한은 `chilseok-workspace`이며 네트워크 프록시가 활성화되어 있다. 샌드박스 안에서 Unity CLI 1.0.1이 열린 Unity 6000.3.28f1 에디터에 연결됐다. `unity status`는 포트 7800과 `ready`를 반환했고, 명령 목록에서 184개를 확인했다.
- Unity CLI의 외부 텔레메트리는 샌드박스의 도메인 규칙으로 차단된다. 게이트는 Unity CLI 자식 프로세스에만 `UNITY_NO_CLI_INVOKED_TELEMETRY=1`을 설정한다.
- 한 일은 CLI 우선 품질 게이트, 배치 모드 대체 경로, 시작 시 브랜치 인수인계 출력, 확인한 명령과 역할 문서화다.
- 바꾼 파일은 `tools/harness/quality-gate.ps1`, `.codex/hooks/session-start.ps1`, `docs/harness/unity-cli.md`, `docs/progress.md`와 이 파일이다. 앞 세션에서 추가한 `.codex/config.toml` 루프백 설정도 작업 트리에 있다.
- 명시적 게이트는 `compile,editmode,playmode`, `mode=cli`, **PASS**다. 컴파일 오류 없이 완료했고 EditMode와 PlayMode는 각각 **WARNING: 0 tests found**다. CLI가 없는 환경을 재현했을 때 열린 에디터 잠금으로 `mode=batch`, **INCOMPLETE**를 반환했다. 실제 배치 실행은 별도로 확인해야 한다.
- 남은 일은 턴별 컴파일 훅과 에이전트·스킬 지침 변경이다. 자동 승인 검토 결과에 따라 사용자 확인을 기다린다. 훅 개별 신뢰는 Desktop의 Review hooks에서 사용자가 진행한다.

## 2026-11-20 · 기획 변경 관리

- AI가 레포 `design/`을 기준으로 삼는 변경 절차를 문서화했다. 사람은 변경 기록, GitHub 이슈와 진행 문서의 반영 순서, ADR과 수치 수정 기준을 정했다.
- 바꾼 파일은 `AGENTS.md`, `design/README.md`, `design/decisions.md`다. 로컬 커밋은 `9ea46b0`이다.
- 품질 게이트는 `compile`, `mode=cli`, PASS다.

## 2026-11-20 · 노션 포트폴리오·기록 자동화 설정

- AI가 Notion 기록 규칙과 두 데이터 소스를 확인하고 레포의 기록 구조와 규칙을 작성했다. 사람은 레포를 원본으로 삼는 방향과 구현 계획을 승인했다.
- `AGENTS.md`, `.agents/skills/{notion-log,dev-task,new-zone}/SKILL.md`, `docs/progress.md`의 이번 작업 한 줄은 `4bf3e98`에 커밋했다. `production/ai-log.md`의 첫 작업 기록은 `8eecacd`에 커밋했다.
- [Notion AI 작업 로그 CHS-LOG-8](https://app.notion.com/p/41e498d79c688192a1c4d1f3828f1aa4?pvs=204)의 필드와 위치를 다시 읽어 확인했다. 근거는 푸시 전 로컬 커밋의 GitHub URL이다.
- Unity 품질 게이트는 `compile`, `mode=cli`, PASS다. 기존 Desktop Notion 연결을 사용했다.

## 2026-11-21 · 무대와 목적 보강

- 사람은 직녀의 탑 유배 각색, 낮은 무대부터 탑까지의 높이 변화, 까치 깃털 수집, 컷 4·5·6 수정과 감축 순서를 정하고 승인했다. AI는 세 기획 문서와 구간표, `F` 형식, 두 솔버 목표와 검증 계획, 내레이션·이미지 목록, ADR-0006과 일정에 반영했다.
- 로컬 커밋은 `20bbfa0`이다. [Notion 결정 CHS-DEC-8](https://app.notion.com/p/41e498d79c688196a23bef752727f293)은 레포 결정과 같은 이유·영향으로 등록한 뒤 다시 읽어 확인했다.
- 구간표 6행, 예시 데이터의 그리드와 깃털 위치, 감축 순서와 컷 5 대체 연출을 문서에서 검증했다. Unity 컴파일 게이트는 에디터 인스턴스가 없어 배치로 넘어간 뒤 Unity CDN 접근이 차단되어 판정 미완료다.
- 당시 GitHub 저장소의 기존 이슈는 0건이었다. 연결 앱의 생성 호출은 `403 Resource not accessible by integration`으로 거부됐고, 인앱 브라우저는 GitHub 로그인이 필요했다. 이후 아래 기획 변경 6건을 전체 기능 백로그에 합쳤다.

### 기존 기획 변경 이슈 6건 — 백로그에 합침

| 기존 초안 | 합친 백로그 이슈 |
| --- | --- |
| 까치 깃털 구간 데이터와 두 최소 회전 지표 | B02 파서, B05·B06 솔버 |
| 구간 깃털 수집과 엔딩 오작교 | B19 로그, B21 수집, B22 엔딩 오작교 |
| 별무리 무대와 마지막 탑 등반 | B13~B18 구간 1~6 |
| 직녀의 탑 유배 각색과 시네마틱 4·5·6번 컷 | B20 내레이션, B23 재생과 전환, B32~B35 컷 4·6, B39·B40 컷 5 |
| 지정 시점에서만 단단한 별자리 발판 | B50 (`nice-to-have`) |
| 칠성 숨은 그림과 숨은 깃털 | B51 (`nice-to-have`) |

## 2026-11-21 · 일의 분담과 감각 판단

- 사람은 자동·승인·결정의 기준과 체감 수치를 정하는 절차를 지정하고 승인했다. AI는 루트·런타임 지침, `notion-log`·`dev-task` 스킬, 코어 수치 문서와 진행표를 갱신했다. 로컬 커밋은 `ce437b2`다.
- `production/tuning.md`의 10개 항목을 코어 수치 표와 대조했다. 런타임 구현 전이므로 필드 위치는 예정이고, 사람이 정한 값과 날짜는 비어 있다.
- Notion AI 작업 로그에서 `감각 판단` 단계와 `조정 전 → 후` 텍스트 칸을 확인했다. 실제 체감 수치 확정이나 기능 제외 결정이 있을 때 기록을 남긴다.
- 문서 일관성 검사와 커밋 diff 검사는 통과했다. Unity 컴파일 게이트는 샌드박스의 Unity CDN 접근 차단으로 판정 미완료다.

## 2026-11-21 · 기능 백로그 작성

- 승인한 59건과 11/22·11/23·11/24 마일스톤을 [기능 백로그 초안](../production/backlog-draft.md)에 정리했다. 초안 번호는 [진행표](../docs/progress.md)에서 참조한다.
- GitHub 이슈 템플릿 5종과 [등록 스크립트](../tools/harness/create-backlog.ps1)를 추가했다. GitHub 번호는 사용자가 터미널에서 실행할 때 생성되며, 출력의 Bxx와 GitHub 번호 매핑을 확인한다.
- 품질 게이트는 에디터를 열어 둔 CLI 모드가 기본이다. 배치의 Unity CDN 접근 차단으로 결과를 받지 못하면 `INCOMPLETE`로 기록한다.
- 59건의 단계·라벨·마일스톤·선행 순서, 진행표 참조, 템플릿 5종과 Windows PowerShell 구문을 정적으로 검증했다. Unity 열린 에디터의 CLI 컴파일 게이트는 PASS다.
