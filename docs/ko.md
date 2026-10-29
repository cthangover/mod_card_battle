# Card Battle — 카드를 대상에 끌어다 놓는 전투

Card Battle은 캐릭터가 큰 카드로, 그 행동은 더 작은 카드로 표현되는 전투 시스템입니다. 캐릭터를 고르고 행동 카드 하나를 대상에 끌어다 놓으세요. Cthangover의 전투 시스템이며, RPG Plus도 FF Battle 대신 이 시스템을 쓸 수 있습니다. 전투 자체 — 시작, 배경, 승리·패배 화면, 경험치와 전리품 — 은 Turn Battle Base가 맡습니다.

## 전투 화면

RPG Plus의 전사가 늑대와 맞서는 장면입니다. 영웅을 클릭하고 Power strike에 마우스를 올려 둔 상태죠:

![카드 전투 화면: 왼쪽 위에 Card battle 패널(Round 1, Enemies alive 1), 오른쪽 위에 End turn 버튼과 속도 버튼, 오른쪽에 늑대의 카드(HP 31/31, DEF 2, ATK 7, AP 1), 왼쪽 아래에 영웅의 카드(HP 42/42, DEF 4, ATK 12, AP 1), 가운데에 전투 로그, 그리고 Actions 패널에는 Basic attack·Defence·Power strike 세 장의 카드와 힌트 "전력으로 휘두르는 일격: 공격의 1.5배에 힘에서 조금 더".](assets/card-hover.jpg)

- **캐릭터 카드**에는 체력(HP), 방어막(DEF, 피해를 먼저 흡수), 공격력(ATK), 행동력(AP)이 표시됩니다.
- **Actions 패널** — 선택한 캐릭터의 행동 카드 모음입니다. 대상(적 또는 아군), AP 소모, 수치가 표시되고, 캐릭터가 지불할 수 없는 카드는 흐리게 보입니다.
- 카드 아래의 **힌트 줄**에는 마우스를 올린 카드가 무엇을 하는지 나옵니다. 일반 공격은 피해, 방어는 방어막, 수식으로 쓰인 스킬은 설명이 표시됩니다.
- **End turn**은 파티 턴을 일찍 끝냅니다. AP가 0으로 떨어지면 턴은 저절로 끝납니다.

## 한 턴의 진행

1. **파티원의 카드를 클릭**하면 그 캐릭터의 행동이 보입니다.
2. **행동 카드를 대상에 끌어다 놓습니다** — 공격은 적 카드에, 방어는 아군 카드에 놓습니다. 대상 쪽에 그 행동을 적용할 수 있는지 표시됩니다.
3. 파티의 행동력이 바닥나면 적들이 행동하고, 새 라운드가 시작됩니다.

![한 라운드 후의 전투 로그와 행동 패널: Shield of Wolf absorbed 2; Power strike: Hero → Wolf · −19 HP; The party is out of action points; Enemy turn; Defence: Wolf · +4 DEF; Player turn · round 2.](assets/card-turn.jpg)

영웅의 Power strike은 21의 피해를 냈습니다. 처음 2는 늑대의 방어막이 흡수했고, 체력은 31에서 12로 19 줄었습니다. 그러고는 늑대가 방어막을 4까지 올립니다.

## 행동

Card Battle은 FF Battle과 같은 기본 행동 유형을 처리합니다:

- `Attack=1.5` — 공격 피해에 그 수를 곱한 값입니다. 대상의 방어막이 먼저 흡수합니다;
- `Defence=4` — 아군에게 방어막을 겁니다;
- `Turn=2` — 그만큼의 턴 동안 기절.

수식으로 쓰인 스킬(`Formula=` 값. Classic RPG Rules의 Power strike 등)도 Battle Formulas를 통해 여기서 작동합니다.

## Card Battle 선택하기

게임 컬렉션 설정에 `"battle.core": "card_battle"`를 넣거나, 전투 전에 시나리오로 바꿔 주세요:

```scenario
action battle.set_core core=card_battle
```

## 문제가 생겼을 때

1. **카드를 끌어다 놓아도 아무 일도 없음.** 캐릭터가 선택되지 않았거나, 행동력이 부족하거나, 대상 진영이 틀립니다(공격은 적에게, 방어는 아군에게).
2. **전투가 FF Battle로 열림.** 게임의 `battle.core` 설정이 `ff_battle`을 가리킵니다.
