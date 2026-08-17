# Code Editor Upgrade Plan — Plain Textarea → Monaco Editor

## 1. Current State (What We Have Now)

The code input area on `/problems/:id` is a plain HTML **`<textarea>`**. Nothing more.

### What's wired up today

| Feature | Status |
|---|---|
| Language selector (Python, C#, JS, Java, C++) | ✅ Works |
| Starter code per language | ✅ Works |
| Cursor position display (Ln / Col) | ✅ Works — reads textarea selectionStart |
| Line numbers panel | ✅ Works — basic `div` count, not synced to scroll |
| Copy button | ✅ Works — clipboard API |
| Fullscreen toggle | ✅ Works |
| Autocomplete toggle | ✅ UI toggle exists — does **nothing** (no engine behind it) |
| Syntax highlighting | ❌ None — raw text |
| Bracket / quote auto-close | ❌ None |
| Auto-indentation | ❌ None — Tab inserts nothing |
| Code folding | ❌ None |
| Find / Replace | ❌ None |
| Error / warning squiggles | ❌ None |
| Real cursor tracking | ❌ Rough approximation via textarea selectionStart |
| Undo / Redo (beyond browser default) | ❌ Browser only |
| Theme (dark) | ✅ Styled with CSS to look dark — not a real editor theme |

### How code gets to the backend

The textarea value is read as a plain string into `currentCode`, which is passed directly to `submissionSvc.run()` and `submissionSvc.submit()`. The backend only cares about the string — it has no idea what editor produced it.

### Pain points right now

- Typing code feels like a Notepad experience. No colors, no matching brackets, Tab key does nothing useful.
- Line numbers `div` panel is not scroll-synced to the textarea — they desync when content overflows.
- The "Autocomplete" checkbox in the toolbar does nothing visible.
- `jumpToLine()` (called by the hint system) uses a manual pixel math hack because the textarea has no concept of lines.
- `applyHintCodeChanges()` directly pokes `textarea.nativeElement.value` — fragile, bypasses Angular.

---

## 2. What We're Going to Use — Monaco Editor

**Library:** [`ngx-monaco-editor-v2`](https://github.com/miki995/ngx-monaco-editor-v2) — the most maintained Angular wrapper for Monaco (the same engine powering VS Code).

**Why Monaco over CodeMirror?**

| | Monaco | CodeMirror 6 |
|---|---|---|
| VS Code parity (feel) | ✅ Identical | Close |
| Angular wrapper quality | ✅ `ngx-monaco-editor-v2` well maintained | Manual setup needed |
| Syntax highlighting (built-in) | Python, C#, JS, Java, C++ — all out of the box | Requires separate language packages |
| IntelliSense / autocomplete | ✅ Built-in per language | Needs extensions |
| Bundle size | ~2 MB (lazy-loaded) | ~300 KB |
| Line numbers, folding, minimap | ✅ Built-in | Extensions needed |
| API surface for jumpToLine | Clean `editor.revealLineInCenter()` | Available |

Monaco is the right call for a LeetCode-style tool. CodeMirror is better when bundle size is the top constraint.

---

## 3. What Needs to Change — Frontend

### 3.1 Install

```bash
npm install ngx-monaco-editor-v2 --save
```

Monaco's assets (worker scripts) must be served. Angular config update needed in `angular.json`:

```json
"assets": [
  ...,
  {
    "glob": "**/*",
    "input": "node_modules/monaco-editor/min/vs",
    "output": "/assets/monaco/vs"
  }
]
```

And in `app.config.ts`, configure the base path:

```ts
provideMonacoEditor({ baseUrl: 'assets/monaco' })
```

### 3.2 Replace the textarea

**File:** `src/app/features/problem-page/problem-page.component.html`

Replace the `<textarea #editorTextarea ...>` and the manual `<div class="editor-line-numbers">` block with:

```html
<ngx-monaco-editor
  class="monaco-editor-host"
  [options]="editorOptions"
  [(ngModel)]="currentCode"
  (onInit)="onMonacoInit($event)">
</ngx-monaco-editor>
```

Monaco handles its own line numbers, scrollbar, and all rendering internally.

### 3.3 Component TypeScript changes

**File:** `src/app/features/problem-page/problem-page.component.ts`

- Import `MonacoEditorModule` (or `NgxMonacoEditorConfig`) and add to `imports`.
- Add `FormsModule` to imports (needed for `[(ngModel)]`).
- Replace `@ViewChild('editorTextarea')` with an `editor` instance reference via `onMonacoInit(editor)`.
- Add `editorOptions` property driven by `selectedLanguage`:

```ts
get editorOptions() {
  return {
    theme: 'vs-dark',
    language: this.monacoLanguage(this.selectedLanguage),
    fontSize: 14,
    minimap: { enabled: false },
    scrollBeyondLastLine: false,
    automaticLayout: true,   // handles panel resize / fullscreen
    tabSize: 4,
    wordWrap: 'on',
  };
}

private monacoLanguage(lang: string): string {
  const map: Record<string, string> = {
    python: 'python',
    csharp: 'csharp',
    javascript: 'javascript',
    java: 'java',
    cpp: 'cpp',
  };
  return map[lang] ?? 'plaintext';
}
```

- Replace `onEditorInput()` — no longer needed; `[(ngModel)]` keeps `currentCode` in sync.
- Replace `updateCursorPosition()` — use Monaco's `onDidChangeCursorPosition` event:

```ts
onMonacoInit(editor: monaco.editor.IStandaloneCodeEditor): void {
  this.monacoEditor = editor;
  editor.onDidChangeCursorPosition(e => {
    this.cursorLine   = e.position.lineNumber;
    this.cursorColumn = e.position.column;
  });
}
```

- Replace `jumpToLine()` hack:

```ts
jumpToLine(lineNumber: number): void {
  this.monacoEditor?.revealLineInCenter(lineNumber);
  this.monacoEditor?.setPosition({ lineNumber, column: 1 });
  this.monacoEditor?.focus();
}
```

- Replace `applyHintCodeChanges()` direct DOM poke — use Monaco's model API:

```ts
// Instead of: this.editorTextareaRef.nativeElement.value = updatedCode
this.monacoEditor?.setValue(updatedCode);
// currentCode is kept in sync via ngModel automatically
```

- Wire the **Autocomplete toggle** that currently does nothing:

```ts
toggleAutocomplete(): void {
  this.isAutocompleteEnabled = !this.isAutocompleteEnabled;
  this.monacoEditor?.updateOptions({
    quickSuggestions: this.isAutocompleteEnabled,
    suggestOnTriggerCharacters: this.isAutocompleteEnabled,
  });
}
```

### 3.4 SCSS changes

**File:** `src/app/features/problem-page/problem-page.component.scss`

- Remove `.editor-line-numbers` styles — Monaco renders its own.
- Remove `.editor-textarea` styles — replaced by `.monaco-editor-host`.
- Add sizing rules for the Monaco host:

```scss
.monaco-editor-host {
  flex: 1;
  height: 100%;
  min-height: 0;
}
```

- The `editor-textarea--highlight` animation (green flash on hint apply) needs to target Monaco's container div instead of a textarea. Use `monacoEditor.updateOptions({ ...flashTheme })` or overlay a CSS class on the `.monaco-editor-host` parent — same visual effect, different target element.

### 3.5 Language change handling

When user switches language in the dropdown, update Monaco's language model instead of just replacing the textarea value:

```ts
onLanguageChange(lang: string): void {
  this.selectedLanguage = lang;
  if (this.monacoEditor) {
    const model = this.monacoEditor.getModel();
    if (model) {
      monaco.editor.setModelLanguage(model, this.monacoLanguage(lang));
    }
  }
  this.currentCode = this.languages.find(l => l.value === lang)?.starterCode ?? '';
}
```

---

## 4. What Does NOT Need to Change — Backend

The backend receives a plain string `code` and a `language` enum — it does not care how the code was written. **Zero backend changes required.**

```
POST /api/submissions
{ problemId, code: "...", language: "Python" }  // same as before
```

The submission service (`submission.service.ts`) is untouched. The run/submit flow is untouched.

---

## 5. Summary of Files to Touch

| File | Change |
|---|---|
| `package.json` | Add `ngx-monaco-editor-v2` |
| `angular.json` | Add Monaco asset path under `assets` |
| `src/app/app.config.ts` | Add `provideMonacoEditor()` |
| `src/app/features/problem-page/problem-page.component.ts` | Replace textarea refs, add Monaco instance, wire autocomplete toggle, fix jumpToLine and applyHintCodeChanges |
| `src/app/features/problem-page/problem-page.component.html` | Replace `<textarea>` + line-numbers div with `<ngx-monaco-editor>` |
| `src/app/features/problem-page/problem-page.component.scss` | Remove textarea styles, add Monaco host sizing, redirect highlight animation |

**Backend:** No changes needed.

---

## 6. New Features Gained Automatically

Once Monaco is in, we get all of this for free — no extra code:

- Syntax highlighting for all 5 languages
- Bracket and quote auto-close
- Smart auto-indent on Enter
- Tab / Shift+Tab indent/unindent
- Full Ctrl+Z / Ctrl+Y undo-redo history
- Find (Ctrl+F) and Replace (Ctrl+H)
- Code folding (collapse functions / blocks)
- Multiple cursors (Alt+Click)
- Select all occurrences (Ctrl+Shift+L)
- Scrollbar with minimap (we'll disable minimap by default to save space)
- Proper line numbers always in sync
- Real cursor position (Ln, Col) — exact, not approximate

The "Autocomplete" toggle will finally actually work.
