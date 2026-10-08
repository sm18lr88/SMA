# SuperMemoAssistant.Plugins.LocalApi

The Local API plugin runs a small HTTP server on your own computer. Browser extensions, user scripts, and command-line tools can then add topics and items to SuperMemo, read elements, and show an element. The server accepts connections only from this computer, and every request needs a secret access token.

The server is off by default. It does not read or change learning data or the schedule. The only element property that it sets for you is the priority of the elements that it creates.

## Set up the server

1. In SMA, open the settings of the **Local API** plugin.
2. Select **Run the local API server**. If necessary, change the port (the default is 47321).
3. Click **Save**. The plugin starts the server and creates an access token.
4. Open the settings again. Click **Copy** next to **Access token**, and paste the token into your extension or script. You can also run "Copy the Local API token" from the command palette (Ctrl+Alt+Shift+P).

To make a new token, click **New token** and then **Save**. The old token stops working immediately. If the server cannot start, for example because another program uses the port, SMA shows a notification and writes the reason to the log.

The server listens on `http://127.0.0.1:47321/` and `http://localhost:47321/`. Windows can refuse the 127.0.0.1 address to a user without administrator rights, because that address needs a URL reservation. Then the plugin listens on `http://localhost:47321/` only, and writes this to the log. In that case, use `localhost` in your URLs.

## Settings

| Setting | Default | Meaning |
| --- | --- | --- |
| Run the local API server | Off | Starts the server. When you clear it, the server stops. |
| Port | 47321 | The TCP port on 127.0.0.1 and localhost. |
| Default parent of new elements | The current element | The parent of a new element when the request does not give `parentId`. If no element is shown, the parent is the collection root. |
| Default priority (%) | 30 | The priority of a new element when the request does not give `priority`. |
| Extra allowed origins | Empty | Web origins, one on each line, that can call the API from a web page. An example is `http://localhost:3000`. |
| Access token | Created when you first turn on the server | The token that every request must send. |

When you save the settings, the server restarts with the new values.

## Endpoints

All paths start with `/api/v1`. Request and response bodies are JSON (`Content-Type: application/json`). Every request needs the header `Authorization: Bearer <token>`.

| Method and path | Body | Result |
| --- | --- | --- |
| `GET /api/v1/status` | None | `200` with `smaVersion`, `apiVersion` (1), `collection` (a name or `null`), `superMemoRunning`, and `currentElement` (`{id, title, type}` or `null`). |
| `GET /api/v1/elements/current` | None | `200` with `{id, title, type, parentId}`, or `404` when no element is shown. |
| `GET /api/v1/elements/{id}` | None | `200` with `{id, title, type, parentId, childCount}`, or `404`. |
| `POST /api/v1/elements` | See below | `201` with `{id}`. Creates one topic or one item. |
| `POST /api/v1/navigate` | `{"id": 123}` | `204`. Shows the element in the element window. |

The body of `POST /api/v1/elements` has these fields:

| Field | Required | Meaning |
| --- | --- | --- |
| `type` | Yes | `"topic"` or `"item"`. |
| `html` | For a topic | The HTML content of the topic. |
| `question`, `answer` | For an item | The HTML of the question and of the answer. |
| `title` | No | The element title. If you do not give it, SMA makes a title from the content. |
| `parentId` | No | The parent element. If you do not give it, the setting "Default parent of new elements" applies. |
| `priority` | No | A number from 0 to 100. If you do not give it, the setting "Default priority" applies. |
| `references` | No | An object with any of `title`, `author`, `link`, `source`, `date`, `comment`, and `email`. The `link` must be an http or https URL. |

The plugin removes active content from `html`, `question`, and `answer`. It removes `script`, `iframe`, `frame`, `object`, `embed`, `applet`, `form`, `base`, `meta`, and `link` elements. It also removes `on...` event attributes, `javascript:` and `vbscript:` URLs, and CSS expressions. It keeps the rest of the markup, for example headings, emphasis, lists, tables, images, and links.

An unknown field in a body is an error, so a misspelled field name does not go unnoticed.

### Errors

An error has the body `{"error": "<message>"}`. The message tells you what to correct.

| Status | Reason |
| --- | --- |
| 400 | The body is not valid. For example, a topic has no `html`, or `priority` is not from 0 to 100. |
| 401 | The token is missing or wrong. |
| 403 | The request comes from a web page, from another computer, or with a wrong Host header. |
| 404 | The endpoint, the element, or the parent does not exist, or no element is shown. |
| 405 | The endpoint does not accept this method. The `Allow` header gives the correct method. |
| 409 | SuperMemo has no open collection. |
| 413 | The body is larger than 2 MB. |
| 415 | The body is not JSON. |
| 500 | SuperMemo or SMA reported an error. The SMA log has the details. |
| 503 | SuperMemo is not running, or it did not answer in 15 seconds (for example, because a dialog is open). |

The server makes only one change in SuperMemo at a time. A second request that changes SuperMemo waits for the first one.

## Examples

### curl (Windows)

```powershell
$token = "<paste the token here>"

curl.exe -H "Authorization: Bearer $token" http://127.0.0.1:47321/api/v1/status

'{"type":"topic","title":"Notes","html":"<p>Read this later.</p>","references":{"link":"https://example.org"}}' |
  curl.exe -X POST -H "Authorization: Bearer $token" -H "Content-Type: application/json" --data-binary "@-" http://127.0.0.1:47321/api/v1/elements
```

### PowerShell

```powershell
$headers = @{ Authorization = "Bearer $token" }
$item    = @{ type = "item"; question = "What is the capital of France?"; answer = "Paris"; priority = 20 } | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:47321/api/v1/elements -Headers $headers -ContentType "application/json" -Body $item
```

### Browser extension

Add the server to the host permissions of the extension, for example `"host_permissions": ["http://127.0.0.1:47321/*"]`. Then call `fetch` from the extension, for example from its service worker:

```js
const token = "<the token that the user pasted into the options of your extension>";

const response = await fetch("http://127.0.0.1:47321/api/v1/elements", {
  method: "POST",
  headers: { "Authorization": `Bearer ${token}`, "Content-Type": "application/json" },
  body: JSON.stringify({
    type: "topic",
    title: document.title,
    html: selectedHtml,
    references: { title: document.title, link: location.href },
  }),
});

if (!response.ok) {
  const { error } = await response.json();
  throw new Error(error);
}
const { id } = await response.json();
```

A user script can call the API with `GM.xmlHttpRequest` and `// @connect 127.0.0.1`. A plain `fetch` from a web page is refused, because the page origin is not allowed.

## Security model

- **Loopback only.** The server registers only `127.0.0.1` and `localhost` prefixes. It never uses `+`, `*`, or `0.0.0.0`. It also refuses every connection that does not come from a loopback address. There is no option for access from other computers.
- **Token.** Every request, except a CORS preflight, needs the token. The token is 32 random bytes from a cryptographic random number generator, encoded as base64url (43 characters). The server compares tokens in constant time. The token is never written to the log.
- **Host header.** The server refuses a request with 403 when the Host header is not `127.0.0.1:<port>` or `localhost:<port>`. This stops DNS rebinding attacks.
- **Origin header.** Browsers send an Origin header with requests from web pages. The server refuses every Origin with 403, except browser extensions (`chrome-extension://`, `moz-extension://`, `safari-web-extension://`) and the extra allowed origins. A web page thus cannot use the API, even when it does not read the response. Requests without an Origin header, for example from curl or scripts, are allowed with a valid token.
- **CORS.** The server answers a preflight only for an allowed origin, and it sends back that exact origin in `Access-Control-Allow-Origin`. It never sends `*`.
- **Limits.** The server accepts only JSON bodies of at most 2 MB. It makes one change in SuperMemo at a time, and it stops waiting for SuperMemo after 15 seconds.
- **Storage.** The token is stored in plain text in the plugin settings file in your SMA profile. Any program that runs as your Windows user can read that file. If you think that someone knows the token, make a new token.

## Test the plugin

The tests do not need SuperMemo. They start the server on a free loopback port with a fake SuperMemo gateway, and they send real HTTP requests with `HttpClient`. They are in `src/Tests/SuperMemoAssistant.Tests/LocalApi`:

```powershell
dotnet test --project src/Tests/SuperMemoAssistant.Tests --filter-namespace "SuperMemoAssistant.Tests.LocalApi"
```

To try the plugin by hand, build SMA, turn on the server in the plugin settings, and use the curl examples above. To see each request in the log (method, path, status, and duration), set the SMA log level to Debug.

The code has two parts. `Server/` holds the request pipeline (routing, token, Host and Origin checks, JSON validation, sanitizing, and errors) and the `HttpListener` loop. These classes have no SuperMemo dependency, and they use the small `ISuperMemoGateway` interface. `SuperMemoGateway` implements that interface with `Svc.SM` inside the plugin.
