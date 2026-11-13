# Data file format

The workspace now stores its shared data in compact binary files:

- `data/team.dat` — gzip-compressed team container.
- `data/tasks.dat` and `data/registries.dat` — gzip-compressed serialized payloads, then encrypted using the existing application encryption code.
- `data/audit.log` remains a text audit log.

The application uses only .NET Framework's built-in `GZipStream`; no NuGet dependency is added. These files are not meant to be inspected or edited in Notepad. Compression is not encryption: `team.dat` is compressed but not encrypted as a whole, and the task/registry files retain the existing encryption behavior.

This format intentionally does not import the previous `team.json`, `tasks.json`, or `registries.json` files. Back up or export any data you need before deploying this version; the app will create new `.dat` files and start with empty data if they are absent.
