# Local Setup

```bash
dotnet build Apiconvert.Core.sln
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

For docs:

```bash
python3 -m venv .venv-docs
source .venv-docs/bin/activate
pip install -r requirements-docs.txt
mkdocs serve
```
