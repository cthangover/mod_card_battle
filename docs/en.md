# Card Battle — battles with cards dragged onto targets

Card Battle is a battle system in which characters are big cards and their actions are smaller cards: pick a character, then drag one of its action cards onto a target. It is the battle system of Cthangover, and RPG Plus can use it instead of FF Battle. The battle itself — its start, backdrop, victory and defeat screens, experience and loot — comes from Turn Battle Base.

## The battle screen

The RPG Plus warrior against a wolf, after clicking the hero and hovering over Power strike:

![The card battle screen: the Card battle panel (Round 1, Enemies alive 1) at the top left, the End turn button and speed buttons at the top right, the wolf's card on the right (HP 31/31, DEF 2, ATK 7, AP 1), the hero's card at the bottom left (HP 42/42, DEF 4, ATK 12, AP 1), the battle log in the middle and the Actions panel with three cards — Basic attack, Defence, Power strike — and the hint "A full swing: one and a half times the attack and a bit more from strength."](assets/card-hover.jpg)

- **Character cards** show health (HP), the shield (DEF, absorbs damage first), attack (ATK) and action points (AP).
- **Actions panel** — the action cards of the selected character, with their target (enemy or ally), cost in AP and values. A card the character cannot pay for is dimmed.
- **The hint line** under the cards shows what the hovered card will do: the damage for a plain attack, the shield for a defence, the description for a skill written as a formula.
- **End turn** ends the party's turn early; with 0 AP left the turn ends by itself.

## A turn

1. **Click a party card** to see its actions.
2. **Drag an action card onto a target** — an enemy card for an attack, an ally card for a defence. The target shows whether the action can be applied there.
3. When the party is out of action points, the enemies act, and a new round starts.

![The battle log and actions panel after one round: Shield of Wolf absorbed 2; Power strike: Hero → Wolf · −19 HP; The party is out of action points; Enemy turn; Defence: Wolf · +4 DEF; Player turn · round 2.](assets/card-turn.jpg)

The hero's Power strike did 21: the wolf's shield of 2 took the first part and its health fell by 19, from 31 to 12. The wolf then raised its shield to 4.

## Actions

Card Battle carries out the same basic kinds of actions as FF Battle:

- `Attack=1.5` — damage of the attack times the number, absorbed first by the target's shield;
- `Defence=4` — a shield on an ally;
- `Turn=2` — stun for that many turns.

Skills written as formulas (`Formula=` values, such as Classic RPG Rules' Power strike) work here too through Battle Formulas.

## Choosing Card Battle

In a game's collection set `"battle.core": "card_battle"`, or switch from a scenario before the battle:

```scenario
action battle.set_core core=card_battle
```

## If something goes wrong

1. **Dragging a card does nothing.** No character is selected, the character has too few action points, or the target is of the wrong side (attacks go onto enemies, defences onto allies).
2. **The battle opens in FF Battle.** The game's `battle.core` setting names `ff_battle`.
