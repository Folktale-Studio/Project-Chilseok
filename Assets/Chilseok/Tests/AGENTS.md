# 테스트 규칙

이 지침은 이 폴더 아래 C# 테스트에 적용한다.

- 폴더: `Assets/Chilseok/Tests/EditMode/`, `Assets/Chilseok/Tests/PlayMode/`. 각 폴더에 테스트용 Assembly Definition을 둔다.
- 순수 C# 로직(ADR-0001)은 EditMode로 테스트한다. PlayMode 테스트는 Unity 생명주기가 꼭 필요한 경우에만 쓴다.
- 테스트 이름: `메서드_조건_기대결과` (예: `Reproject_BlockOverlapsPlayer_RefusesRotation`).
- 하나의 테스트는 하나의 동작만 검증한다. Arrange / Act / Assert 순서.
- 작은 격자는 테스트 안에서 텍스트 리터럴로 만든다(구간 파일과 같은 형식). 읽기 쉬운 테스트가 문서다.
- `ZoneSolverTests`는 `Assets/Chilseok/Levels/zone-*.txt`를 모두 읽어 (1) 풀리는지 (2) 최소 회전 수가 파일의 `rotations` 범위 안인지 검사한다. 구간마다 `[TestCaseSource]`로 하나씩 나온다.
- 시간은 `dt`를 직접 넘겨서 결정적으로 만든다. `Time.deltaTime`이나 실제 대기(`WaitForSeconds`)에 의존하지 않는다.
- 테스트를 통과시키려고 테스트를 고치거나 지우지 않는다. 테스트가 틀렸다고 판단되면 멈추고 이유를 보고한다.
- 계획 문서의 테스트 명세에 있는 항목은 빠짐없이 테스트로 만든다.
