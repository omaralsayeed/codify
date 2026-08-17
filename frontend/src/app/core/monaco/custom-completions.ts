/**
 * custom-completions.ts
 *
 * Curated snippet / API completion providers for each supported language.
 * These are registered ONCE globally on the Monaco instance — not per editor.
 *
 * Call registerCustomCompletions() from onMonacoInit() after the first editor
 * initialises. Subsequent language switches reuse the same providers because
 * Monaco completion providers are language-scoped, not editor-scoped.
 *
 * No backend changes, no new npm packages.
 */

// ── Python ────────────────────────────────────────────────────────────────────

const PYTHON_SNIPPETS = [
  {
    label: 'forr',
    detail: 'for i in range(n)',
    kind: 27, // Snippet
    insertText: 'for ${1:i} in range(${2:n}):\n\t${3:pass}',
    insertTextRules: 4, // InsertAsSnippet
    documentation: 'For loop with range',
  },
  {
    label: 'fore',
    detail: 'for i, v in enumerate(...)',
    kind: 27,
    insertText: 'for ${1:i}, ${2:v} in enumerate(${3:items}):\n\t${4:pass}',
    insertTextRules: 4,
    documentation: 'Enumerate loop',
  },
  {
    label: 'lenc',
    detail: 'len(collection)',
    kind: 1, // Function
    insertText: 'len(${1:collection})',
    insertTextRules: 4,
    documentation: 'Get length of a sequence',
  },
  {
    label: 'rangef',
    detail: 'range(start, stop, step)',
    kind: 1,
    insertText: 'range(${1:start}, ${2:stop}, ${3:step})',
    insertTextRules: 4,
    documentation: 'range with start, stop, step',
  },
  {
    label: 'sortd',
    detail: 'sorted(iterable, key=...)',
    kind: 1,
    insertText: 'sorted(${1:iterable}, key=${2:lambda x: x})',
    insertTextRules: 4,
    documentation: 'Sorted with custom key',
  },
  {
    label: 'dictdef',
    detail: 'collections.defaultdict',
    kind: 7, // Class
    insertText: 'from collections import defaultdict\n${1:d} = defaultdict(${2:int})',
    insertTextRules: 4,
    documentation: 'defaultdict initialisation',
  },
  {
    label: 'heappush',
    detail: 'heapq.heappush / heappop',
    kind: 1,
    insertText: 'import heapq\nheapq.heappush(${1:heap}, ${2:item})',
    insertTextRules: 4,
    documentation: 'Push to min-heap',
  },
  {
    label: 'infn',
    detail: 'float("inf")',
    kind: 12, // Value
    insertText: 'float("inf")',
    insertTextRules: 0,
    documentation: 'Positive infinity constant',
  },
  {
    label: 'defi',
    detail: 'def function(args):',
    kind: 27,
    insertText: 'def ${1:name}(${2:args}):\n\t${3:pass}',
    insertTextRules: 4,
    documentation: 'Function definition',
  },
  {
    label: 'clss',
    detail: 'class ClassName:',
    kind: 27,
    insertText: 'class ${1:ClassName}:\n\tdef __init__(self${2:, args}):\n\t\t${3:pass}',
    insertTextRules: 4,
    documentation: 'Class skeleton',
  },
  {
    label: 'lcomp',
    detail: '[x for x in items if cond]',
    kind: 27,
    insertText: '[${1:expr} for ${2:x} in ${3:items}${4: if ${5:condition}}]',
    insertTextRules: 4,
    documentation: 'List comprehension',
  },
  {
    label: 'bisect',
    detail: 'bisect_left / bisect_right',
    kind: 1,
    insertText: 'from bisect import bisect_left\nbisect_left(${1:arr}, ${2:x})',
    insertTextRules: 4,
    documentation: 'Binary search via bisect',
  },
];

// ── Java ──────────────────────────────────────────────────────────────────────

const JAVA_SNIPPETS = [
  {
    label: 'sout',
    detail: 'System.out.println(...)',
    kind: 27,
    insertText: 'System.out.println(${1});',
    insertTextRules: 4,
    documentation: 'Print line to stdout',
  },
  {
    label: 'main',
    detail: 'public static void main(String[] args)',
    kind: 27,
    insertText: 'public static void main(String[] args) {\n\t${1}\n}',
    insertTextRules: 4,
    documentation: 'Main method skeleton',
  },
  {
    label: 'fori',
    detail: 'for (int i = 0; i < n; i++)',
    kind: 27,
    insertText: 'for (int ${1:i} = 0; ${1:i} < ${2:n}; ${1:i}++) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Indexed for loop',
  },
  {
    label: 'fore',
    detail: 'for (Type item : collection)',
    kind: 27,
    insertText: 'for (${1:var} ${2:item} : ${3:collection}) {\n\t${4}\n}',
    insertTextRules: 4,
    documentation: 'Enhanced for-each loop',
  },
  {
    label: 'arrlist',
    detail: 'new ArrayList<>()',
    kind: 7,
    insertText: 'ArrayList<${1:Integer}> ${2:list} = new ArrayList<>();',
    insertTextRules: 4,
    documentation: 'ArrayList initialisation',
  },
  {
    label: 'hashmap',
    detail: 'new HashMap<>()',
    kind: 7,
    insertText: 'HashMap<${1:String}, ${2:Integer}> ${3:map} = new HashMap<>();',
    insertTextRules: 4,
    documentation: 'HashMap initialisation',
  },
  {
    label: 'hashset',
    detail: 'new HashSet<>()',
    kind: 7,
    insertText: 'HashSet<${1:Integer}> ${2:set} = new HashSet<>();',
    insertTextRules: 4,
    documentation: 'HashSet initialisation',
  },
  {
    label: 'pqueue',
    detail: 'new PriorityQueue<>()',
    kind: 7,
    insertText: 'PriorityQueue<${1:Integer}> ${2:pq} = new PriorityQueue<>();',
    insertTextRules: 4,
    documentation: 'Min-heap PriorityQueue',
  },
  {
    label: 'scanner',
    detail: 'Scanner sc = new Scanner(System.in)',
    kind: 27,
    insertText: 'Scanner ${1:sc} = new Scanner(System.in);\n${2:int n = ${1:sc}.nextInt();}',
    insertTextRules: 4,
    documentation: 'Scanner for stdin',
  },
  {
    label: 'arrsort',
    detail: 'Arrays.sort(arr)',
    kind: 1,
    insertText: 'Arrays.sort(${1:arr});',
    insertTextRules: 4,
    documentation: 'Sort an array',
  },
  {
    label: 'mxval',
    detail: 'Integer.MAX_VALUE',
    kind: 12,
    insertText: 'Integer.MAX_VALUE',
    insertTextRules: 0,
    documentation: 'Max int constant',
  },
  {
    label: 'mnval',
    detail: 'Integer.MIN_VALUE',
    kind: 12,
    insertText: 'Integer.MIN_VALUE',
    insertTextRules: 0,
    documentation: 'Min int constant',
  },
];

// ── C# ────────────────────────────────────────────────────────────────────────

const CSHARP_SNIPPETS = [
  {
    label: 'cw',
    detail: 'Console.WriteLine(...)',
    kind: 27,
    insertText: 'Console.WriteLine(${1});',
    insertTextRules: 4,
    documentation: 'Print line to stdout',
  },
  {
    label: 'fori',
    detail: 'for (int i = 0; i < n; i++)',
    kind: 27,
    insertText: 'for (int ${1:i} = 0; ${1:i} < ${2:n}; ${1:i}++) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Indexed for loop',
  },
  {
    label: 'fore',
    detail: 'foreach (var item in collection)',
    kind: 27,
    insertText: 'foreach (var ${1:item} in ${2:collection}) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Foreach loop',
  },
  {
    label: 'dict',
    detail: 'new Dictionary<K,V>()',
    kind: 7,
    insertText: 'var ${1:dict} = new Dictionary<${2:string}, ${3:int}>();',
    insertTextRules: 4,
    documentation: 'Dictionary initialisation',
  },
  {
    label: 'list',
    detail: 'new List<T>()',
    kind: 7,
    insertText: 'var ${1:list} = new List<${2:int}>();',
    insertTextRules: 4,
    documentation: 'List<T> initialisation',
  },
  {
    label: 'hashset',
    detail: 'new HashSet<T>()',
    kind: 7,
    insertText: 'var ${1:set} = new HashSet<${2:int}>();',
    insertTextRules: 4,
    documentation: 'HashSet initialisation',
  },
  {
    label: 'pqueue',
    detail: 'new PriorityQueue<T, P>()',
    kind: 7,
    insertText: 'var ${1:pq} = new PriorityQueue<${2:int}, ${3:int}>();',
    insertTextRules: 4,
    documentation: '.NET 6+ PriorityQueue',
  },
  {
    label: 'arrSort',
    detail: 'Array.Sort(arr)',
    kind: 1,
    insertText: 'Array.Sort(${1:arr});',
    insertTextRules: 4,
    documentation: 'Sort an array in-place',
  },
  {
    label: 'linq',
    detail: 'using System.Linq;',
    kind: 9, // Module
    insertText: 'using System.Linq;',
    insertTextRules: 0,
    documentation: 'Import LINQ namespace',
  },
  {
    label: 'mxval',
    detail: 'int.MaxValue',
    kind: 12,
    insertText: 'int.MaxValue',
    insertTextRules: 0,
    documentation: 'Max int constant',
  },
];

// ── JavaScript ────────────────────────────────────────────────────────────────

const JAVASCRIPT_SNIPPETS = [
  {
    label: 'forof',
    detail: 'for (const x of arr)',
    kind: 27,
    insertText: 'for (const ${1:item} of ${2:arr}) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'For-of loop',
  },
  {
    label: 'fori',
    detail: 'for (let i = 0; i < n; i++)',
    kind: 27,
    insertText: 'for (let ${1:i} = 0; ${1:i} < ${2:n}; ${1:i}++) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Indexed for loop',
  },
  {
    label: 'map',
    detail: 'new Map()',
    kind: 7,
    insertText: 'const ${1:map} = new Map();',
    insertTextRules: 4,
    documentation: 'Map initialisation',
  },
  {
    label: 'set',
    detail: 'new Set()',
    kind: 7,
    insertText: 'const ${1:set} = new Set();',
    insertTextRules: 4,
    documentation: 'Set initialisation',
  },
  {
    label: 'arrs',
    detail: 'arr.sort((a,b) => a - b)',
    kind: 1,
    insertText: '${1:arr}.sort((a, b) => a - b);',
    insertTextRules: 4,
    documentation: 'Numeric sort ascending',
  },
  {
    label: 'arrf',
    detail: 'arr.filter(x => cond)',
    kind: 1,
    insertText: '${1:arr}.filter(${2:x} => ${3:condition})',
    insertTextRules: 4,
    documentation: 'Array filter',
  },
  {
    label: 'arrm',
    detail: 'arr.map(x => expr)',
    kind: 1,
    insertText: '${1:arr}.map(${2:x} => ${3:x})',
    insertTextRules: 4,
    documentation: 'Array map',
  },
  {
    label: 'arrr',
    detail: 'arr.reduce((acc, x) => ...)',
    kind: 1,
    insertText: '${1:arr}.reduce((${2:acc}, ${3:x}) => ${4:acc + x}, ${5:0})',
    insertTextRules: 4,
    documentation: 'Array reduce',
  },
  {
    label: 'inf',
    detail: 'Infinity',
    kind: 12,
    insertText: 'Infinity',
    insertTextRules: 0,
    documentation: 'Positive infinity constant',
  },
  {
    label: 'log',
    detail: 'console.log(...)',
    kind: 1,
    insertText: 'console.log(${1});',
    insertTextRules: 4,
    documentation: 'Log to console',
  },
];

// ── C++ ───────────────────────────────────────────────────────────────────────

const CPP_SNIPPETS = [
  {
    label: 'cout',
    detail: 'cout << ... << endl',
    kind: 27,
    insertText: 'cout << ${1} << endl;',
    insertTextRules: 4,
    documentation: 'Print to stdout',
  },
  {
    label: 'cin',
    detail: 'cin >> var',
    kind: 27,
    insertText: 'cin >> ${1:var};',
    insertTextRules: 4,
    documentation: 'Read from stdin',
  },
  {
    label: 'fori',
    detail: 'for (int i = 0; i < n; i++)',
    kind: 27,
    insertText: 'for (int ${1:i} = 0; ${1:i} < ${2:n}; ${1:i}++) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Indexed for loop',
  },
  {
    label: 'fore',
    detail: 'for (auto& x : collection)',
    kind: 27,
    insertText: 'for (auto& ${1:x} : ${2:collection}) {\n\t${3}\n}',
    insertTextRules: 4,
    documentation: 'Range-based for loop',
  },
  {
    label: 'vec',
    detail: 'vector<T> v',
    kind: 7,
    insertText: 'vector<${1:int}> ${2:v};',
    insertTextRules: 4,
    documentation: 'Vector declaration',
  },
  {
    label: 'umap',
    detail: 'unordered_map<K,V>',
    kind: 7,
    insertText: 'unordered_map<${1:int}, ${2:int}> ${3:mp};',
    insertTextRules: 4,
    documentation: 'Hash map',
  },
  {
    label: 'uset',
    detail: 'unordered_set<T>',
    kind: 7,
    insertText: 'unordered_set<${1:int}> ${2:st};',
    insertTextRules: 4,
    documentation: 'Hash set',
  },
  {
    label: 'pqueue',
    detail: 'priority_queue<T>',
    kind: 7,
    insertText: 'priority_queue<${1:int}> ${2:pq};',
    insertTextRules: 4,
    documentation: 'Max-heap priority queue',
  },
  {
    label: 'sort',
    detail: 'sort(begin, end)',
    kind: 1,
    insertText: 'sort(${1:v}.begin(), ${1:v}.end());',
    insertTextRules: 4,
    documentation: 'Sort a container',
  },
  {
    label: 'mxval',
    detail: 'INT_MAX',
    kind: 12,
    insertText: 'INT_MAX',
    insertTextRules: 0,
    documentation: 'Max int constant',
  },
  {
    label: 'mnval',
    detail: 'INT_MIN',
    kind: 12,
    insertText: 'INT_MIN',
    insertTextRules: 0,
    documentation: 'Min int constant',
  },
  {
    label: 'bits',
    detail: '#include <bits/stdc++.h>',
    kind: 9,
    insertText: '#include <bits/stdc++.h>\nusing namespace std;',
    insertTextRules: 0,
    documentation: 'Competitive programming include-all header',
  },
];

// ── Snippet map ───────────────────────────────────────────────────────────────

const SNIPPET_MAP: Record<string, typeof PYTHON_SNIPPETS> = {
  python:     PYTHON_SNIPPETS,
  java:       JAVA_SNIPPETS,
  csharp:     CSHARP_SNIPPETS,
  javascript: JAVASCRIPT_SNIPPETS,
  cpp:        CPP_SNIPPETS,
};

// ── Registration ──────────────────────────────────────────────────────────────

/** Tracks whether providers have already been registered (global to Monaco). */
let _registered = false;

/**
 * Registers one CompletionItemProvider per language.
 * Safe to call multiple times — registers only once per page load.
 *
 * Call this inside onMonacoInit() after the editor instance is ready,
 * so window.monaco is guaranteed to be loaded.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function registerCustomCompletions(monacoGlobal: any): void {
  if (_registered) return;
  _registered = true;

  Object.entries(SNIPPET_MAP).forEach(([lang, snippets]) => {
    monacoGlobal.languages.registerCompletionItemProvider(lang, {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      provideCompletionItems(model: any, position: any) {
        const word  = model.getWordUntilPosition(position);
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber:   position.lineNumber,
          startColumn:     word.startColumn,
          endColumn:       word.endColumn,
        };
        return {
          suggestions: snippets.map(s => ({ ...s, range })),
        };
      },
    });
  });
}
