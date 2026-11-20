---
paths:
  - "Assets/Chilseok/**/*.cs"
---

# Unity C# 규칙

- 네임스페이스: 런타임 `Chilseok.*`, 에디터 `Chilseok.EditorTools`. 에디터 코드는 `Editor/` 폴더에만 둔다.
- 어셈블리: `Chilseok.Runtime`, `Chilseok.Editor`, `Chilseok.Tests.EditMode`, `Chilseok.Tests.PlayMode`. `.asmdef`는 텍스트로 만들어도 되지만 `.meta`는 만들지 않는다.
- 필드: `[SerializeField] private`(접근자 생략 가능) + 한국어 `[Tooltip]`. 비공개 필드는 `_camelCase`, 공개 멤버는 `PascalCase`.
- 컴포넌트 참조는 Awake에서 캐시하고, 비어 있을 때만 자동으로 찾는다.
- 이벤트 구독은 해제 지점(OnDisable/OnDestroy)을 함께 작성한다.
- 핫패스에서 LINQ·new 배열·문자열 연결을 쓰지 않는다.
- 실행 순서에 의존하면 `[DefaultExecutionOrder]`로 명시한다. 기준: 입력 -100, 플레이어 0, 구간 추적 10, 카메라 LateUpdate.
- 격자 좌표는 `Vector3Int`, 화면 투영 좌표는 `Vector2Int`로 구분한다. 시점은 `ViewDirection` enum(0·90·180·270)만 쓴다.
- 주석은 "왜"를 한국어로 짧게 적는다.
