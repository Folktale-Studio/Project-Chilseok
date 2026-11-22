# 플레이 조정표

기본값과 범위는 [시점 회전 코어 수치 표](../design/perspective-core.md#수치)에서 가져온다. 위치의 ‘예정’은 구현 시 실제 필드로 갱신한다. 공유 설정은 [ADR-0002](../docs/adr/0002-tunable-numbers-not-literals.md)를 따른다. 사람이 값을 정한 뒤 **사람이 정한 값**과 **날짜**를 채운다.

| 항목 | 위치(컴포넌트.필드) | 기본값 | 범위 | 사람이 정한 값 | 날짜 |
| --- | --- | --- | --- | --- | --- |
| 이동 속도 | `MovementSettings._moveSpeed` | 4칸/초 | 3~5칸/초 | | |
| 점프 높이 | `MovementSettings._jumpHeight` | 1.25칸 | 1.1~1.4칸 | | |
| 점프 가로 거리 | `MovementSettings._jumpDistance` | 2칸 | 1.8~2.4칸, 가로 접근 속도 기준 | | |
| 중력 | `MovementSettings._gravity` | 30칸/초² | 25~40칸/초² | | |
| 회전 시간 | `MovementSettings._rotationSeconds` | 0.35초 | 0.25~0.5초 | | |
| 회전 입력 버퍼 | `MovementSettings._rotationBufferSeconds` | 0.15초 | 0.1~0.2초 | | |
| 회전 거부 표시 | `ViewRotator._rejectedIndicator` | 회색 큐브 X | 표시 오브젝트 교체 | | |
| 카메라 추적 보간 | `PlayerCameraFollow._smoothSeconds` | 0.2초 | 0~0.5초 | | |
| 코요테 타임 | `MovementSettings._coyoteSeconds` | 0.08초 | 0.05~0.12초 | | |
| 낙하 리셋 높이 | `PuzzleMovement.ResetHeight` | y < −3 | − | | |
| 디더 내부 해상도 | `DitherSettings._internalResolution` | 480×270 px | 가로 320~640 px, 세로 180~360 px | | |
| Bayer 행렬 | `DitherSettings._bayerSize` | 4×4 | 4×4 / 8×8 | | |
| 디더 강도 | `DitherSettings._strength` | 1 | 0~1 (정규화 강도) | | |
| 디더 밝기 | `DitherSettings._brightness` | 0 | −1~+1 (정규화 오프셋) | | |
| 디더 디졸브 | `DitherSettings._dissolve` | 0 | 0~1 (진행도) | | |
| 디더 반전 | `DitherSettings._invert` | 끔 | 끔 / 켬 | | |
| 디더 보일링 | `DitherSettings._boiling` | 0 | 0~1 (정규화 강도) | | |

이동·점프 수치의 조합은 1칸 높이와 1칸 틈을 넘고 2칸 높이와 2칸 틈은 넘지 못하는지 PlayMode로 확인한다. 범위가 ‘−’인 항목은 수치 표에 조정 범위가 정해질 때까지 기본값을 쓴다.
