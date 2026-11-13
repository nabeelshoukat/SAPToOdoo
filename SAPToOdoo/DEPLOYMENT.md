# Deploying the SAP-Odoo Integration Gateway

## 1. Before you deploy — verify the server (per SAP.docx section 21)

The gateway is useless on a box that can't reach SAP. Check these on the
**client's Windows server** first:

- SAP Business One DI API is installed and registered (the same requirement
  the PHP `new COM("SAPbobsCOM.Company")` script needs).
- DI API version is compatible with the client's SAP Business One version.
- Bitness matches: 64-bit DI API needs the `win-x64` build below; 32-bit DI
  API needs `win-x86`.
- Network access from that server to the SAP server and the SAP License
  Server (`SAP-HY` / `SAP-HY:30000` per current config).
- Windows firewall allows inbound traffic on whatever port you bind Kestrel
  to (see step 4).

If DI API itself can't connect once deployed, that's an environment problem
(see `/health/sap` response) — not something to patch around in code.

## 2. Build the publish package

From this project folder, on a machine with the .NET SDK (this one is fine):

```powershell
.\publish.ps1              # win-x64 (default — modern SAP B1 servers)
.\publish.ps1 -Runtime win-x86   # only if the client's DI API is 32-bit
```

This produces `SAPToOdoo-publish-win-x64.zip` — a **self-contained** build,
so the client server does **not** need the .NET runtime installed. Copy that
zip to the server and extract it, e.g. to `C:\Apps\SAPToOdoo\`.

## 3. Configure secrets on the server

`appsettings.json` in the package only has non-secret SAP topology (server
name, company DB, license server). Credentials and the API key must be set
as **environment variables** on the server — never edit them into
`appsettings.json`:

```powershell
[Environment]::SetEnvironmentVariable("Sap__UserName", "awais", "Machine")
[Environment]::SetEnvironmentVariable("Sap__Password", "Hy@test12", "Machine")
[Environment]::SetEnvironmentVariable("Sap__DbUserName", "diconnect", "Machine")
[Environment]::SetEnvironmentVariable("Sap__DbPassword", "Hy@test12", "Machine")
[Environment]::SetEnvironmentVariable("Authentication__ApiKey", "<generate-a-new-secret-for-production>", "Machine")
```

Use `__` (double underscore) — that's how .NET configuration maps env vars
to nested keys like `Sap:UserName`. Generate a fresh API key for production;
don't reuse the `dev-test-key` used during development.

**Restart the machine or sign out/in** after setting `Machine`-scope
variables so the service picks them up.

## 4. Run it once manually to verify

```powershell
cd C:\Apps\SAPToOdoo
$env:ASPNETCORE_URLS = "http://localhost:5000"
.\SAPToOdoo.exe
```

Then from the same server:

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/health/sap
```

`/health/sap` should now return `"success": true` — this is the real proof
the DI API connection works. If it fails, the error message tells you
whether it's a missing DI API registration, bad credentials, or a
SAP-reported connection error (check `errorCode`/SAP error code).

Stop it with Ctrl+C once confirmed.

## 5. Install as a Windows Service (so it survives reboot/logoff)

The app already has Windows Service hosting built in
(`Microsoft.Extensions.Hosting.WindowsServices`), so no wrapper tool is
needed — just register the exe:

```powershell
sc.exe create "SapOdooGateway" binPath= "C:\Apps\SAPToOdoo\SAPToOdoo.exe" start= auto
sc.exe description "SapOdooGateway" "SAP-Odoo Integration Gateway (.NET 9 / SAP DI API)"
sc.exe start "SapOdooGateway"
```

Note the required space after each `=` in `sc.exe` syntax.

To set the listening port and environment for the service, either set the
env vars at `Machine` scope (step 3) plus:

```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://+:5000", "Machine")
```

or edit `appsettings.json`'s `Kestrel` section instead. Then:

```powershell
sc.exe stop "SapOdooGateway"
sc.exe start "SapOdooGateway"
```

To uninstall later: `sc.exe delete "SapOdooGateway"`.

## 6. Point Odoo at it

Odoo should call `http://<server>:5000/api/v1/sap/...` with header
`X-API-Key: <the production key from step 3>`. No SAP credentials, DI API,
or SAP network access are ever needed on the Odoo side — that's the whole
point of the gateway.

## 7. TLS note

`UseHttpsRedirection()` is enabled in the app but Kestrel isn't bound to an
HTTPS port by default in this package (no certificate is included). Two
options if Odoo talks to this server over an untrusted network:

- Put IIS or another reverse proxy in front to terminate TLS, forwarding to
  the gateway's HTTP port, or
- Bind Kestrel directly to HTTPS with a certificate:
  `ASPNETCORE_URLS=https://+:5001` plus `ASPNETCORE_Kestrel__Certificates__Default__Path`
  and `...__Password` env vars pointing at a `.pfx`.

If the gateway and Odoo are both on the same trusted internal network, plain
HTTP behind the firewall is a reasonable MVP choice — just don't expose that
port to the internet.
