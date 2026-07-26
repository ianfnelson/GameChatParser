# GameChatParser

A console application that reads an exported group chat, finds every daily puzzle result
shared in it, and ranks the participants over the current and previous month and the
current and previous year.

It replaces two separate applications,
[WordleParser](https://github.com/ianfnelson/WordleParser) and
[ConnectionsParser](https://github.com/ianfnelson/ConnectionsParser), which did the same
job one game at a time. Both games are now reported from a single run over a single
export, and a third game can be added without touching the parsing, grouping, ranking or
rendering code.

There is more background on the original Wordle version at
[Who Reigns Supreme? Parsing Our WhatsApp Chat For Wordle Glory](https://blog.iannelson.uk/who-reigns-supreme-parsing-our-whatsapp-chat-for-wordle-glory/).

## Usage

Export the chat from WhatsApp without media, which produces a `_chat.txt`, then point the
application at it:

```bash
dotnet run --project src/GameChatParser -- ~/dev/_chat.txt
```

The output is ready to paste straight back into the chat, where the fenced blocks keep
the columns lined up:

````text
*Wordle — 2026*
```
1. Nadia Corbin     207    3.928
2. Carol Whelan     207    3.981
3. Katie Munro      199    4.146
```
*Connections — 2026*
```
1. Nadia Corbin     207   35.570
2. Joe Whelan       162   31.728
3. Yvonne Clarke    148   30.750
```
````

The columns are position, player, puzzles played in the period, and mean score. Tables
are emitted in period order, with both games shown against each period: current year,
previous year, current month, previous month.

Player names in this document, and in the test suite, are invented. The application is
run against a private family chat, so no real participant is named here.

## How a result becomes a ranking

The pipeline is the same for every game. Only the middle step, turning a message into a
score, knows anything about a particular game.

1. **Read the chat.** A message begins with a bracketed timestamp and a sender name.
   Everything up to the next such header belongs to the same message, which is what keeps
   a grid of coloured squares attached to the summary line above it.
2. **Extract scores.** Every message is offered to every game. A game returns a score, or
   nothing where the message holds no result of its kind. A message carrying results for
   two games counts for both.
3. **Date the result.** Daily puzzles are numbered, and the number is what dates the
   result, not the timestamp on the message. Somebody catching up on Sunday still has
   their Friday puzzle counted against Friday.
4. **Discard repeats.** A player counts once per puzzle, however many times they post it.
   The first posting wins.
5. **Group into periods.** Scores are grouped by calendar year and by calendar month, and
   the two most recent of each that hold any results are reported. Periods are relative to
   the data, not to today's date, so a chat that went quiet in March still reports on
   March.
6. **Rank.** Within a period, each player's scores are averaged, and the players are
   sorted by that average. Ranking is by mean rather than total, so somebody who misses a
   fortnight is not punished for it.
7. **Render.** Each table is written as a bold heading above a fenced block.

### Positions and ties

Positions use standard competition ranking. Players with the same average share a
position, marked with `=` instead of `.`, and the positions they consume are skipped:

```
1. Carol Whelan      26    3.885
2= Nadia Corbin      26    4.154
2= Yvonne Clarke     24    4.154
4. Joe Whelan        26    4.423
```

Two averages count as equal when they differ by less than 0.0001, since averages that
ought to be identical can differ in the last bits after division. Tied players are listed
alphabetically, which affects the order they are printed in but not the position they are
given.

## Wordle

Wordle gives you six guesses at a five-letter word, and the shared result reports how many
you took.

**Recognised by** a line beginning `Wordle`, followed by the puzzle number and the result,
such as `Wordle 1,341 4/6`. The thousands separator is optional, and anything after the
result, such as a hard mode asterisk, is ignored.

**Scored** as the number of guesses taken, so **a lower average is better**.

| Result | Score |
| --- | --- |
| `1/6` … `6/6` | 1 … 6 |
| `X/6`, the word was not found | 7 |

A failure scores 7, one worse than the maximum of six guesses. That charges a player more
for a failure than for a scrape home, without letting one bad day swamp the rest of their
month the way a heavier penalty would.

**Dated** by adding the puzzle number to 19 June 2021, which puts Wordle 1,281 on
21 December 2024. That is a day later than Wordle's original launch, because the New York
Times shifted the numbering by one after taking the game over.

## Connections

Connections gives you sixteen words to sort into four groups of four, and four mistakes
before you lose. Unlike Wordle the shared result carries no summary of how you did, only
the grid, so the score has to be reconstructed from it.

**Recognised by** a `Puzzle #619` line together with at least one row of the grid. A row
is any line holding exactly four Connections squares, which are 🟨 yellow, 🟩 green,
🟦 blue and 🟪 purple. Width is what tells a Connections grid from a Wordle one: Wordle
shares two of the same colours but its rows are five squares wide.

**Scored** by reading each row of the grid:

| Row | Meaning | Points |
| --- | --- | --- |
| Four identical squares | a group solved | +10 |
| Any other four squares | a wrong guess | −1 |

so that:

```
score = (10 × groups solved) − mistakes
```

**A higher average is better.** Only seven scores are actually reachable, because solving
three groups completes the fourth for you, so a game can never end with three groups and
a loss:

| Outcome | Groups | Mistakes | Score |
| --- | --- | --- | --- |
| Won, flawless | 4 | 0 | 40 |
| Won | 4 | 1 | 39 |
| Won | 4 | 2 | 38 |
| Won, on the last life | 4 | 3 | 37 |
| Lost, two groups found | 2 | 4 | 16 |
| Lost, one group found | 1 | 4 | 6 |
| Lost, nothing found | 0 | 4 | −4 |

Winning always beats losing however untidily it was done. Note that the scale is not
evenly spread: the four winning scores sit within three points of each other, while the
losing scores are spread over twenty, so a player's average is driven far more by how
often they lose than by how cleanly they win.

**Dated** by adding the puzzle number to 11 June 2023, which puts Puzzle #619 on
19 February 2025 and Puzzle #1 on 12 June 2023.

## Adding another game

Implement `IGame` and register it. Nothing else changes.

```csharp
public sealed class QuordleGame : PuzzleGame
{
    public override string Name => "Quordle";

    public override RankingDirection RankingDirection => RankingDirection.LowerIsBetter;

    protected override DateOnly PuzzleZeroDate => new(2022, 1, 23);

    public override GameScore? TryParseScore(ChatMessage message)
    {
        // Return a GameScore, or null where the message holds no Quordle result.
    }
}
```

Add it to `GameRegistry.Default`, where the order decides which game is printed first
against each period. Deriving from `PuzzleGame` brings the puzzle-number-to-date
arithmetic with it; a game dated some other way can implement `IGame` directly.

The grouping, ranking, tie handling and rendering are all game agnostic, and read a game
only through `Name` and `RankingDirection`.

## Project layout

| Project | Purpose |
| --- | --- |
| `src/GameChatParser.Core` | Reading chats, recognising games, ranking and rendering |
| `src/GameChatParser` | The console application |
| `src/GameChatParser.Tests` | xUnit tests |

Within `Core`:

| Namespace | Purpose |
| --- | --- |
| `Chat` | `IChatReader` and the WhatsApp export reader |
| `Games` | `IGame`, the `PuzzleGame` base class, the registry and the games themselves |
| `Scoring` | `GameScore`, one player's result for one puzzle |
| `Reporting` | Grouping into periods, ranking, and rendering |

Nothing in `Core` touches the file system: the report is built from a sequence of lines,
which is what lets the tests drive the whole pipeline from a string.

## Building and testing

```bash
dotnet build
dotnet test
```

Targets .NET 10, and uses the `.slnx` solution format.
