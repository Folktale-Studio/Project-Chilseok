# ADR-0001: 게임 규칙은 순수 C#, 솔버와 게임이 같은 코드를 쓴다

- 상태: 채택 (2026-11-20)

## 맥락
이 게임의 핵심 지표(풀이 가능 여부, 최소 회전 수)는 솔버가 낸다. 솔버가 쓰는 규칙과 플레이어가 겪는 규칙이 다르면 지표가 거짓이 된다.

## 결정
- 투영(`ProjectionMap`), 깊이 갱신·재배치·회전 거부(`Reprojector`), 이동 판정, 구간 파서(`ZoneParser`), 솔버(`PuzzleSolver`)는 `Chilseok.Puzzle`의 순수 C# 클래스로 만든다. UnityEngine 의존은 `Vector3Int`/`Vector2Int` 같은 값 타입까지만 허용한다.
- MonoBehaviour(`PlayerController2D`, `ViewRotator`, `ZoneTracker`)는 입력·시간·표시를 연결만 하고, 판정은 위 클래스를 호출한다.
- 시간 의존 로직은 `float dt`를 인자로 받는다.

## 결과
- 규칙을 EditMode 테스트로 빠르게 검증할 수 있다.
- 컨트롤러의 연속 이동(점프 포물선)과 솔버의 추상 행동이 어긋날 수 있다. 점프 수치 범위와 PlayMode 테스트로 막는다.
