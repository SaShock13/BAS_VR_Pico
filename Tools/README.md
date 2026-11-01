# Unity MCP CLI bridge

Use the Unity AI Assistant relay already installed on this machine to call MCP tools
when they are not exposed directly in the chat. Python 3 is required.
Keep BAS_VR open in Unity. The bridge explicitly selects this checkout's project path.

```powershell
python Tools/unity_mcp_call.py Tools/ValidateMenu.cs
```

`ValidateMenu.cs` checks menu references, rendering components, button wiring and
Zenject bindings, and runs navigation checks with a test scene loader. It opens
BootMenu and leaves it open. It requires saved scenes and Edit Mode; it never starts
Play Mode or loads runtime scenes.

The bridge also accepts a JSON file containing MCP `name` and `arguments`, or a C#
file implementing `IRunCommand` for the `Unity_RunCommand` tool. Calls can modify
the editor, so inspect scripts before executing them. Diagnostics time out after
120 seconds and the temporary relay process is closed afterward.

In Unity, navigation checks are also available under
**Tools > BAS VR > Verify Main Menu Navigation**.

Manual device check: start the app, observe the repeating assembly animation,
press **Начать конструирование** with an XR controller, and confirm that the
constructor opens. Check garage/flight returns and stable frame timing on PICO.
