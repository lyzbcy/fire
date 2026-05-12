---
name: fire-unity-mcp
description: 'Fire Unity MCP workflow for scene automation, hierarchy inspection, prefab and GameObject editing, Unity console triage, script compilation checks, and editor-driven validation in D:/Unity_test/fire. Use for Unity MCP, scene setup, play mode checks, and automated repair.'
argument-hint: 'Describe the Fire Unity task, scene, object, script, or console error.'
user-invocable: false
---

# Fire Unity MCP Workflow

## When to Use
- Editing scenes, prefabs, GameObjects, transforms, or components inside D:/Unity_test/fire.
- Diagnosing Unity console errors or verifying a fix after script changes.
- Inspecting hierarchy, assets, editor state, or play mode through Unity MCP.
- Automating repetitive Unity editor work instead of editing scene files manually.

## Procedure
1. Identify the smallest concrete target in D:/Unity_test/fire: scene, prefab, GameObject, script, asset, or console error.
2. If Unity MCP is available, inspect editor capabilities and current state before mutating anything.
3. Prefer MCP tools for editor-owned data.
4. Use file edits for C# code and text-based project files.
5. After script edits, verify compilation and read the Unity console before continuing.
6. Finish with the cheapest focused validation that proves the intended behavior or editor state.

## Unity MCP Tool Selection
- Use mcp_unitymcp_manage_scene for scene loading, saving, hierarchy inspection, and scene structure changes.
- Use mcp_unitymcp_manage_gameobject for GameObject creation, component inspection, and object-level mutations.
- Use mcp_unitymcp_manage_asset for asset search, prefab discovery, and verifying project resources.
- Use mcp_unitymcp_manage_editor for play mode control and editor state changes.
- Use mcp_unitymcp_read_console after script edits or failed behavior checks.
- Use mcp_unitymcp_refresh_unity when the editor state needs a refresh after changes.

## Guardrails
- Stay inside D:/Unity_test/fire unless the user explicitly asks for another folder.
- Do not edit Unity scene YAML by hand when an MCP editor action can do the job safely.
- Do not rely on newly edited scripts until Unity compilation has completed cleanly.
- When creating a new scene, include a Camera and a main Directional Light unless the scene design intentionally excludes them.

## Validation Checklist
- Confirm the intended object, component, or script was changed.
- Check compile status and Unity console when scripts are involved.
- Run the smallest relevant editor or play mode check for the touched slice.
- Report any remaining warnings, missing references, or assumptions.
