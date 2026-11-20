---
name: cinematic-cut
description: "도입 시네마틱 컷 하나를 그림 준비 → 레이어 분리 → Timeline 연출 → 디더 확인 순서로 만든다."
---

사용자가 준 인자(컷 번호 1~6)에 해당하는 컷을 만든다. 기준은 `design/cinematic-intro.md`의 컷 표(장면, 디더 연출, 범위)다.

1. **그림 확인**: `Assets/Art/Cinematic/cutNN/`에 필요한 그림(본 그림, 편집본, 따로 뗀 요소)이 있는지 `design/cinematic-intro.md`의 "필요한 그림" 목록과 비교한다. 없으면 사람에게 무엇을 뽑아야 하는지(프롬프트 포함) 알리고 멈춘다. 이미 디더링된 그림이면 "블러로 그레이스케일 복원"이 필요하다고 표시한다.
2. **계획**: 레이어(이름, 깊이 Z), 움직임(컷마다 핵심 하나), 디더 연출 값의 시간표(초 단위), 자막, 다음 컷으로의 전환을 표로 보여 주고 승인을 받는다.
3. **구현**: Codex의 `unity-specialist` 에이전트를 호출한다. 레이어 쿼드와 Timeline은 에디터 메뉴(`Tools/Chilseok/Cinematic/Build Cut NN`)로 만든다. Timeline·씬 파일을 텍스트로 고치지 않는다.
4. **확인**: 사람에게 Play 모드에서 컷을 보고 GIF나 스크린샷을 남겨 달라고 한다(QA 유형 Visual/Feel). 수치 조정은 메뉴의 상수만 바꿔 다시 실행한다.
5. **기록**: `docs/progress.md`의 시네마틱 항목과 `design/cinematic-intro.md`의 해당 컷 상태를 갱신한다.
