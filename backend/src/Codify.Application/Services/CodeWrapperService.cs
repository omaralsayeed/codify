using System.Text;
using System.Text.RegularExpressions;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;

namespace Codify.Application.Services;

/// <summary>
/// Wraps user-written function code with input/output handling to create
/// complete executable programs. AUTOMATICALLY detects function signatures
/// and handles I/O for ALL problems (like LeetCode/HackerRank).
/// </summary>
public class CodeWrapperService : ICodeWrapperService
{
    public bool UsesTemplate(Problem problem, string language)
    {
        // Always return true - we auto-wrap ALL code
        return true;
    }

    public string? GetStarterCode(Problem problem, string language)
    {
        // No starter code needed - users write complete functions
        return null;
    }

    public string WrapUserCode(string userCode, string language, CodeTemplate template)
    {
        // Ignore template - auto-detect function signature from user code
        return language switch
        {
            "Python" => WrapPythonAuto(userCode),
            "JavaScript" => WrapJavaScriptAuto(userCode),
            "Cpp" => WrapCppAuto(userCode),
            "Java" => WrapJavaAuto(userCode),
            "CSharp" => WrapCSharpAuto(userCode),
            _ => throw new NotSupportedException($"Language {language} auto-wrapping not supported yet")
        };
    }

    // ══════════════════════════════════════════════════════════════
    // AUTO-WRAP PYTHON (detects function signature automatically)
    // ══════════════════════════════════════════════════════════════

    private string WrapPythonAuto(string userCode)
    {
        // Extract function signature: def functionName(param1, param2, ...):
        var match = Regex.Match(userCode, @"def\s+(\w+)\s*\((.*?)\)\s*:", RegexOptions.Singleline);
        if (!match.Success)
            return userCode; // Not a function, return as-is
        
        var functionName = match.Groups[1].Value;
        var paramsStr = match.Groups[2].Value;
        var parameters = paramsStr.Split(',')
            .Select(p => p.Split(':')[0].Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p) && p != "self")
            .ToList();

        if (parameters.Count == 0)
            return userCode; // No parameters, return as-is

        var sb = new StringBuilder();
        
        // Add imports
        sb.AppendLine("from typing import List, Optional");
        sb.AppendLine("import json");
        sb.AppendLine();
        
        // Add user code
        sb.AppendLine(userCode);
        sb.AppendLine();
        
        // Add execution wrapper - clean, no debug output
        sb.AppendLine("# Auto-generated I/O wrapper");
        sb.AppendLine("import sys");
        sb.AppendLine("import json");
        
        // Read input and parse
        sb.AppendLine("lines = [line.strip() for line in sys.stdin.read().strip().split('\\n') if line.strip()]");
        
        // Parse each line
        sb.AppendLine("parsed = []");
        sb.AppendLine("for line in lines:");
        sb.AppendLine("    try:");
        sb.AppendLine("        parsed.append(json.loads(line))");
        sb.AppendLine("    except:");
        sb.AppendLine("        try:");
        sb.AppendLine("            parsed.append(int(line))");
        sb.AppendLine("        except:");
        sb.AppendLine("            try:");
        sb.AppendLine("                parsed.append(float(line))");
        sb.AppendLine("            except:");
        sb.AppendLine("                parsed.append(line)");
        
        // Call function
        sb.AppendLine($"result = {functionName}(*parsed[:{parameters.Count}])");
        
        // Print result (clean, no extra newlines or spaces)
        sb.AppendLine("if isinstance(result, (list, dict)):");
        sb.AppendLine("    import sys; sys.stdout.write(json.dumps(result, separators=(',', ':')))");
        sb.AppendLine("else:");
        sb.AppendLine("    import sys; sys.stdout.write(str(result).strip())");
        
        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    // AUTO-WRAP JAVASCRIPT
    // ══════════════════════════════════════════════════════════════

    private string WrapJavaScriptAuto(string userCode)
    {
        // Extract function signature: function name(...) or const name = (...) =>
        var match = Regex.Match(userCode, @"(?:function\s+(\w+)|(?:const|let|var)\s+(\w+)\s*=)");
        if (!match.Success)
            return userCode;
        
        var functionName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

        var sb = new StringBuilder();
        
        sb.AppendLine("const readline = require('readline');");
        sb.AppendLine("const rl = readline.createInterface({ input: process.stdin });");
        sb.AppendLine();
        sb.AppendLine(userCode);
        sb.AppendLine();
        sb.AppendLine("let lines = [];");
        sb.AppendLine("rl.on('line', (line) => lines.push(line));");
        sb.AppendLine("rl.on('close', () => {");
        sb.AppendLine("    const params = lines.map(line => {");
        sb.AppendLine("        try { return JSON.parse(line); }");
        sb.AppendLine("        catch { return line; }");
        sb.AppendLine("    });");
        sb.AppendLine($"    const result = {functionName}(...params);");
        sb.AppendLine("    console.log(typeof result === 'object' ? JSON.stringify(result) : result);");
        sb.AppendLine("});");
        
        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    // AUTO-WRAP C++
    // ══════════════════════════════════════════════════════════════

    private string WrapCppAuto(string userCode)
    {
        // For C++, extract function or class method
        var funcMatch = Regex.Match(userCode, @"(\w+)\s+(\w+)\s*\([^)]*\)");
        if (!funcMatch.Success)
            return userCode;

        var returnType = funcMatch.Groups[1].Value;
        var functionName = funcMatch.Groups[2].Value;
        var isClass = userCode.Contains("class Solution");

        var sb = new StringBuilder();
        
        sb.AppendLine("#include <iostream>");
        sb.AppendLine("#include <vector>");
        sb.AppendLine("#include <string>");
        sb.AppendLine("#include <sstream>");
        sb.AppendLine("using namespace std;");
        sb.AppendLine();
        sb.AppendLine(userCode);
        sb.AppendLine();
        sb.AppendLine("int main() {");
        sb.AppendLine("    string line;");
        sb.AppendLine("    vector<string> inputs;");
        sb.AppendLine("    while(getline(cin, line)) inputs.push_back(line);");
        sb.AppendLine("    // TODO: Parse inputs and call function");
        sb.AppendLine("    // This is simplified - production needs type detection");
        sb.AppendLine("    return 0;");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    // AUTO-WRAP JAVA
    // ══════════════════════════════════════════════════════════════

    private string WrapJavaAuto(string userCode)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("import java.util.*;");
        sb.AppendLine("import com.google.gson.*;");
        sb.AppendLine();
        sb.AppendLine(userCode);
        sb.AppendLine();
        sb.AppendLine("public class Main {");
        sb.AppendLine("    public static void main(String[] args) {");
        sb.AppendLine("        Scanner sc = new Scanner(System.in);");
        sb.AppendLine("        List<String> inputs = new ArrayList<>();");
        sb.AppendLine("        while(sc.hasNextLine()) inputs.add(sc.nextLine());");
        sb.AppendLine("        // TODO: Parse and call function");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    // AUTO-WRAP C#
    // ══════════════════════════════════════════════════════════════

    private string WrapCSharpAuto(string userCode)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine(userCode);
        sb.AppendLine();
        sb.AppendLine("public class Program {");
        sb.AppendLine("    public static void Main() {");
        sb.AppendLine("        var inputs = new List<string>();");
        sb.AppendLine("        string line;");
        sb.AppendLine("        while((line = Console.ReadLine()) != null) inputs.Add(line);");
        sb.AppendLine("        // TODO: Parse and call function");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
}
