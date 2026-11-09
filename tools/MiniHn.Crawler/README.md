# MiniHn.Crawler

Fills the posts store with **real Hacker News stories**, so `GET /posts` can be
demonstrated on real data instead of hand-typed rows.

This tool lives on the `tooling/hn-crawler` branch and is deliberately **not part
of `main` or of the `v0.1.0` submission**: the brief asks for persistent storage
and `GET /posts` and for nothing more, and a seed tool inside the submitted tree
would be exactly that "more". The branch exists so the data is one command away.

## Use

```bash
git switch tooling/hn-crawler

dotnet run --project tools/MiniHn.Crawler -- --count 30          # add 30 stories
dotnet run --project tools/MiniHn.Crawler -- --count 5 --dry-run  # print, write nothing
dotnet run --project tools/MiniHn.Crawler -- --clear --count 50   # start over
dotnet run --project tools/MiniHn.Crawler -- --db other/minihn.db
```

Then run the API and read it back:

```bash
dotnet run --project src/MiniHn.Api
curl -s http://localhost:5080/posts
```

## Source

The official Hacker News API (<https://github.com/HackerNews/API>):
`v0/topstories.json` for the current top 500 ids, then `v0/item/<id>.json` for
each one. No key, no HTML scraping, no login. Reads are paced (`--delay`,
120 ms by default) and the tool identifies itself in the User-Agent.

## How an item becomes a post

The Phase I schema is `id` + `content`, so a story collapses into one line:

```
<title> — <url> — <body of an Ask HN post, markup stripped>
```

* comments, jobs, polls, dead and deleted items are skipped - only live stories
  with a title are stored;
* `content` is truncated to the schema's 10,000 character bound, never through a
  surrogate pair, so the crawl cannot build a row the `CHECK` constraint refuses;
* already stored content is read once up front and re-crawling adds nothing: a
  second run of the same command reports the stories as skipped;
* a save every ten rows, so an interrupted crawl still leaves a consistent store;
* an unreachable API exits non-zero and writes nothing - "we could not look" is
  not "there is nothing there".

Everything above that is pure (`PostContent`, `CrawlerOptions`) is covered by
`tests/MiniHn.Crawler.Tests`; the HTTP call itself is not mocked, because a test
asserting that a stub was called proves nothing about Hacker News.
