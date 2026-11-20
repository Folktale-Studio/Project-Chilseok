# ADR-0004: 구간 데이터는 텍스트 파일로 둔다

- 상태: 채택 (2026-11-20)

## 맥락
구간은 AI가 초안을 쓰고, 솔버가 검증하고, 사람이 고른다. ScriptableObject(`.asset`)는 AI가 텍스트로 쓸 수 없고(ADR-0003), diff로 읽기도 어렵다.

## 결정
- 구간은 `Assets/Chilseok/Levels/zone-NN.txt`에 층별 ASCII 격자로 쓴다. 형식은 `design/perspective-core.md`의 "구간 데이터 형식".
- 런타임은 `TextAsset`으로 읽어 `ZoneParser`로 해석한다. 에디터의 씬 셋업 메뉴도 같은 파서를 쓴다.
- `ZoneSolverTests`가 모든 구간 파일을 읽어 풀이 가능 여부와 목표 최소 회전 수(`rotations`)를 검사한다.

## 결과
- 구간 수정이 코드 리뷰처럼 diff로 보이고, 품질 게이트가 잘못된 구간을 잡는다.
- 형식 오류는 파서가 줄 번호와 함께 알려야 한다.
