"""Call the local Unity MCP relay, scoped to this checkout."""
import json
import queue
import subprocess
import sys
import threading
import time
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')
root = Path(__file__).resolve().parents[1]
relay = Path.home() / '.unity/relay/relay_win.exe'
request = Path(sys.argv[1])
if request.suffix == '.cs':
    payload = {'name': 'Unity_RunCommand', 'arguments': {
        'Code': request.read_text(encoding='utf-8-sig'), 'Title': request.stem}}
else:
    payload = json.loads(request.read_text(encoding='utf-8-sig'))
process = subprocess.Popen([str(relay), '--mcp', '--project-path', str(root)],
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    text=True, encoding='utf-8')
messages = queue.Queue()
threading.Thread(target=lambda: [messages.put(line) for line in process.stdout], daemon=True).start()
threading.Thread(target=lambda: [None for line in process.stderr], daemon=True).start()

def send(message):
    process.stdin.write(json.dumps(message) + '\n')
    process.stdin.flush()

def receive(request_id, timeout=120):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        message = json.loads(messages.get(timeout=max(0.1, deadline - time.monotonic())))
        if message.get('id') == request_id:
            return message
    raise TimeoutError('Unity MCP response timed out')

try:
    send({'jsonrpc': '2.0', 'id': 1, 'method': 'initialize', 'params': {
        'protocolVersion': '2024-11-05', 'capabilities': {},
        'clientInfo': {'name': 'BAS_VR CLI', 'version': '1'}}})
    receive(1, 15)
    send({'jsonrpc': '2.0', 'method': 'notifications/initialized'})
    send({'jsonrpc': '2.0', 'id': 2, 'method': 'tools/call', 'params': payload})
    response = receive(2)
    result = response.get('result', {})
    structured = result.get('structuredContent')
    if structured:
        data = structured.get('data', {})
        data.pop('localFixedCode', None)
        print(json.dumps(structured, ensure_ascii=False))
    else:
        print(json.dumps(response, ensure_ascii=False))
    if 'error' in response or result.get('isError') or (structured and not structured.get('success', True)):
        sys.exit(1)
finally:
    process.terminate()
    process.wait(timeout=5)
