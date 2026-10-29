# Card Battle — combates en los que se arrastran cartas sobre los objetivos

Card Battle es un sistema de combate en el que los personajes son cartas grandes y sus acciones, cartas más pequeñas: elige un personaje y arrastra una de sus cartas de acción sobre un objetivo. Es el sistema de combate de Cthangover, y RPG Plus puede usarlo en lugar de FF Battle. El combate en sí — el inicio, el fondo, las pantallas de victoria y derrota, la experiencia y el botín — lo pone Turn Battle Base.

## La pantalla de combate

El guerrero de RPG Plus contra un lobo, tras hacer clic en el héroe y pasar el cursor por Power strike:

![La pantalla de combate con cartas: el panel Card battle (Round 1, Enemies alive 1) arriba a la izquierda, el botón End turn y los botones de velocidad arriba a la derecha, la carta del lobo a la derecha (HP 31/31, DEF 2, ATK 7, AP 1), la carta del héroe abajo a la izquierda (HP 42/42, DEF 4, ATK 12, AP 1), el registro de combate en medio y el panel Actions con tres cartas — Basic attack, Defence, Power strike — y la pista «Un golpe completo: ataque y medio, y un poco más por fuerza».](assets/card-hover.jpg)

- **Las cartas de personaje** muestran la salud (HP), el escudo (DEF, absorbe el daño primero), el ataque (ATK) y los puntos de acción (AP).
- **El panel Actions** — las cartas de acción del personaje seleccionado, con su objetivo (enemigo o aliado), su coste en AP y sus valores. Las cartas que el personaje no puede pagar aparecen atenuadas.
- **La línea de pista** bajo las cartas dice qué hará la carta bajo el cursor: el daño de un ataque normal, el escudo de una defensa, la descripción de una habilidad escrita como fórmula.
- **End turn** termina antes el turno del grupo; con 0 AP restantes el turno acaba solo.

## Un turno

1. **Haz clic en la carta de un miembro del grupo** para ver sus acciones.
2. **Arrastra una carta de acción sobre un objetivo** — la carta de un enemigo para un ataque, la de un aliado para una defensa. El objetivo indica si la acción se le puede aplicar.
3. Cuando el grupo se queda sin puntos de acción, actúan los enemigos y empieza una ronda nueva.

![El registro de combate y el panel de acciones tras una ronda: Shield of Wolf absorbed 2; Power strike: Hero → Wolf · −19 HP; The party is out of action points; Enemy turn; Defence: Wolf · +4 DEF; Player turn · round 2.](assets/card-turn.jpg)

El Power strike del héroe sacó 21: los 2 primeros los absorbió el escudo del lobo y su salud cayó 19, de 31 a 12. Después el lobo subió su escudo a 4.

## Las acciones

Card Battle ejecuta los mismos tipos básicos de acciones que FF Battle:

- `Attack=1.5` — el daño del ataque multiplicado por el número; lo absorbe primero el escudo del objetivo;
- `Defence=4` — un escudo sobre un aliado;
- `Turn=2` — aturdimiento durante ese número de turnos.

Las habilidades escritas como fórmulas (valores `Formula=`, como el Power strike de Classic RPG Rules) también funcionan aquí, mediante Battle Formulas.

## Cómo elegir Card Battle

En la colección del juego pon `"battle.core": "card_battle"`, o cambia de sistema desde un escenario antes del combate:

```scenario
action battle.set_core core=card_battle
```

## Si algo sale mal

1. **Arrastrar la carta no hace nada.** No hay personaje seleccionado, al personaje le faltan puntos de acción o el objetivo es del bando equivocado (los ataques van a los enemigos, las defensas a los aliados).
2. **El combate se abre en FF Battle.** El ajuste `battle.core` del juego nombra `ff_battle`.
