# Card Battle —— カードをターゲットにドラッグする戦闘

Card Battle は、キャラクターが大きなカード、そのアクションが小さなカードとして表されるバトルシステムです。キャラクターを選び、そのアクションカードの一枚をターゲットへドラッグします。Cthangover のバトルシステムであり、RPG Plus は FF Battle の代わりにこれを使えます。戦闘そのもの——開始、背景、勝利・敗北の画面、経験値と戦利品——は Turn Battle Base が担います。

## 戦闘画面

RPG Plus の戦士対狼。英雄をクリックし、Power strike にカーソルを重ねたところです：

![カードバトルの画面：左上に Card battle パネル（Round 1、Enemies alive 1）、右上に End turn ボタンと速度ボタン、右側に狼のカード（HP 31/31、DEF 2、ATK 7、AP 1）、左下に英雄のカード（HP 42/42、DEF 4、ATK 12、AP 1）、中央に戦闘ログ、そして Actions パネルには Basic attack・Defence・Power strike の3枚のカードとヒント「渾身の一撃：攻撃の1.5倍に力の値を少し上乗せ」。](assets/card-hover.jpg)

- **キャラクターカード**には体力（HP）、シールド（DEF。ダメージを最初に引き受ける）、攻撃（ATK）、行動ポイント（AP）が表示されます。
- **Actions パネル**——選択中キャラクターのアクションカードです。対象（敵か味方）、AP 消費、数値が示され、支払えないカードは薄く表示されます。
- カードの下の**ヒント行**には、カーソルを合わせたカードの効果が出ます。通常攻撃ならダメージ、防御ならシールド、数式で書かれたスキルなら説明です。
- **End turn** はパーティのターンを途中で終わらせます。AP が 0 になればターンは自然に終わります。

## ターンの流れ

1. **パーティのカードをクリック**して、そのアクションを表示します。
2. **アクションカードをターゲットへドラッグ**します。攻撃なら敵のカード、防御なら味方のカードです。その対象に効くかどうかはカード側で示されます。
3. パーティの行動ポイントを使い切ると、今度は敵が行動し、新しいラウンドが始まります。

![1ラウンド後の戦闘ログとアクションパネル：Shield of Wolf absorbed 2／Power strike: Hero → Wolf · −19 HP／The party is out of action points／Enemy turn／Defence: Wolf · +4 DEF／Player turn · round 2。](assets/card-turn.jpg)

英雄の Power strike は 21 のダメージ。最初の 2 は狼のシールドが受け止め、体力は 31 から 12 へと 19 減りました。その後、狼はシールドを 4 まで上げます。

## アクション

Card Battle は、FF Battle と同じ基本種類のアクションを実行します：

- `Attack=1.5`——攻撃ダメージに数値を掛けたもの。まず対象のシールドが受け止めます；
- `Defence=4`——味方にシールドを付与；
- `Turn=2`——そのターン数だけスタン。

数式で書かれたスキル（`Formula=` 値。Classic RPG Rules の Power strike など）も、Battle Formulas を通してここで機能します。

## Card Battle の選び方

ゲームのコレクション設定で `"battle.core": "card_battle"` を指定するか、戦闘前にシナリオから切り替えてください：

```scenario
action battle.set_core core=card_battle
```

## うまくいかないときは

1. **カードをドラッグしても何も起きない。** キャラクターが選ばれていない、行動ポイントが足りない、または対象の陣営が違います（攻撃は敵へ、防御は味方へ）。
2. **戦闘が FF Battle で開く。** ゲームの `battle.core` 設定が `ff_battle` になっています。
