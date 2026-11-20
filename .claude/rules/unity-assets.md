---
paths:
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.controller"
  - "**/*.asset"
  - "**/*.meta"
  - "**/*.mat"
  - "**/*.anim"
  - "**/*.playable"
  - "**/*.inputactions"
  - "Assets/ThirdParty/**"
  - "ProjectSettings/**"
  - "Packages/**"
---

# 유니티 에셋·서드파티 규칙

- 이 경로의 파일은 **읽기만** 한다. 텍스트로 편집·생성·삭제하지 않는다. (ADR-0003, `protect-files` 훅이 막는다)
- 바꿔야 하면 에디터 메뉴 스크립트(`Assets/Chilseok/Editor/`)를 고치거나 Unity MCP 도구를 쓴다. 둘 다 안 되면 사람에게 인스펙터에서 바꿀 값을 정확히 알려준다.
- `.meta` 파일은 Unity가 만든다. `.cs`를 새로 만들어도 `.meta`는 만들지 않는다. `.meta`를 지우거나 다시 만들면 참조가 끊긴다.
- 패키지 추가·제거는 사람이 Package Manager에서 한다. 필요하면 패키지 이름과 이유를 알려준다.
- 씬 YAML을 읽을 때는 필요한 부분만 검색한다. 씬 파일은 크다.
- `Assets/ThirdParty/`는 외부 에셋(폰트, 대체용 캐릭터 등)이다. 절대 수정하지 않고, 쓰면 `CREDITS.md`에 출처와 라이선스를 적는다.
