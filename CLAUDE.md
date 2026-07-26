# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build
dotnet test

# One test class, or one test by name
dotnet test --filter "FullyQualifiedName~WordleGameTests"
dotnet test --filter "FullyQualifiedName~Dates_the_result_from_the_puzzle_number"

# Run against a chat export
dotnet run --project src/GameChatParser -- ~/dev/_chat.txt
```

.NET 10, `.slnx` solution format, xUnit v3. `Directory.Build.props` sets `TreatWarningsAsErrors`, so a warning fails the build. The test project needs `OutputType=Exe`, which xUnit v3 requires.

## Architecture

The README documents the ranking algorithms in full. What matters structurally:

**The pipeline is game agnostic.** `ReportBuilder` reads the chat once, offers every message to every game, then groups, ranks and renders. Grouping, ranking, tie handling and rendering read a game only through `Name` and `RankingDirection`. Only `IGame.TryParseScore` knows anything about a particular game.

**Adding a game** means implementing `IGame` (or deriving from `PuzzleGame` for the date arithmetic) and adding a line to `GameRegistry.Default`. Nothing else changes. `ReportBuilderTests.Reports_a_game_that_was_added_without_touching_anything_else` guards this.

**`Core` never touches the file system.** `ReportBuilder.Build` takes `IEnumerable<string>` of lines; only `Program.cs` calls `File.ReadLines`. Keep it that way, since it is what lets tests drive the whole pipeline from a string literal.

**Puzzle numbers date results, not message timestamps.** A result posted days late still counts for the day it belongs to. `ChatMessage.Timestamp` is parsed but nothing in the ranking uses it.

## Things that will bite you

**Puzzle epochs are calibrated against real shared results, not against the games' launch dates.** Wordle's `PuzzleZeroDate` is a day later than Wordle launched, because the New York Times renumbered. Changing either epoch by one day silently moves every result into a neighbouring month and quietly corrupts every leaderboard. Verify against a known pairing (Wordle 1,281 is 21 December 2024; Connections #619 is 19 February 2025).

**Connections squares are outside the basic multilingual plane.** Never put them in a regex character class: `[🟨🟩🟦🟪]{4}` compiles to a class of UTF-16 code units and matches two squares rather than four. This was a real bug inherited from the predecessor. Classify rows by iterating runes, as `ConnectionsGame.ClassifyRow` does. Row width is also what tells a four-wide Connections grid from a five-wide Wordle one, which share two colours.

**Test player names are load-bearing in two ways.** `MarkdownReportRendererTests` asserts byte-exact column padding, so changing a name's *length* breaks it. `LeaderboardBuilderTests` tie-breaks alphabetically, so changing a name's *ordinal order* breaks it. Both are deliberate; match the existing lengths and ordering when adding or renaming test players.

**Periods are relative to the data, not to today.** The two most recent years and months *that hold results* are reported, so a chat that went quiet still reports on when it was last active. Tests rely on this; do not switch to `DateTime.Now`.

**Connections needs at least one grid row.** A message naming a puzzle with no grid must be ignored, not scored zero.

**The Wordle summary line is anchored to the start of a line.** Prose mentioning a puzzle number does not count as a result.

## Privacy

The application is run against a private family WhatsApp chat. **No real participant may be named anywhere in this repository**, including test data, code comments, README samples, commit messages and GitHub issues or pull requests. The player names throughout are invented. The chat export itself lives outside the repository and must never be committed; `tmp/` is git ignored and is the place for any output containing real names.

## Verifying a change to scoring or output

This application replaced [WordleParser](https://github.com/ianfnelson/WordleParser) and [ConnectionsParser](https://github.com/ianfnelson/ConnectionsParser), and reproduces their output byte for byte. When changing parsing, grouping or rendering, run all three against the same export and diff the leaderboards. That check is what catches a regression the unit tests cannot see, and it is stronger than any assertion in the suite.

Note that it stops being available once the Connections scoring is deliberately changed, which is proposed in issue #2.
