# ADR-0002: 조율 가능한 수치는 리터럴로 박지 않는다

- 상태: 채택 (2026-11-20)

## 결정
- 개체별 수치는 `[SerializeField]` + 한국어 `[Tooltip]`으로 노출한다.
- 여러 곳이 공유하는 수치(이동·점프·회전 시간, 디더 설정)는 ScriptableObject(`MovementSettings`, `DitherSettings`)에 둔다. 에셋은 에디터 메뉴로 만든다(ADR-0003).
- 초기값과 범위의 근거는 `design/perspective-core.md`의 수치 표다. 수치를 바꾸면 표도 고친다.
