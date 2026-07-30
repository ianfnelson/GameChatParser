# GameChatParser

A console application that reads an exported group chat, finds every daily puzzle result
shared in it, and ranks the participants over the current and previous month and the
current and previous year.

It replaces two separate applications,
[WordleParser](https://github.com/ianfnelson/WordleParser) and
[ConnectionsParser](https://github.com/ianfnelson/ConnectionsParser), which did the same
job one game at a time. Wordle, Connections and Strands are now reported from a single run
over a single export, and a fourth game can be added without touching the parsing,
grouping, ranking or rendering code.

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
1. Nadia    207    3.928
2. Carol    207    3.981
3. Katie    199    4.146
```
*Connections — 2026*
```
1. Nadia     207    1.952
2. Joe       162    2.327
3. Yvonne    148    2.622
```
*Strands — 2026*
```
1. Carol     39     1.229
2. Joe       40     1.288
3. Katie     33     1.919
```
````

The columns are position, player, puzzles played in the period, and mean score. Tables
are emitted in period order, with every game shown against each period: current year,
previous year, current month, previous month.

### Names

A phone screen has little horizontal room, so players are shown by forename alone
wherever that is enough to tell them apart. Where a forename is shared, everybody
sharing it gains a surname initial, and where that still leaves two of them looking
alike, they are all shown under their whole name:

```
1. Nadia       207    3.928
2. Carol W.    207    3.981
3. Carol M.    199    4.146
```

Names are shortened against the whole report rather than one table at a time, so a
player reads the same way wherever they turn up in it. The name column then takes only
the room the longest name on that table needs, leaving three spaces before the scores.

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
7. **Render.** Names are shortened to the shortest form that still tells the players
   apart, and each table is written as a bold heading above a fenced block.

### Positions and ties

Positions use standard competition ranking. Players with the same average share a
position, marked with `=` instead of `.`, and the positions they consume are skipped:

```
1. Carol      26    3.885
2= Nadia      26    4.154
2= Yvonne     24    4.154
4. Joe        26    4.423
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

**Scored** by counting everything that went wrong, reading each row of the grid:

| Row | Meaning | Points |
| --- | --- | --- |
| Four identical squares | a group solved | 0 |
| Any other four squares | a wrong guess | +1 |

and charging a further point for each group that was never found:

```
score = mistakes + (4 − groups solved)
```

**A lower average is better**, and a flawless win scores 0, on the same principle as the
rest of the scoring here: count what went wrong. Only seven scores are actually reachable,
because solving three groups completes the fourth for you, so a game can never end with
three groups and a loss:

| Outcome | Groups | Mistakes | Score |
| --- | --- | --- | --- |
| Won, flawless | 4 | 0 | 0 |
| Won | 4 | 1 | 1 |
| Won | 4 | 2 | 2 |
| Won, on the last life | 4 | 3 | 3 |
| Lost, two groups found | 2 | 4 | 6 |
| Lost, one group found | 1 | 4 | 7 |
| Lost, nothing found | 0 | 4 | 8 |

Winning always beats losing however untidily it was done, since a three point gap
separates the worst win from the best loss. The scale is otherwise deliberately even: a
three point band across the wins and a two point one across the losses, so how cleanly a
player wins counts for about as much as how often they lose. The earlier scoring, carried
over from `ConnectionsParser`, was `(10 × groups solved) − mistakes`, which spread the
losses over twenty points and the wins over three, and so measured little beyond the loss
rate.

**Dated** by adding the puzzle number to 11 June 2023, which puts Puzzle #619 on
19 February 2025 and Puzzle #1 on 12 June 2023.

## Strands

Strands gives you a themed six-by-eight grid of letters, a clue, and a count of theme
words to find. One of those words is the spangram, a word or short phrase naming the theme
itself, which spans two opposite edges of the grid. Finding three words that are not theme
words earns a hint, which lights up the letters of one theme word. A result is only shared
for a completed puzzle, so there is no losing outcome to score, and as with Connections
there is no summary line: the score has to be read out of the grid.

**Recognised by** a line beginning `Strands #832`, followed by at least one row of the
grid:

```text
Strands #832
“Track event”
🔵🟡🔵🔵
🔵💡🔵🔵
```

A row is any line holding no letters or digits, which leaves the clue out of it, and the
first must hold at least four items. The game wraps its grid four to a line and no puzzle
holds fewer than five theme words, so every share opens with a full row; the rows after it
count however short, since the last one is a remainder and carries hints like any other.
The first line that is not a row ends the grid, so a remark typed underneath a share is
not read as part of it.

Items are classified by how often they appear rather than against a fixed set of emoji.
💡 is the hint; of what remains, the item appearing repeatedly is a theme word and the one
appearing once is the spangram. The New York Times swaps the set on holiday puzzles, as it
did on 4 July, when 🎆 stood for the theme words and 🇺🇸 for the spangram, and counting
frequencies reads that grid correctly without knowing anything about fireworks.

**Scored** on the two things that separate one completed game from another, the hints
taken and how late the spangram turned up:

```
score = hints + 0.5 × (k − 1) / (n − 1)
```

where `hints` is the number of 💡, `n` is the number of theme words found including the
spangram, and `k` is the spangram's place among them. Hints are ignored when working out
`k`, so taking one never moves the spangram's position.

| Grid | Hints | Spangram | Score |
| --- | --- | --- | --- |
| `🟡🔵🔵🔵` `🔵🔵🔵🔵` | 0 | 1st of 8 | 0.000 |
| `🔵🔵🔵🔵` `🔵🔵🟡` | 0 | 7th of 7 | 0.500 |
| `💡🔵💡🔵` `💡🔵🔵🟡` `🔵` | 3 | 5th of 6 | 3.400 |
| `💡🔵💡🔵` `💡🔵🟡🔵` `💡🔵` | 4 | 4th of 6 | 4.300 |

**A lower average is better**, and a clean game with the spangram spotted first scores 0.
Hints are the primary measure, one point each, on the same principle as the rest of the
scoring here: count what went wrong. The spangram term is the tie-break, and it is capped
at half a hint deliberately, so the whole distance between finding the spangram first and
finding it last is worth less than a single hint. Hint discipline therefore always decides
the order, and the bonus can only separate players who are already level on hints.

Dividing by `n − 1` gives every puzzle the same 0 to 1 span whether it holds five theme
words or nine, so a short puzzle is neither easier nor harder to score well on than a long
one.

**Dated** by adding the puzzle number to 3 March 2024, which puts Strands #1 on
4 March 2024, the day the game entered beta, and Strands #764 on 6 April 2026.

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
