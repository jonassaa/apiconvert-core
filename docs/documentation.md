# apiconvert documentation

## Documentation outline (with section goals)
- Overview and core concepts: define organizations, converters, mappings, and forwarding using UI terminology.
- User flows: walk through the happy-path setup from org creation to testing inbound traffic.
- Inbound pipeline: describe each processing step from auth through logging.
- Formats and serialization rules: document JSON, XML, and Query parameters in both directions.
- Mapping rules: explain field rules, array rules, transforms, conditions, defaults, and path syntax.
- Request handling: list supported HTTP methods, size limits, errors, forwarding behavior, and response modes.
- End-to-end examples: provide working examples for each format combination.
- Edge cases and troubleshooting: highlight common errors and how to resolve them.

## Deployment
- Azure App Service: `docs/app-service.md`

## Overview and core concepts
apiconvert lets you receive partner payloads, map them to your internal schema, and forward transformed
requests to your API. The UI is organized around these core concepts:

- Organization: the top-level container for converters and logs.
- Converter: defines the inbound endpoint, forwarding destination, and auth settings.
- Mapping rules (Conversion): defines how incoming payloads are reshaped.
- Inbound URL: the public endpoint used by your partners.
- Forwarding: the outbound request apiconvert makes after mapping.
- Logs: optional request/response capture for troubleshooting.

## User flows (UI-aligned)

### 1) Create an organization
- Sign in and create your organization.
- Invite teammates if needed.

### 2) Create a converter
- Navigate to Converters.
- Click "Create converter".
- Provide:
  - Name
  - Inbound path
  - Forward URL
  - Forward method
  - Inbound response
  - Forward headers JSON

### 3) Configure inbound authentication
- Go to the "Authentication" tab.
- Choose inbound auth mode:
  - None
  - Bearer token
  - Basic auth
  - Header auth

### 4) Configure mapping rules
- Go to the "Conversion" tab.
- Choose Input format and Output format.
- Add Field rules and Array rules.
- Use Example input and Expected output to preview changes.
- Click "Save rules".

### 5) Enable logging and retention
- In Converter settings, toggle "Enable request logs".
- Set retention days and log size limits if needed.

### 6) Test inbound requests
- Use the Inbound URL displayed in the converter.
- Send test requests with curl or Postman.
- Check logs for status and latency.

## Inbound pipeline (end-to-end)
Inbound requests follow this pipeline:

1) Auth: validate inbound auth mode (bearer/basic/header/none).
2) Parse: parse payload according to Input format.
3) Map: apply Field rules and Array rules.
4) Format: serialize output according to Output format.
5) Forward: send the transformed payload to the Forward URL.
6) Respond: return passthrough response or minimal ACK.
7) Log: optionally capture request/response data with redaction.

## Formats and serialization rules

### JSON
- Input: parsed with JSON.parse.
- Output: JSON.stringify (compact for forwarding, pretty in preview).
- If no mappings are defined, output is the input JSON object (or {}).

### XML
- Input: parsed with fast-xml-parser.
- Output: serialized with fast-xml-parser (compact, no pretty printing for forwarding).
- Attributes are preserved with the "@_" prefix.
- Tag values and attribute values are parsed into booleans/numbers when possible.

Example XML attribute mapping:
<root id="123"><name>Acme</name></root>

Parsed representation:
{
  "root": {
    "@_id": 123,
    "name": "Acme"
  }
}

### Query parameters
- Input: query strings are parsed into nested objects.
- Output: only object outputs are allowed. Non-object outputs cause an error.
- Keys are sorted at each object level before serialization.
- Nested objects are flattened into dot notation (customer.email).
- Bracket syntax is supported for indexing (items[0].sku).
- Repeated keys are collected into arrays.
- Arrays are serialized as repeated keys (items=one&items=two).
- Arrays of objects are JSON-stringified per item.
- Encoding uses URLSearchParams (percent-encoding and standard query escaping).

Query parsing rules:
- Dot notation creates nested objects (a.b=1 -> { a: { b: 1 } }).
- Brackets create nested objects or arrays (a[b]=1, items[0]=x).
- Numeric bracket segments become arrays.
- Repeated keys become arrays (tag=one&tag=two -> { tag: ["one", "two"] }).

Query output rules:
- Root output must be an object.
- Keys are sorted alphabetically at each level.
- Objects flatten into dot notation in the output.

## Mapping rules (Conversion)
Mapping rules are stored as ConversionRules v2 and edited in the "Conversion" tab.

### Field rules
Field rules write a value to an output path.

Fields:
- Output path: destination path in the output object.
- Source type: Path, Constant, Transform, or Condition.
- Default value: used when the source resolves to null/undefined/empty string.

Source types:
- Path: uses a dot/bracket path from the input (e.g., customer.id, items[0].sku).
- Constant: literal value, parsed into boolean/number/null when possible.
- Transform: applies a transform to a path.
- Condition: writes a value based on a comparison.

Transforms:
- toLowerCase, toUpperCase, number, boolean, concat.
- For concat, the source path is comma-separated tokens.
  - Tokens can be input paths or constants prefixed by const:.
  - Example: "firstName,lastName,const: (VIP)".

Conditions:
- Operators: exists, equals, notEquals, includes, gt, lt.
- Comparison values are parsed to boolean/number/null when possible.
- includes supports string contains and array membership.

Default values:
- Applied when the source resolves to undefined, null, or empty string.

Path syntax:
- Dot notation for objects: customer.email
- Brackets for arrays: items[0].sku
- Numeric segments are array indices: items.0.sku
- Root access:
  - $ uses the full input object.
  - $.path or $[0] reads from root regardless of array mappings.

### Array rules
Array rules map a list of items from input to output.

Fields:
- Input path: resolves to an array in the input payload.
- Output path: destination path for the array in output.
- Coerce single: when enabled, a non-array value is wrapped into a one-item array.
- Item mappings: Field rules applied to each array item.

Example array rule:
- Input path: order.lines
- Output path: items
- Item mappings:
  - outputPath: sku, source.path: sku
  - outputPath: qty, source.transform: number on qty

## Request handling

### Supported HTTP methods
Inbound endpoints accept: GET, POST, PUT, PATCH.

### Body size limits
- Max inbound body size: 1,000,000 bytes.
- Requests above the limit return 413 (Payload too large).

### Rate limits
- Default: 60 requests per minute per converter.
- 429 responses include a Retry-After header (60 seconds).

### Auth modes
Inbound auth modes are configured per converter:
- None
- Bearer token (Authorization: Bearer <token> or x-apiconvert-token)
- Basic auth (Authorization: Basic <base64>)
- Header auth (custom header name and value)

### Forwarding behavior
- Forward method defaults to inbound method if not set.
- Forward headers JSON adds static headers to the outbound request.
- Content-Type is set based on output format:
  - application/json
  - application/xml
  - application/x-www-form-urlencoded (query output)
- Output format "query" appends parameters to the Forward URL and sends no body.
- Forwarding timeout: 10 seconds.

### Response modes
- Passthrough: returns the forward response body/status to the caller.
- Return minimal ACK: returns { ok: true, request_id } with status 202.

### Logging and redaction
When "Enable request logs" is on:
- Request/response headers and bodies are captured (with size limits).
- Defaults: 32 KB body, 8 KB headers, 30-day retention.
- Sensitive headers are redacted by default: authorization, cookie, set-cookie,
  x-apiconvert-token.

## End-to-end examples
Each example includes:
- Inbound request
- Mapping rules (ConversionRules v2)
- Forwarded payload

Replace {ORG_ID}, {INBOUND_PATH}, and {TOKEN} with your values.

### JSON -> JSON
Inbound request:

curl -X POST \
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"customer":{"id":"c-123","email":"jane@example.com"}}'

Mapping rules:
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
{"userId":"c-123","contact":{"email":"jane@example.com"}}

### XML -> JSON
Inbound request:

curl -X POST \
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/xml" \
  -d '<root><customer id="c-123"><email>jane@example.com</email></customer></root>'

Mapping rules:
{
  "version": 2,
  "inputFormat": "xml",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "root.customer.@_id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "root.customer.email" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
{"userId":123,"contact":{"email":"jane@example.com"}}

### JSON -> XML
Inbound request:

curl -X POST \
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"order":{"id":"o-9","total":42}}'

Mapping rules:
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "xml",
  "fieldMappings": [
    { "outputPath": "root.order.@_id", "source": { "type": "path", "path": "order.id" }, "defaultValue": "" },
    { "outputPath": "root.order.total", "source": { "type": "transform", "path": "order.total", "transform": "number" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
<root><order id="o-9"><total>42</total></order></root>

### Query -> JSON
Inbound request:

curl -X GET \
  "https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH}?customer.id=c-123&customer.email=jane@example.com&tags=vip&tags=trial"

Mapping rules:
{
  "version": 2,
  "inputFormat": "query",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" },
    { "outputPath": "labels", "source": { "type": "path", "path": "tags" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
{"userId":"c-123","contact":{"email":"jane@example.com"},"labels":["vip","trial"]}

### JSON -> Query
Inbound request:

curl -X POST \
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \
  -H "Content-Type: application/json" \
  -d '{"customer":{"id":"c-123","email":"jane@example.com"},"tags":["vip","trial"]}'

Mapping rules:
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "query",
  "fieldMappings": [
    { "outputPath": "customer.id", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "customer.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" },
    { "outputPath": "tags", "source": { "type": "path", "path": "tags" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded URL (body omitted):
https://forward.example.com/api/receive?customer.email=jane%40example.com&customer.id=c-123&tags=vip&tags=trial

## Errors and edge cases
- Invalid JSON/XML/query payloads return 400 with a format-specific error message.
- Conversion rules errors return 400 with details for the invalid rule.
- Output format "query" requires an object root; other types error.
- Forwarding errors return 502 and include a request_id for traceability.
- Converters must be enabled; disabled converters return 403.
- Inbound path and converter lookups return 404 when missing.

## Testing inbound requests
- Use the Inbound URL and the configured auth mode.
- For query input, send parameters on the URL (GET recommended).
- Check logs for request_id, status, and latency.
- Use "Preview output" in the Conversion tab before saving rules.
