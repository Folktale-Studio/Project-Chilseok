# 런타임 스크립트 규칙

이 지침은 이 폴더 아래 C# 파일에 적용한다.

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
- 런타임 코드에서 `UnityEditor`를 참조하지 않는다. 필요하면 `#if UNITY_EDITOR`로 감싼다. Windows 플레이어 빌드에서도 동작하는지 확인한다.

## 감각 판단과 수치 조정

- 시간·높이·속도·해상도·강도·흔들림 크기의 최종값은 사람이 정한다. `design/perspective-core.md` 수치 표의 초기값을 `[SerializeField]`로 노출하고 한국어 `[Tooltip]`에 단위와 조정 가능한 범위를 적는다. 공유 수치는 ADR-0002에 따른 설정 에셋에 둔다.
- 이동·점프 관련 값은 1칸 오르기와 1칸 틈 건너기를 지키고 2칸 높이와 틈을 넘지 못하는 조합만 허용한다. Tooltip과 수치 표에 이 조건과 검증 범위를 함께 적는다. 단일 값의 문서 범위에 들어도 다른 값과 조합했을 때 이동 규칙이 깨지면 허용하지 않는다. PlayMode에서 검증한다.
- 이슈 완료 보고의 **사람이 조정할 값**에 항목, 컴포넌트와 필드, 기본값, 범위를 넣는다. 사람이 정한 값은 같은 PR에서 수치 표 초기값과 `production/tuning.md`에 반영하고, `$notion-log`로 조정 전후를 기록한다. 사람이 사용 후 기능을 빼기로 하면 AI 작업 로그(감각 판단·폐기)와 결정 로그(범위 축소)에 각각 1건 기록한다.
