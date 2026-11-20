# 도입 시네마틱

목적: 대사 없이 약 55초 안에 "함께였던 두 사람이 갈라졌고, 견우가 다시 길을 나선다"를 전한다.
연출 방식: 극적인 카메라 앵글의 흑백 일러스트를 레이어로 나눠 원근 카메라와 Timeline으로 움직이고, 화면 마지막 패스에서 1비트 디더링한다(ADR-0005). 레퍼런스: Who's Lila(2D 그림 + 뼈 애니메이션 + 실시간 디더링).

## 컷

| 컷 | 장면 | 디더 연출 | 범위 | 상태 |
| --- | --- | --- | --- | --- |
| 1 | 하늘 강 전경. 프롤로그 문장 | 점이 피어나는 페이드, 별 반짝임 | 필수 | 그림 대기 |
| 2 | 성실하던 시절: 왼쪽 소를 모는 견우, 오른쪽 베틀 앞의 직녀 | 양쪽에서 중앙으로 패닝 | 있으면 좋음 | 그림 대기 |
| 3 | 함께 살던 한때: 중앙에 나란히 앉은 두 사람의 실루엣 | 디더 디졸브 전환 | 있으면 좋음 | 그림 대기 |
| 4 | 이별: 은하수가 갈라지며 두 사람이 양끝으로 밀려남 | 좁은 강 → 넓은 강 디졸브, 먹·한지 반전 | 필수 | 그림 대기 |
| 5 | 기다림과 징조: 하늘을 향해 손을 뻗는 견우 위로 달이 바뀜 | 달 교체 순환, 별 반짝임, 느린 카메라 밀기 | 추천 | 본 그림 보유(디더링됨, 블러 복원 필요) |
| 6 | 출발: 까치가 날아오르고 견우가 삿갓을 고쳐 씀 | 디더 디졸브로 인게임 전환 | 필수 | 그림 대기 |

갈라놓는 존재(옥황상제)는 등장시키지 않고 강이 넓어지는 장면으로만 처리한다.

### 5번 컷 달 순환

보름달 → 그믐달 → 달 없는 밤 → 초승달 → 상현달. 이 순환을 점점 빠르게 2~3번 돌려 일 년이 흘렀음을 보여 주고(첫 순환 장당 약 0.5초, 마지막 순환 장당 약 0.15초), 상현달에서 1초 멈춘 뒤 별 하나가 유독 반짝인다. 달 없는 밤은 달 레이어를 끈다.

상현달에서 멈추는 이유: 칠석은 음력 7월 7일이고, 음력 7일 밤의 달은 오른쪽 절반이 빛나는 상현달이다. 보름달(음력 15일쯤)로 끝나면 날짜가 맞지 않는다.

## 필요한 그림

`Assets/Art/Cinematic/cutNN/`에 둔다. 16:9, 1920×1080 이상, 디더링 없는 흑백. 움직임용 그림은 원본을 이미지 편집으로 고쳐 만든다.

| 컷 | 본 그림 | 추가 그림 |
| --- | --- | --- |
| 1 | 하늘 강 전경 | 없음 |
| 2 | 성실하던 시절 (2:1로 넓게) | 없음 |
| 3 | 함께 살던 한때 실루엣 | 없음 |
| 4 | 좁은 강 부감 | 넓은 강 편집본 |
| 5 | 손을 뻗는 견우 (보유) | 달 4장(보름·그믐·초승·상현, 검은 배경, 같은 크기·위치), 손을 지운 배경판 |
| 6 | 삿갓을 고쳐 쓰는 견우 | 까치를 지운 편집본, 배경 없는 까치 |

## 프롬프트

**공통 앞부분** (견우 시안, 공주풍 직녀 시안, 5번 컷 그림을 참조로 첨부)
```
Black and white ink illustration, manga-style bold linework, high contrast,
dramatic cinematic camera angle with strong perspective, simple grayscale
shading, no halftone, no dither, no text, 16:9.
Gyeonu: young Korean cowherd, wide conical straw hat, topknot, white headband
tied at the side, light jeogori with a bow, rope belt with tassels, dark baggy
trousers, white leg wraps, straw sandals.
Jiknyeo: celestial princess, low braided bun with a ribbon and ornate hairpin,
light jeogori with dark collar, long dark skirt with stars and waves on the hem.
Keep both character designs identical to the reference images.
```

1. `Extreme low-angle wide shot from the grass of a quiet riverbank, looking up at a vast river of stars flowing across the night sky. On the near bank a tiny silhouette of an ox grazing, on the far bank a tiny weaving hut with a single glowing window. Huge sky filling most of the frame, countless small bright stars.`
2. `Wide 2:1 panoramic composition split by a thin vertical stream of stars in the center. Left half: Gyeonu in a low-angle medium shot, diligently leading an ox through a field by a rope, facing right. Right half: Jiknyeo seated at a wooden loom, focused on her weaving, shuttle in hand, facing left. Calm, hardworking mood, the two unaware of each other.`
3. `Shot from behind and slightly below, Gyeonu and Jiknyeo sitting side by side at the center of the frame on a grassy riverbank, shoulders touching, her head resting on his shoulder, his straw hat set down beside them, both looking up at the glowing river of stars. Dark silhouettes backlit by bright starlight, the ox and the loom left idle in the distance.`
4. `Extreme high-angle aerial view looking straight down from the sky. A river of stars runs diagonally through the frame. Gyeonu and Jiknyeo stand on opposite banks, small figures reaching toward each other across a narrow stream.`
   편집본: `Keep everything identical. Only change: the river of stars is now vast, bright and wide, filling most of the frame, and the two figures are pushed far apart to opposite edges, still reaching out.`
5. 달: `Same art style, a [full moon / waning crescent moon with only a thin sliver lit on the LEFT side / waxing crescent moon with only a thin sliver lit on the RIGHT side / first quarter half moon with exactly the RIGHT half lit] alone on a plain black background, centered, same size in every image, glowing softly.` 뽑은 뒤 빛나는 방향을 확인하고 틀리면 좌우 반전한다.
6. `Low-angle close-up of Gyeonu from the chest up, gripping the brim of his straw hat with one hand to set it firmly, tear streaks drying on his cheeks, determined eyes looking up and to the right. A magpie bursts upward past him with wings spread, night sky with a few clouds behind.`
   편집본: `Keep everything identical. Only change: remove the magpie and fill the area with the same sky.` / 까치: `Same art style, a single magpie with wings spread in flight, side view, on a plain white background.`

## 구현 메모

- 레이어는 쿼드로 깊이(Z 0.5~10)별로 세우고 원근 카메라를 천천히 민다. 움직임은 컷마다 핵심 하나만 둔다(과하면 기괴해진다).
- 손가락·머리끈은 Unity 2D Animation 뼈, 큰 동작은 그림 2~3장 교체(초당 6~8장).
- 이미 디더링된 그림은 점 간격만큼 가우시안 블러(원본 1673px 폭 기준 σ≈4)로 그레이스케일을 복원해 쓴다.
- 디더 연출 값은 `DitherSettings` 하나로 Timeline에서 움직인다. 보일링 노이즈는 끄거나 아주 약하게.
