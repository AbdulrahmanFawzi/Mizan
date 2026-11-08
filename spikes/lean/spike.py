"""Lean sandbox spike: answers the open questions V1-V5 from
docs/domain-discovery/mvp1/02-business-rules.md before we design the entities.

Throwaway code, standard library only. Usage (from this folder):
  python3 spike.py customer   # create a sandbox customer
  python3 spike.py link       # serve the Link SDK page on http://localhost:8765
  python3 spike.py fetch      # download accounts, balances, transactions into out/
  python3 spike.py analyze    # print what the data says about V1-V5
  python3 spike.py refresh    # ask Lean to refresh, then run fetch again to compare (V1)
"""
import collections
import datetime as dt
import http.server as http_server
import json
import os
import pathlib
import sys
import urllib.error
import urllib.parse
import urllib.request

HERE = pathlib.Path(__file__).parent
OUT = HERE / "out"
STATE = OUT / "state.json"
API = "https://sandbox.sa.leantech.me"
# The docs mention both hosts for the sandbox token; we try them in order.
AUTH_HOSTS = ["https://auth.sandbox.sa.leantech.me", "https://auth.sa.leantech.me"]


def env():
    values = {}
    for line in (HERE / ".env").read_text().splitlines():
        if "=" in line and not line.strip().startswith("#"):
            key, value = line.split("=", 1)
            values[key.strip()] = value.strip()
    if not values.get("LEAN_APP_TOKEN") or not values.get("LEAN_CLIENT_SECRET"):
        sys.exit("Fill LEAN_APP_TOKEN and LEAN_CLIENT_SECRET in .env first.")
    return values


def state():
    return json.loads(STATE.read_text()) if STATE.exists() else {}


def save_state(**changes):
    OUT.mkdir(exist_ok=True)
    data = state() | changes
    STATE.write_text(json.dumps(data, indent=2))


def http(method, url, token=None, body=None, form=None):
    headers = {"accept": "application/json"}
    data = None
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode()
    if form is not None:
        headers["Content-Type"] = "application/x-www-form-urlencoded"
        data = urllib.parse.urlencode(form).encode()
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            raw = response.read()
            return response.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        raw = error.read().decode(errors="replace")
        try:
            return error.code, json.loads(raw)
        except ValueError:
            return error.code, raw


def token(scope="api"):
    keys = env()
    form = {
        "client_id": keys["LEAN_APP_TOKEN"],
        "client_secret": keys["LEAN_CLIENT_SECRET"],
        "grant_type": "client_credentials",
        "scope": scope,
    }
    errors = []
    for host in AUTH_HOSTS:
        status, body = http("POST", f"{host}/oauth2/token", form=form)
        if status == 200:
            return body["access_token"]
        errors.append(f"{host}: {status} {str(body)[:200]}")
    sys.exit("Could not get a token:\n" + "\n".join(errors))


def cmd_customer():
    app_user_id = f"spike-{dt.datetime.now():%Y%m%d%H%M%S}"
    status, body = http("POST", f"{API}/customers/v1/", token(), {"app_user_id": app_user_id})
    print(status, body)
    if status in (200, 201):
        save_state(customer_id=body["customer_id"], app_user_id=app_user_id)


def cmd_link():
    customer_id = state().get("customer_id") or sys.exit("Run `customer` first.")
    customer_token = token(f"customer.{customer_id}")
    page = (HERE / "link.template.html").read_text()
    page = (page.replace("__APP_TOKEN__", env()["LEAN_APP_TOKEN"])
                .replace("__CUSTOMER_ID__", customer_id)
                .replace("__ACCESS_TOKEN__", customer_token)
                .replace("__ACCESS_TO__", (dt.date.today() + dt.timedelta(days=365)).isoformat()))
    (HERE / "link.html").write_text(page)
    if os.environ.get("PAGE_ONLY"):
        return
    os.chdir(HERE)
    print("Open http://localhost:8765/link.html , link a test user, then press Ctrl+C here.")
    http_server.HTTPServer(("127.0.0.1", 8765), http_server.SimpleHTTPRequestHandler).serve_forever()


def get_all(path, api_token, entity_id, key, extra=None):
    items, page = [], 0
    while True:
        query = {"entity_id": entity_id, "page": page, "size": 100} | (extra or {})
        status, body = http("GET", f"{API}{path}?{urllib.parse.urlencode(query)}", api_token)
        if status != 200:
            return items, {"status": status, "body": body}
        data = body.get("data") or {}
        items += data.get(key) or []
        info = data.get("page") or {}
        if page + 1 >= info.get("total_pages", 1):
            return items, body
        page += 1


def cmd_fetch():
    customer_id = state().get("customer_id") or sys.exit("Run `customer` first.")
    api_token = token()
    status, entities = http("GET", f"{API}/customers/v1/{customer_id}/entities", api_token)
    if status != 200 or not entities:
        sys.exit(f"No entity yet ({status}): {entities}. Did the link finish?")
    entity = entities[0] if isinstance(entities, list) else entities
    entity_id = entity["id"]
    save_state(entity_id=entity_id)

    stamp = f"{dt.datetime.now():%H%M%S}"
    run = OUT / f"run-{stamp}"
    run.mkdir(parents=True)
    (run / "entities.json").write_text(json.dumps(entities, indent=2))

    accounts, raw = get_all("/data/v2/accounts", api_token, entity_id, "accounts")
    (run / "accounts.json").write_text(json.dumps(raw, indent=2))
    for account in accounts:
        account_id = account.get("account_id") or account.get("id")
        _, balances = get_all(f"/data/v2/accounts/{account_id}/balances", api_token, entity_id, "balances")
        (run / f"balances-{account_id}.json").write_text(json.dumps(balances, indent=2))
        transactions, last = get_all(f"/data/v2/accounts/{account_id}/transactions", api_token, entity_id,
                                     "transactions", {"insights": "true"})
        (run / f"transactions-{account_id}.json").write_text(json.dumps(transactions, indent=2))
        print(f"account {account_id}: {len(transactions)} transactions (last response status: {last.get('status')})")
    print(f"Saved to {run}")


def cmd_refresh():
    entity_id = state().get("entity_id") or sys.exit("Run `fetch` first.")
    print(http("POST", f"{API}/data/v2/refreshes", token(), {"entity_id": entity_id}))


def latest_runs():
    return sorted(p for p in OUT.glob("run-*") if p.is_dir())


def cmd_analyze():
    runs = latest_runs() or sys.exit("Run `fetch` first.")
    run = runs[-1]
    transactions = []
    for file in run.glob("transactions-*.json"):
        transactions += json.loads(file.read_text())
    if not transactions:
        sys.exit("No transactions in the latest run.")

    def field(t, *path):
        for key in path:
            t = t.get(key) if isinstance(t, dict) else None
        return t

    dates = sorted(str(field(t, "booking_date_time"))[:10] for t in transactions if t.get("booking_date_time"))
    print(f"\n== V5 volume: {len(transactions)} transactions, {dates[0] if dates else '?'} .. {dates[-1] if dates else '?'}")
    print("   by month:", dict(collections.Counter(d[:7] for d in dates)))

    def count(*path):
        return collections.Counter(str(field(t, *path)) for t in transactions).most_common(25)

    print("\n== V1 status:", count("status"))
    print("   mutability:", count("transaction_mutability"))
    print("\n== V2 transaction_type:", count("transaction_type"))
    print("   sub_transaction_type:", count("sub_transaction_type"))
    print("   credit_debit_indicator:", count("credit_debit_indicator"))
    print("\n== V4 insights.category:", count("insights", "category"))
    print("   MCC present:", sum(1 for t in transactions if field(t, "merchant_details", "merchant_category_code")))
    print("   merchant_name present:", sum(1 for t in transactions if field(t, "merchant_details", "merchant_name")))
    print("   currencies:", count("amount", "currency"))

    credits = [t for t in transactions if t.get("credit_debit_indicator") == "CREDIT"]
    debit_merchants = {str(field(t, "merchant_details", "merchant_name")).lower()
                       for t in transactions if t.get("credit_debit_indicator") == "DEBIT"}
    refund_like = [t for t in credits if str(field(t, "merchant_details", "merchant_name")).lower() in debit_merchants
                   and field(t, "merchant_details", "merchant_name")]
    print(f"\n== V3 credits: {len(credits)}; credits from a merchant we also paid: {len(refund_like)}")
    print("   credit descriptions:", collections.Counter(str(t.get("transaction_information"))[:40] for t in credits).most_common(10))

    print("\n== One full transaction (shape):")
    print(json.dumps(transactions[0], indent=2)[:2500])

    if len(runs) > 1:
        before = {}
        for file in runs[-2].glob("transactions-*.json"):
            before |= {t.get("transaction_id"): t for t in json.loads(file.read_text())}
        after = {t.get("transaction_id"): t for t in transactions}
        changed = [i for i in after if i in before and before[i] != after[i]]
        print(f"\n== V1 across runs: same ids {len(set(before) & set(after))}, only before {len(set(before) - set(after))},"
              f" only after {len(set(after) - set(before))}, changed {len(changed)}")


if __name__ == "__main__":
    commands = {"customer": cmd_customer, "link": cmd_link, "fetch": cmd_fetch, "analyze": cmd_analyze,
                "refresh": cmd_refresh}
    if len(sys.argv) != 2 or sys.argv[1] not in commands:
        sys.exit(__doc__)
    commands[sys.argv[1]]()
