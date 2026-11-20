---
paths:
  - "Assets/Chilseok/Scripts/**/*.cs"
---

# 런타임 스크립트 규칙

- `namespace Chilseok` 아래 시스템별로 나눈다: `Chilseok.Puzzle`, `Chilseok.Player`, `Chilseok.CameraSystem`, `Chilseok.Rendering`, `Chilseok.Cinematic`, `Chilseok.Flow`.
- 규칙성 로직(투영, 재배치, 회전 거부, 이동 판정, 솔버, 구간 파서)은 `Chilseok.Puzzle`의 **순수 C# 클래스**로 만들고, MonoBehaviour는 연결만 한다. 솔버와 플레이어가 같은 규칙 코드를 쓴다. (ADR-0001)
- 시간 의존 로직은 `float dt`를 인자로 받는다. 순수 로직 안에서 `Time.deltaTime`을 직접 읽지 않는다.
- 게임 규칙 수치는 리터럴로 박지 않는다. 개체별은 `[SerializeField]` + 한국어 `[Tooltip]`, 공유 데이터는 ScriptableObject. (ADR-0002)
- 다른 컴포넌트 참조는 `[SerializeField]`로 받고, 비어 있으면 `Awake`에서 `GetComponent`/`GetComponentInParent`로 찾는다.
- 필수 참조가 없으면 `Debug.LogWarning("[클래스명] ...", this)`를 남기고 `enabled = false`로 멈춘다. 예외를 던지거나 조용히 넘어가지 않는다.
- `Update`/`LateUpdate`에서 할당하지 않는다. LINQ 금지. 투영 맵처럼 큰 배열은 구간 진입·회전 때 한 번 만들어 재사용한다.
- `Camera.main`, `FindObjectOfType`는 매 프레임 호출하지 않는다.
- 게임플레이 입력은 `PlayerInputReader`를 통해서만 읽는다. 다른 스크립트가 `Keyboard.current` 등 장치를 직접 읽지 않는다.
- 이벤트는 `event Action`을 쓰고, 구독한 쪽이 `OnDisable`/`OnDestroy`에서 해제한다.
- 애니메이터 상태·파라미터 이름은 `const string` 상수로 두고, 에디터 메뉴와 같은 상수를 공유한다.
- 런타임 코드에서 `UnityEditor`를 참조하지 않는다. 필요하면 `#if UNITY_EDITOR`로 감싼다. WebGL 빌드가 깨지지 않는지 생각한다(스레드, 파일 IO 금지).
