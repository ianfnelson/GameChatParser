# GameChatParser

A console application that reads an exported group chat, finds every daily puzzle result
shared in it, and ranks the participants over the current and previous month and the
current and previous year.

It replaces two separate applications,
[WordleParser](https://github.com/ianfnelson/WordleParser) and
[ConnectionsParser](https://github.com/ianfnelson/ConnectionsParser), which did the same
job one game at a time. Wordle, Connections, Strands and both Zanagrams puzzles are now
reported from a single run over a single export, and a further game can be added without
touching the parsing, grouping, ranking or rendering code.

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
*Connections — 2026*
```
1. Nadia     213    2.005
2. Joe       168    2.298
3. Yvonne    152    2.592
```
*Connections — 2025*
```
1. Nadia       33    1.121
2. Yvonne     180    1.856
3. Joe        276    2.174
```
*Connections — August*
```
1. Nadia       1    0.000
2. Carol       1    1.000
3. Joe         1    2.000
```
*Connections — July*
```
1. Nadia      31    2.710
2. Joe        28    3.036
3. Bea        15    3.133
```
````

The columns are position, player, puzzles played in the period, and the figure the game is
ranked on, which for most of them is a mean score. A game's tables are kept together and
the games run in name order, so the report reads game by game: current year, previous
year, current month, previous month, then on to the next game. That run was taken on the
first of the month, which is why August has a single puzzle in it.

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
5. **Settle the relative scores.** Most games score a result on its own terms and skip
   this step. A game ranked against the day's field, as Zanagrams is, gets its whole set
   of scores here and returns the set it should be ranked on, which lets it rewrite them
   against each other and drop the ones it cannot rank. It happens once, after the repeats
   have gone and before the periods are split out, so that a day's field is the whole
   day's field.
6. **Group into periods.** Scores are grouped by calendar year and by calendar month, and
   the two most recent of each that hold any results are reported. Periods are relative to
   the data, not to today's date, so a chat that went quiet in March still reports on
   March.
7. **Rank.** Within a period, each player's scores are reduced to one figure, by default
   their mean, and the players are sorted by it. Ranking is by mean rather than total, so
   somebody who misses a fortnight is not punished for it.
8. **Render.** Names are shortened to the shortest form that still tells the players
   apart, and each table is written as a bold heading above a fenced block. A game's
   tables run together, yearly ahead of monthly and most recent first, and the games
   themselves are ordered by name.

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

## Zanagrams

[Zanagrams](https://pzlgames.com/games/zanagrams/) gives you a cluster of letters joined by paths and a
clue for each word to find. You trace a word by dragging along the paths, the letters you
used disappear, and the grid collapses inwards until every word is found. A hint reveals
the next group of letters in a word. A result is only shared for a completed puzzle, so as
with Strands there is no losing outcome to score.

There are two puzzles a day, an Original and a harder Master, sharing a puzzle number and
a share format. They are **two games here rather than one**, because they are not the same
game and they are not played by the same people: one player takes the Original every day and
has never touched the Master, so a combined table would rank them against a field they never
met.

**Recognised by** a heading naming the puzzle, `Zanagrams #12` for the Original and
`Zanagrams Master #12` for the Master, above the time it was solved in:

```text
Zanagrams #12

🎉 Solved in 01:22

🚀 02:02 faster than global average

💡 0 hints used

https://zanagrams.com/
```

The heading is anchored to the start of a line, so prose mentioning a puzzle number is not
read as a share, and each game's heading is written so that it cannot match the other's.
The game wrote `Complete in` before it settled on `Solved in`, and the earliest shares carry
no `💡` line at all; both older forms parse, and a missing hint line counts as no hints. A
message naming a puzzle with no time to read is ignored rather than scored, on the same
principle as a Connections message with no grid. No failed or abandoned result appears in
the export, so the format for one, if there is one, is unknown.

### The move to a new site

On 26 August 2026 the game moved from `zanagrams.com` to `pzlgames.com`, which rewrote the
share around the same three facts:

```text
🔵 Zanagrams #73

🔥 Solved in 02:43

🚀 00:14 faster than global

🎯 Perfect solve!

https://pzlgames.com/games/zanagrams/
```

Four things changed, and **both formats parse**, since the export holds two months of the
old one:

- **The heading gained a coloured disc**, 🔵 for the Original and 🟠 for the Master, so the
  headings allow one in front of the game's name. Both discs sit outside the basic
  multilingual plane, which is a trap worth naming: to a regex, working on UTF-16 code
  units, each disc is a surrogate pair rather than one character, so a class of symbols
  alone would match half of one.
- **The solve time changed emoji**, 🔥 for 🎉. The phrase `Solved in` is what picks the line
  out, so nothing had to change for it.
- **The boast lost a word**, `faster than global` for `faster than global average`. It is
  ignored either way.
- **The hint line went**, replaced by a `🎯 Perfect solve!` badge on the shares that took
  none: 44 of the 51 shares since the move carry it, against 90% of the older shares
  reporting no hints. A count the site never shares cannot be read, so a share from the new
  site is charged for no hints, and the badge is left unread rather than turned into a
  penalty for the shares missing it.

**The move also renumbered the game.** The last puzzle on the old numbering was #63, shared
on 25 August 2026; the next day's, which would have been #64, was shared as #73. Since the
puzzle number is what dates a result, a new site's number is restated nine lower before it
is used, which is what keeps a September result out of the middle of September's leaderboard
and puts both sites' shares of one puzzle in one field. The number is what says which
numbering a share is on, rather than the disc in front of it: the old numbering stopped at
63 and can never produce a higher one, so anything above 63 came from the new site however
it was written.

**The `🚀` line is deliberately ignored.** It only ever appears when the player beat the
global average, so it is missing from 200 of the export's 417 shares, and it is missing
precisely for the slower results, which is the least useful way for data to be absent. The
figure it reports also drifts through the day as more people play: three players sharing
the same puzzle implied global averages of 196, 204 and 208 seconds. A share carrying the
line scores exactly what the same share without it would.

### Why the score is relative

Wordle and Connections both share a figure that means the same thing on every day of the
year: four guesses is four guesses, and a player's scores can simply be averaged. A
Zanagrams solve time means nothing on its own, because it is mostly a measure of how hard
that day's puzzle was. The export's times run from 32 seconds to nearly 30 minutes, and
most of that spread is the puzzle rather than the player. Averaging raw times would rank
whoever happened to play on the easy days, and would quietly reward anyone who declined to
post a bad one.

So a result is scored against the rest of that puzzle's field. **Scored** in four steps:

1. **Charge the hints**, at 20 seconds each, giving an adjusted time.
2. **Drop the uncontested puzzles.** A puzzle only one player posted has nobody to compare
   against.
3. **Compare against the rest of the field.** A player's score for a puzzle is the ratio of
   their adjusted time to the geometric mean of the *other* players' adjusted times for the
   same puzzle, held as a logarithm.
4. **Average, then convert back.** A player's figure for a period is the exponentiated mean
   of those logarithms.

```
score  = log(own adjusted time) − mean(log(each other player's adjusted time))
figure = exp(mean(score))
```

The result reads as a pace index. **A lower figure is better**: `1.000` is family par, and
`0.647` means the player typically solves in about 65% of the time the rest of the family
needs.

Hints cost 20 seconds because a hint is a partial reveal rather than a solved word, and the
family already treats it as a last resort: of the 358 shares reporting their hints, 324 took
none, 20 took one, 5 took two and 9 took three. The penalty is big enough that buying a hint
cannot buy a better placing, and small enough that one hint does not wreck a month. At 20
seconds no result in the export changes position, and the shares since the move report no
hints to charge for at all.

Three details are worth stating, because each is easy to get wrong:

**Why the geometric mean, and why logarithms.** With plain ratios the scale is lopsided:
solving in half the time would score 0.5, half a point below par, while taking twice as long
would score 2.0, a whole point above it, so bad days would count double. That matters here
because the times span more than an order of magnitude. In log space the two are symmetric,
and a hard puzzle that inflates everybody's time cancels out exactly.

**Why the field excludes the player.** Measuring against a baseline that includes your own
time lets your own result set part of the bar you are judged against, which flattens both
very good and very bad days, and flattens them more the fewer players posted. Leaving
yourself out makes the figure mean the same thing whether two players posted or four.

**Why uncontested puzzles vanish.** A puzzle only one player posted counts towards nobody's
figure and towards nobody's `Played` count, so `Played` always matches the sample the figure
was built from. That costs 7 of the export's 71 Original puzzles and 1 of its 60 Master
ones, nearly all of them in the first week before the rest of the family took the game up.

**Dated** by adding the puzzle number to 23 June 2026, which puts Zanagrams #1 on 24 June
2026, and a number from the new site there too once it has been restated nine lower, which
puts its #73 on 26 August 2026. Both puzzles of a day share a number and so share a date.

Because a pace index is continuous rather than a mean of small integers, the tie handling
described above will effectively never be exercised by these two tables.

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

Add it to `GameRegistry.Default`, where the order does not matter, since the report runs
the games in name order. Deriving from `PuzzleGame` brings the puzzle-number-to-date
arithmetic with it; a game dated some other way can implement `IGame` directly.

The grouping, ranking, tie handling and rendering are all game agnostic, and read a game
only through `Name` and `RankingDirection`.

### Games whose score is relative

`IGame` carries two more members, both of which default to doing nothing interesting, for a
game whose result cannot be scored while reading a single message:

| Member | Default | Called |
| --- | --- | --- |
| `Normalise(scores)` | returns them untouched | once, on the game's whole set of scores |
| `Summarise(scores)` | their mean | once per player per period |

`Normalise` is where Zanagrams turns solve times into pace indices, and where it drops the
puzzles it cannot rank. It runs after the repeats have been discarded and before the scores
are grouped into periods, and both halves of that matter: a player who posted the same
result twice would otherwise appear twice in a puzzle's field and shift the baseline for
everybody else, and a game normalised period by period would give a player one figure in
the yearly table and another in the monthly one.

`Summarise` is where Zanagrams exponentiates the mean of its logarithms, since the geometric
mean the score is defined against is not an arithmetic mean of anything.

`PuzzleGame` restates both defaults as `virtual` methods, so that a game deriving from it
can override them. This is not decoration: a default interface member is not inherited as a
virtual one, so without the restatement a puzzle game declaring its own `Normalise` would
compile and then quietly never be called.

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
