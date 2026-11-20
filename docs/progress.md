# 진행 상황

마감 2026-11-24(화) 23:59. 한 세션 = 한 단계. 끝나면 체크하고 "AI / 사람" 한 줄을 남긴다.
밀리면 자르는 순서: 구간 6→5 → 사운드 → 엔딩 연출 → 구간 5→4 → 회전 연출(즉시 전환). 투영·솔버·디더링은 끝까지 남긴다.

## Phase 0 — 환경 (11/20, 사람 중심)
- [ ] Unity 6 (6000.3) URP 프로젝트를 이 레포 루트에 생성
- [ ] 패키지 설치: Input System, Test Framework, Timeline, 2D Animation, (선택) Cinemachine
- [ ] File → Build Profiles에서 Windows(64비트) 활성화, Player → Active Input Handling = Input System
- [ ] 폴더·asmdef·빈 씬 생성 (AI: 에디터 메뉴 `Tools/Chilseok/0. Setup Project` 작성 → 사람: 실행)
- [ ] `tools/harness/quality-gate.ps1 -Checks compile` PASS
- [ ] `design/game-design.md` 내보내기(원본 문서 → Markdown)
- 완료 기준: 빈 프로젝트에서 게이트 compile·editmode PASS(0 tests)

## Phase 1 — 퍼즐 코어 (11/21)
- [ ] `ZoneParser` + 형식 오류 테스트
- [ ] `ProjectionMap`(4시점) + 테스트
- [ ] `Reprojector`(깊이 갱신, 재배치, 회전 거부) + 테스트
- [ ] `PuzzleSolver`(BFS, 지표) + `ZoneSolverTests`
- [ ] 예시 구간 `zone-02.txt`(perspective-core.md) 솔버 통과
- 완료 기준: 게이트 compile·editmode PASS, 솔버가 zone-02 최소 회전 1을 낸다

## Phase 2 — 디더 렌더 (11/21, Phase 1과 병렬 가능)
- [ ] `DitherRenderFeature` + `DitherSettings`(밝기, 디졸브, 반전, 해상도, 보일링)
- [ ] 큐브 몇 개 씬에서 디더 화면 캡처, Windows 실행 빌드에서 확인
- 완료 기준: 에디터와 Windows 실행 빌드의 디더 화면 스크린샷

## Phase 3 — 플레이어와 카메라 (11/22)
- [ ] `PlayerInputReader`, `PlayerController2D`(투영 맵 위 이동·점프·코요테·리셋)
- [ ] `ViewRotator`(0.35초 회전, 입력 잠금, 회전 거부 흔들림), `ZoneTracker`
- [ ] 점프 수치가 솔버 가정(1칸 오르기·1칸 틈)을 지키는 PlayMode 테스트
- [ ] 씬 셋업 메뉴: 구간 텍스트 → 블록 배치, 구간 사이 길
- 완료 기준: zone-02를 손으로 클리어

## Phase 4 — 구간 6개와 흐름 (11/22)
- [ ] `$new-zone`으로 구간 1~6 (초안 → 솔버 → 사람 선택)
- [ ] `LevelFlow`: 까치 도착, 내레이션 한 줄, 다음 구간 안내, 클리어 로그
- [ ] `production/metrics/zones.md` 지표 표
- 완료 기준: 여정을 처음부터 끝까지 플레이 가능

## Phase 5 — 캐릭터와 아트 (11/22 오후)
- [ ] 견우 3D 변환(A포즈 시안 → 이미지 기반 3D 생성) → 임포트·리깅 메뉴. 3시간 안에 안 되면 Quaternius로 대체
- [ ] 까치 스프라이트, 폰트, 내레이션 UI
- 완료 기준: 디더 화면에서 30px 견우의 삿갓 실루엣이 읽힌다

## Phase 6 — 도입 시네마틱 (11/22 그림, 11/23 오전 구현)
- [ ] 필수 컷 1·4·6 (`$cinematic-cut`)
- [ ] 추천 컷 5(달 순환), 있으면 좋음 2·3
- 완료 기준: 시네마틱 → 디더 디졸브 → 첫 구간으로 끊김 없이 이어진다

## Phase 7 — QA·빌드·README (11/23)
- [ ] `$team-qa`로 핵심 이슈 QA, 열린 S1/S2 없음
- [ ] 테스트 1~3명, `$playtest-report`, 조정
- [ ] Windows PC(x86-64) 빌드 폴더를 ZIP으로 묶고, 압축을 풀어 실행을 확인한 뒤 itch.io에 Windows 다운로드로 올린다
- [ ] README: 게임 소개, AI·사람 역할 분담표, 하네스 구조, 구간 지표·비교표
- 완료 기준: 실행을 확인한 Windows 빌드의 itch.io 다운로드 링크, README 완성

## Phase 8 — 제출 (11/24)
- [ ] 지원서 작성·제출 (23:59 전)

## 기록
| 날짜 | 단계 | AI가 한 일 | 사람이 한 일 |
| --- | --- | --- | --- |
| 11/20 | 기획 | 장르·컨셉 선택지, 기획안, 목업, 하네스 이식 | 장르·컨셉·연출 방향 결정, 캐릭터 시안 생성 |
| 11/20 | 하네스를 Codex로 이전 | 변환 | 방향 결정·검토 |
