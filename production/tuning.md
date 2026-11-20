# 플레이 조정표

기본값과 범위는 [시점 회전 코어 수치 표](../design/perspective-core.md#수치)에서 가져온다. 위치의 ‘예정’은 구현 시 실제 필드로 갱신한다. 공유 설정은 [ADR-0002](../docs/adr/0002-tunable-numbers-not-literals.md)를 따른다. 사람이 값을 정한 뒤 **사람이 정한 값**과 **날짜**를 채운다.

| 항목 | 위치(컴포넌트.필드) | 기본값 | 범위 | 사람이 정한 값 | 날짜 |
| --- | --- | --- | --- | --- | --- |
| 이동 속도 | 예정 `MovementSettings._moveSpeed` | 4칸/초 | 3~5 | | |
| 점프 높이 | 예정 `MovementSettings._jumpHeight` | 1.25칸 | 1.1~1.4 | | |
| 점프 가로 거리 | 예정 `MovementSettings._jumpDistance` | 2칸 | 1.8~2.4 | | |
| 중력 | 예정 `MovementSettings._gravity` | 30칸/초² | 25~40 | | |
| 회전 시간 | 예정 `MovementSettings._rotationSeconds` | 0.35초 | 0.25~0.5 | | |
| 회전 입력 버퍼 | 예정 `MovementSettings._rotationBufferSeconds` | 0.15초 | 0.1~0.2 | | |
| 코요테 타임 | 예정 `MovementSettings._coyoteSeconds` | 0.08초 | 0.05~0.12 | | |
| 낙하 리셋 높이 | 예정 `PlayerController2D._resetBelowY` | y < −3 | − | | |
| 디더 내부 해상도 | 예정 `DitherSettings._internalResolution` | 480×270 | 320×180~640×360 | | |
| Bayer 행렬 | 예정 `DitherSettings._bayerSize` | 4×4 | 4×4 / 8×8 | | |

이동·점프 수치의 조합은 1칸 높이와 1칸 틈을 넘고 2칸 높이와 2칸 틈은 넘지 못하는지 PlayMode로 확인한다. 범위가 ‘−’인 항목은 수치 표에 조정 범위가 정해질 때까지 기본값을 쓴다.
