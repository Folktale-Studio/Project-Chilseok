---
paths:
  - "Assets/Chilseok/Editor/**/*.cs"
---

# 에디터 스크립트 규칙

- `namespace Chilseok.EditorTools` 안에 둔다. 메뉴는 `Tools/Chilseok/` 아래에 번호를 붙여 실행 순서를 드러낸다(예: `Tools/Chilseok/1. Setup Scenes`).
- 씬·프리팹·애니메이터·Timeline·ScriptableObject·머티리얼 에셋과 임포트 설정은 이 폴더의 에디터 코드로 만든다. (ADR-0003)
- 메뉴는 멱등이어야 한다. 이미 있으면 갱신하고, 없으면 만든다 ("Create Or Update").
- 경로는 파일 상단의 `const string`으로 모은다. 서드파티 경로(`Assets/ThirdParty/...`)도 여기서만 참조한다.
- 씬 오브젝트를 만들거나 바꾸면 `Undo.RegisterCreatedObjectUndo` / `Undo.RecordObject` / `Undo.AddComponent`로 되돌릴 수 있게 하고, `EditorSceneManager.MarkSceneDirty`로 표시한다. 씬을 자동 저장하지 않는다.
- 에셋을 찾지 못하면 `Debug.LogError($"[Chilseok] ...: {path}")`처럼 무엇이 없는지 경로와 함께 알리고 중단한다.
- 구간 텍스트(`Assets/Chilseok/Levels/*.txt`)를 씬에 블록으로 깔아 주는 메뉴는 런타임과 같은 파서(`Chilseok.Puzzle.ZoneParser`)를 쓴다.
